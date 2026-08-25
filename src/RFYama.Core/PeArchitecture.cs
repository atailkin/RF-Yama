namespace RFYama.Core;

public enum PeMimari
{
    Bilinmiyor,
    X86,
    X64,
    Arm64
}

/// <summary>
/// PE başlığından 32/64-bit tespiti.
///
/// 32-bit bir oyun 64-bit DLL'i sessizce yüklemez (ve tersi). Bu uyuşmazlık "kurdum ama
/// çalışmıyor" şikâyetlerinin en yaygın nedenidir; kurulumdan önce yakalamak gerekir.
/// </summary>
public static class PeArchitecture
{
    private const ushort MachineI386 = 0x014C;
    private const ushort MachineAmd64 = 0x8664;
    private const ushort MachineArm64 = 0xAA64;

    private const ushort CharacteristicsDll = 0x2000;

    public static PeMimari Oku(string yol)
    {
        try
        {
            using var akis = new FileStream(yol, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Oku(akis).Mimari;
        }
        catch (IOException) { return PeMimari.Bilinmiyor; }
        catch (UnauthorizedAccessException) { return PeMimari.Bilinmiyor; }
    }

    public static (PeMimari Mimari, bool DllMi) Detay(string yol)
    {
        try
        {
            using var akis = new FileStream(yol, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return Oku(akis);
        }
        catch (IOException) { return (PeMimari.Bilinmiyor, false); }
        catch (UnauthorizedAccessException) { return (PeMimari.Bilinmiyor, false); }
    }

    private static (PeMimari Mimari, bool DllMi) Oku(Stream akis)
    {
        using var okuyucu = new BinaryReader(akis);

        if (akis.Length < 0x40) return (PeMimari.Bilinmiyor, false);

        // DOS başlığı: "MZ"
        if (okuyucu.ReadUInt16() != 0x5A4D) return (PeMimari.Bilinmiyor, false);

        // e_lfanew — PE başlığının konumu
        akis.Seek(0x3C, SeekOrigin.Begin);
        var peKonumu = okuyucu.ReadInt32();
        if (peKonumu <= 0 || peKonumu + 24 > akis.Length) return (PeMimari.Bilinmiyor, false);

        akis.Seek(peKonumu, SeekOrigin.Begin);

        // "PE\0\0"
        if (okuyucu.ReadUInt32() != 0x00004550) return (PeMimari.Bilinmiyor, false);

        var machine = okuyucu.ReadUInt16();

        // COFF başlığı: Machine(2) NumberOfSections(2) TimeDateStamp(4) PointerToSymbolTable(4)
        // NumberOfSymbols(4) SizeOfOptionalHeader(2) Characteristics(2)
        akis.Seek(peKonumu + 4 + 18, SeekOrigin.Begin);
        var characteristics = okuyucu.ReadUInt16();

        var mimari = machine switch
        {
            MachineI386 => PeMimari.X86,
            MachineAmd64 => PeMimari.X64,
            MachineArm64 => PeMimari.Arm64,
            _ => PeMimari.Bilinmiyor
        };

        return (mimari, (characteristics & CharacteristicsDll) != 0);
    }

    public static string Ad(PeMimari mimari) => mimari switch
    {
        PeMimari.X86 => "32-bit",
        PeMimari.X64 => "64-bit",
        PeMimari.Arm64 => "ARM64",
        _ => "bilinmiyor"
    };
}
