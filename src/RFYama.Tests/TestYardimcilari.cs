using System.Globalization;

namespace RFYama.Tests;

/// <summary>Test süresince yaşayan, sonunda silinen geçici klasör.</summary>
internal sealed class GeciciKlasor : IDisposable
{
    public string Yol { get; }

    public GeciciKlasor(string? adEki = null)
    {
        Yol = Path.Combine(Path.GetTempPath(), $"rfyama-test-{adEki}{Guid.NewGuid():N}");
        Directory.CreateDirectory(Yol);
    }

    public string AltKlasor(params string[] parcalar)
    {
        var yol = Path.Combine(new[] { Yol }.Concat(parcalar).ToArray());
        Directory.CreateDirectory(yol);
        return yol;
    }

    public string Dosya(string goreliAd, string icerik)
    {
        var yol = Path.Combine(Yol, goreliAd);
        Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
        File.WriteAllText(yol, icerik);
        return yol;
    }

    public string Baytlar(string goreliAd, byte[] icerik)
    {
        var yol = Path.Combine(Yol, goreliAd);
        Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
        File.WriteAllBytes(yol, icerik);
        return yol;
    }

    public void Dispose()
    {
        try { Directory.Delete(Yol, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

/// <summary>Blok boyunca geçerli kültürü değiştirir, çıkışta eski hâline döndürür.</summary>
internal sealed class KulturKapsami : IDisposable
{
    private readonly CultureInfo _oncekiKultur;
    private readonly CultureInfo _oncekiArayuz;

    public KulturKapsami(string kultur)
    {
        _oncekiKultur = Thread.CurrentThread.CurrentCulture;
        _oncekiArayuz = Thread.CurrentThread.CurrentUICulture;
        var yeni = new CultureInfo(kultur);
        Thread.CurrentThread.CurrentCulture = yeni;
        Thread.CurrentThread.CurrentUICulture = yeni;
    }

    public void Dispose()
    {
        Thread.CurrentThread.CurrentCulture = _oncekiKultur;
        Thread.CurrentThread.CurrentUICulture = _oncekiArayuz;
    }
}
