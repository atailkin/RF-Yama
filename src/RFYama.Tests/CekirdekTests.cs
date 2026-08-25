using RFYama.Core;

namespace RFYama.Tests;

public class SteamKimlikTests
{
    [Theory]
    [InlineData("76561197960265729", true)]
    [InlineData("76561198000000000", true)]
    [InlineData("76561197960265728", true)]
    [InlineData("76561197960265727", false)]
    [InlineData("123", false)]
    [InlineData("", false)]
    [InlineData("abc", false)]
    [InlineData("-76561197960265729", false)]
    public void SteamId64_DogruSekildeDogrulanir(string girdi, bool beklenen)
        => Assert.Equal(beklenen, SteamKimlik.GecerliMi(girdi));

    [Fact]
    public void UretilenKimlikler_GecerliVeFarkli()
    {
        var kimlikler = Enumerable.Range(0, 50).Select(_ => SteamKimlik.Uret()).ToList();

        Assert.All(kimlikler, k => Assert.True(SteamKimlik.GecerliMi(k)));
        Assert.True(kimlikler.Distinct().Count() > 40, "uretilen kimlikler yeterince farkli degil");
    }

    [Theory]
    [InlineData("https://store.steampowered.com/app/440/Team_Fortress_2/", "440")]
    [InlineData("http://store.steampowered.com/app/1010270", "1010270")]
    [InlineData("store.steampowered.com/app/570/Dota_2/", "570")]
    [InlineData("steam://rungameid/440", "440")]
    [InlineData("480", "480")]
    public void MagazaBaglantisindan_AppIdCikarilir(string girdi, string beklenen)
        => Assert.Equal(beklenen, SteamKimlik.BaglantidanAppIdCikar(girdi));

    [Theory]
    [InlineData("")]
    [InlineData("herhangi bir metin")]
    [InlineData("https://example.com/app/440")]
    public void GecersizGirdide_AppIdNullDoner(string girdi)
        => Assert.Null(SteamKimlik.BaglantidanAppIdCikar(girdi));
}

public class PeArchitectureTests
{
    private static string SistemYolu(string altKlasor, string ad)
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), altKlasor, ad);

    [Fact]
    public void System32Exe_64BitOkunur()
    {
        var yol = SistemYolu("System32", "notepad.exe");
        if (!File.Exists(yol)) return; // sistemde yoksa sinanacak bir sey yok
        Assert.Equal(PeMimari.X64, PeArchitecture.Oku(yol));
    }

    [Fact]
    public void SysWow64Exe_32BitOkunur()
    {
        var yol = SistemYolu("SysWOW64", "notepad.exe");
        if (!File.Exists(yol)) return; // 32-bit Windows uzerinde SysWOW64 bulunmaz
        Assert.Equal(PeMimari.X86, PeArchitecture.Oku(yol));
    }

    [Fact]
    public void DllBayragi_DogruOkunur()
    {
        var dll = SistemYolu("System32", "kernel32.dll");
        var exe = SistemYolu("System32", "notepad.exe");
        if (!File.Exists(dll) || !File.Exists(exe)) return;

        Assert.True(PeArchitecture.Detay(dll).DllMi);
        Assert.False(PeArchitecture.Detay(exe).DllMi);
    }

    [Fact]
    public void PeOlmayanDosya_BilinmiyorDoner()
    {
        using var klasor = new GeciciKlasor();
        var yol = klasor.Dosya("duz.txt", "bu bir PE dosyasi degil");
        Assert.Equal(PeMimari.Bilinmiyor, PeArchitecture.Oku(yol));
    }

    [Fact]
    public void OlmayanDosya_BilinmiyorDoner()
        => Assert.Equal(PeMimari.Bilinmiyor, PeArchitecture.Oku(@"C:\olmayan\dosya.exe"));
}

public class TargetValidatorTests
{
    private static string Windows(string? alt = null)
    {
        var kok = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return alt is null ? kok : Path.Combine(kok, alt);
    }

    [Fact]
    public void System32_KesinlikleReddedilir()
    {
        Assert.True(TargetValidator.YasakliKlasorMu(Windows("System32"), out var neden));
        Assert.Contains("Windows", neden);
    }

    [Fact]
    public void SysWow64_KesinlikleReddedilir()
        => Assert.True(TargetValidator.YasakliKlasorMu(Windows("SysWOW64"), out _));

    [Fact]
    public void WindowsKlasoru_Reddedilir()
        => Assert.True(TargetValidator.YasakliKlasorMu(Windows(), out _));

    [Fact]
    public void SurucuKoku_Reddedilir()
    {
        Assert.True(TargetValidator.YasakliKlasorMu(@"C:\", out var neden));
        Assert.Contains("k", neden);
    }

    [Fact]
    public void ProgramFilesKoku_Reddedilir_AltKlasoruKabulEdilir()
    {
        var kok = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        Assert.True(TargetValidator.YasakliKlasorMu(kok, out _));
        Assert.False(TargetValidator.YasakliKlasorMu(Path.Combine(kok, "BirOyun"), out _));
    }

    [Fact]
    public void SiradanOyunKlasoru_KabulEdilir()
    {
        using var klasor = new GeciciKlasor();
        Assert.False(TargetValidator.YasakliKlasorMu(klasor.Yol, out _));
    }

    [Fact]
    public void YasakliKlasor_DogrulamadaHataUretir()
    {
        var sonuc = TargetValidator.Dogrula(Windows("System32"), new[] { "winmm.dll" });

        Assert.False(sonuc.Gecerli);
        Assert.Contains(sonuc.Bulgular, b => b.Seviye == BulguSeviyesi.Hata);
    }

    [Fact]
    public void ExeIcermeyenKlasor_UyariUretir_AmaEngellemez()
    {
        using var klasor = new GeciciKlasor();
        var sonuc = TargetValidator.Dogrula(klasor.Yol, Array.Empty<string>());

        Assert.True(sonuc.Gecerli);
        Assert.Contains(sonuc.Bulgular, b => b.Seviye == BulguSeviyesi.Uyari);
    }

    [Fact]
    public void KilitliDosya_HataOlarakBildirilir()
    {
        using var klasor = new GeciciKlasor();
        var hedef = klasor.Dosya("winmm.dll", "sahte");

        using var kilit = new FileStream(hedef, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        var sonuc = TargetValidator.Dogrula(klasor.Yol, new[] { "winmm.dll" });

        Assert.False(sonuc.Gecerli);
        Assert.Contains(sonuc.Bulgular,
            b => b.Seviye == BulguSeviyesi.Hata && b.Mesaj.Contains("winmm.dll"));
    }

    [Fact]
    public void OyunExeleri_KurulumDosyalariniEler()
    {
        using var klasor = new GeciciKlasor();
        klasor.Dosya("unins000.exe", new string('a', 100));
        klasor.Dosya("vcredist_x64.exe", new string('a', 100));
        klasor.Dosya("UnityCrashHandler64.exe", new string('a', 100));
        klasor.Dosya("Oyun.exe", new string('a', 5000));

        Assert.Equal(new[] { "Oyun.exe" }, TargetValidator.OyunExeleri(klasor.Yol));
    }

    [Fact]
    public void OyunExeleri_EnBuyugunuOneAlir()
    {
        using var klasor = new GeciciKlasor();
        klasor.Dosya("kucuk.exe", new string('a', 100));
        klasor.Dosya("buyuk.exe", new string('a', 90000));

        Assert.Equal("buyuk.exe", TargetValidator.OyunExeleri(klasor.Yol)[0]);
    }
}
