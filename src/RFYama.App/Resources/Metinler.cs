namespace RFYama.App.Resources;

/// <summary>
/// Arayüzde görünen metinler. Tek dosyada toplandı ki dil düzeltmesi için tek yere bakılsın.
/// (Doğrulama ve kurulum mesajları bağlamlarına yakın durmaları için RFYama.Core içindedir.)
/// </summary>
public static class Metinler
{
    public const string UygulamaAdi = "RF-Yama";
    public const string PencereBasligi = "RF-Yama — Yama Kurulum Aracı";

    // Sekmeler
    public const string SekmeKurulum = "Kurulum";
    public const string SekmeLog = "Log";

    // Kurulum sekmesi
    public const string HedefKlasorBasligi = "Oyun klasörü";
    public const string HedefKlasorAciklama =
        "Yamanın kurulacağı klasörü seçin. Oyunun .exe dosyasının bulunduğu klasör olmalıdır.";
    public const string Gozat = "Gözat…";
    public const string DurumBasligi = "Durum";
    public const string DosyalarBasligi = "Kurulacak dosyalar";
    public const string Kur = "Kur";
    public const string Kaldir = "Kaldır";
    public const string KlasorSecPencere = "Oyunun kurulu olduğu klasörü seçin";

    public const string PayloadBos =
        "payload klasörü boş. unsteam.dll, winmm.dll ve unsteam.ini dosyalarını " +
        "programın yanındaki payload klasörüne koyun.";

    public const string ZatenKurulu = "Bu klasörde kurulum mevcut.";
    public const string KuruluDegil = "Bu klasörde kurulum yok.";

    // Log sekmesi
    public const string LogBasligi = "İşlem logu";
    public const string LogKaydet = "Dosyaya kaydet…";
    public const string LogTemizle = "Temizle";

    // Tema
    // Emoji kullanılmıyor: StatusStrip metni Segoe UI ile çizer ve ☀/🌙 renkli emoji yerine
    // içi boş bir daire olarak düşer.
    public const string KoyuTemayaGec = "Koyu tema";
    public const string AcikTemayaGec = "Açık tema";

    // AppID
    public const string AppIdAra = "AppID ara";
    public const string AppIdAraniyor = "Steam mağazasında aranıyor…";
    public const string AppIdListeHatasi =
        "Steam mağazasına ulaşılamadı. İnternet bağlantınızı kontrol edip tekrar deneyin.";
    public const string AppIdAdayYok =
        "Steam mağazasında bu klasör adına benzeyen bir oyun bulunamadı. " +
        "AppID'yi unsteam.ini içine elle yazabilirsiniz.";

    // Genel
    public const string Hata = "Hata";
    public const string Uyari = "Uyarı";
    public const string Bilgi = "Bilgi";

    public const string UzerineYazmaOnayi =
        "Hedef klasörde aşağıdaki dosyalar zaten var ve üzerlerine yazılacak:\n\n{0}\n\n" +
        "Yedek ALINMAZ; özgün içerik kalıcı olarak kaybolur. Bu dosyalar başka bir moda ait " +
        "olabilir. Devam edilsin mi?";

    public const string KaldirOnayi =
        "Bu klasöre kurulan yama dosyaları silinecek. Kurulumdan sonra değiştirilmiş dosyalar " +
        "olduğu gibi bırakılır. Devam edilsin mi?";
}
