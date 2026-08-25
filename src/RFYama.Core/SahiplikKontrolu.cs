using System.Text.RegularExpressions;

namespace RFYama.Core;

public sealed record SteamKurulumBilgisi(
    string AppId,
    string Ad,
    string KurulumKlasoru,
    string ManifestYolu);

/// <summary>
/// Oyunun bu bilgisayara Steam üzerinden kurulup kurulmadığını, hedef klasörden yukarı çıkarak
/// <c>steamapps\appmanifest_&lt;appid&gt;.acf</c> dosyasından tespit eder.
///
/// BİLİNÇLİ OLARAK OKUNMAYAN DOSYALAR: loginusers.vdf, config.vdf, ssfn*. Bunlar oturum
/// belirteci içerir; okunmaları antivirüslerin kimlik-hırsızlığı sezgiselleriyle birebir
/// eşleşir ve programın "trojan" olarak işaretlenmesine yol açar. .acf dosyaları ise düz
/// metindir, gizli veri içermez, okunmaları sıradan dosya G/Ç'sidir.
///
/// Bu bir satın alma kanıtı değildir — "bu oyun bu makineye Steam üzerinden kuruldu" sinyalidir.
/// Bu yüzden bulunamaması kurulumu engellemez, yalnızca uyarı üretir.
/// </summary>
public static class SahiplikKontrolu
{
    private static readonly Regex VdfCift = new("\"([^\"]+)\"\\s*\"([^\"]*)\"", RegexOptions.Compiled);

    /// <summary>
    /// Hedef klasörden yukarı çıkarak steamapps klasörünü bulur ve bu klasöre karşılık gelen
    /// appmanifest dosyasını okur. Bulunamazsa null.
    /// </summary>
    public static SteamKurulumBilgisi? Bul(string hedefKlasor)
    {
        var steamapps = SteamappsKlasoruBul(hedefKlasor, out var oyunKlasorAdi);
        if (steamapps is null || oyunKlasorAdi is null) return null;

        foreach (var manifest in ManifestleriListele(steamapps))
        {
            var alanlar = ManifestOku(manifest);
            if (alanlar is null) continue;

            if (!alanlar.TryGetValue("installdir", out var installdir)) continue;

            if (string.Equals(installdir, oyunKlasorAdi, StringComparison.OrdinalIgnoreCase))
            {
                return new SteamKurulumBilgisi(
                    alanlar.GetValueOrDefault("appid", string.Empty),
                    alanlar.GetValueOrDefault("name", oyunKlasorAdi),
                    installdir,
                    manifest);
            }
        }

        return null;
    }

    /// <summary>Belirli bir AppID'nin manifestini arar.</summary>
    public static SteamKurulumBilgisi? BulAppId(string hedefKlasor, string appId)
    {
        if (string.IsNullOrWhiteSpace(appId) || appId == "0") return null;

        var steamapps = SteamappsKlasoruBul(hedefKlasor, out _);
        if (steamapps is null) return null;

        var yol = Path.Combine(steamapps, $"appmanifest_{appId}.acf");
        if (!File.Exists(yol)) return null;

        var alanlar = ManifestOku(yol);
        if (alanlar is null) return null;

        return new SteamKurulumBilgisi(
            alanlar.GetValueOrDefault("appid", appId),
            alanlar.GetValueOrDefault("name", string.Empty),
            alanlar.GetValueOrDefault("installdir", string.Empty),
            yol);
    }

    /// <summary>
    /// Kurulum ekranında gösterilecek sahiplik bulgusu. Engelleyici değildir: taşınmış
    /// kurulumlar ve Steam kütüphanesi dışındaki meşru kopyalar vardır.
    /// </summary>
    public static DogrulamaBulgusu Degerlendir(string hedefKlasor)
    {
        var bilgi = Bul(hedefKlasor);
        if (bilgi is not null)
        {
            var ad = string.IsNullOrWhiteSpace(bilgi.Ad) ? bilgi.KurulumKlasoru : bilgi.Ad;
            return new DogrulamaBulgusu(BulguSeviyesi.Bilgi,
                $"\"{ad}\" bu bilgisayara Steam üzerinden kurulmuş görünüyor (AppID {bilgi.AppId}).");
        }

        return new DogrulamaBulgusu(BulguSeviyesi.Uyari,
            "Bu klasör için Steam kurulum kaydı bulunamadı. Oyunu Steam kütüphanenizden " +
            "kurduysanız klasörü taşımış olabilirsiniz; kuruluma devam edebilirsiniz.");
    }

    /// <summary>
    /// Hedef klasörden AppID'yi yerel olarak tespit eder — Steam mağazasına istek gerekmez.
    /// Bulunamazsa null.
    /// </summary>
    public static string? AppIdTespitEt(string hedefKlasor)
    {
        var bilgi = Bul(hedefKlasor);
        return string.IsNullOrWhiteSpace(bilgi?.AppId) ? null : bilgi.AppId;
    }

    /// <summary>
    /// Hedef klasörden yukarı çıkarak steamapps klasörünü arar.
    /// Beklenen kalıp: ...\steamapps\common\&lt;OyunKlasoru&gt;\[alt klasörler]
    /// </summary>
    private static string? SteamappsKlasoruBul(string hedefKlasor, out string? oyunKlasorAdi)
    {
        oyunKlasorAdi = null;

        DirectoryInfo? dizin;
        try
        {
            dizin = new DirectoryInfo(Path.GetFullPath(hedefKlasor));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        // Oyun kendi klasörünün alt klasörlerinde de olabilir (ör. ...\common\Oyun\bin\).
        // "common" bulunana kadar yukarı çık; "common"un hemen altındaki klasör oyun klasörüdür.
        var onceki = dizin;
        while (dizin is not null)
        {
            if (string.Equals(dizin.Name, "common", StringComparison.OrdinalIgnoreCase) &&
                dizin.Parent is not null &&
                string.Equals(dizin.Parent.Name, "steamapps", StringComparison.OrdinalIgnoreCase))
            {
                oyunKlasorAdi = onceki?.Name;
                return dizin.Parent.FullName;
            }

            onceki = dizin;
            dizin = dizin.Parent;
        }

        return null;
    }

    private static IEnumerable<string> ManifestleriListele(string steamappsKlasoru)
    {
        try
        {
            return Directory.EnumerateFiles(steamappsKlasoru, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// .acf dosyasındaki "anahtar" "değer" çiftlerini okur. İç içe bloklar düzleştirilir;
    /// ihtiyaç duyulan alanlar (appid, name, installdir) en üst düzeydedir ve ilk görülen
    /// değer korunur.
    /// </summary>
    private static Dictionary<string, string>? ManifestOku(string yol)
    {
        string metin;
        try
        {
            // .acf dosyaları UTF-8'dir; büyük olamayacakları için tamamı okunur.
            metin = File.ReadAllText(yol);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        var sonuc = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in VdfCift.Matches(metin))
        {
            var anahtar = m.Groups[1].Value;
            if (!sonuc.ContainsKey(anahtar))
                sonuc[anahtar] = m.Groups[2].Value;
        }

        return sonuc.Count == 0 ? null : sonuc;
    }
}
