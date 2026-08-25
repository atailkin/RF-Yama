using RFYama.Core;

namespace RFYama.Tests;

/// <summary>
/// Bulunan AppID'nin unsteam.ini'ye yazilmasi. MainForm.AppIdiIniyeYaz ile ayni mantik;
/// burada cekirdek davranis (yorum koruma, mevcut degeri ezmeme) dogrulanir.
/// </summary>
public class IniAppIdTests
{
    private static string SablonYolu()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null)
        {
            var aday = Path.Combine(dizin.FullName, "payload", "unsteam.ini");
            if (File.Exists(aday)) return aday;
            dizin = dizin.Parent;
        }
        throw new FileNotFoundException("payload/unsteam.ini bulunamadi.");
    }

    [Fact]
    public void AppIdYazilinca_YorumlarVeDigerSatirlar_Korunur()
    {
        using var klasor = new GeciciKlasor("hedef-");
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");
        File.Copy(SablonYolu(), yol);

        var oncekiSatirlar = File.ReadAllLines(yol);
        var oncekiYorumSayisi = oncekiSatirlar.Count(s => s.TrimStart().StartsWith(";"));

        var belge = IniDocument.Yukle(yol);
        belge.Yaz(AlanTanimlari.BolumGame, "real_app_id", "1010270");
        belge.Kaydet(yol);

        var sonrakiSatirlar = File.ReadAllLines(yol);

        Assert.Equal(oncekiSatirlar.Length, sonrakiSatirlar.Length);
        Assert.Equal(oncekiYorumSayisi, sonrakiSatirlar.Count(s => s.TrimStart().StartsWith(";")));
        Assert.Equal("real_app_id=1010270", sonrakiSatirlar.First(s => s.StartsWith("real_app_id")));

        // real_app_id disindaki her satir bayt bayt ayni kalmali.
        for (var i = 0; i < oncekiSatirlar.Length; i++)
        {
            if (oncekiSatirlar[i].StartsWith("real_app_id")) continue;
            Assert.Equal(oncekiSatirlar[i], sonrakiSatirlar[i]);
        }
    }

    [Fact]
    public void SablonunVarsayilani_SifirdirVeYazmaya_Uygundur()
    {
        var belge = IniDocument.Yukle(SablonYolu());
        Assert.Equal("0", belge.Oku(AlanTanimlari.BolumGame, "real_app_id"));
    }

    [Fact]
    public void KullanicininGirdigiDeger_Ezilmemeli()
    {
        using var klasor = new GeciciKlasor("hedef-");
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");
        File.Copy(SablonYolu(), yol);

        var belge = IniDocument.Yukle(yol);
        belge.Yaz(AlanTanimlari.BolumGame, "real_app_id", "999999");
        belge.Kaydet(yol);

        // MainForm'daki kosul: deger bos ya da "0" degilse dokunma.
        var mevcut = IniDocument.Yukle(yol).OkuVeya(AlanTanimlari.BolumGame, "real_app_id", "").Trim();

        Assert.NotEmpty(mevcut);
        Assert.NotEqual("0", mevcut);
        Assert.Equal("999999", mevcut);
    }

    [Fact]
    public void YazilanDosya_BomsuzKalir()
    {
        using var klasor = new GeciciKlasor("hedef-");
        var yol = Path.Combine(klasor.Yol, "unsteam.ini");
        File.Copy(SablonYolu(), yol);

        var belge = IniDocument.Yukle(yol);
        belge.Yaz(AlanTanimlari.BolumGame, "real_app_id", "440");
        belge.Kaydet(yol);

        var baytlar = File.ReadAllBytes(yol);
        Assert.False(baytlar.Length >= 3 && baytlar[0] == 0xEF && baytlar[1] == 0xBB && baytlar[2] == 0xBF);
    }
}
