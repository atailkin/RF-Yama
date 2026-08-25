using System.ComponentModel;
using System.Diagnostics;

namespace RFYama.Core;

public static class OyunBaslatici
{
    /// <summary>
    /// Yapılandırılmış oyunu, INI'deki başlatma parametreleriyle hedef klasörden başlatır.
    /// Çalışma klasörü hedef klasör olmalıdır; aksi hâlde proxy DLL yüklenmez.
    /// </summary>
    public static (bool Basarili, string Mesaj) Baslat(string hedefKlasor, string exeAdi, string? parametreler)
    {
        if (string.IsNullOrWhiteSpace(exeAdi))
            return (false, "Ayarlar sekmesinde oyun dosyası seçilmemiş.");

        var yol = Path.Combine(hedefKlasor, exeAdi);
        if (!File.Exists(yol))
            return (false, $"Oyun dosyası bulunamadı: {exeAdi}");

        try
        {
            var baslangic = new ProcessStartInfo
            {
                FileName = yol,
                // DLL arama sırası çalışma klasöründen başlar; bu yüzden hedef klasör olmalı.
                WorkingDirectory = hedefKlasor,
                UseShellExecute = false
            };

            if (!string.IsNullOrWhiteSpace(parametreler))
                baslangic.Arguments = parametreler;

            Process.Start(baslangic);
            return (true, $"\"{exeAdi}\" başlatıldı.");
        }
        catch (Win32Exception ex)
        {
            return (false, $"Oyun başlatılamadı: {ex.Message}");
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return (false, $"Oyun başlatılamadı: {ex.Message}");
        }
    }

    /// <summary>Uygulamayı yönetici hakkıyla yeniden başlatır.</summary>
    public static bool YoneticiOlarakYenidenBaslat(string? argumanlar = null)
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return false;

            var baslangic = new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Verb = "runas"
            };

            if (!string.IsNullOrWhiteSpace(argumanlar))
                baslangic.Arguments = argumanlar;

            Process.Start(baslangic);
            return true;
        }
        catch (Win32Exception)
        {
            // Kullanıcı UAC penceresini reddetti.
            return false;
        }
    }

    /// <summary>Bir klasörü Windows Gezgini'nde açar.</summary>
    public static void KlasorAc(string klasor)
    {
        if (!Directory.Exists(klasor)) return;
        try
        {
            Process.Start(new ProcessStartInfo { FileName = klasor, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or IOException)
        {
            // Gezgin açılamadıysa yapacak bir şey yok.
        }
    }
}
