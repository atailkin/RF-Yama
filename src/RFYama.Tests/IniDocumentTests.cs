using System.Text;
using RFYama.Core;

namespace RFYama.Tests;

public class IniDocumentTests
{
    /// <summary>
    /// Elle yazılmış, yorumlu bir dosya. game_launch_options satırındaki ';' bilinçli olarak
    /// vardır: Win32 GetPrivateProfileString satır içi ';' karakterini yorum saymaz, biz de
    /// saymamalıyız.
    /// </summary>
    private static readonly string[] KaynakSatirlar =
    {
        "; unsteam yapılandırması",
        "# ikinci yorum biçimi",
        "",
        "[loader]",
        "exe_file = oyun.exe",
        "dll_file=unsteam.dll",
        "",
        "[game]",
        "offline_mode=0",
        "steam_id=0",
        "player_name=Ahmet Çağrı Şıkoğlu",
        "real_app_id=1010270",
        "fake_app_id=480",
        "language=turkish",
        "game_launch_options=-windowed ;bu bir yorum değil",
        ""
    };

    private static string Kaynak() => string.Join("\r\n", KaynakSatirlar);

    [Fact]
    public void TekDegerDegisince_DigerSatirlarBaytBaytAyniKalir()
    {
        var belge = IniDocument.Ayristir(Kaynak());
        belge.Yaz("game", "offline_mode", "1");

        var sonuc = belge.Metin().Split("\r\n");

        Assert.Equal(KaynakSatirlar.Length, sonuc.Length);
        for (var i = 0; i < KaynakSatirlar.Length; i++)
        {
            if (KaynakSatirlar[i].StartsWith("offline_mode"))
            {
                Assert.Equal("offline_mode=1", sonuc[i]);
                continue;
            }
            Assert.Equal(KaynakSatirlar[i], sonuc[i]);
        }
    }

    [Fact]
    public void EsittirCevresindekiBosluklar_Korunur()
    {
        var belge = IniDocument.Ayristir("[loader]\r\nexe_file = oyun.exe\r\n");
        belge.Yaz("loader", "exe_file", "baska.exe");

        Assert.Contains("exe_file = baska.exe", belge.Metin());
    }

    [Fact]
    public void SatirIciNoktaliVirgul_YorumSayilmaz_DegerinParcasidir()
    {
        var belge = IniDocument.Ayristir(Kaynak());
        Assert.Equal("-windowed ;bu bir yorum değil", belge.Oku("game", "game_launch_options"));
    }

    [Fact]
    public void YorumSatirlari_AnahtarOlarakAyrismaz()
    {
        var belge = IniDocument.Ayristir(Kaynak());
        var anahtarlar = belge.TumAnahtarlar().Select(a => a.Anahtar).ToList();

        Assert.DoesNotContain(anahtarlar, a => a.StartsWith(";") || a.StartsWith("#"));
        Assert.Contains("player_name", anahtarlar);
    }

    [Fact]
    public void YeniAnahtar_DosyaSonuna_Degil_BolumSonunaEklenir()
    {
        var belge = IniDocument.Ayristir(Kaynak());
        belge.Yaz("loader", "yeni_ayar", "değer");

        var satirlar = belge.Metin().Split("\r\n").ToList();
        var yeniIndeks = satirlar.FindIndex(s => s.StartsWith("yeni_ayar"));
        var gameIndeks = satirlar.FindIndex(s => s == "[game]");

        Assert.True(yeniIndeks > 0, "yeni anahtar bulunamadı");
        Assert.True(yeniIndeks < gameIndeks, "yeni anahtar [loader] bölümünün dışına eklenmiş");
    }

    [Fact]
    public void OlmayanBolum_BasligiylaBirlikteOlusturulur()
    {
        var belge = IniDocument.Ayristir(Kaynak());
        belge.Yaz("network", "port", "27015");

        var metin = belge.Metin();
        Assert.Contains("[network]", metin);
        Assert.Contains("port=27015", metin);
        Assert.Equal("27015", belge.Oku("network", "port"));
    }

    [Fact]
    public void TaninmayanAnahtarlar_YukleKaydetDongusunuAtlatir()
    {
        var kaynak = Kaynak() + "gelecekteki_ayar=42\r\n";
        var belge = IniDocument.Ayristir(kaynak);

        // Bilinen bir alanı değiştir, sonra tanınmayan anahtarın hâlâ orada olduğunu doğrula.
        belge.Yaz("game", "language", "english");

        Assert.Equal("42", belge.Oku("game", "gelecekteki_ayar"));
        Assert.Contains("gelecekteki_ayar=42", belge.Metin());
    }

    [Fact]
    public void KaydedilenUtf8Dosya_BomIcermez()
    {
        using var klasor = new GeciciKlasor();
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");

        var belge = IniDocument.Ayristir(Kaynak());
        belge.Kaydet(yol);

        var baytlar = File.ReadAllBytes(yol);
        Assert.False(baytlar.Length >= 3 && baytlar[0] == 0xEF && baytlar[1] == 0xBB && baytlar[2] == 0xBF,
            "BOM yazıldı — GetPrivateProfileString ilk bölüm başlığını okuyamaz");
    }

    [Fact]
    public void TurkceKarakterler_BomsuzUtf8Turunda_Bozulmaz()
    {
        using var klasor = new GeciciKlasor();
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");
        const string ad = "Şükrü İğdır Çağla Öz";

        var belge = IniDocument.Ayristir(Kaynak());
        belge.Yaz("game", "player_name", ad);
        belge.Kaydet(yol);

        var geri = IniDocument.Yukle(yol);
        Assert.Equal(ad, geri.Oku("game", "player_name"));
    }

    [Fact]
    public void TurkceKarakterler_Windows1254Turunda_Bozulmaz()
    {
        IniDocument.KodSayfalariniKaydet();

        using var klasor = new GeciciKlasor();
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");
        const string ad = "Şükrü İğdır Çağla Öz";

        var belge = IniDocument.Ayristir(Kaynak());
        belge.Kodlama = Encoding.GetEncoding(1254);
        belge.Yaz("game", "player_name", ad);
        belge.Kaydet(yol);

        var geri = IniDocument.Yukle(yol);
        Assert.Equal(1254, geri.Kodlama.CodePage);
        Assert.Equal(ad, geri.Oku("game", "player_name"));
    }

    [Fact]
    public void MevcutKodlama_KaydetmedeKorunur()
    {
        IniDocument.KodSayfalariniKaydet();

        using var klasor = new GeciciKlasor();
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");

        // Windows-1254 olarak yaz
        File.WriteAllBytes(yol, Encoding.GetEncoding(1254).GetBytes("[game]\r\nplayer_name=Çağrı\r\n"));

        var belge = IniDocument.Yukle(yol);
        Assert.Equal(1254, belge.Kodlama.CodePage);

        belge.Yaz("game", "language", "turkish");
        belge.Kaydet(yol);

        // Hâlâ 1254 olmalı; sessizce UTF-8'e çevrilmemeli.
        var geri = IniDocument.Yukle(yol);
        Assert.Equal(1254, geri.Kodlama.CodePage);
        Assert.Equal("Çağrı", geri.Oku("game", "player_name"));
    }

    [Fact]
    public void BomluDosya_TespitEdilirVeOkunur()
    {
        using var klasor = new GeciciKlasor();
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");

        File.WriteAllText(yol, "[game]\r\nlanguage=turkish\r\n", new UTF8Encoding(true));

        var belge = IniDocument.Yukle(yol);
        Assert.Equal("turkish", belge.Oku("game", "language"));
        Assert.NotEmpty(belge.Kodlama.GetPreamble());
    }

    [Fact]
    public void SatirSonuBicimi_Korunur()
    {
        var belge = IniDocument.Ayristir("[game]\nlanguage=turkish\n");
        belge.Yaz("game", "language", "english");

        Assert.DoesNotContain("\r\n", belge.Metin());
    }

    [Fact]
    public void DegerIcindekiSatirSonu_Temizlenir()
    {
        var belge = IniDocument.Ayristir("[game]\r\nplayer_name=eski\r\n");
        belge.Yaz("game", "player_name", "satir1\r\nsatir2");

        var satirlar = belge.Metin().Split("\r\n");
        Assert.Contains("player_name=satir1satir2", satirlar);
    }
}
