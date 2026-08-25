using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RFYama.Core;

/// <summary>
/// Makinedeki Steam kurulumunu ve tüm kütüphane klasörlerini bulur.
///
/// Mevcut sahiplik kontrolü yalnızca hedef klasörün İÇİNDE bulunduğu kütüphaneye bakıyordu;
/// oyun başka bir diske taşındığında bu yetmiyor. Burada tüm kütüphaneler taranır.
///
/// Yine yalnızca düz metin .vdf/.acf dosyaları okunur; oturum belirteci içeren
/// loginusers.vdf, config.vdf ve ssfn* dosyalarına dokunulmaz.
/// </summary>
public static class SteamKutuphaneleri
{
    private static readonly Regex VdfCift = new(@"""([^""]+)""\s*""([^""]*)""", RegexOptions.Compiled);

    /// <summary>Steam'in kurulu olduğu klasör. Bulunamazsa null.</summary>
    public static string? SteamKlasoru()
    {
        foreach (var (kok, altAnahtar, deger) in new[]
                 {
                     (Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
                     (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
                     (Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath")
                 })
        {
            try
            {
                using var anahtar = kok.OpenSubKey(altAnahtar);
                var yol = anahtar?.GetValue(deger) as string;
                if (!string.IsNullOrWhiteSpace(yol) && Directory.Exists(yol))
                    return Path.GetFullPath(yol);
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException
                                           or IOException or PlatformNotSupportedException)
            {
                // Kayıt defteri okunamadıysa aşağıdaki bilinen yollara düşülür.
            }
        }

        // Kayıt defteri yoksa/erişilemiyorsa yaygın konumlar.
        foreach (var ozel in new[] { Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolder.ProgramFiles })
        {
            var yol = Path.Combine(Environment.GetFolderPath(ozel), "Steam");
            if (Directory.Exists(yol)) return yol;
        }

        return null;
    }

    /// <summary>
    /// Tüm Steam kütüphanelerinin steamapps klasörleri. Ana kurulum ve
    /// libraryfolders.vdf'te listelenen ek diskler dahil.
    /// </summary>
    public static List<string> SteamappsKlasorleri()
    {
        var sonuc = new List<string>();

        var steam = SteamKlasoru();
        if (steam is null) return sonuc;

        var anaSteamapps = Path.Combine(steam, "steamapps");
        if (Directory.Exists(anaSteamapps)) sonuc.Add(anaSteamapps);

        // libraryfolders.vdf Steam sürümüne göre steamapps altında ya da kökte olabilir.
        foreach (var aday in new[]
                 {
                     Path.Combine(steam, "steamapps", "libraryfolders.vdf"),
                     Path.Combine(steam, "config", "libraryfolders.vdf")
                 })
        {
            if (!File.Exists(aday)) continue;

            foreach (var yol in KutuphaneYollariniAyristir(aday))
            {
                var steamapps = Path.Combine(yol, "steamapps");
                if (Directory.Exists(steamapps) &&
                    !sonuc.Any(v => string.Equals(v, steamapps, StringComparison.OrdinalIgnoreCase)))
                {
                    sonuc.Add(steamapps);
                }
            }
        }

        return sonuc;
    }

    private static IEnumerable<string> KutuphaneYollariniAyristir(string vdfYolu)
    {
        string metin;
        try
        {
            metin = File.ReadAllText(vdfYolu);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (Match m in VdfCift.Matches(metin))
        {
            if (!string.Equals(m.Groups[1].Value, "path", StringComparison.OrdinalIgnoreCase)) continue;

            // VDF içinde ters bölü çiftlenmiş yazılır: "D:\SteamLibrary"
            var yol = m.Groups[2].Value.Replace(@"\\", @"\");
            if (!string.IsNullOrWhiteSpace(yol)) yield return yol;
        }
    }

    /// <summary>Bir steamapps klasöründeki tüm appmanifest kayıtlarını okur.</summary>
    public static IEnumerable<SteamKurulumBilgisi> Manifestler(string steamappsKlasoru)
    {
        IEnumerable<string> dosyalar;
        try
        {
            dosyalar = Directory.EnumerateFiles(steamappsKlasoru, "appmanifest_*.acf", SearchOption.TopDirectoryOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var dosya in dosyalar)
        {
            string metin;
            try
            {
                metin = File.ReadAllText(dosya);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var alanlar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in VdfCift.Matches(metin))
            {
                if (!alanlar.ContainsKey(m.Groups[1].Value))
                    alanlar[m.Groups[1].Value] = m.Groups[2].Value;
            }

            if (alanlar.TryGetValue("appid", out var appid) &&
                alanlar.TryGetValue("installdir", out var installdir))
            {
                yield return new SteamKurulumBilgisi(
                    appid,
                    alanlar.GetValueOrDefault("name", installdir),
                    installdir,
                    dosya);
            }
        }
    }
}
