using System.Text.Json;

namespace RFYama.Core;

/// <summary>%APPDATA%\RFYama\settings.json içinde saklanan uygulama tercihleri.</summary>
public sealed class AppSettings
{
    public string? SonHedefKlasor { get; set; }
    public string? SonKodlama { get; set; }
    public int PencereGenislik { get; set; }
    public int PencereYukseklik { get; set; }

    /// <summary>Koyu tema seçili mi. Durum çubuğundaki tema düğmesiyle değişir.</summary>
    public bool KaranlikMod { get; set; }

    private static readonly JsonSerializerOptions Secenekler = new() { WriteIndented = true };

    public static string Klasor()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RFYama");

    public static string Yolu() => Path.Combine(Klasor(), "settings.json");

    public static AppSettings Yukle()
    {
        try
        {
            var yol = Yolu();
            if (!File.Exists(yol)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(yol), Secenekler) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
    }

    public void Kaydet()
    {
        try
        {
            Directory.CreateDirectory(Klasor());
            File.WriteAllText(Yolu(), JsonSerializer.Serialize(this, Secenekler), IniDocument.BomsuzUtf8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Tercihlerin kaydedilememesi işlevselliği etkilemez; sessizce geçilir.
        }
    }
}
