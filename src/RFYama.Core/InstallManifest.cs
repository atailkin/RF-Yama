using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RFYama.Core;

public sealed class KurulanDosya
{
    public string Ad { get; set; } = string.Empty;

    /// <summary>Kurulum anındaki içeriğin SHA-256'sı. Kaldırma yalnızca bu hash hâlâ
    /// eşleşiyorsa dosyayı siler — böylece kurmadığımız bir dosya asla silinmez.</summary>
    public string Sha256 { get; set; } = string.Empty;
}

/// <summary>
/// Bir kurulumun kaydı. Yedek alınmaz; kayıt yalnızca hangi dosyaların eklendiğini bilmek
/// içindir. Oyun klasörü temiz kalsın diye %APPDATA% altında saklanır.
/// </summary>
public sealed class KurulumKaydi
{
    public int Surum { get; set; } = 2;
    public DateTimeOffset Zaman { get; set; } = DateTimeOffset.Now;
    public string UygulamaSurumu { get; set; } = string.Empty;
    public string HedefKlasor { get; set; } = string.Empty;
    public List<KurulanDosya> Kurulanlar { get; set; } = new();

    public static string HashHesapla(string dosyaYolu)
    {
        using var akis = File.OpenRead(dosyaYolu);
        return Convert.ToHexString(SHA256.HashData(akis));
    }
}

/// <summary>
/// Kurulum kayıtlarının kalıcı deposu: %APPDATA%\RFYama\kurulumlar.json.
///
/// Kayıt hedef klasöre değil buraya yazılır; oyun klasörüne yalnızca yama dosyaları girer.
/// Depo silinirse Kaldır, payload'daki adlarla eşleşen dosyalara geri düşer.
/// </summary>
public static class KurulumDeposu
{
    /// <summary>
    /// Deponun bulunduğu klasör. Normalde %APPDATA%RFYama; testler gerçek kullanıcı kaydına
    /// dokunmamak için geçici bir klasöre yönlendirir.
    /// </summary>
    public static string? KlasorGecersizKil { get; set; }

    public static string Klasor() => KlasorGecersizKil ?? AppSettings.Klasor();

    public static string Yolu() => Path.Combine(Klasor(), "kurulumlar.json");

    private static readonly JsonSerializerOptions Secenekler = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static List<KurulumKaydi> Tumu()
    {
        try
        {
            var yol = Yolu();
            if (!File.Exists(yol)) return new List<KurulumKaydi>();
            return JsonSerializer.Deserialize<List<KurulumKaydi>>(File.ReadAllText(yol), Secenekler)
                   ?? new List<KurulumKaydi>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new List<KurulumKaydi>();
        }
    }

    public static KurulumKaydi? Bul(string hedefKlasor)
    {
        var aranan = Normalize(hedefKlasor);
        return Tumu().FirstOrDefault(k =>
            string.Equals(Normalize(k.HedefKlasor), aranan, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Aynı hedef için önceki kayıt varsa değiştirir.</summary>
    public static void Yaz(KurulumKaydi kayit)
    {
        var aranan = Normalize(kayit.HedefKlasor);
        var liste = Tumu()
            .Where(k => !string.Equals(Normalize(k.HedefKlasor), aranan, StringComparison.OrdinalIgnoreCase))
            .ToList();

        liste.Add(kayit);
        Kaydet(liste);
    }

    public static void Sil(string hedefKlasor)
    {
        var aranan = Normalize(hedefKlasor);
        var liste = Tumu()
            .Where(k => !string.Equals(Normalize(k.HedefKlasor), aranan, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Kaydet(liste);
    }

    private static void Kaydet(List<KurulumKaydi> liste)
    {
        try
        {
            Directory.CreateDirectory(Klasor());
            File.WriteAllText(Yolu(), JsonSerializer.Serialize(liste, Secenekler), IniDocument.BomsuzUtf8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kayıt yazılamazsa kurulum yine geçerlidir; Kaldır ad eşleşmesine geri düşer.
        }
    }

    private static string Normalize(string yol)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(yol));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return yol;
        }
    }
}
