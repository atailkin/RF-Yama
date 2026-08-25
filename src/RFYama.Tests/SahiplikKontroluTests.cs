using RFYama.Core;

namespace RFYama.Tests;

public class SahiplikKontroluTests
{
    /// <summary>Gerçek bir appmanifest_*.acf dosyasının biçimi (VDF).</summary>
    private const string Manifest =
        "\"AppState\"\n" +
        "{\n" +
        "\t\"appid\"\t\t\"1010270\"\n" +
        "\t\"name\"\t\t\"Ornek Oyun\"\n" +
        "\t\"installdir\"\t\t\"OrnekOyun\"\n" +
        "\t\"StateFlags\"\t\t\"4\"\n" +
        "}\n";

    private static (GeciciKlasor Kok, string OyunKlasoru) SteamKutuphanesiKur(
        string installdir = "OrnekOyun", string? oyunAltKlasoru = null)
    {
        var kok = new GeciciKlasor();
        var steamapps = kok.AltKlasor("steamapps");

        var parcalar = new List<string> { "steamapps", "common", installdir };
        if (oyunAltKlasoru is not null) parcalar.AddRange(oyunAltKlasoru.Split('/'));
        var oyunKlasoru = kok.AltKlasor(parcalar.ToArray());

        File.WriteAllText(Path.Combine(steamapps, "appmanifest_1010270.acf"), Manifest);
        return (kok, oyunKlasoru);
    }

    [Fact]
    public void AppManifestVarsa_KurulumTespitEdilir()
    {
        var (kok, oyunKlasoru) = SteamKutuphanesiKur();
        using var _ = kok;

        var bilgi = SahiplikKontrolu.Bul(oyunKlasoru);

        Assert.NotNull(bilgi);
        Assert.Equal("1010270", bilgi!.AppId);
        Assert.Equal("Ornek Oyun", bilgi.Ad);
    }

    [Fact]
    public void OyunAltKlasordeyse_De_KurulumTespitEdilir()
    {
        var (kok, binKlasoru) = SteamKutuphanesiKur(oyunAltKlasoru: "bin/x64");
        using var _ = kok;

        var bilgi = SahiplikKontrolu.Bul(binKlasoru);

        Assert.NotNull(bilgi);
        Assert.Equal("1010270", bilgi!.AppId);
    }

    [Fact]
    public void AppId_YerelOlarakTespitEdilir_AgErisimiOlmadan()
    {
        var (kok, oyunKlasoru) = SteamKutuphanesiKur();
        using var _ = kok;

        Assert.Equal("1010270", SahiplikKontrolu.AppIdTespitEt(oyunKlasoru));
    }

    [Fact]
    public void BelirliAppId_ManifestiBulunur()
    {
        var (kok, oyunKlasoru) = SteamKutuphanesiKur();
        using var _ = kok;

        Assert.NotNull(SahiplikKontrolu.BulAppId(oyunKlasoru, "1010270"));
        Assert.Null(SahiplikKontrolu.BulAppId(oyunKlasoru, "999999"));
        Assert.Null(SahiplikKontrolu.BulAppId(oyunKlasoru, "0"));
    }

    [Fact]
    public void SteamKutuphanesiDisi_UyariUretir_AmaEngellemez()
    {
        using var klasor = new GeciciKlasor();

        var bulgu = SahiplikKontrolu.Degerlendir(klasor.Yol);

        Assert.Equal(BulguSeviyesi.Uyari, bulgu.Seviye);
    }

    [Fact]
    public void KurulumBulununca_BilgiSeviyesindeBulguDoner()
    {
        var (kok, oyunKlasoru) = SteamKutuphanesiKur();
        using var _ = kok;

        var bulgu = SahiplikKontrolu.Degerlendir(oyunKlasoru);

        Assert.Equal(BulguSeviyesi.Bilgi, bulgu.Seviye);
        Assert.Contains("Ornek Oyun", bulgu.Mesaj);
    }

    [Fact]
    public void EslesmeyenInstalldir_KurulumSaymaz()
    {
        var (kok, _) = SteamKutuphanesiKur();
        using var kapsam = kok;

        var baskaOyun = kok.AltKlasor("steamapps", "common", "BaskaOyun");

        Assert.Null(SahiplikKontrolu.Bul(baskaOyun));
    }

    /// <summary>
    /// Bu test, sahiplik kontrolünün kimlik dosyalarına dokunmadığını belgeler. Bu dosyalar
    /// oturum belirteci içerir; okunmaları antivirüs yanlış pozitifinin başlıca nedenidir.
    /// </summary>
    [Fact]
    public void KimlikDosyalari_OkunmazVeGerekliDegildir()
    {
        var (kok, oyunKlasoru) = SteamKutuphanesiKur();
        using var _ = kok;

        // loginusers.vdf / config.vdf hiç oluşturulmadı; tespit yine de çalışmalı.
        Assert.False(File.Exists(Path.Combine(kok.Yol, "config", "loginusers.vdf")));
        Assert.Equal("1010270", SahiplikKontrolu.AppIdTespitEt(oyunKlasoru));
    }
}
