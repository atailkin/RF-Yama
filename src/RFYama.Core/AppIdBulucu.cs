using System.Globalization;
using System.Text;

namespace RFYama.Core;

public enum AppIdKaynagi
{
    /// <summary>Oyunun yanındaki steam_appid.txt — kesin bilgi.</summary>
    SteamAppIdDosyasi,

    /// <summary>Steam kütüphanelerindeki appmanifest_*.acf — kesin bilgi.</summary>
    SteamKurulumKaydi,

    /// <summary>Steam uygulama listesinde ada göre eşleşme — TAHMİN, doğrulanmalı.</summary>
    AdEslesmesi
}

public sealed record AppIdSonucu(string AppId, string? Ad, AppIdKaynagi Kaynak)
{
    /// <summary>Ad eşleşmesi tahmindir; kullanıcıya onaylatılmalıdır.</summary>
    public bool Kesin => Kaynak != AppIdKaynagi.AdEslesmesi;
}

/// <summary>
/// Oyunun Steam AppID'sini bulur. Sıra bilinçlidir: önce kesin ve yerel kaynaklar, en son
/// tahmine dayalı ve ağ gerektiren arama.
///
/// 1. Oyunun yanındaki steam_appid.txt — oyun taşınsa bile onunla birlikte gider.
/// 2. Tüm Steam kütüphanelerindeki appmanifest_*.acf kayıtları.
/// 3. Steam'in genel uygulama listesinde ada göre arama (yalnızca kullanıcı isterse).
/// </summary>
public static class AppIdBulucu
{
    /// <summary>Ağa çıkmadan, yalnızca yerel kaynaklardan arar.</summary>
    public static AppIdSonucu? YerelBul(string hedefKlasor)
        => SteamAppIdDosyasindanBul(hedefKlasor) ?? KurulumKaydindanBul(hedefKlasor);

    /// <summary>
    /// Oyunun yanındaki steam_appid.txt dosyasını okur. Steamworks SDK'sı bu dosyayı kullanır;
    /// oyun klasörü başka diske taşınsa bile içinde kalır — taşınmış kurulumun asıl çözümü budur.
    /// </summary>
    public static AppIdSonucu? SteamAppIdDosyasindanBul(string hedefKlasor)
    {
        foreach (var klasor in AramaKlasorleri(hedefKlasor))
        {
            var yol = Path.Combine(klasor, "steam_appid.txt");
            if (!File.Exists(yol)) continue;

            try
            {
                var ham = File.ReadAllText(yol).Trim();
                // Dosya bazen sonunda satır sonu veya BOM taşır; yalnızca baştaki rakamları al.
                var rakamlar = new string(ham.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray());

                if (rakamlar.Length > 0 &&
                    uint.TryParse(rakamlar, NumberStyles.None, CultureInfo.InvariantCulture, out var id) &&
                    id > 0)
                {
                    return new AppIdSonucu(
                        id.ToString(CultureInfo.InvariantCulture), null, AppIdKaynagi.SteamAppIdDosyasi);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Okunamadıysa sonraki kaynağa geç.
            }
        }

        return null;
    }

    /// <summary>
    /// TÜM Steam kütüphanelerindeki kurulum kayıtlarını tarar ve installdir adı hedef klasörle
    /// eşleşeni arar. Eski kod yalnızca hedefin içinde bulunduğu kütüphaneye bakıyordu.
    /// </summary>
    public static AppIdSonucu? KurulumKaydindanBul(string hedefKlasor)
    {
        var adaylar = KlasorAdlari(hedefKlasor);
        if (adaylar.Count == 0) return null;

        foreach (var steamapps in SteamKutuphaneleri.SteamappsKlasorleri())
        {
            foreach (var manifest in SteamKutuphaneleri.Manifestler(steamapps))
            {
                if (adaylar.Any(a => string.Equals(a, manifest.KurulumKlasoru, StringComparison.OrdinalIgnoreCase)))
                    return new AppIdSonucu(manifest.AppId, manifest.Ad, AppIdKaynagi.SteamKurulumKaydi);
            }
        }

        return null;
    }

    /// <summary>
    /// Oyun adı taşıması beklenmeyen genel klasör adları. Bunlar arama terimi olarak
    /// kullanılmaz: mağazada "temp", "data", "game" gibi adlarla gerçek oyunlar vardır ve
    /// tam eşleşme puanı alıp doğru sonucu geçerler.
    /// </summary>
    private static readonly HashSet<string> GenelKlasorAdlari = new(StringComparer.OrdinalIgnoreCase)
    {
        "temp", "tmp", "game", "games", "oyun", "oyunlar", "bin", "binaries", "build",
        "steam", "steamapps", "common", "steamlibrary", "library", "app", "apps",
        "program files", "program files (x86)", "x64", "x86", "win32", "win64",
        "release", "debug", "data", "content", "client", "files", "new folder",
        "yeni klasör", "downloads", "indirilenler", "desktop", "masaüstü", "documents",
        "belgeler", "users", "kullanıcılar"
    };

    /// <summary>
    /// Mağaza aramasına gönderilecek terimler: hedef klasörün ve üst klasörlerinin adları.
    /// Ayraç ve alt çizgiler boşluğa çevrilir ki "RF_Online" da eşleşsin.
    /// </summary>
    public static List<string> AramaTerimleri(string hedefKlasor)
        => KlasorAdlari(hedefKlasor)
            .Select(a => a.Replace('_', ' ').Replace('-', ' ').Replace('.', ' ').Trim())
            .Where(a => a.Length >= 3 && !GenelKlasorAdlari.Contains(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>
    /// Mağaza aramasından dönen adayları, klasör adına yakınlığa göre sıralar. Sonuçlar
    /// TAHMİNDİR; en olası olan başta olacak şekilde kullanıcıya seçtirilmelidir.
    /// </summary>
    public static List<AppIdSonucu> AdaGoreAra(string hedefKlasor, IReadOnlyList<SteamUygulama> uygulamalar)
    {
        // Klasör adları hedeften yukarı doğru sıralıdır. Hedef klasörün kendi adı en güçlü
        // sinyaldir; her üst seviye zayıflar. Ağırlık olmadan "…\Games\Oyun" gibi bir yolda
        // üst klasör adıyla eşleşen alakasız bir oyun doğru sonucu geçebiliyor.
        var aranan = KlasorAdlari(hedefKlasor)
            .Select((ad, derinlik) => (Ad: Normalize(ad), Agirlik: DerinlikAgirligi(derinlik)))
            .Where(a => a.Ad.Length >= 3 && !GenelKlasorAdlari.Contains(a.Ad))
            .GroupBy(a => a.Ad, StringComparer.Ordinal)
            .Select(g => (Ad: g.Key, Agirlik: g.Max(x => x.Agirlik)))
            .ToList();

        if (aranan.Count == 0) return new List<AppIdSonucu>();

        var puanli = new List<(double Puan, string Ad, string AppId)>();

        foreach (var uygulama in uygulamalar)
        {
            var normalAd = Normalize(uygulama.Ad);
            if (normalAd.Length == 0) continue;

            var enIyi = 0d;
            foreach (var (a, agirlik) in aranan)
            {
                // Tam eşleşme en güçlü sinyal; kapsama daha zayıf.
                var ham =
                    string.Equals(normalAd, a, StringComparison.Ordinal) ? 100
                    : normalAd.StartsWith(a, StringComparison.Ordinal) ? 70
                    : a.StartsWith(normalAd, StringComparison.Ordinal) ? 60
                    : normalAd.Contains(a, StringComparison.Ordinal) ? 40
                    : 0;

                if (ham > 0) enIyi = Math.Max(enIyi, ham * agirlik);
            }

            if (enIyi > 0) puanli.Add((enIyi, uygulama.Ad, uygulama.AppId));
        }

        return puanli
            .OrderByDescending(p => p.Puan)
            .ThenBy(p => p.Ad.Length)
            .Take(15)
            .Select(p => new AppIdSonucu(p.AppId, p.Ad, AppIdKaynagi.AdEslesmesi))
            .ToList();
    }

    /// <summary>
    /// Hedeften yukarı çıkıldıkça klasör adının oyun adı olma ihtimali düşer.
    /// 0 = hedef klasörün kendisi.
    /// </summary>
    private static double DerinlikAgirligi(int derinlik) => derinlik switch
    {
        0 => 1.00,
        1 => 0.55,
        _ => 0.30
    };

    /// <summary>
    /// Ad karşılaştırması için normalleştirme: yalnızca harf ve rakam kalır, tamamı küçük harf.
    ///
    /// ToLowerInvariant ZORUNLUDUR: tr-TR kültüründe ToLower() "I" harfini "ı"ya çevirir ve
    /// "RF Online" adı "rf onlıne" olur; karşılaştırılan iki taraf farklı çevrildiğinde eşleşme
    /// sessizce kaçar.
    /// </summary>
    public static string Normalize(string metin)
    {
        if (string.IsNullOrWhiteSpace(metin)) return string.Empty;

        var sb = new StringBuilder(metin.Length);
        foreach (var karakter in metin.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(karakter)) sb.Append(karakter);
        }
        return sb.ToString();
    }

    /// <summary>
    /// Oyunun adını taşıyabilecek klasör adları: hedef klasör ve bin\x64 gibi bir alt klasörde
    /// olma ihtimaline karşı üst klasörleri.
    /// </summary>
    private static List<string> KlasorAdlari(string hedefKlasor)
    {
        var sonuc = new List<string>();

        try
        {
            var dizin = new DirectoryInfo(Path.GetFullPath(hedefKlasor));
            var derinlik = 0;

            while (dizin?.Parent is not null && derinlik < 3)
            {
                sonuc.Add(dizin.Name);
                dizin = dizin.Parent;
                derinlik++;

                // steamapps\common kalıbına ulaşıldıysa daha yukarısı oyun adı olamaz.
                if (string.Equals(dizin.Name, "common", StringComparison.OrdinalIgnoreCase)) break;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return sonuc;
        }

        return sonuc;
    }

    /// <summary>steam_appid.txt aranacak klasörler: hedef ve bir seviye alt klasörleri.</summary>
    private static IEnumerable<string> AramaKlasorleri(string hedefKlasor)
    {
        yield return hedefKlasor;

        List<string> altlar;
        try
        {
            altlar = Directory.EnumerateDirectories(hedefKlasor)
                .Where(k => !Path.GetFileName(k).StartsWith('.'))
                .Take(30)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var alt in altlar) yield return alt;
    }
}
