using System.Globalization;

namespace RFYama.Core;

public enum AlanTuru
{
    Metin,
    Sayi,
    Onay,
    Liste,
    Klasor,
    /// <summary>Hedef klasördeki .exe dosyalarından seçim.</summary>
    OyunDosyasi,
    /// <summary>payload\ klasöründeki .dll dosyalarından seçim.</summary>
    YamaDll,
    /// <summary>SteamID64 — "Rastgele oluştur" düğmesi eşlik eder.</summary>
    SteamKimligi,
    /// <summary>AppID — mağaza bağlantısı yapıştırıldığında yerel olarak ayrıştırılır.</summary>
    UygulamaKimligi
}

public sealed class AlanTanimi
{
    public required string Bolum { get; init; }
    public required string Anahtar { get; init; }
    public required string Etiket { get; init; }
    public AlanTuru Tur { get; init; } = AlanTuru.Metin;
    public string Varsayilan { get; init; } = string.Empty;
    public string? Aciklama { get; init; }
    public string[]? Secenekler { get; init; }
    public string DogruDeger { get; init; } = "1";
    public string YanlisDeger { get; init; } = "0";

    /// <summary>Geçerliyse null, değilse Türkçe hata mesajı döndürür.</summary>
    public Func<string, string?>? Dogrula { get; init; }
}

/// <summary>
/// unsteam.ini alanlarının tek kaynak listesi. Ayarlar formu bu listeden üretilir; yeni bir ayar
/// eklemek buraya bir satır eklemektir. Burada tanımlı olmayan ama dosyada bulunan anahtarlar
/// Gelişmiş sekmesindeki tabloda düzenlenebilir — hiçbir ayar sessizce kaybolmaz.
/// </summary>
public static class AlanTanimlari
{
    public const string BolumLoader = "loader";
    public const string BolumGame = "game";

    /// <summary>Steam dil kodları (Steamworks API'sinin kullandığı adlar).</summary>
    public static readonly string[] Diller =
    {
        "turkish", "english", "german", "french", "spanish", "latam", "italian",
        "russian", "polish", "portuguese", "brazilian", "dutch", "danish", "finnish",
        "norwegian", "swedish", "czech", "hungarian", "romanian", "bulgarian", "greek",
        "ukrainian", "japanese", "koreana", "schinese", "tchinese", "thai", "vietnamese",
        "arabic"
    };

    public static readonly IReadOnlyList<AlanTanimi> Tumu = new List<AlanTanimi>
    {
        new()
        {
            Bolum = BolumLoader,
            Anahtar = "exe_file",
            Etiket = "Oyun dosyası",
            Tur = AlanTuru.OyunDosyasi,
            Aciklama = "Başlatılacak oyunun .exe dosyası. Hedef klasör seçildiğinde otomatik doldurulur.",
            Dogrula = d => string.IsNullOrWhiteSpace(d)
                ? "Oyun dosyası boş bırakılamaz."
                : d.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : "Dosya adı .exe ile bitmeli."
        },
        new()
        {
            Bolum = BolumLoader,
            Anahtar = "dll_file",
            Etiket = "Yüklenecek DLL",
            Tur = AlanTuru.YamaDll,
            Varsayilan = "unsteam.dll",
            Aciklama = "Oyuna enjekte edilecek yama DLL'i.",
            Dogrula = d => string.IsNullOrWhiteSpace(d)
                ? "DLL adı boş bırakılamaz."
                : d.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : "Dosya adı .dll ile bitmeli."
        },

        new()
        {
            Bolum = BolumGame,
            Anahtar = "offline_mode",
            Etiket = "Çevrimdışı mod",
            Tur = AlanTuru.Onay,
            Varsayilan = "0",
            Aciklama = "Açıkken oyun hiçbir ağ bağlantısı kurmaya çalışmaz."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "steam_id",
            Etiket = "Steam kimliği",
            Tur = AlanTuru.SteamKimligi,
            Varsayilan = "0",
            Aciklama = "0 bırakılırsa otomatik atanır. Çok oyunculu oturumda sizi ayırt eden numaradır.",
            Dogrula = d =>
            {
                var k = (d ?? string.Empty).Trim();
                if (k.Length == 0 || k == "0") return null;
                if (!ulong.TryParse(k, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    return "Yalnızca rakam girilebilir.";
                return SteamKimlik.GecerliMi(k)
                    ? null
                    : "Geçerli bir SteamID64 değil (17 haneli, 7656119… ile başlar).";
            }
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "player_name",
            Etiket = "Oyuncu adı",
            Tur = AlanTuru.Metin,
            Aciklama = "Diğer oyuncuların göreceği ad. Türkçe karakter kullanabilirsiniz.",
            Dogrula = d => (d ?? string.Empty).Length > 64
                ? "Oyuncu adı en fazla 64 karakter olabilir."
                : null
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "real_app_id",
            Etiket = "Gerçek uygulama kimliği",
            Tur = AlanTuru.UygulamaKimligi,
            Varsayilan = "0",
            Aciklama = "Oyunun Steam AppID'si. Mağaza bağlantısını yapıştırırsanız numara kendiliğinden ayrılır.",
            Dogrula = d => string.IsNullOrWhiteSpace(d) || uint.TryParse(d.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out _)
                ? null
                : "Yalnızca rakam girilebilir."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "fake_app_id",
            Etiket = "Sahte uygulama kimliği",
            Tur = AlanTuru.Sayi,
            Varsayilan = "480",
            Aciklama = "Steam istemcisine bildirilen kimlik. 480 (Spacewar) çakışmayı önler; değiştirmeniz gerekmez.",
            Dogrula = d => string.IsNullOrWhiteSpace(d) || uint.TryParse(d.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out _)
                ? null
                : "Yalnızca rakam girilebilir."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "language",
            Etiket = "Dil",
            Tur = AlanTuru.Liste,
            Varsayilan = "turkish",
            Secenekler = Diller,
            Aciklama = "Oyuna bildirilecek dil."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "beta_name",
            Etiket = "Beta dalı",
            Tur = AlanTuru.Metin,
            Varsayilan = "public",
            Aciklama = "Oyunun sürüm dalı. Normalde \"public\" kalır."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "saves_path",
            Etiket = "Kayıt klasörü",
            Tur = AlanTuru.Klasor,
            Aciklama = "Boş bırakılırsa varsayılan konum kullanılır."
        },
        new()
        {
            Bolum = BolumGame,
            Anahtar = "game_launch_options",
            Etiket = "Başlatma parametreleri",
            Tur = AlanTuru.Metin,
            Aciklama = "Oyuna geçirilecek komut satırı parametreleri."
        }
    };

    public static AlanTanimi? Bul(string bolum, string anahtar)
        => Tumu.FirstOrDefault(a =>
            string.Equals(a.Bolum, bolum, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.Anahtar, anahtar, StringComparison.OrdinalIgnoreCase));

    public static bool Taniniyor(string bolum, string anahtar) => Bul(bolum, anahtar) is not null;

    /// <summary>Tüm alanları varsayılan değerleriyle içeren yeni bir INI belgesi üretir.</summary>
    public static IniDocument VarsayilanBelge()
    {
        var belge = IniDocument.Bos();
        foreach (var alan in Tumu)
            belge.Yaz(alan.Bolum, alan.Anahtar, alan.Varsayilan);
        return belge;
    }
}
