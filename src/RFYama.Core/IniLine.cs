namespace RFYama.Core;

public enum IniSatirTuru
{
    Bos,
    Yorum,
    Bolum,
    AnahtarDeger,
    Taninmayan
}

/// <summary>
/// INI dosyasındaki tek bir satır. Ham metni olduğu gibi saklar; bir değer değiştirildiğinde
/// yalnızca değerin kapladığı aralık yeniden yazılır, satırın geri kalanı (girinti, anahtar
/// yazımı, '=' çevresindeki boşluklar) korunur.
/// </summary>
public sealed class IniLine
{
    public IniSatirTuru Tur { get; private init; }

    /// <summary>Satırın ham metni, satır sonu karakterleri hariç.</summary>
    public string Ham { get; private set; } = string.Empty;

    /// <summary>Bölüm satırlarında bölüm adı; anahtar satırlarında ait olduğu bölüm.</summary>
    public string? Bolum { get; internal set; }

    public string? Anahtar { get; private init; }

    // Ham metin içinde değerin başladığı indeks ve uzunluğu.
    private int _degerBaslangic;
    private int _degerUzunluk;

    public string Deger
    {
        get => Tur == IniSatirTuru.AnahtarDeger
            ? Ham.Substring(_degerBaslangic, _degerUzunluk)
            : string.Empty;
        set
        {
            if (Tur != IniSatirTuru.AnahtarDeger)
                throw new InvalidOperationException("Yalnızca anahtar=değer satırlarının değeri ayarlanabilir.");

            var yeni = value ?? string.Empty;
            // Değer satır sonu içeremez; içerirse dosya bozulur.
            yeni = yeni.Replace("\r", string.Empty).Replace("\n", string.Empty);

            Ham = string.Concat(
                Ham.AsSpan(0, _degerBaslangic),
                yeni,
                Ham.AsSpan(_degerBaslangic + _degerUzunluk));
            _degerUzunluk = yeni.Length;
        }
    }

    public static IniLine Ayristir(string ham)
    {
        var kirpik = ham.Trim();

        if (kirpik.Length == 0)
            return new IniLine { Tur = IniSatirTuru.Bos, Ham = ham };

        // Yorum: yalnızca satırın ilk boşluk-dışı karakteri ';' veya '#' ise.
        // Satır içi ';' YORUM SAYILMAZ — Win32 GetPrivateProfileString da saymaz ve
        // game_launch_options gibi alanlar meşru olarak ';' içerebilir.
        if (kirpik[0] == ';' || kirpik[0] == '#')
            return new IniLine { Tur = IniSatirTuru.Yorum, Ham = ham };

        if (kirpik[0] == '[')
        {
            var kapanis = kirpik.IndexOf(']');
            if (kapanis > 1)
            {
                return new IniLine
                {
                    Tur = IniSatirTuru.Bolum,
                    Ham = ham,
                    Bolum = kirpik.Substring(1, kapanis - 1).Trim()
                };
            }
            return new IniLine { Tur = IniSatirTuru.Taninmayan, Ham = ham };
        }

        var esittir = ham.IndexOf('=');
        if (esittir < 0)
            return new IniLine { Tur = IniSatirTuru.Taninmayan, Ham = ham };

        var anahtar = ham.Substring(0, esittir).Trim();
        if (anahtar.Length == 0)
            return new IniLine { Tur = IniSatirTuru.Taninmayan, Ham = ham };

        // Değer aralığı: '=' sonrasındaki boşluklar atlanır, satır sonundaki boşluklar dışarıda kalır.
        var baslangic = esittir + 1;
        while (baslangic < ham.Length && (ham[baslangic] == ' ' || ham[baslangic] == '\t'))
            baslangic++;

        var son = ham.Length;
        while (son > baslangic && (ham[son - 1] == ' ' || ham[son - 1] == '\t'))
            son--;

        return new IniLine
        {
            Tur = IniSatirTuru.AnahtarDeger,
            Ham = ham,
            Anahtar = anahtar,
            _degerBaslangic = baslangic,
            _degerUzunluk = son - baslangic
        };
    }

    /// <summary>Var olmayan bir anahtarı bölüme eklemek için yeni satır üretir.</summary>
    public static IniLine YeniAnahtar(string anahtar, string deger)
        => Ayristir($"{anahtar}={deger}");

    public static IniLine YeniBolum(string bolum)
        => Ayristir($"[{bolum}]");

    public static IniLine YeniBos()
        => new() { Tur = IniSatirTuru.Bos, Ham = string.Empty };
}
