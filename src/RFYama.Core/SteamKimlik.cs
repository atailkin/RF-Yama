using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace RFYama.Core;

/// <summary>
/// SteamID64 doğrulama ve üretme. Ağ erişimi yok — tamamen yerel aritmetik.
/// </summary>
public static class SteamKimlik
{
    /// <summary>Bireysel hesapların SteamID64 tabanı (universe 1, type 1, instance 1).</summary>
    public const ulong BireyselTaban = 76561197960265728UL;

    private const ulong EnBuyukHesapNo = 0xFFFFFFFFUL;

    public static bool GecerliMi(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin)) return false;
        if (!ulong.TryParse(metin.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var deger))
            return false;
        return GecerliMi(deger);
    }

    public static bool GecerliMi(ulong deger)
        => deger >= BireyselTaban && deger <= BireyselTaban + EnBuyukHesapNo;

    /// <summary>Rastgele, biçimsel olarak geçerli bir SteamID64 üretir.</summary>
    public static string Uret()
    {
        // 1..2_000_000_000 aralığı gerçek hesap numaralarına yakın durur.
        var hesapNo = (ulong)RandomNumberGenerator.GetInt32(1, 2_000_000_000);
        return (BireyselTaban + hesapNo).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Steam mağaza bağlantısından AppID çıkarır. Ağ isteği yapmaz — yalnızca metin ayrıştırır.
    /// Örnek: https://store.steampowered.com/app/440/Team_Fortress_2/ → "440"
    /// </summary>
    public static string? BaglantidanAppIdCikar(string? girdi)
    {
        if (string.IsNullOrWhiteSpace(girdi)) return null;
        var metin = girdi.Trim();

        // Zaten düz sayı ise doğrudan kabul et.
        if (Regex.IsMatch(metin, @"^\d{1,10}$")) return metin;

        var eslesme = Regex.Match(
            metin,
            @"(?:store\.steampowered\.com|steamcommunity\.com)/(?:app|apps)/(\d{1,10})",
            RegexOptions.IgnoreCase);

        if (eslesme.Success) return eslesme.Groups[1].Value;

        // steam://rungameid/440 gibi protokol bağlantıları
        eslesme = Regex.Match(metin, @"steam://(?:run|rungameid|store)/(\d{1,10})", RegexOptions.IgnoreCase);
        return eslesme.Success ? eslesme.Groups[1].Value : null;
    }
}
