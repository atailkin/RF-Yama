using System.Globalization;
using System.Text;

namespace RFYama.Core;

/// <summary>
/// Yorumları, satır sırasını ve boş satırları koruyan INI okuyucu/yazıcı.
///
/// İki tasarım kararı bilinçlidir:
///
/// 1. Tüm bölüm/anahtar karşılaştırmaları <see cref="StringComparer.OrdinalIgnoreCase"/> ile
///    yapılır. tr-TR kültüründe "ID".ToLower() → "ıd" olduğu için kültür duyarlı karşılaştırma
///    "ID"/"id" eşleşmesini kaçırır.
///
/// 2. Yeni dosyalar BOM'suz UTF-8 yazılır. DLL tarafı INI'yi Win32 GetPrivateProfileString ile
///    okuyorsa UTF-8 BOM'u ilk bölüm başlığını bozar ve tüm aramalar sessizce başarısız olur.
/// </summary>
public sealed class IniDocument
{
    private readonly List<IniLine> _satirlar = new();

    public static readonly Encoding BomsuzUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static bool _kodSayfalariKayitli;

    /// <summary>Windows-1254 gibi ANSI kod sayfaları .NET 8'de sağlayıcı kaydı gerektirir.</summary>
    public static void KodSayfalariniKaydet()
    {
        if (_kodSayfalariKayitli) return;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _kodSayfalariKayitli = true;
    }

    public Encoding Kodlama { get; set; } = BomsuzUtf8;

    public string SatirSonu { get; set; } = "\r\n";

    public IReadOnlyList<IniLine> Satirlar => _satirlar;

    public static IniDocument Bos() => new();

    public static IniDocument Yukle(string yol)
    {
        KodSayfalariniKaydet();
        var baytlar = File.ReadAllBytes(yol);
        var kodlama = KodlamaTespitEt(baytlar);
        var metin = MetneCevir(baytlar, kodlama);
        return Ayristir(metin, kodlama);
    }

    public static IniDocument Ayristir(string metin, Encoding? kodlama = null)
    {
        var belge = new IniDocument
        {
            Kodlama = kodlama ?? BomsuzUtf8,
            SatirSonu = metin.Contains("\r\n") ? "\r\n" : metin.Contains('\n') ? "\n" : "\r\n"
        };

        // Son satır sonu karakterinden sonra boş bir eleman oluşmasını engelle, ama dosya
        // gerçekten boş satırla bitiyorsa bunu Metin() sırasında geri ekle.
        var satirlar = metin.Split('\n');
        var sonuncuBos = satirlar.Length > 0 && satirlar[^1].Length == 0;
        var sayi = sonuncuBos ? satirlar.Length - 1 : satirlar.Length;

        string? aktifBolum = null;
        for (var i = 0; i < sayi; i++)
        {
            var ham = satirlar[i].TrimEnd('\r');
            var satir = IniLine.Ayristir(ham);

            if (satir.Tur == IniSatirTuru.Bolum)
                aktifBolum = satir.Bolum;
            else
                satir.Bolum = aktifBolum;

            belge._satirlar.Add(satir);
        }

        belge.SonSatirSonuVar = sonuncuBos;
        return belge;
    }

    /// <summary>Dosya satır sonu karakteriyle bitiyor mu (POSIX geleneği).</summary>
    public bool SonSatirSonuVar { get; set; } = true;

    public string? Oku(string bolum, string anahtar)
        => AnahtarSatiriBul(bolum, anahtar)?.Deger;

    public string OkuVeya(string bolum, string anahtar, string varsayilan)
        => AnahtarSatiriBul(bolum, anahtar)?.Deger ?? varsayilan;

    /// <summary>
    /// Sayıyı InvariantCulture ile yazar. tr-TR'de varsayılan biçimlendirme ondalık ayırıcı
    /// olarak ',' kullanır; "1,5" INI'ye yazılırsa yerel okuyucu değeri yanlış yorumlar.
    /// Sayısal bir değeri asla doğrudan ToString() ile yazmayın.
    /// </summary>
    public void YazSayi(string bolum, string anahtar, double deger)
        => Yaz(bolum, anahtar, deger.ToString(CultureInfo.InvariantCulture));

    public void YazSayi(string bolum, string anahtar, long deger)
        => Yaz(bolum, anahtar, deger.ToString(CultureInfo.InvariantCulture));

    public double? OkuSayi(string bolum, string anahtar)
    {
        var ham = Oku(bolum, anahtar);
        if (ham is null) return null;
        return double.TryParse(ham.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? d
            : null;
    }

    public bool VarMi(string bolum, string anahtar)
        => AnahtarSatiriBul(bolum, anahtar) is not null;

    /// <summary>
    /// Değeri ayarlar. Anahtar varsa yalnızca değer aralığı yeniden yazılır. Yoksa anahtar,
    /// dosyanın sonuna değil, ait olduğu bölümün sonuna eklenir. Bölüm de yoksa oluşturulur.
    /// </summary>
    public void Yaz(string bolum, string anahtar, string deger)
    {
        var mevcut = AnahtarSatiriBul(bolum, anahtar);
        if (mevcut is not null)
        {
            mevcut.Deger = deger;
            return;
        }

        var bolumSonu = BolumSonuIndeksi(bolum);
        if (bolumSonu < 0)
        {
            // Bölüm yok: dosyanın sonuna bölüm başlığıyla birlikte ekle.
            if (_satirlar.Count > 0 && _satirlar[^1].Tur != IniSatirTuru.Bos)
                _satirlar.Add(IniLine.YeniBos());

            var basliksatiri = IniLine.YeniBolum(bolum);
            _satirlar.Add(basliksatiri);

            var yeniSatir = IniLine.YeniAnahtar(anahtar, deger);
            yeniSatir.Bolum = bolum;
            _satirlar.Add(yeniSatir);
            return;
        }

        var eklenecek = IniLine.YeniAnahtar(anahtar, deger);
        eklenecek.Bolum = bolum;
        _satirlar.Insert(bolumSonu, eklenecek);
    }

    public bool Sil(string bolum, string anahtar)
    {
        var satir = AnahtarSatiriBul(bolum, anahtar);
        if (satir is null) return false;
        _satirlar.Remove(satir);
        return true;
    }

    /// <summary>Belgedeki tüm anahtar=değer çiftleri, dosyadaki sırayla.</summary>
    public IEnumerable<(string Bolum, string Anahtar, string Deger)> TumAnahtarlar()
    {
        foreach (var satir in _satirlar)
        {
            if (satir.Tur == IniSatirTuru.AnahtarDeger && satir.Anahtar is not null)
                yield return (satir.Bolum ?? string.Empty, satir.Anahtar, satir.Deger);
        }
    }

    public IEnumerable<string> Bolumler()
        => _satirlar.Where(s => s.Tur == IniSatirTuru.Bolum && s.Bolum is not null)
                    .Select(s => s.Bolum!)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>Bir bölümü, başlığı ve içindeki tüm satırlarla birlikte kaldırır.</summary>
    public bool BolumSil(string bolum)
    {
        var silinecek = _satirlar
            .Where(s => string.Equals(s.Bolum, bolum, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (silinecek.Count == 0) return false;
        foreach (var s in silinecek) _satirlar.Remove(s);
        return true;
    }

    public string Metin()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < _satirlar.Count; i++)
        {
            sb.Append(_satirlar[i].Ham);
            if (i < _satirlar.Count - 1 || SonSatirSonuVar)
                sb.Append(SatirSonu);
        }
        return sb.ToString();
    }

    public void Kaydet(string yol)
    {
        KodSayfalariniKaydet();
        var metin = Metin();
        var govde = Kodlama.GetBytes(metin);
        var onek = Kodlama.GetPreamble();

        var hedefKlasor = Path.GetDirectoryName(Path.GetFullPath(yol));
        if (!string.IsNullOrEmpty(hedefKlasor))
            Directory.CreateDirectory(hedefKlasor);

        if (onek.Length == 0)
        {
            File.WriteAllBytes(yol, govde);
        }
        else
        {
            var tumu = new byte[onek.Length + govde.Length];
            Buffer.BlockCopy(onek, 0, tumu, 0, onek.Length);
            Buffer.BlockCopy(govde, 0, tumu, onek.Length, govde.Length);
            File.WriteAllBytes(yol, tumu);
        }
    }

    private IniLine? AnahtarSatiriBul(string bolum, string anahtar)
        => _satirlar.FirstOrDefault(s =>
            s.Tur == IniSatirTuru.AnahtarDeger &&
            string.Equals(s.Bolum, bolum, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.Anahtar, anahtar, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Bölümün son anlamlı satırından hemen sonraki ekleme indeksi. Bölümün sonundaki boş
    /// satırlar dışarıda bırakılır ki yeni anahtar boşluktan önce eklensin.
    /// </summary>
    private int BolumSonuIndeksi(string bolum)
    {
        var sonIndeks = -1;
        for (var i = 0; i < _satirlar.Count; i++)
        {
            if (string.Equals(_satirlar[i].Bolum, bolum, StringComparison.OrdinalIgnoreCase))
                sonIndeks = i;
        }

        if (sonIndeks < 0) return -1;

        while (sonIndeks >= 0 && _satirlar[sonIndeks].Tur == IniSatirTuru.Bos)
            sonIndeks--;

        return sonIndeks + 1;
    }

    // --- Kodlama tespiti -------------------------------------------------------------------

    public static Encoding KodlamaTespitEt(byte[] baytlar)
    {
        KodSayfalariniKaydet();

        if (baytlar.Length >= 3 && baytlar[0] == 0xEF && baytlar[1] == 0xBB && baytlar[2] == 0xBF)
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

        if (baytlar.Length >= 2 && baytlar[0] == 0xFF && baytlar[1] == 0xFE)
            return new UnicodeEncoding(bigEndian: false, byteOrderMark: true);

        if (baytlar.Length >= 2 && baytlar[0] == 0xFE && baytlar[1] == 0xFF)
            return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);

        // BOM yok. Geçerli UTF-8 ise UTF-8 kabul et; değilse Türkçe ANSI (Windows-1254).
        try
        {
            new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(baytlar);
            return BomsuzUtf8;
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding(1254);
        }
    }

    private static string MetneCevir(byte[] baytlar, Encoding kodlama)
    {
        var onek = kodlama.GetPreamble();
        if (onek.Length > 0 && baytlar.Length >= onek.Length &&
            baytlar.Take(onek.Length).SequenceEqual(onek))
        {
            return kodlama.GetString(baytlar, onek.Length, baytlar.Length - onek.Length);
        }
        return kodlama.GetString(baytlar);
    }
}
