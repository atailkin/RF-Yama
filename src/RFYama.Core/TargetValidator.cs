using System.ComponentModel;
using System.Diagnostics;

namespace RFYama.Core;

public enum BulguSeviyesi
{
    Bilgi,
    Uyari,
    Hata
}

public sealed record DogrulamaBulgusu(BulguSeviyesi Seviye, string Mesaj);

public sealed class DogrulamaSonucu
{
    public List<DogrulamaBulgusu> Bulgular { get; } = new();

    /// <summary>Hiç Hata seviyesinde bulgu yoksa kuruluma izin verilir.</summary>
    public bool Gecerli => Bulgular.All(b => b.Seviye != BulguSeviyesi.Hata);

    public bool UyariVar => Bulgular.Any(b => b.Seviye == BulguSeviyesi.Uyari);

    public void Ekle(BulguSeviyesi seviye, string mesaj) => Bulgular.Add(new DogrulamaBulgusu(seviye, mesaj));
}

public static class TargetValidator
{
    /// <summary>Kurulum yapılmasına asla izin verilmeyen klasörler.</summary>
    public static bool YasakliKlasorMu(string klasor, out string neden)
    {
        neden = string.Empty;

        string tam;
        try
        {
            tam = Path.TrimEndingDirectorySeparator(Path.GetFullPath(klasor));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            neden = "Klasör yolu geçersiz.";
            return true;
        }

        // Sürücü kökü — C:\ gibi
        var kok = Path.GetPathRoot(tam);
        if (!string.IsNullOrEmpty(kok) &&
            string.Equals(Path.TrimEndingDirectorySeparator(kok), tam, StringComparison.OrdinalIgnoreCase))
        {
            neden = "Sürücünün kök dizinine kurulum yapılamaz. Oyunun kendi klasörünü seçin.";
            return true;
        }

        var windows = Path.TrimEndingDirectorySeparator(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows));

        if (AltindaMi(tam, windows) || string.Equals(tam, windows, StringComparison.OrdinalIgnoreCase))
        {
            neden =
                "Windows klasörüne kurulum yapılamaz. winmm.dll bir sistem dosyası adıdır; " +
                "System32 veya SysWOW64 içine kopyalanması işletim sistemini bozar. " +
                "Lütfen oyunun kurulu olduğu klasörü seçin.";
            return true;
        }

        // Program Files köklerinin kendisi (alt klasörleri meşru oyun kurulumları olabilir)
        foreach (var ozel in new[]
                 {
                     Environment.SpecialFolder.ProgramFiles,
                     Environment.SpecialFolder.ProgramFilesX86
                 })
        {
            var yol = Environment.GetFolderPath(ozel);
            if (string.IsNullOrEmpty(yol)) continue;
            if (string.Equals(tam, Path.TrimEndingDirectorySeparator(yol), StringComparison.OrdinalIgnoreCase))
            {
                neden = "Program Files klasörünün kendisi değil, oyunun kendi klasörü seçilmelidir.";
                return true;
            }
        }

        // Uygulamanın kendi klasörü — kendi dosyalarının üzerine yazmasını engelle
        var kendiKlasor = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        if (string.Equals(tam, kendiKlasor, StringComparison.OrdinalIgnoreCase))
        {
            neden = "RF-Yama'nın kendi klasörü hedef olarak seçilemez.";
            return true;
        }

        return false;
    }

    private static bool AltindaMi(string yol, string ustKlasor)
    {
        if (string.IsNullOrEmpty(ustKlasor)) return false;
        var ust = Path.TrimEndingDirectorySeparator(ustKlasor) + Path.DirectorySeparatorChar;
        return yol.StartsWith(ust, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Hedef klasörü kurulum öncesi bütünüyle doğrular: yasaklı klasör, yazma izni, kilitli
    /// dosyalar ve mimari uyumu.
    /// </summary>
    public static DogrulamaSonucu Dogrula(string hedefKlasor, IReadOnlyList<string> kurulacakDosyalar)
    {
        var sonuc = new DogrulamaSonucu();

        if (string.IsNullOrWhiteSpace(hedefKlasor))
        {
            sonuc.Ekle(BulguSeviyesi.Hata, "Hedef klasör seçilmedi.");
            return sonuc;
        }

        if (!Directory.Exists(hedefKlasor))
        {
            sonuc.Ekle(BulguSeviyesi.Hata, "Seçilen klasör bulunamadı.");
            return sonuc;
        }

        if (YasakliKlasorMu(hedefKlasor, out var neden))
        {
            sonuc.Ekle(BulguSeviyesi.Hata, neden);
            return sonuc;
        }

        // Yazma izni
        if (!YazilabilirMi(hedefKlasor, out var yazmaHatasi))
            sonuc.Ekle(BulguSeviyesi.Hata, yazmaHatasi);

        // Klasörde oyun var mı
        var exeler = OyunExeleri(hedefKlasor);
        if (exeler.Count == 0)
            sonuc.Ekle(BulguSeviyesi.Uyari, "Bu klasörde hiç .exe dosyası yok. Doğru klasörü seçtiğinizden emin olun.");

        // Kilitli dosyalar
        foreach (var dosyaAdi in kurulacakDosyalar)
        {
            var hedef = Path.Combine(hedefKlasor, dosyaAdi);
            if (!File.Exists(hedef)) continue;

            if (KilitliMi(hedef))
            {
                var surec = KilitleyenSurecAdi(hedefKlasor);
                sonuc.Ekle(BulguSeviyesi.Hata, surec is null
                    ? $"\"{dosyaAdi}\" şu anda başka bir program tarafından kullanılıyor. Oyunu kapatıp tekrar deneyin."
                    : $"\"{dosyaAdi}\" kullanımda. Önce \"{surec}\" programını kapatın.");
            }
        }

        return sonuc;
    }

    /// <summary>Yama DLL'leri ile hedefteki oyunun mimarisini karşılaştırır.</summary>
    public static DogrulamaBulgusu? MimariKontrol(string oyunExeYolu, IReadOnlyList<string> yamaDllYollari)
    {
        if (!File.Exists(oyunExeYolu)) return null;

        var oyun = PeArchitecture.Oku(oyunExeYolu);
        if (oyun == PeMimari.Bilinmiyor) return null;

        foreach (var dll in yamaDllYollari)
        {
            if (!File.Exists(dll)) continue;
            var dllMimari = PeArchitecture.Oku(dll);
            if (dllMimari == PeMimari.Bilinmiyor) continue;

            if (dllMimari != oyun)
            {
                return new DogrulamaBulgusu(BulguSeviyesi.Hata,
                    $"Mimari uyuşmuyor: oyun {PeArchitecture.Ad(oyun)}, " +
                    $"\"{Path.GetFileName(dll)}\" ise {PeArchitecture.Ad(dllMimari)}. " +
                    $"Oyun bu DLL'i yükleyemez. {PeArchitecture.Ad(oyun)} sürümünü payload klasörüne koyun.");
            }
        }

        return new DogrulamaBulgusu(BulguSeviyesi.Bilgi,
            $"Mimari uyumlu: {PeArchitecture.Ad(oyun)}.");
    }

    private static bool YazilabilirMi(string klasor, out string hata)
    {
        hata = string.Empty;
        var deneme = Path.Combine(klasor, $".rfyama-yazma-testi-{Guid.NewGuid():N}.tmp");
        try
        {
            using (var fs = new FileStream(deneme, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                fs.WriteByte(0);
            }
            File.Delete(deneme);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            hata = "Bu klasöre yazma izniniz yok. Uygulamayı yönetici olarak çalıştırmayı deneyin.";
            return false;
        }
        catch (IOException ex)
        {
            hata = $"Klasöre yazılamıyor: {ex.Message}";
            return false;
        }
    }

    public static bool KilitliMi(string dosyaYolu)
    {
        try
        {
            using var fs = new FileStream(dosyaYolu, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Salt okunur dosya kilitli değildir; kurulum sırasında öznitelik temizlenir.
            return false;
        }
    }

    /// <summary>
    /// Hedef klasörden çalışan bir süreç varsa adını döndürür. MainModule erişimi başka
    /// kullanıcıya ait veya yükseltilmiş süreçlerde hata verir; bu durumda o süreç atlanır.
    /// </summary>
    public static string? KilitleyenSurecAdi(string hedefKlasor)
    {
        var ust = Path.TrimEndingDirectorySeparator(Path.GetFullPath(hedefKlasor)) + Path.DirectorySeparatorChar;

        foreach (var surec in Process.GetProcesses())
        {
            try
            {
                var yol = surec.MainModule?.FileName;
                if (yol is not null && yol.StartsWith(ust, StringComparison.OrdinalIgnoreCase))
                    return surec.ProcessName;
            }
            catch (Win32Exception) { /* erişim yok — atla */ }
            catch (InvalidOperationException) { /* süreç sonlandı — atla */ }
            catch (NotSupportedException) { /* uzak süreç — atla */ }
            finally
            {
                surec.Dispose();
            }
        }

        return null;
    }

    private static readonly string[] YokSayilanExeOnekleri =
    {
        "unins", "vcredist", "dxsetup", "dxwebsetup", "oalinst", "directx",
        "crashhandler", "unitycrashhandler", "crashreport", "setup", "redist"
    };

    /// <summary>
    /// Klasördeki aday oyun .exe'leri, en olası olan başta olacak şekilde.
    /// Kurulum/yardımcı programlar ayıklanır; kalanlar boyuta göre sıralanır çünkü oyun
    /// çalıştırılabiliri neredeyse her zaman klasörün en büyük exe'sidir.
    /// </summary>
    public static List<string> OyunExeleri(string klasor)
    {
        try
        {
            var klasorAdi = new DirectoryInfo(klasor).Name;

            return new DirectoryInfo(klasor)
                .EnumerateFiles("*.exe", SearchOption.TopDirectoryOnly)
                .Where(f => !YokSayilanExeOnekleri.Any(o =>
                    f.Name.StartsWith(o, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(f => string.Equals(
                    Path.GetFileNameWithoutExtension(f.Name), klasorAdi, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(f => f.Length)
                .Select(f => f.Name)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }
}
