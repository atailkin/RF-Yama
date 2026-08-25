using System.Globalization;
using RFYama.App.Forms;
using RFYama.App.Resources;
using RFYama.Core;

namespace RFYama.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Windows-1254 gibi ANSI kod sayfaları .NET 8'de sağlayıcı kaydı gerektirir.
        IniDocument.KodSayfalariniKaydet();

        // Arayüz Türkçe. Dosya G/Ç'de sayı biçimlendirmesi ve anahtar karşılaştırması
        // RFYama.Core içinde bilinçli olarak InvariantCulture/Ordinal kullanır — tr-TR'de
        // "ID".ToLower() → "ıd" olduğu ve ondalık ayırıcı ',' olduğu için.
        var turkce = new CultureInfo("tr-TR");
        CultureInfo.DefaultThreadCurrentCulture = turkce;
        CultureInfo.DefaultThreadCurrentUICulture = turkce;
        Thread.CurrentThread.CurrentCulture = turkce;
        Thread.CurrentThread.CurrentUICulture = turkce;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.ThreadException += (_, e) => HataGoster(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex) HataGoster(ex);
        };

        Application.Run(new MainForm());
    }

    private static void HataGoster(Exception ex)
    {
        var gunlukYolu = HataGunluguneYaz(ex);

        MessageBox.Show(
            $"Beklenmeyen bir hata oluştu:{Environment.NewLine}{Environment.NewLine}" +
            $"{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
            (gunlukYolu is null
                ? string.Empty
                : $"Ayrıntılar kaydedildi:{Environment.NewLine}{gunlukYolu}{Environment.NewLine}{Environment.NewLine}") +
            "Program kapanmadıysa çalışmaya devam edebilirsiniz.",
            Metinler.Hata,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    /// <summary>
    /// Yığın izini %APPDATA%\RFYama\hata.log dosyasına ekler. Kullanıcı hata bildirirken bu
    /// dosyayı gönderebilsin diye kalıcıdır.
    /// </summary>
    private static string? HataGunluguneYaz(Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppSettings.Klasor());
            var yol = Path.Combine(AppSettings.Klasor(), "hata.log");

            File.AppendAllText(yol,
                $"--- {DateTime.Now:yyyy-MM-dd HH:mm:ss} ---{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}{Environment.NewLine}",
                IniDocument.BomsuzUtf8);

            return yol;
        }
        catch (Exception hata) when (hata is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
