using RFYama.Core;

namespace RFYama.App.Forms;

/// <summary>
/// Steam uygulama listesinden gelen aday AppID'leri kullanıcıya seçtirir.
///
/// Ad eşleşmesi tahmindir: aynı adı taşıyan demo, sunucu ve bölgesel sürümler olabilir.
/// Bu yüzden hiçbir aday kendiliğinden uygulanmaz, kullanıcı onaylar.
/// </summary>
public sealed class AppIdSecimFormu : Form
{
    private readonly ListView _liste = new();
    private readonly Button _tamam = new();

    public string? SecilenAppId { get; private set; }

    public AppIdSecimFormu(IReadOnlyList<AppIdSonucu> adaylar, string oyunKlasorAdi)
    {
        Text = "AppID seçin";
        MinimumSize = new Size(520, 380);
        Size = new Size(560, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = false;
        Font = new Font("Segoe UI", 9f);

        var aciklama = new Label
        {
            Text = $"\"{oyunKlasorAdi}\" için Steam listesinde bulunan adaylar.\n" +
                   "Bu eşleşmeler oyunun adına dayanır, kesin değildir — doğru olanı seçin.",
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(4, 6, 4, 4)
        };

        _liste.Dock = DockStyle.Fill;
        _liste.View = View.Details;
        _liste.FullRowSelect = true;
        _liste.MultiSelect = false;
        _liste.HideSelection = false;
        _liste.Columns.Add("Oyun", 380);
        _liste.Columns.Add("AppID", 110);

        foreach (var aday in adaylar)
        {
            var oge = new ListViewItem(aday.Ad ?? "(adsız)") { Tag = aday.AppId };
            oge.SubItems.Add(aday.AppId);
            _liste.Items.Add(oge);
        }

        if (_liste.Items.Count > 0) _liste.Items[0].Selected = true;

        _liste.DoubleClick += (_, _) => { if (SecimiAl()) DialogResult = DialogResult.OK; };
        _liste.SelectedIndexChanged += (_, _) => _tamam.Enabled = _liste.SelectedItems.Count > 0;

        var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(0, 10, 0, 0) };

        _tamam.Text = "Seç";
        _tamam.Width = 110;
        _tamam.Height = 30;
        _tamam.Dock = DockStyle.Right;
        _tamam.Enabled = _liste.Items.Count > 0;
        _tamam.Click += (_, _) => { if (SecimiAl()) DialogResult = DialogResult.OK; };

        var iptal = new Button
        {
            Text = "İptal",
            Width = 110,
            Height = 30,
            Dock = DockStyle.Right,
            DialogResult = DialogResult.Cancel
        };

        altPanel.Controls.Add(_tamam);
        altPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        altPanel.Controls.Add(iptal);

        Controls.Add(_liste);
        Controls.Add(altPanel);
        Controls.Add(aciklama);

        AcceptButton = _tamam;
        CancelButton = iptal;

        Tema.Uygula(this, Tema.Karanlik);
    }

    private bool SecimiAl()
    {
        if (_liste.SelectedItems.Count == 0) return false;
        SecilenAppId = _liste.SelectedItems[0].Tag as string;
        return !string.IsNullOrEmpty(SecilenAppId);
    }
}
