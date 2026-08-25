using System.Reflection;

namespace RFYama.Core;

public sealed record KurulumSonucu(
    bool Basarili,
    string Mesaj,
    KurulumKaydi? Kayit = null);

/// <summary>
/// Yama dosyalarını hedef klasöre kurar ve kaldırır.
///
/// <para>
/// YEDEK ALINMAZ. Hedefte aynı adlı bir dosya varsa doğrudan üzerine yazılır ve özgün içerik
/// kalıcı olarak kaybolur. <c>winmm.dll</c> standart bir proxy DLL yuvası olduğu için hedef
/// klasörde başka bir moda ait bir winmm.dll bulunabilir; kurulum onu geri dönüşsüz değiştirir.
/// <see cref="UzerineYazilacaklar"/> ile hangi dosyaların ezileceği kurulum öncesi kullanıcıya
/// gösterilmelidir.
/// </para>
///
/// <para>
/// Kaldırma, dosyayı yalnızca içeriğinin SHA-256'sı kurulumda yazdığımız değerle eşleşiyorsa
/// siler. Böylece kurmadığımız ya da sonradan değiştirilmiş bir dosya asla silinmez.
/// </para>
/// </summary>
public static class PayloadInstaller
{
    /// <summary>Hedefte varsa üzerine yazılmayan dosyalar — kullanıcının ayarları burada.</summary>
    public static readonly string[] UzerineYazilmayanlar = { "unsteam.ini" };

    public static string PayloadKlasoru()
        => Path.Combine(AppContext.BaseDirectory, "payload");

    /// <summary>
    /// Kurulabilir sayılan uzantılar. Beyaz liste kullanılıyor ki payload klasörüne konan
    /// OKUBENI.txt gibi dosyalar kurulum listesinde görünmesin.
    /// </summary>
    private static readonly string[] KurulabilirUzantilar = { ".dll", ".ini" };

    /// <summary>payload klasöründeki kurulabilir dosyalar.</summary>
    public static List<string> PayloadDosyalari(string? payloadKlasoru = null)
    {
        var klasor = payloadKlasoru ?? PayloadKlasoru();
        if (!Directory.Exists(klasor)) return new List<string>();

        return Directory.EnumerateFiles(klasor, "*", SearchOption.TopDirectoryOnly)
            .Where(y => KurulabilirUzantilar.Contains(Path.GetExtension(y), StringComparer.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(a => !string.IsNullOrEmpty(a))
            .Select(a => a!)
            .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<string> PayloadDllleri(string? payloadKlasoru = null)
        => PayloadDosyalari(payloadKlasoru)
            .Where(a => a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// Seçilen dosyalardan hangilerinin hedefte zaten var olduğunu, yani üzerine yazılacağını
    /// döndürür. Yedek alınmadığı için bu liste kurulum onayında gösterilmelidir.
    /// </summary>
    public static List<string> UzerineYazilacaklar(string hedefKlasor, IReadOnlyList<string> dosyalar)
        => dosyalar
            .Where(ad => !UzerineYazilmayanlar.Contains(ad, StringComparer.OrdinalIgnoreCase))
            .Where(ad => File.Exists(Path.Combine(hedefKlasor, ad)))
            .ToList();

    public static KurulumSonucu Kur(
        string hedefKlasor,
        IReadOnlyList<string> dosyalar,
        string? payloadKlasoru = null,
        Action<string>? gunluk = null)
    {
        var payload = payloadKlasoru ?? PayloadKlasoru();
        gunluk ??= _ => { };

        if (!Directory.Exists(payload))
            return new KurulumSonucu(false, $"payload klasörü bulunamadı: {payload}");

        if (dosyalar.Count == 0)
            return new KurulumSonucu(false, "Kurulacak dosya seçilmedi.");

        foreach (var ad in dosyalar)
        {
            if (!File.Exists(Path.Combine(payload, ad)))
                return new KurulumSonucu(false, $"payload klasöründe \"{ad}\" yok.");
        }

        var kayit = new KurulumKaydi
        {
            HedefKlasor = hedefKlasor,
            UygulamaSurumu = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "?"
        };

        // Hata hâlinde temizlik için: bu çalıştırmada hedefe yazdığımız dosyalar.
        var yazilanlar = new List<string>();

        try
        {
            foreach (var ad in dosyalar)
            {
                var kaynak = Path.Combine(payload, ad);
                var hedef = Path.Combine(hedefKlasor, ad);
                var mevcut = File.Exists(hedef);

                if (mevcut && UzerineYazilmayanlar.Contains(ad, StringComparer.OrdinalIgnoreCase))
                {
                    gunluk($"\"{ad}\" zaten var, ayarlarınız korunuyor — atlandı.");
                    continue;
                }

                if (mevcut)
                {
                    OzniteliginKilidiniAc(hedef);
                    gunluk($"\"{ad}\" üzerine yazılıyor (özgün dosya geri alınamaz).");
                }

                File.Copy(kaynak, hedef, overwrite: true);
                yazilanlar.Add(hedef);

                kayit.Kurulanlar.Add(new KurulanDosya
                {
                    Ad = ad,
                    Sha256 = KurulumKaydi.HashHesapla(hedef)
                });

                gunluk($"\"{ad}\" kuruldu.");
            }

            if (kayit.Kurulanlar.Count > 0)
                KurulumDeposu.Yaz(kayit);

            return new KurulumSonucu(true,
                kayit.Kurulanlar.Count == 0
                    ? "Kurulacak yeni dosya yoktu; her şey zaten yerindeydi."
                    : $"{kayit.Kurulanlar.Count} dosya kuruldu.",
                kayit);
        }
        catch (UnauthorizedAccessException)
        {
            KismiKurulumuTemizle(yazilanlar, gunluk);
            return new KurulumSonucu(false,
                "Bu klasöre yazma izniniz yok. Uygulamayı yönetici olarak çalıştırıp tekrar deneyin. " +
                "Bu çalıştırmada kopyalanan dosyalar silindi.");
        }
        catch (IOException ex)
        {
            KismiKurulumuTemizle(yazilanlar, gunluk);
            return new KurulumSonucu(false,
                $"Kurulum sırasında hata: {ex.Message}. Bu çalıştırmada kopyalanan dosyalar silindi.");
        }
    }

    /// <summary>
    /// Kurulum yarıda kaldığında yalnızca bu çalıştırmada kopyalanan dosyaları siler. Yedek
    /// olmadığı için üzerine yazılmış özgün dosyalar geri getirilemez.
    /// </summary>
    private static void KismiKurulumuTemizle(List<string> yazilanlar, Action<string> gunluk)
    {
        if (yazilanlar.Count == 0) return;
        gunluk("Hata oluştu, bu çalıştırmada kopyalanan dosyalar siliniyor…");

        foreach (var yol in yazilanlar)
        {
            try
            {
                OzniteliginKilidiniAc(yol);
                File.Delete(yol);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                gunluk($"Silinemedi: {Path.GetFileName(yol)}");
            }
        }
    }

    public static KurulumKaydi? SonKurulum(string hedefKlasor) => KurulumDeposu.Bul(hedefKlasor);

    public static bool KuruluMu(string hedefKlasor)
    {
        var kayit = KurulumDeposu.Bul(hedefKlasor);
        if (kayit is null) return false;

        // Kayıt var ama dosyaların hiçbiri yerinde değilse kurulu sayma.
        return kayit.Kurulanlar.Any(d => File.Exists(Path.Combine(hedefKlasor, d.Ad)));
    }

    /// <summary>
    /// Kurulumu kaldırır: kayıttaki dosyaları siler. Bir dosyanın SHA-256'sı kurulumda
    /// yazdığımız değerden farklıysa o dosya SİLİNMEZ — kurmadığımız veya kullanıcının
    /// sonradan değiştirdiği bir dosyayı yok etmemek için.
    /// </summary>
    public static KurulumSonucu Kaldir(string hedefKlasor, Action<string>? gunluk = null)
    {
        gunluk ??= _ => { };

        var kayit = KurulumDeposu.Bul(hedefKlasor);
        if (kayit is null)
            return new KurulumSonucu(false, "Bu klasörde RF-Yama kurulum kaydı bulunamadı.");

        var silinen = 0;
        var atlanan = new List<string>();

        try
        {
            foreach (var dosya in kayit.Kurulanlar)
            {
                var yol = Path.Combine(hedefKlasor, dosya.Ad);
                if (!File.Exists(yol))
                {
                    gunluk($"\"{dosya.Ad}\" zaten yok.");
                    continue;
                }

                OzniteliginKilidiniAc(yol);

                var suankiHash = KurulumKaydi.HashHesapla(yol);
                if (!string.Equals(suankiHash, dosya.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    atlanan.Add(dosya.Ad);
                    gunluk($"\"{dosya.Ad}\" kurulumdan sonra değişmiş — silinmedi, olduğu gibi bırakıldı.");
                    continue;
                }

                File.Delete(yol);
                silinen++;
                gunluk($"\"{dosya.Ad}\" kaldırıldı.");
            }

            KurulumDeposu.Sil(hedefKlasor);

            var mesaj = $"Kaldırma tamamlandı: {silinen} dosya silindi.";
            if (atlanan.Count > 0)
                mesaj += $" Değiştirilmiş oldukları için silinmeyenler: {string.Join(", ", atlanan)}.";

            return new KurulumSonucu(true, mesaj, kayit);
        }
        catch (UnauthorizedAccessException)
        {
            return new KurulumSonucu(false,
                "Kaldırma için yazma izni gerekiyor. Uygulamayı yönetici olarak çalıştırın.");
        }
        catch (IOException ex)
        {
            return new KurulumSonucu(false, $"Kaldırma sırasında hata: {ex.Message}");
        }
    }

    /// <summary>Salt okunur öznitelikli dosyalar kopyalanamaz; özniteliği temizler.</summary>
    private static void OzniteliginKilidiniAc(string yol)
    {
        try
        {
            var oznitelik = File.GetAttributes(yol);
            if (oznitelik.HasFlag(FileAttributes.ReadOnly))
                File.SetAttributes(yol, oznitelik & ~FileAttributes.ReadOnly);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Öznitelik temizlenemediyse kopyalama zaten anlamlı bir hata verecek.
        }
    }
}
