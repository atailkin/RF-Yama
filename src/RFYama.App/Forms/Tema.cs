using System.Runtime.InteropServices;

namespace RFYama.App.Forms;

/// <summary>
/// Açık/koyu tema.
///
/// WinForms'un yerleşik bir karanlık modu yoktur: renkler kontrol ağacına elle uygulanır.
/// Palet tek yerde tutulur ki yeni bir kontrol eklendiğinde iki ayrı renk listesi
/// güncellenmek zorunda kalmasın.
///
/// Seçim <see cref="RFYama.Core.AppSettings.KaranlikMod"/> içinde saklanır ve açılışta
/// uygulanır.
/// </summary>
public static class Tema
{
    /// <summary>
    /// Koyu temada gri (ikincil) gösterilmesi gereken kontroller <c>Tag</c> alanına bunu
    /// koyar. Renk doğrudan okunamaz: bir kez uygulandıktan sonra kontrolün ForeColor'ı
    /// artık GrayText olmadığı için tema geri alınamaz hâle gelirdi.
    /// </summary>
    public const string SoluEtiket = "solu";

    public static bool Karanlik { get; private set; }

    // --- Palet ---------------------------------------------------------------------------

    public static Color Zemin => Karanlik ? Color.FromArgb(32, 32, 32) : SystemColors.Control;
    public static Color KutuZemini => Karanlik ? Color.FromArgb(43, 43, 43) : SystemColors.Window;
    public static Color DugmeZemini => Karanlik ? Color.FromArgb(58, 58, 58) : SystemColors.ButtonFace;
    public static Color Yazi => Karanlik ? Color.FromArgb(240, 240, 240) : SystemColors.ControlText;
    public static Color SoluYazi => Karanlik ? Color.FromArgb(160, 160, 160) : SystemColors.GrayText;
    public static Color Kenarlik => Karanlik ? Color.FromArgb(80, 80, 80) : SystemColors.ControlDark;

    // Durum renkleri koyu zeminde ayrı tutulur: açık tema için seçilen koyu kırmızı/yeşil
    // tonları 32,32,32 üzerinde okunmaz.
    public static Color Basari => Karanlik ? Color.FromArgb(106, 201, 138) : Color.FromArgb(23, 111, 60);
    public static Color UyariRengi => Karanlik ? Color.FromArgb(233, 180, 76) : Color.FromArgb(176, 104, 0);
    public static Color HataRengi => Karanlik ? Color.FromArgb(240, 124, 132) : Color.FromArgb(192, 28, 40);

    // --- Uygulama ------------------------------------------------------------------------

    public static void Uygula(Form form, bool karanlik)
    {
        Karanlik = karanlik;

        form.SuspendLayout();
        try
        {
            BasligiBoya(form, karanlik);
            KontroleUygula(form);
        }
        finally
        {
            form.ResumeLayout();
        }

        form.Invalidate(invalidateChildren: true);
    }

    private static void KontroleUygula(Control kontrol)
    {
        switch (kontrol)
        {
            case TabControl sekmeler:
                sekmeler.BackColor = Zemin;
                sekmeler.ForeColor = Yazi;
                // Sekme başlıkları yalnızca koyu temada elle çizilir; açık temada işletim
                // sisteminin görsel stili kendi çizimini yapsın.
                sekmeler.DrawMode = Karanlik ? TabDrawMode.OwnerDrawFixed : TabDrawMode.Normal;
                break;

            case TabPage sayfa:
                // UseVisualStyleBackColor açıkken BackColor yok sayılır.
                sayfa.UseVisualStyleBackColor = !Karanlik;
                sayfa.BackColor = Zemin;
                sayfa.ForeColor = Yazi;
                break;

            case GroupBox kutu:
                kutu.BackColor = Zemin;
                kutu.ForeColor = Yazi;
                break;

            case Button dugme:
                dugme.BackColor = DugmeZemini;
                dugme.ForeColor = Yazi;
                dugme.FlatStyle = Karanlik ? FlatStyle.Flat : FlatStyle.Standard;
                dugme.FlatAppearance.BorderColor = Kenarlik;
                dugme.UseVisualStyleBackColor = !Karanlik;
                break;

            case TextBox kutucuk:
                kutucuk.BackColor = KutuZemini;
                kutucuk.ForeColor = Yazi;
                break;

            case CheckedListBox liste:
                liste.BackColor = KutuZemini;
                liste.ForeColor = Yazi;
                break;

            case ListView listeGorunumu:
                listeGorunumu.BackColor = KutuZemini;
                listeGorunumu.ForeColor = Yazi;
                break;

            case Label etiket:
                etiket.ForeColor = SoluEtiket.Equals(etiket.Tag) ? SoluYazi : Yazi;
                break;

            case StatusStrip durumCubugu:
                durumCubugu.BackColor = Zemin;
                durumCubugu.ForeColor = Yazi;
                foreach (ToolStripItem oge in durumCubugu.Items)
                {
                    oge.BackColor = Zemin;
                    oge.ForeColor = Yazi;
                }
                break;

            default:
                kontrol.BackColor = Zemin;
                kontrol.ForeColor = Yazi;
                break;
        }

        foreach (Control cocuk in kontrol.Controls)
            KontroleUygula(cocuk);
    }

    /// <summary>
    /// Koyu temada sekme başlıklarını çizer. <see cref="TabDrawMode.OwnerDrawFixed"/>
    /// yalnızca koyu temada açıldığı için açık temada bu işleyici hiç çağrılmaz.
    /// </summary>
    public static void SekmeBasligiCiz(object? gonderen, DrawItemEventArgs e)
    {
        if (gonderen is not TabControl sekmeler || e.Index < 0 || e.Index >= sekmeler.TabPages.Count)
            return;

        // Son sekmenin sağında kalan şerit boşluğunu WinForms işletim sistemi temasıyla
        // doldurur; koyu zeminde açık renk bir bant olarak görünür. Yalnızca o boşluk boyanır,
        // hiçbir sekmenin üstüne gelinmez — böylece çizim sırası ne olursa olsun güvenlidir.
        var sonSekme = sekmeler.GetTabRect(sekmeler.TabPages.Count - 1);
        if (sonSekme.Right < sekmeler.Width)
        {
            e.Graphics.SetClip(new Rectangle(
                sonSekme.Right, 0, sekmeler.Width - sonSekme.Right, sonSekme.Bottom));

            using (var seritFircasi = new SolidBrush(Zemin))
                e.Graphics.FillRectangle(seritFircasi, sekmeler.ClientRectangle);

            e.Graphics.SetClip(e.Bounds);
        }

        var secili = e.Index == sekmeler.SelectedIndex;

        using (var firca = new SolidBrush(secili ? KutuZemini : Zemin))
            e.Graphics.FillRectangle(firca, e.Bounds);

        TextRenderer.DrawText(
            e.Graphics,
            sekmeler.TabPages[e.Index].Text,
            sekmeler.Font,
            e.Bounds,
            secili ? Yazi : SoluYazi,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    // --- Pencere başlığı -----------------------------------------------------------------

    private const int DwmwaKaranlikBaslik = 20;       // Windows 10 20H1 ve sonrası
    private const int DwmwaKaranlikBaslikEski = 19;   // Windows 10 1809–1909

    [DllImport("dwmapi.dll", SetLastError = true)]
    private static extern int DwmSetWindowAttribute(IntPtr pencere, int oznitelik, ref int deger, int boyut);

    /// <summary>
    /// Pencere başlık çubuğunu koyu yapar. Desteklemeyen sürümlerde çağrı hata kodu döndürür
    /// ve başlık açık kalır — işlevsel bir sorun değildir.
    /// </summary>
    private static void BasligiBoya(Form form, bool karanlik)
    {
        if (!form.IsHandleCreated) return;

        var deger = karanlik ? 1 : 0;
        try
        {
            if (DwmSetWindowAttribute(form.Handle, DwmwaKaranlikBaslik, ref deger, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, DwmwaKaranlikBaslikEski, ref deger, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // dwmapi.dll yok (masaüstü kompozisyonu devre dışı) — başlık açık kalır.
        }
        catch (EntryPointNotFoundException)
        {
        }
    }
}
