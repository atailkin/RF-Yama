using RFYama.Core;

namespace RFYama.Tests;

/// <summary>
/// Uçtan uca kurulum/kaldırma. Gerçek yama dosyaları henüz yazılmadığı için yer tutucu
/// içeriklerle çalışılır — sınanan şey dosya taşıma ve kayıt mantığı.
///
/// YEDEK YOK: üzerine yazılan dosya geri getirilemez. Testler bunun bilinçli davranış
/// olduğunu ve buna karşılık kaldırmanın kurmadığımız dosyaları silmediğini doğrular.
/// </summary>
[Collection("KurulumDeposu")]
public class KurulumTests : IDisposable
{
    private const string OzgunWinmmIcerigi = "BASKA BIR MODUN WINMM DLL DOSYASI";
    private readonly GeciciKlasor _depoKlasoru;

    public KurulumTests()
    {
        // Depoyu geçici bir klasöre yönlendir: testler kullanıcının gerçek
        // %APPDATA%\RFYama\kurulumlar.json dosyasına asla dokunmasın.
        _depoKlasoru = new GeciciKlasor("depo-");
        KurulumDeposu.KlasorGecersizKil = _depoKlasoru.Yol;
    }

    public void Dispose()
    {
        KurulumDeposu.KlasorGecersizKil = null;
        _depoKlasoru.Dispose();
    }

    private static GeciciKlasor PayloadKur(out string payloadYolu)
    {
        var klasor = new GeciciKlasor("payload-");
        klasor.Dosya("unsteam.dll", "sahte unsteam icerigi");
        klasor.Dosya("winmm.dll", "sahte winmm icerigi");
        klasor.Dosya("unsteam.ini", "[game]\r\nlanguage=english\r\n");
        klasor.Dosya("OKUBENI.txt", "bu kurulum listesinde gorunmemeli");
        payloadYolu = klasor.Yol;
        return klasor;
    }

    [Fact]
    public void PayloadListesi_YalnizcaDllVeIniIcerir()
    {
        using var payload = PayloadKur(out var payloadYolu);

        var dosyalar = PayloadInstaller.PayloadDosyalari(payloadYolu);

        Assert.Equal(new[] { "unsteam.dll", "unsteam.ini", "winmm.dll" }, dosyalar);
        Assert.DoesNotContain("OKUBENI.txt", dosyalar);
    }

    [Fact]
    public void Kurulum_DosyalariKopyalar_VeKayitYazar()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        var sonuc = PayloadInstaller.Kur(
            hedef.Yol, new[] { "unsteam.dll", "winmm.dll" }, payloadYolu);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.True(File.Exists(Path.Combine(hedef.Yol, "unsteam.dll")));
        Assert.True(File.Exists(Path.Combine(hedef.Yol, "winmm.dll")));
        Assert.True(PayloadInstaller.KuruluMu(hedef.Yol));
        Assert.NotNull(KurulumDeposu.Bul(hedef.Yol));
    }

    [Fact]
    public void Kurulum_HedefKlasorde_YedekKlasoruOlusturmaz()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");
        hedef.Dosya("winmm.dll", OzgunWinmmIcerigi);

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll", "winmm.dll" }, payloadYolu);

        // Oyun klasorune yalnizca yama dosyalari girmeli; hicbir alt klasor olusmamali.
        Assert.Empty(Directory.GetDirectories(hedef.Yol));
        Assert.Equal(
            new[] { "unsteam.dll", "winmm.dll" },
            Directory.GetFiles(hedef.Yol).Select(Path.GetFileName).OrderBy(a => a).ToArray());
    }

    [Fact]
    public void UzerineYazilacaklar_MevcutDosyalariBildirir_IniyiHaricTutar()
    {
        using var hedef = new GeciciKlasor("hedef-");
        hedef.Dosya("winmm.dll", OzgunWinmmIcerigi);
        hedef.Dosya("unsteam.ini", "[game]\r\n");

        var ezilecekler = PayloadInstaller.UzerineYazilacaklar(
            hedef.Yol, new[] { "unsteam.dll", "winmm.dll", "unsteam.ini" });

        // unsteam.dll hedefte yok, unsteam.ini uzerine yazilmayanlar listesinde.
        Assert.Equal(new[] { "winmm.dll" }, ezilecekler);
    }

    [Fact]
    public void MevcutDosya_GeriDonussuz_UzerineYazilir()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");
        hedef.Dosya("winmm.dll", OzgunWinmmIcerigi);

        var sonuc = PayloadInstaller.Kur(hedef.Yol, new[] { "winmm.dll" }, payloadYolu);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.Equal("sahte winmm icerigi", File.ReadAllText(Path.Combine(hedef.Yol, "winmm.dll")));
        Assert.DoesNotContain(OzgunWinmmIcerigi, File.ReadAllText(Path.Combine(hedef.Yol, "winmm.dll")));
    }

    [Fact]
    public void Kaldirma_KurulanDosyalariSiler()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll", "winmm.dll" }, payloadYolu);
        var kaldirma = PayloadInstaller.Kaldir(hedef.Yol);

        Assert.True(kaldirma.Basarili, kaldirma.Mesaj);
        Assert.False(File.Exists(Path.Combine(hedef.Yol, "unsteam.dll")));
        Assert.False(File.Exists(Path.Combine(hedef.Yol, "winmm.dll")));
        Assert.False(PayloadInstaller.KuruluMu(hedef.Yol));
        Assert.Null(KurulumDeposu.Bul(hedef.Yol));
    }

    /// <summary>
    /// Yedek olmadigi icin tek koruma budur: hash eslesmiyorsa dosya bizim koydugumuz hâlinde
    /// degildir ve silinmez.
    /// </summary>
    [Fact]
    public void Kaldirma_DegistirilmisDosyayiSilmez()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "winmm.dll" }, payloadYolu);

        // Kullanici veya baska bir arac dosyayi kurulumdan sonra degistirsin.
        var yol = Path.Combine(hedef.Yol, "winmm.dll");
        File.WriteAllText(yol, "BASKA BIR ARAC BUNU DEGISTIRDI");

        var kaldirma = PayloadInstaller.Kaldir(hedef.Yol);

        Assert.True(kaldirma.Basarili, kaldirma.Mesaj);
        Assert.True(File.Exists(yol), "degistirilmis dosya silinmemeliydi");
        Assert.Equal("BASKA BIR ARAC BUNU DEGISTIRDI", File.ReadAllText(yol));
        Assert.Contains("winmm.dll", kaldirma.Mesaj);
    }

    [Fact]
    public void Kaldirma_KayitYokken_HataDoner()
    {
        using var hedef = new GeciciKlasor("hedef-");

        var kaldirma = PayloadInstaller.Kaldir(hedef.Yol);

        Assert.False(kaldirma.Basarili);
    }

    [Fact]
    public void Kaldirma_IkinciKez_Calismaz()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll" }, payloadYolu);

        Assert.True(PayloadInstaller.Kaldir(hedef.Yol).Basarili);
        Assert.False(PayloadInstaller.Kaldir(hedef.Yol).Basarili);
    }

    [Fact]
    public void MevcutIniDosyasi_UzerineYazilmaz()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        const string kullaniciAyarlari = "[game]\r\nplayer_name=Cagri\r\nlanguage=turkish\r\n";
        hedef.Dosya("unsteam.ini", kullaniciAyarlari);

        var sonuc = PayloadInstaller.Kur(
            hedef.Yol, new[] { "unsteam.dll", "unsteam.ini" }, payloadYolu);

        Assert.True(sonuc.Basarili, sonuc.Mesaj);
        Assert.Equal(kullaniciAyarlari, File.ReadAllText(Path.Combine(hedef.Yol, "unsteam.ini")));
    }

    [Fact]
    public void OlmayanIniDosyasi_SablondanKopyalanir()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.ini" }, payloadYolu);

        Assert.True(File.Exists(Path.Combine(hedef.Yol, "unsteam.ini")));
    }

    [Fact]
    public void SaltOkunurHedefDosya_UzerineYazilabilir()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        var yol = hedef.Dosya("winmm.dll", OzgunWinmmIcerigi);
        File.SetAttributes(yol, FileAttributes.ReadOnly);

        try
        {
            var sonuc = PayloadInstaller.Kur(hedef.Yol, new[] { "winmm.dll" }, payloadYolu);
            Assert.True(sonuc.Basarili, sonuc.Mesaj);
        }
        finally
        {
            if (File.Exists(yol)) File.SetAttributes(yol, FileAttributes.Normal);
        }
    }

    [Fact]
    public void PayloadtaOlmayanDosya_KurulumuReddeder()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        var sonuc = PayloadInstaller.Kur(hedef.Yol, new[] { "olmayan.dll" }, payloadYolu);

        Assert.False(sonuc.Basarili);
        Assert.Contains("olmayan.dll", sonuc.Mesaj);
    }

    [Fact]
    public void Kurulum_KayittaHashleriSaklar()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll" }, payloadYolu);
        var kayit = KurulumDeposu.Bul(hedef.Yol);

        Assert.NotNull(kayit);
        var kurulan = Assert.Single(kayit!.Kurulanlar);
        Assert.Equal("unsteam.dll", kurulan.Ad);
        Assert.Equal(
            KurulumKaydi.HashHesapla(Path.Combine(hedef.Yol, "unsteam.dll")),
            kurulan.Sha256);
    }

    [Fact]
    public void Depo_AyniHedefIcin_TekKayitTutar()
    {
        using var payload = PayloadKur(out var payloadYolu);
        using var hedef = new GeciciKlasor("hedef-");

        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll" }, payloadYolu);
        PayloadInstaller.Kur(hedef.Yol, new[] { "unsteam.dll", "winmm.dll" }, payloadYolu);

        var eslesenler = KurulumDeposu.Tumu()
            .Count(k => string.Equals(k.HedefKlasor, hedef.Yol, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(1, eslesenler);
        Assert.Equal(2, KurulumDeposu.Bul(hedef.Yol)!.Kurulanlar.Count);
    }

    [Fact]
    public void VarsayilanBelge_TumAlanlariIcerir_VeDlcsBolumuYok()
    {
        var belge = AlanTanimlari.VarsayilanBelge();

        foreach (var alan in AlanTanimlari.Tumu)
            Assert.True(belge.VarMi(alan.Bolum, alan.Anahtar), $"{alan.Anahtar} eksik");

        Assert.DoesNotContain("dlcs", belge.Bolumler(), StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("[dlcs]", belge.Metin());
    }

    [Fact]
    public void VarsayilanBelge_SahteAppIdVarsayilani480()
    {
        var belge = AlanTanimlari.VarsayilanBelge();
        Assert.Equal("480", belge.Oku(AlanTanimlari.BolumGame, "fake_app_id"));
    }
}
