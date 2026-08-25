using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RFYama.Core;

public sealed record SteamUygulama(string AppId, string Ad);

/// <summary>
/// Steam mağazasında ada göre oyun arar (ad -> AppID). Yerel kaynaklardan AppID
/// bulunamadığında SON ÇARE olarak kullanılır.
///
/// <para>
/// NOT: Yaygın olarak bilinen <c>ISteamApps/GetAppList</c> uç noktası artık anahtarsız genel
/// API'de YOKTUR — Steam'in kendi GetSupportedAPIList çıktısında ISteamApps altında yalnızca
/// GetSDRConfig görünür ve tüm GetAppList sürümleri 404 döner. Bunun yerine mağazanın arama
/// uç noktası kullanılır; hem çalışır hem de 10 MB'lık tam listeyi indirmek yerine sunucu
/// tarafında arama yapar.
/// </para>
///
/// <para>
/// İstek yalnızca kullanıcı açıkça istediğinde yapılır; uygulama açılışta veya arka planda
/// kendiliğinden ağa çıkmaz. Gönderilen tek veri arama terimidir (oyun klasörünün adı);
/// hiçbir kişisel veri, kimlik veya dosya yolu gitmez.
/// </para>
/// </summary>
public static class SteamMagazaAramasi
{
    public const string MagazaAramaUcNoktasi = "https://store.steampowered.com/api/storesearch/";
    public const string Topluluk = "https://steamcommunity.com/actions/SearchApps/";

    public static TimeSpan Zaman { get; set; } = TimeSpan.FromSeconds(30);

    // Aynı oturumda aynı terim için tekrar ağa çıkmamak adına küçük bir bellek önbelleği.
    private static readonly Dictionary<string, List<SteamUygulama>> Onbellek =
        new(StringComparer.OrdinalIgnoreCase);

    private sealed class MagazaYanit
    {
        [JsonPropertyName("items")] public List<MagazaOge>? Items { get; set; }
    }

    private sealed class MagazaOge
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private sealed class ToplulukOge
    {
        [JsonPropertyName("appid")] public string? AppId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    /// <summary>
    /// Verilen terimlerin her biri için mağazada arama yapar ve sonuçları birleştirir.
    /// Ağ hatasında istisna fırlatır; çağıran tarafın kullanıcıya bildirmesi beklenir.
    /// </summary>
    public static async Task<List<SteamUygulama>> Ara(
        IEnumerable<string> terimler,
        CancellationToken iptal = default)
    {
        var sonuc = new List<SteamUygulama>();
        var gorulen = new HashSet<string>(StringComparer.Ordinal);

        using var istemci = YeniIstemci();

        foreach (var terim in terimler)
        {
            if (string.IsNullOrWhiteSpace(terim) || terim.Trim().Length < 3) continue;

            foreach (var uygulama in await TerimAra(istemci, terim.Trim(), iptal).ConfigureAwait(false))
            {
                if (gorulen.Add(uygulama.AppId)) sonuc.Add(uygulama);
            }
        }

        return sonuc;
    }

    private static async Task<List<SteamUygulama>> TerimAra(
        HttpClient istemci, string terim, CancellationToken iptal)
    {
        lock (Onbellek)
        {
            if (Onbellek.TryGetValue(terim, out var onbellekli)) return onbellekli;
        }

        List<SteamUygulama> bulunan;
        try
        {
            bulunan = await MagazadanAra(istemci, terim, iptal).ConfigureAwait(false);

            // Mağaza aramasında sonuç yoksa topluluk uç noktasını dene: bazı oyunlar
            // mağazada listelenmese de topluluk dizininde bulunabiliyor.
            if (bulunan.Count == 0)
                bulunan = await TopluluktanAra(istemci, terim, iptal).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            bulunan = new List<SteamUygulama>();
        }

        lock (Onbellek) { Onbellek[terim] = bulunan; }
        return bulunan;
    }

    private static async Task<List<SteamUygulama>> MagazadanAra(
        HttpClient istemci, string terim, CancellationToken iptal)
    {
        // cc/l parametreleri bölgesel sonuç farkını azaltmak için sabit tutulur.
        var adres = $"{MagazaAramaUcNoktasi}?term={Uri.EscapeDataString(terim)}&cc=US&l=en";

        await using var akis = await istemci.GetStreamAsync(adres, iptal).ConfigureAwait(false);
        var yanit = await JsonSerializer
            .DeserializeAsync<MagazaYanit>(akis, cancellationToken: iptal)
            .ConfigureAwait(false);

        if (yanit?.Items is null) return new List<SteamUygulama>();

        return yanit.Items
            // type == "app": paket, DLC ve demo kayıtları elenir.
            .Where(o => o.Id > 0 &&
                        !string.IsNullOrWhiteSpace(o.Name) &&
                        string.Equals(o.Type, "app", StringComparison.OrdinalIgnoreCase))
            .Select(o => new SteamUygulama(o.Id.ToString(CultureInfo.InvariantCulture), o.Name!))
            .ToList();
    }

    private static async Task<List<SteamUygulama>> TopluluktanAra(
        HttpClient istemci, string terim, CancellationToken iptal)
    {
        var adres = Topluluk + Uri.EscapeDataString(terim);

        await using var akis = await istemci.GetStreamAsync(adres, iptal).ConfigureAwait(false);
        var ogeler = await JsonSerializer
            .DeserializeAsync<List<ToplulukOge>>(akis, cancellationToken: iptal)
            .ConfigureAwait(false);

        if (ogeler is null) return new List<SteamUygulama>();

        return ogeler
            .Where(o => !string.IsNullOrWhiteSpace(o.AppId) && !string.IsNullOrWhiteSpace(o.Name))
            .Select(o => new SteamUygulama(o.AppId!, o.Name!))
            .ToList();
    }

    private static HttpClient YeniIstemci()
    {
        var istemci = new HttpClient { Timeout = Zaman };
        istemci.DefaultRequestHeaders.UserAgent.ParseAdd("RF-Yama/1.0");
        return istemci;
    }
}
