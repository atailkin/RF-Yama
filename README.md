# RF-Yama

Oyunların online özelliklerini aktif eden bir yamalama programı.

Amaç: kullanıcının **sahip olduğu**, çevrimdışı çalışan oyunları çok oyunculu
oynanabilir hale getirmektir.

### AppID tespiti

`ini dosyasının` içindeki `real_app_id` alanı, oyunun Steam'den AppID'sini ister. Program bunu önce local dosyalardan arar ve eğer bulamazsa Steam'in GetAppList özelliği ile aramaya başlar

## Kullanım

İndirilen dosyayı (RF-Yama-win-x64.zip) sağ tıklayarak tümünü ayıkla seçeneğine basın, ayıklanan dosyayı açın ve RFYama.exe programına çift tıklayın.
İlk işlem olarak Gözat... butonuna basıp oyunun klasörünü seçtikten sonra sağ alttan klasörü seçin butonuna basın. Eğer AppID Otomatik bulunamadı diyorsa AppID ara tuşuna basın ve ordan oyunu seçin (AppID aramak en fazla 3 dakikayı bulabilir)
ve en son olarak kur butonuna basın ve işlem tamamlanınca programı kapatıp oyun dosyasından oyuna girebilirsiniz

## Kişisel Veriler

Program hiç bir şekilde veri toplayıp göndermez, belirli durumlarda gereken internet bağlantısı sadece Steam sunucularından oyunun ID'sini getirmek içindir.

## Sorumluluk Reddi Beyanı

**Bu yazılım eğitim amaçlı oluşturulmuştur.**
Bu araç sadece kodlama eğitimi ve kişisel kullanım amaçlıdır
Ticari kullanıma izin verilmez
Kullanıcılar bu programı kendi sorumluluklarında kullanırlar
Programda hiç bir şekilde korsan yazılım kullanılmaz veya paylaşılmaz. 
Mağaza araması yalnızca **ad → AppID** yönünde kullanılır; oyun ayrıntıları, fiyat veya DLC
listesi, **oyun dosyaları** alan bir çağrı yoktur.

## Sorun bildirme ve öneriler

Beklenmeyen bir hata olursa yığın izi `%APPDATA%\RFYama\hata.log` dosyasına eklenir ve hata
penceresinde yolu gösterilir. Kullanıcılar https://github.com/atailkin/RF-Yama/issues kısmından hata logunu ve önerileri paylaşabilir
İşlem geçmişi ayrıca **Log** sekmesinde tutulur ve `.log` olarak dışa aktarılabilir.