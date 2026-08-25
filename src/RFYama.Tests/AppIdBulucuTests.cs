using RFYama.Core;

namespace RFYama.Tests;

public class AppIdBulucuTests
{
    [Fact]
    public void SteamAppIdDosyasi_Okunur()
    {
        using var klasor = new GeciciKlasor("oyun-");
        klasor.Dosya("steam_appid.txt", "1010270");

        var sonuc = AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol);

        Assert.NotNull(sonuc);
        Assert.Equal("1010270", sonuc!.AppId);
        Assert.Equal(AppIdKaynagi.SteamAppIdDosyasi, sonuc.Kaynak);
        Assert.True(sonuc.Kesin);
    }

    [Fact]
    public void SteamAppIdDosyasi_SonundaSatirSonuVarsa_YineOkunur()
    {
        using var klasor = new GeciciKlasor("oyun-");
        klasor.Dosya("steam_appid.txt", "480\r\n");

        Assert.Equal("480", AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol)!.AppId);
    }

    [Fact]
    public void SteamAppIdDosyasi_AltKlasordeyse_De_Bulunur()
    {
        using var klasor = new GeciciKlasor("oyun-");
        var bin = klasor.AltKlasor("bin");
        File.WriteAllText(Path.Combine(bin, "steam_appid.txt"), "570");

        Assert.Equal("570", AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol)!.AppId);
    }

    [Fact]
    public void SteamAppIdDosyasi_Yoksa_NullDoner()
    {
        using var klasor = new GeciciKlasor("oyun-");
        Assert.Null(AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol));
    }

    [Fact]
    public void SteamAppIdDosyasi_GecersizIcerik_NullDoner()
    {
        using var klasor = new GeciciKlasor("oyun-");
        klasor.Dosya("steam_appid.txt", "bu bir sayi degil");

        Assert.Null(AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol));
    }

    [Fact]
    public void SifirAppId_Kabul_Edilmez()
    {
        using var klasor = new GeciciKlasor("oyun-");
        klasor.Dosya("steam_appid.txt", "0");

        Assert.Null(AppIdBulucu.SteamAppIdDosyasindanBul(klasor.Yol));
    }

    /// <summary>
    /// Kullanicinin bildirdigi asil senaryo: oyun Steam kutuphanesi disina tasinmis. .acf
    /// bulunamaz ama steam_appid.txt oyunla birlikte tasindigi icin AppID yine cozulur.
    /// </summary>
    [Fact]
    public void TasinmisKurulum_SteamAppIdDosyasiyla_Cozulur()
    {
        using var klasor = new GeciciKlasor("tasinmis-oyun-");
        klasor.Dosya("OrnekOyun.exe", "sahte exe");
        klasor.Dosya("steam_appid.txt", "1010270");

        // steamapps\common kalibi yok; .acf tabanli tespit calismaz.
        Assert.Null(SahiplikKontrolu.Bul(klasor.Yol));

        var sonuc = AppIdBulucu.YerelBul(klasor.Yol);

        Assert.NotNull(sonuc);
        Assert.Equal("1010270", sonuc!.AppId);
    }

    // --- Ad esleştirme ------------------------------------------------------------------

    private static readonly List<SteamUygulama> OrnekListe = new()
    {
        new("1010270", "RF Online"),
        new("2000000", "RF Online Classic"),
        new("440", "Team Fortress 2"),
        new("570", "Dota 2"),
        new("999999", "Islander"),
    };

    [Fact]
    public void AdaGoreArama_TamEslesmeyi_OneAlir()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("RF Online");

        var adaylar = AppIdBulucu.AdaGoreAra(oyun, OrnekListe);

        Assert.NotEmpty(adaylar);
        Assert.Equal("1010270", adaylar[0].AppId);
        Assert.Equal(AppIdKaynagi.AdEslesmesi, adaylar[0].Kaynak);
        Assert.False(adaylar[0].Kesin, "ad eslesmesi kesin sayilmamali");
    }

    [Fact]
    public void AdaGoreArama_NoktalamaVeBosluktan_Etkilenmez()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("RF-Online");

        Assert.Equal("1010270", AppIdBulucu.AdaGoreAra(oyun, OrnekListe)[0].AppId);
    }

    [Fact]
    public void AdaGoreArama_AlakasizKlasorde_SonucVermez()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("zzqqxx");

        Assert.Empty(AppIdBulucu.AdaGoreAra(oyun, OrnekListe));
    }

    /// <summary>
    /// tr-TR kulturunde ToLower() "I" harfini "ı"ya cevirir. Karsilastirmanin iki tarafi
    /// farkli cevrilirse "RF Online" gibi icinde I gecen adlar sessizce eslesmez.
    /// </summary>
    [Fact]
    public void TurkceKulturde_IHarfliAdlar_Yine_Eslesir()
    {
        using var _ = new KulturKapsami("tr-TR");
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("RF ONLINE");

        var adaylar = AppIdBulucu.AdaGoreAra(oyun, OrnekListe);

        Assert.NotEmpty(adaylar);
        Assert.Equal("1010270", adaylar[0].AppId);
    }

    [Fact]
    public void TurkceKulturde_Normalize_InvariantDavranir()
    {
        using var _ = new KulturKapsami("tr-TR");

        // Kulturun gercekten tuzakli oldugunu dogrula.
        Assert.Equal("ı", "I".ToLower(System.Globalization.CultureInfo.CurrentCulture));

        // Normalize InvariantCulture kullandigi icin "I" -> "i" olmali.
        Assert.Equal("islander", AppIdBulucu.Normalize("Islander"));
        Assert.Equal("rfonline", AppIdBulucu.Normalize("RF Online"));
        Assert.Equal("rfonline", AppIdBulucu.Normalize("rf-online!"));
    }

    [Fact]
    public void Normalize_BosGirdide_BosDoner()
    {
        Assert.Equal(string.Empty, AppIdBulucu.Normalize(""));
        Assert.Equal(string.Empty, AppIdBulucu.Normalize("   "));
        Assert.Equal(string.Empty, AppIdBulucu.Normalize("!!!"));
    }

    [Fact]
    public void YerelBul_SteamAppIdDosyasini_KurulumKaydindanOnceTercihEder()
    {
        using var kok = new GeciciKlasor();
        var steamapps = kok.AltKlasor("steamapps");
        var oyun = kok.AltKlasor("steamapps", "common", "OrnekOyun");

        File.WriteAllText(Path.Combine(steamapps, "appmanifest_555.acf"),
            "\"AppState\"\n{\n\t\"appid\"\t\t\"555\"\n\t\"installdir\"\t\t\"OrnekOyun\"\n}\n");

        // steam_appid.txt farkli bir deger soyluyor; kesin kaynak o olmali.
        File.WriteAllText(Path.Combine(oyun, "steam_appid.txt"), "1010270");

        var sonuc = AppIdBulucu.YerelBul(oyun);

        Assert.Equal("1010270", sonuc!.AppId);
        Assert.Equal(AppIdKaynagi.SteamAppIdDosyasi, sonuc.Kaynak);
    }

    // --- Genel klasor adlari ve derinlik agirligi ----------------------------------------

    [Fact]
    public void GenelUstKlasorAdlari_AramaTerimiOlmaz()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("Games", "OrnekOyun");

        var terimler = AppIdBulucu.AramaTerimleri(oyun);

        Assert.Contains("OrnekOyun", terimler);
        Assert.DoesNotContain(terimler, t => string.Equals(t, "Games", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(terimler, t => string.Equals(t, "Temp", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gercek hata: test klasoru %TEMP% altinda oldugu icin "Temp" de arama terimi olmustu ve
    /// magazada gercekten "temp" adli bir oyun (3670560) tam eslesme puani alip Team Fortress
    /// 2'yi geciyordu. Hem genel-ad listesi hem derinlik agirligi bunu engellemeli.
    /// </summary>
    [Fact]
    public void UstKlasorleEslesenAlakasizOyun_HedefKlasorAdiniGecemez()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("Team Fortress 2");

        var liste = new List<SteamUygulama>
        {
            new("3670560", "temp"),           // ust klasor adiyla tam eslesir, kisa ad
            new("440", "Team Fortress 2"),    // hedef klasor adiyla tam eslesir
        };

        var adaylar = AppIdBulucu.AdaGoreAra(oyun, liste);

        Assert.NotEmpty(adaylar);
        Assert.Equal("440", adaylar[0].AppId);
    }

    [Fact]
    public void HedefKlasorAdi_UstKlasorAdindan_DahaAgirBasar()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("Portal 2", "Half-Life 2");

        var liste = new List<SteamUygulama>
        {
            new("400", "Portal 2"),      // ust klasor -- tam eslesme ama zayif agirlik
            new("220", "Half-Life 2"),   // hedef klasor -- tam eslesme, tam agirlik
        };

        Assert.Equal("220", AppIdBulucu.AdaGoreAra(oyun, liste)[0].AppId);
    }

    [Fact]
    public void AltCizgiliKlasorAdi_AramaTeriminde_BoslugaCevrilir()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("Team_Fortress_2");

        Assert.Contains("Team Fortress 2", AppIdBulucu.AramaTerimleri(oyun));
    }
}
