# RF-Yama

Oyunların çevrimiçi (online) özelliklerini aktif eden bir yamalama programı.

> [!IMPORTANT]
> **Amaç:** Kullanıcının **sahip olduğu**, çevrimdışı çalışan oyunları çok oyunculu oynanabilir hale getirmektir.

## İçindekiler
- [AppID Tespiti](#appid-tespiti)
- [Kullanım](#kullanım)
- [Kişisel Veriler](#kişisel-veriler)
- [Sorumluluk Reddi Beyanı](#sorumluluk-reddi-beyanı)
- [SSS (Sıkça Sorulan Sorular)](#sss-sıkça-sorulan-sorular)
- [Bilinen Sınırlamalar](#bilinen-sınırlamalar)
- [Sorun Bildirme ve Öneriler](#sorun-bildirme-ve-öneriler)

## AppID Tespiti

`ini` dosyasının içindeki `real_app_id` alanı, oyunun Steam üzerindeki AppID'sini gerektirir. Program bu bilgiyi şu sırayla arar:

1. Önce yerel (local) dosyalardan arar.
2. Bulamazsa Steam'in `GetAppList` özelliğini kullanarak arar.

> [!NOTE]
> Mağaza araması yalnızca **ad → AppID** yönünde çalışır. Oyun ayrıntıları, fiyat ya da DLC listesi çekilmez; oyun dosyalarına erişen herhangi bir çağrı yapılmaz.

## Kullanım

1. İndirilen `RF-Yama-win-x64.zip` dosyasına sağ tıklayıp **Tümünü Ayıkla**'yı seçin.
2. Ayıklanan klasörü açın ve `RFYama.exe` dosyasına çift tıklayın.
3. **Gözat...** butonuna basarak oyunun kurulu olduğu klasörü seçin.
4. Sağ alttaki **Klasörü Seç** butonuna basın.
5. AppID otomatik bulunamazsa **AppID Ara** butonuna basıp listeden oyunu seçin.
6. **Kur** butonuna basın.
7. İşlem tamamlandığında programı kapatıp oyunu kendi klasöründen başlatabilirsiniz.

![RF-Yama kullanım ekranı](https://github.com/user-attachments/assets/5f612755-cb7f-4f35-94d5-e5cc5e425457)

> [!TIP]
> AppID araması, bağlantı hızına bağlı olarak **en fazla 3 dakika** sürebilir.

## Kişisel Veriler

> [!NOTE]
> Program hiçbir şekilde kullanıcı verisi toplamaz ya da göndermez. Gereken internet bağlantısı yalnızca Steam sunucularından oyunun AppID bilgisini almak içindir.

## Sorumluluk Reddi Beyanı

> [!CAUTION]
> - Bu yazılım **yalnızca eğitim ve kişisel kullanım amacıyla** oluşturulmuştur.
> - Program hiçbir şekilde korsan yazılım kullanmaz veya paylaşmaz.
> - Kullanıcılar bu programı **kendi sorumluluklarında** kullanır.
> - Mağaza araması yalnızca ad → AppID yönünde kullanılır; oyun dosyalarına erişim sağlanmaz.

## SSS (Sıkça Sorulan Sorular)

**S: Bu program virüs mü?**
C: Hayır, programı antivirüsünüzü açık tutarak rahatça kullanabilirsiniz, ayrıca teknik bilginiz var ise VirusTotal kullanarak ya da direkt kaynak kodlarını inceleyerek test edebilirsiniz.

**S: Program internet bağlantısı istiyor, güvenli mi?**
C: Evet. İnternet bağlantısı yalnızca Steam sunucularından oyunun AppID bilgisini çekmek için kullanılır, başka hiçbir veri gönderilmez veya toplanmaz.

**S: AppID otomatik bulunamadı, ne yapmalıyım?**
C: **AppID Ara** butonuna basıp listeden oyununuzu manuel olarak seçebilirsiniz. Bu işlem bağlantı hızına göre en fazla 3 dakika sürebilir.

**S: Bu programı kullanmak yasal mı?**
C: Program yalnızca **sahip olduğunuz** orijinal oyunlar üzerinde kullanılmak üzere tasarlanmıştır ve hiçbir korsan içerik barındırmaz ya da paylaşmaz. Kullanım sorumluluğu tamamen kullanıcıya aittir.

**S: Program ücretsiz mi?**
C: Evet, RF-Yama tamamen ücretsizdir ve açık kaynaklıdır; ticari amaçla asla satılmayacaktır.

**S: Hangi işletim sistemlerinde çalışır?**
C: Şu an yalnızca 64 bit (x64) Windows sistemlerinde çalışmaktadır.

> [!NOTE]
> Sorunuzun cevabını burada bulamadıysanız [GitHub Issues](https://github.com/atailkin/RF-Yama/issues) üzerinden sorabilirsiniz.

## Bilinen Sınırlamalar

> [!WARNING]
> - 32 bit oyunlarda çalışmaz.
> - Özel (custom) sunucu kullanan oyunlarda çalışmaz.
> - Epic Games, Rockstar Launcher gibi Steam dışı platform oyunlarında çalışmaz; yalnızca Steam ile uyumludur.
> - Aşırı büyük kurumsal oyunlarda çalışmaz (örnek: GTA V, Rainbow Six, Mortal Kombat serisi, CS2).

## Sorun Bildirme ve Öneriler

Beklenmeyen bir hata oluşursa:
- Hata yığın izi (stack trace), `%APPDATA%\RFYama\hata.log` dosyasına otomatik olarak eklenir.
- Hata penceresinde dosyanın yolu gösterilir.

> [!TIP]
> Hatalarınızı ve önerilerinizi [GitHub Issues](https://github.com/atailkin/RF-Yama/issues) sayfasından, `hata.log` dosyasını da ekleyerek paylaşabilirsiniz.

İşlem geçmişi ayrıca uygulamanın **Log** sekmesinde tutulur ve `.log` formatında dışa aktarılabilir.
