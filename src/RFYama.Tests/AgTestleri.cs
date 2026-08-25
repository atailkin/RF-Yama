using RFYama.Core;

namespace RFYama.Tests;

/// <summary>
/// Gerçek Steam uç noktalarına çıkan testler. Varsayılan test koşusunda ÇALIŞMAZ:
///
///   dotnet test --filter "Kategori!=Ag"      (çevrimdışı, hızlı)
///   dotnet test --filter "Kategori=Ag"       (uç noktaları doğrula)
///
/// Bu testleri tutmanın nedeni somut: ISteamApps/GetAppList uç noktası sessizce genel
/// API'den kaldırıldı ve yalnızca gerçek istek atınca fark edildi. Aynı şey mağaza arama
/// uç noktasının başına gelirse burada yakalanır.
/// </summary>
[Trait("Kategori", "Ag")]
public class AgTestleri
{
    [Fact]
    public async Task MagazaAramasi_BilinenOyunlariBulur()
    {
        var sonuc = await SteamMagazaAramasi.Ara(new[] { "Team Fortress 2", "Dota 2" });

        Assert.Contains(sonuc, u => u.AppId == "440");
        Assert.Contains(sonuc, u => u.AppId == "570");
    }

    [Fact]
    public async Task UctanUca_KlasorAdindan_AppIdBulunur()
    {
        using var kok = new GeciciKlasor();
        var oyun = kok.AltKlasor("Team Fortress 2");

        Assert.Null(AppIdBulucu.YerelBul(oyun));

        var terimler = AppIdBulucu.AramaTerimleri(oyun);
        Assert.Contains("Team Fortress 2", terimler);

        var uygulamalar = await SteamMagazaAramasi.Ara(terimler);
        var adaylar = AppIdBulucu.AdaGoreAra(oyun, uygulamalar);

        Assert.NotEmpty(adaylar);
        Assert.Equal("440", adaylar[0].AppId);
    }
}
