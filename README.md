# RF-Yama

Oyunların çevrimiçi (online) özelliklerini aktif eden bir yamalama programı.

> [!IMPORTANT]
> **Amaç:** Kullanıcının **sahip olduğu**, çevrimdışı çalışan oyunları çok oyunculu oynanabilir hale getirmektir.

## İçindekiler
- [AppID Tespiti](#appid-tespiti)
- [Kullanım](#kullanım)
- [Kişisel Veriler](#kişisel-veriler)
- [Sorumluluk Reddi Beyanı](#sorumluluk-reddi-beyanı)
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

## Sorun Bildirme ve Öneriler

Beklenmeyen bir hata oluşursa:
- Hata yığın izi (stack trace), `%APPDATA%\RFYama\hata.log` dosyasına otomatik olarak eklenir.
- Hata penceresinde dosyanın yolu gösterilir.

> [!TIP]
> Hatalarınızı ve önerilerinizi [GitHub Issues](https://github.com/atailkin/RF-Yama/issues) sayfasından, `hata.log` dosyasını da ekleyerek paylaşabilirsiniz.

İşlem geçmişi ayrıca uygulamanın **Log** sekmesinde tutulur ve `.log` formatında dışa aktarılabilir.
