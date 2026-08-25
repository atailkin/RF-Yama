using RFYama.Core;

namespace RFYama.Tests;

/// <summary>
/// payload\unsteam.ini şablonunun gerçek içeriğine karşı çalışan testler. Şablon, arayüzün
/// ürettiği alan listesiyle uyumsuz kalırsa burada yakalanır.
/// </summary>
public class SablonTests
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
        throw new FileNotFoundException("payload/unsteam.ini bulunamadı.");
    }

    [Fact]
    public void Sablon_BomsuzUtf8()
    {
        var baytlar = File.ReadAllBytes(SablonYolu());
        Assert.False(baytlar.Length >= 3 && baytlar[0] == 0xEF && baytlar[1] == 0xBB && baytlar[2] == 0xBF,
            "şablon BOM ile kaydedilmiş — GetPrivateProfileString ilk bölümü okuyamaz");
    }

    [Fact]
    public void Sablon_ArayuzunTumAlanlariniIcerir()
    {
        var belge = IniDocument.Yukle(SablonYolu());

        foreach (var alan in AlanTanimlari.Tumu)
            Assert.True(belge.VarMi(alan.Bolum, alan.Anahtar),
                $"şablonda eksik anahtar: [{alan.Bolum}] {alan.Anahtar}");
    }

    [Fact]
    public void Sablon_TaninmayanAnahtarIcermez()
    {
        var belge = IniDocument.Yukle(SablonYolu());

        var taninmayanlar = belge.TumAnahtarlar()
            .Where(a => !AlanTanimlari.Taniniyor(a.Bolum, a.Anahtar))
            .Select(a => $"[{a.Bolum}] {a.Anahtar}")
            .ToList();

        Assert.True(taninmayanlar.Count == 0,
            "şablonda arayüzde karşılığı olmayan anahtar var: " + string.Join(", ", taninmayanlar));
    }

    [Fact]
    public void Sablon_DlcsBolumuIcermez()
    {
        var belge = IniDocument.Yukle(SablonYolu());
        Assert.DoesNotContain("dlcs", belge.Bolumler(), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sablon_DuzenlendiginedeYorumlariKorur()
    {
        var yol = SablonYolu();
        var ozgunSatirlar = File.ReadAllLines(yol);
        var yorumSayisi = ozgunSatirlar.Count(s => s.TrimStart().StartsWith(";"));

        Assert.True(yorumSayisi > 5, "şablonda beklenen yorum satırları yok");

        var belge = IniDocument.Yukle(yol);
        belge.Yaz(AlanTanimlari.BolumGame, "player_name", "Çağrı");
        belge.Yaz(AlanTanimlari.BolumGame, "offline_mode", "1");

        var sonucSatirlar = belge.Metin().Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

        Assert.Equal(yorumSayisi, sonucSatirlar.Count(s => s.TrimStart().StartsWith(";")));
        Assert.Equal(ozgunSatirlar.Length, sonucSatirlar.Length);
        Assert.Equal("player_name=Çağrı", sonucSatirlar.First(s => s.StartsWith("player_name")));
        Assert.Equal("offline_mode=1", sonucSatirlar.First(s => s.StartsWith("offline_mode")));
    }

    [Fact]
    public void Sablon_VarsayilanDegerleri_ArayuzleUyumlu()
    {
        var belge = IniDocument.Yukle(SablonYolu());

        Assert.Equal("480", belge.Oku(AlanTanimlari.BolumGame, "fake_app_id"));
        Assert.Equal("unsteam.dll", belge.Oku(AlanTanimlari.BolumLoader, "dll_file"));
        Assert.Equal("public", belge.Oku(AlanTanimlari.BolumGame, "beta_name"));
    }
}
