using System.Globalization;
using RFYama.Core;

namespace RFYama.Tests;

/// <summary>
/// tr-TR kültürünün iki klasik tuzağı:
///   1. "ID".ToLower() sonucu "ıd" (noktasız ı) olur. Kültür duyarlı karşılaştırma "ID"/"id"
///      eşleşmesini kaçırır; bu yüzden anahtar karşılaştırmaları Ordinal olmak zorunda.
///   2. Ondalık ayırıcı virgüldür. Sayı InvariantCulture ile yazılmazsa INI'ye "1,5" yazılır
///      ve yerel okuyucu değeri yanlış yorumlar.
/// </summary>
public class TurkceKulturTests
{
    [Fact]
    public void TurkceKulturde_BuyukIIceren_AnahtarlarCozulur()
    {
        using var _ = new KulturKapsami("tr-TR");

        var belge = IniDocument.Ayristir("[game]\r\nID=5\r\nreal_app_id=440\r\n");

        Assert.Equal("5", belge.Oku("game", "ID"));
        Assert.Equal("5", belge.Oku("game", "id"));
        Assert.Equal("5", belge.Oku("game", "Id"));
        Assert.Equal("440", belge.Oku("GAME", "REAL_APP_ID"));
    }

    [Fact]
    public void TurkceKulturde_BolumAdlari_HarfBuyuklugundenBagimsizCozulur()
    {
        using var _ = new KulturKapsami("tr-TR");

        var belge = IniDocument.Ayristir("[LOADER]\r\nexe_file=oyun.exe\r\n");

        Assert.Equal("oyun.exe", belge.Oku("loader", "exe_file"));
        Assert.Equal("oyun.exe", belge.Oku("Loader", "EXE_FILE"));
    }

    [Fact]
    public void TurkceKulturde_Yazma_MevcutAnahtariBulur_YenisiniEklemez()
    {
        using var _ = new KulturKapsami("tr-TR");

        var belge = IniDocument.Ayristir("[game]\r\nLANGUAGE=english\r\n");
        belge.Yaz("game", "language", "turkish");

        Assert.Single(belge.TumAnahtarlar());
        Assert.Equal("turkish", belge.Oku("game", "LANGUAGE"));
    }

    [Fact]
    public void TurkceKulturde_OndalikSayi_NoktaIleYazilir()
    {
        using var _ = new KulturKapsami("tr-TR");

        // Kültürün gerçekten virgül kullandığını doğrula; aksi hâlde test bir şey kanıtlamaz.
        Assert.Equal("1,5", 1.5.ToString(CultureInfo.CurrentCulture));

        var belge = IniDocument.Bos();
        belge.YazSayi("game", "olcek", 1.5);

        Assert.Equal("1.5", belge.Oku("game", "olcek"));
        Assert.Equal(1.5, belge.OkuSayi("game", "olcek"));
    }

    [Fact]
    public void TurkceKulturde_SteamKimligi_DogruUretilirVeDogrulanir()
    {
        using var _ = new KulturKapsami("tr-TR");

        var uretilen = SteamKimlik.Uret();

        Assert.DoesNotContain(".", uretilen);
        Assert.DoesNotContain(",", uretilen);
        Assert.Equal(17, uretilen.Length);
        Assert.True(SteamKimlik.GecerliMi(uretilen));
    }

    [Fact]
    public void TurkceKulturde_AlanDogrulamalari_Calisir()
    {
        using var _ = new KulturKapsami("tr-TR");

        var exeAlani = AlanTanimlari.Bul(AlanTanimlari.BolumLoader, "exe_file")!;

        // Buyuk harfli ".EXE" tam olarak tr-TR ToLower tuzaginin tetiklendigi yer.
        Assert.Null(exeAlani.Dogrula!("OYUN.EXE"));
        Assert.Null(exeAlani.Dogrula!("oyun.exe"));
        Assert.NotNull(exeAlani.Dogrula!("oyun.txt"));
    }

    [Fact]
    public void TurkceKulturde_TaninmayanAnahtarTespiti_Dogru()
    {
        using var _ = new KulturKapsami("tr-TR");

        Assert.True(AlanTanimlari.Taniniyor("GAME", "LANGUAGE"));
        Assert.True(AlanTanimlari.Taniniyor("game", "language"));
        Assert.False(AlanTanimlari.Taniniyor("game", "bilinmeyen_ayar"));
    }
}
