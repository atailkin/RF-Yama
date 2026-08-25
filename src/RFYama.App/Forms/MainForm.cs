using RFYama.App.Resources;
using RFYama.Core;

namespace RFYama.App.Forms;

public sealed class MainForm : Form
{
    private readonly AppSettings _ayarlar;

    private string? _hedefKlasor;

    /// <summary>Son doldurulan bulgular — tema değişince yeniden boyanabilsin diye saklanır.</summary>
    private IReadOnlyList<DogrulamaBulgusu> _sonBulgular = Array.Empty<DogrulamaBulgusu>();

    // Kurulum
    private readonly TextBox _hedefKutusu = new();
    private readonly CheckedListBox _dosyaListesi = new();
    private readonly ListView _durumListesi = new();
    private readonly Button _kurDugmesi = new();
    private readonly Button _kaldirDugmesi = new();
    private readonly Button _appIdAraDugmesi = new();

    /// <summary>Hedef klasor icin bulunan AppID; bulunamadiysa null.</summary>
    private AppIdSonucu? _appId;

    // Log
    private readonly TextBox _logKutusu = new();

    private readonly StatusStrip _durumCubugu = new();
    private readonly ToolStripStatusLabel _durumEtiketi = new();
    private readonly ToolStripButton _temaDugmesi = new();

    public MainForm()
    {
        IniDocument.KodSayfalariniKaydet();
        _ayarlar = AppSettings.Yukle();

        ArayuzKur();
        BaslangicDurumu();
    }

    // --- Arayüz kurulumu -------------------------------------------------------------------

    private void ArayuzKur()
    {
        Text = Metinler.PencereBasligi;
        PencereSimgesiniAyarla();
        MinimumSize = new Size(760, 520);
        Size = new Size(
            _ayarlar.PencereGenislik > 0 ? _ayarlar.PencereGenislik : 900,
            _ayarlar.PencereYukseklik > 0 ? _ayarlar.PencereYukseklik : 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.Dpi;

        var sekmeler = new TabControl { Dock = DockStyle.Fill, Padding = new Point(12, 6) };
        sekmeler.TabPages.Add(KurulumSekmesi());
        sekmeler.TabPages.Add(LogSekmesi());
        // DrawMode yalnızca koyu temada OwnerDrawFixed'e çekilir; açık temada bu işleyici
        // hiç çağrılmaz ve sekmeler işletim sisteminin görsel stiliyle çizilir.
        sekmeler.DrawItem += Tema.SekmeBasligiCiz;

        _durumEtiketi.Spring = true;
        _durumEtiketi.TextAlign = ContentAlignment.MiddleLeft;

        _temaDugmesi.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _temaDugmesi.Alignment = ToolStripItemAlignment.Right;
        _temaDugmesi.Click += (_, _) => TemayiUygula(!Tema.Karanlik, kaydet: true);

        // Spring'li etiket önce eklenir: kalan alanı doldurup düğmeyi sağ alta iter.
        _durumCubugu.Items.Add(_durumEtiketi);
        _durumCubugu.Items.Add(_temaDugmesi);

        // Dock sırası: Fill önce eklenir; kenarlara yaslananlar sonra yerleşir.
        Controls.Add(sekmeler);
        Controls.Add(_durumCubugu);

        FormClosing += (_, _) =>
        {
            _ayarlar.SonHedefKlasor = _hedefKlasor;
            _ayarlar.PencereGenislik = Width;
            _ayarlar.PencereYukseklik = Height;
            _ayarlar.Kaydet();
        };
    }

    /// <summary>
    /// Pencere ve görev çubuğu simgesini exe'ye gömülü simgeden alır. WinForms varsayılan
    /// olarak kendi genel simgesini kullanır; ApplicationIcon yalnızca dosya gezginindeki
    /// görünümü değiştirir.
    /// </summary>
    private void PencereSimgesiniAyarla()
    {
        try
        {
            var exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe)) return;

            var simge = Icon.ExtractAssociatedIcon(exe);
            if (simge is not null) Icon = simge;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            // Simge alınamazsa WinForms varsayılanı kullanılır; işlevsel bir kayıp yok.
        }
    }

    private TabPage KurulumSekmesi()
    {
        var sayfa = new TabPage(Metinler.SekmeKurulum) { Padding = new Padding(12), UseVisualStyleBackColor = true };

        // --- orta alan (Fill) — önce eklenmeli
        var orta = new Panel { Dock = DockStyle.Fill };

        var durumKutusu = new GroupBox
        {
            Text = Metinler.DurumBasligi,
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        _durumListesi.Dock = DockStyle.Fill;
        _durumListesi.View = View.Details;
        _durumListesi.HeaderStyle = ColumnHeaderStyle.None;
        _durumListesi.FullRowSelect = true;
        _durumListesi.MultiSelect = false;
        _durumListesi.BorderStyle = BorderStyle.FixedSingle;
        _durumListesi.Columns.Add("Durum", -2);
        durumKutusu.Controls.Add(_durumListesi);

        var dosyaKutusu = new GroupBox
        {
            Text = Metinler.DosyalarBasligi,
            Dock = DockStyle.Left,
            Width = 260,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 12, 0)
        };
        _dosyaListesi.Dock = DockStyle.Fill;
        _dosyaListesi.CheckOnClick = true;
        _dosyaListesi.IntegralHeight = false;
        _dosyaListesi.BorderStyle = BorderStyle.FixedSingle;
        // ItemCheck, işaret değişmeden önce tetiklenir; doğrulamayı bir tur sonraya bırak.
        _dosyaListesi.ItemCheck += (_, _) =>
        {
            if (!IsHandleCreated) return;
            BeginInvoke(() =>
            {
                if (_hedefKlasor is not null) HedefiDogrula();
            });
        };
        dosyaKutusu.Controls.Add(_dosyaListesi);

        var ayirici = new Panel { Dock = DockStyle.Left, Width = 12 };

        orta.Controls.Add(durumKutusu);
        orta.Controls.Add(ayirici);
        orta.Controls.Add(dosyaKutusu);

        // --- üst: hedef klasör
        var hedefKutusu = new GroupBox
        {
            Text = Metinler.HedefKlasorBasligi,
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(10, 6, 10, 10)
        };

        var hedefSatiri = new Panel { Dock = DockStyle.Bottom, Height = 28 };
        _hedefKutusu.Dock = DockStyle.Fill;
        _hedefKutusu.ReadOnly = true;

        var gozatDugmesi = new Button { Text = Metinler.Gozat, Width = 100, Dock = DockStyle.Right };
        gozatDugmesi.Click += (_, _) => KlasorSec();

        hedefSatiri.Controls.Add(_hedefKutusu);
        hedefSatiri.Controls.Add(gozatDugmesi);

        var hedefAciklama = new Label
        {
            Text = Metinler.HedefKlasorAciklama,
            Dock = DockStyle.Fill,
            Tag = Tema.SoluEtiket
        };

        hedefKutusu.Controls.Add(hedefAciklama);
        hedefKutusu.Controls.Add(hedefSatiri);

        // --- alt: düğmeler
        var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(0, 12, 0, 0) };

        _kurDugmesi.Text = Metinler.Kur;
        _kurDugmesi.Width = 130;
        _kurDugmesi.Height = 32;
        _kurDugmesi.Dock = DockStyle.Right;
        _kurDugmesi.Click += (_, _) => Kur();

        _kaldirDugmesi.Text = Metinler.Kaldir;
        _kaldirDugmesi.Width = 120;
        _kaldirDugmesi.Height = 32;
        _kaldirDugmesi.Dock = DockStyle.Right;
        _kaldirDugmesi.Margin = new Padding(0, 0, 8, 0);
        _kaldirDugmesi.Click += (_, _) => KaldirmayiCalistir();

        var bosluk1 = new Panel { Dock = DockStyle.Right, Width = 8 };

        _appIdAraDugmesi.Text = Metinler.AppIdAra;
        _appIdAraDugmesi.Width = 150;
        _appIdAraDugmesi.Height = 32;
        _appIdAraDugmesi.Dock = DockStyle.Left;
        _appIdAraDugmesi.Click += async (_, _) => await AppIdiSteamdenAra();

        altPanel.Controls.Add(_appIdAraDugmesi);
        altPanel.Controls.Add(_kurDugmesi);
        altPanel.Controls.Add(bosluk1);
        altPanel.Controls.Add(_kaldirDugmesi);

        sayfa.Controls.Add(orta);
        sayfa.Controls.Add(altPanel);
        sayfa.Controls.Add(hedefKutusu);

        return sayfa;
    }

    private TabPage LogSekmesi()
    {
        var sayfa = new TabPage(Metinler.SekmeLog) { Padding = new Padding(12), UseVisualStyleBackColor = true };

        var kutu = new GroupBox { Text = Metinler.LogBasligi, Dock = DockStyle.Fill, Padding = new Padding(8) };
        _logKutusu.Dock = DockStyle.Fill;
        _logKutusu.Multiline = true;
        _logKutusu.ReadOnly = true;
        _logKutusu.ScrollBars = ScrollBars.Both;
        _logKutusu.WordWrap = false;
        _logKutusu.BorderStyle = BorderStyle.FixedSingle;
        _logKutusu.Font = new Font("Consolas", 9.5f);
        kutu.Controls.Add(_logKutusu);

        var altPanel = new Panel { Dock = DockStyle.Bottom, Height = 46, Padding = new Padding(0, 10, 0, 0) };

        var kaydetDugmesi = new Button { Text = Metinler.LogKaydet, Width = 150, Height = 30, Dock = DockStyle.Right };
        kaydetDugmesi.Click += (_, _) => LogaKaydet();

        var temizleDugmesi = new Button { Text = Metinler.LogTemizle, Width = 110, Height = 30, Dock = DockStyle.Right };
        temizleDugmesi.Click += (_, _) => _logKutusu.Clear();

        altPanel.Controls.Add(kaydetDugmesi);
        altPanel.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 8 });
        altPanel.Controls.Add(temizleDugmesi);

        sayfa.Controls.Add(kutu);
        sayfa.Controls.Add(altPanel);

        return sayfa;
    }

    // --- Tema ------------------------------------------------------------------------------

    /// <summary>
    /// Kaydedilmiş tema, pencere tutamacı oluştuktan sonra uygulanır: başlık çubuğunu koyu
    /// yapan DWM çağrısı geçerli bir tutamaç ister.
    /// </summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        TemayiUygula(_ayarlar.KaranlikMod, kaydet: false);
    }

    /// <summary>Temayı uygular ve düğme metnini bir sonraki duruma göre günceller.</summary>
    private void TemayiUygula(bool karanlik, bool kaydet)
    {
        Tema.Uygula(this, karanlik);
        _temaDugmesi.Text = karanlik ? Metinler.AcikTemayaGec : Metinler.KoyuTemayaGec;

        // Durum satırlarının renkleri temaya bağlı; listeyi mevcut içerikle yeniden boya.
        DurumListesiYenidenBoya();

        if (!kaydet) return;
        _ayarlar.KaranlikMod = karanlik;
        _ayarlar.Kaydet();
    }

    // --- Durum ve log ----------------------------------------------------------------------

    private void Log(string mesaj)
    {
        _logKutusu.AppendText($"[{DateTime.Now:HH:mm:ss}] {mesaj}{Environment.NewLine}");
    }

    private void DurumBildir(string mesaj)
    {
        _durumEtiketi.Text = mesaj;
        Log(mesaj);
    }

    private void DurumListesiDoldur(IEnumerable<DogrulamaBulgusu> bulgular)
    {
        _sonBulgular = bulgular.ToList();
        DurumListesiYenidenBoya();
    }

    private void DurumListesiYenidenBoya()
    {
        _durumListesi.BeginUpdate();
        _durumListesi.Items.Clear();

        foreach (var bulgu in _sonBulgular)
        {
            var (isaret, renk) = bulgu.Seviye switch
            {
                BulguSeviyesi.Hata => ("✕", Tema.HataRengi),
                BulguSeviyesi.Uyari => ("!", Tema.UyariRengi),
                _ => ("✓", Tema.Basari)
            };

            _durumListesi.Items.Add(new ListViewItem($"{isaret}  {bulgu.Mesaj}") { ForeColor = renk });
        }

        _durumListesi.Columns[0].Width = -2;
        _durumListesi.EndUpdate();
    }

    // --- Başlangıç -------------------------------------------------------------------------

    private void BaslangicDurumu()
    {
        PayloadListesiDoldur();

        if (!string.IsNullOrEmpty(_ayarlar.SonHedefKlasor) && Directory.Exists(_ayarlar.SonHedefKlasor))
        {
            HedefKlasoruAyarla(_ayarlar.SonHedefKlasor);
        }
        else
        {
            DurumListesiDoldur(new[]
            {
                new DogrulamaBulgusu(BulguSeviyesi.Bilgi, "Başlamak için oyun klasörünü seçin.")
            });
            DugmeDurumlariniTazele();
        }

        Log($"{Metinler.UygulamaAdi} başlatıldı.");
    }

    private void PayloadListesiDoldur()
    {
        _dosyaListesi.Items.Clear();
        var dosyalar = PayloadInstaller.PayloadDosyalari();

        if (dosyalar.Count == 0)
        {
            DurumListesiDoldur(new[] { new DogrulamaBulgusu(BulguSeviyesi.Uyari, Metinler.PayloadBos) });
            Log(Metinler.PayloadBos);
            return;
        }

        foreach (var dosya in dosyalar)
            _dosyaListesi.Items.Add(dosya, isChecked: true);
    }

    // --- Hedef klasör ----------------------------------------------------------------------

    private void KlasorSec()
    {
        using var secici = new FolderBrowserDialog
        {
            Description = Metinler.KlasorSecPencere,
            UseDescriptionForTitle = true,
            SelectedPath = _hedefKlasor ?? string.Empty
        };

        if (secici.ShowDialog(this) == DialogResult.OK)
            HedefKlasoruAyarla(secici.SelectedPath);
    }

    private void HedefKlasoruAyarla(string klasor)
    {
        _hedefKlasor = klasor;
        _hedefKutusu.Text = klasor;
        Log($"Hedef klasör: {klasor}");

        AppIdiYerelBul();
        HedefiDogrula();
        DugmeDurumlariniTazele();
    }

    /// <summary>
    /// AppID'yi yalnızca yerel kaynaklardan arar: ağa çıkmaz, anında sonuçlanır.
    /// Klasör her değiştiğinde çalışır.
    /// </summary>
    private void AppIdiYerelBul()
    {
        _appId = null;
        if (_hedefKlasor is null) return;

        _appId = AppIdBulucu.YerelBul(_hedefKlasor);
        if (_appId is null)
        {
            Log("AppID yerel kaynaklarda bulunamadı.");
            return;
        }

        var kaynak = _appId.Kaynak == AppIdKaynagi.SteamAppIdDosyasi
            ? "steam_appid.txt"
            : "Steam kurulum kaydı";
        Log($"AppID bulundu: {_appId.AppId} ({kaynak})");
    }

    private void HedefiDogrula()
    {
        if (_hedefKlasor is null) return;

        var secilenler = SecilenDosyalar();
        var sonuc = TargetValidator.Dogrula(_hedefKlasor, secilenler);

        // Mimari kontrolü, ilk aday oyun exe'si üzerinden.
        var exeler = TargetValidator.OyunExeleri(_hedefKlasor);
        if (exeler.Count > 0)
        {
            var dllYollari = PayloadInstaller.PayloadDllleri()
                .Select(d => Path.Combine(PayloadInstaller.PayloadKlasoru(), d))
                .ToList();

            if (dllYollari.Count > 0)
            {
                var bulgu = TargetValidator.MimariKontrol(Path.Combine(_hedefKlasor, exeler[0]), dllYollari);
                if (bulgu is not null) sonuc.Bulgular.Add(bulgu);
            }
        }

        // Sahiplik sinyali — engelleyici değil.
        sonuc.Bulgular.Add(SahiplikKontrolu.Degerlendir(_hedefKlasor));

        // AppID durumu. Bulunamaması kurulumu engellemez; yalnızca unsteam.ini içindeki
        // real_app_id alanı 0 kalır ve kullanıcının elle girmesi gerekir.
        if (_appId is not null)
        {
            var etiket = _appId.Ad is null ? _appId.AppId : $"{_appId.AppId} — {_appId.Ad}";
            var kaynak = _appId.Kaynak switch
            {
                AppIdKaynagi.SteamAppIdDosyasi => "steam_appid.txt dosyasından",
                AppIdKaynagi.SteamKurulumKaydi => "Steam kurulum kaydından",
                _ => "Steam mağazasından, ada göre"
            };
            sonuc.Ekle(BulguSeviyesi.Bilgi, $"AppID: {etiket} ({kaynak}).");
        }
        else
        {
            sonuc.Ekle(BulguSeviyesi.Uyari,
                "AppID otomatik bulunamadı. Oyun Steam kütüphanesi dışına taşınmış olabilir. " +
                "\"AppID ara\" ile Steam mağazasında arayabilirsiniz.");
        }

        // Kurulum durumu
        sonuc.Ekle(BulguSeviyesi.Bilgi,
            PayloadInstaller.KuruluMu(_hedefKlasor) ? Metinler.ZatenKurulu : Metinler.KuruluDegil);

        DurumListesiDoldur(sonuc.Bulgular);
        _kurDugmesi.Enabled = sonuc.Gecerli && secilenler.Count > 0;
    }

    private List<string> SecilenDosyalar()
        => _dosyaListesi.CheckedItems.Cast<object>().Select(o => o.ToString()!).ToList();

    private void DugmeDurumlariniTazele()
    {
        var klasorVar = _hedefKlasor is not null && Directory.Exists(_hedefKlasor);

        _kaldirDugmesi.Enabled = klasorVar && PayloadInstaller.KuruluMu(_hedefKlasor!);

        // Ağ araması yalnızca bir klasör seçiliyken anlamlı.
        _appIdAraDugmesi.Enabled = klasorVar;
    }

    /// <summary>
    /// Yerel kaynaklar yetmediğinde Steam'in genel uygulama listesinden ada göre arar.
    ///
    /// Bu, uygulamanın ağa çıktığı TEK yerdir ve yalnızca kullanıcı düğmeye bastığında
    /// çalışır. İstek parametresizdir; hiçbir kullanıcı verisi gönderilmez. Sonuçlar tahmin
    /// olduğu için kendiliğinden uygulanmaz, kullanıcıya seçtirilir.
    /// </summary>
    private async Task AppIdiSteamdenAra()
    {
        if (_hedefKlasor is null) return;

        _appIdAraDugmesi.Enabled = false;
        var eskiDurum = _durumEtiketi.Text;
        _durumEtiketi.Text = Metinler.AppIdAraniyor;
        Cursor = Cursors.WaitCursor;

        try
        {
            var terimler = AppIdBulucu.AramaTerimleri(_hedefKlasor);
            if (terimler.Count == 0)
            {
                MessageBox.Show(Metinler.AppIdAdayYok, Metinler.Bilgi,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Log($"Steam mağazasında aranıyor: {string.Join(", ", terimler)}");
            var uygulamalar = await SteamMagazaAramasi.Ara(terimler).ConfigureAwait(true);
            Log($"Mağaza {uygulamalar.Count} sonuç döndürdü.");

            var adaylar = AppIdBulucu.AdaGoreAra(_hedefKlasor, uygulamalar);

            // Mağaza zaten terime göre filtrelemiş olabilir; yerel sıralama hiçbirini
            // beğenmezse ham sonuçları olduğu gibi göster.
            if (adaylar.Count == 0 && uygulamalar.Count > 0)
            {
                adaylar = uygulamalar
                    .Select(u => new AppIdSonucu(u.AppId, u.Ad, AppIdKaynagi.AdEslesmesi))
                    .Take(15)
                    .ToList();
            }
            if (adaylar.Count == 0)
            {
                Log("Ada göre eşleşen oyun bulunamadı.");
                MessageBox.Show(Metinler.AppIdAdayYok, Metinler.Bilgi,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var klasorAdi = new DirectoryInfo(_hedefKlasor).Name;
            using var secim = new AppIdSecimFormu(adaylar, klasorAdi);

            if (secim.ShowDialog(this) != DialogResult.OK || secim.SecilenAppId is null) return;

            var secilen = adaylar.First(a => a.AppId == secim.SecilenAppId);
            _appId = secilen;
            Log($"AppID seçildi: {secilen.AppId} — {secilen.Ad} (ada göre eşleşme).");

            HedefiDogrula();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            Log($"Steam listesi alınamadı: {ex.Message}");
            MessageBox.Show(Metinler.AppIdListeHatasi, Metinler.Hata,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _durumEtiketi.Text = eskiDurum;
            _appIdAraDugmesi.Enabled = _hedefKlasor is not null;
        }
    }

    /// <summary>
    /// Kurulumdan sonra unsteam.ini içindeki real_app_id alanını doldurur.
    ///
    /// Yalnızca alan boş ya da 0 ise yazılır — kullanıcının kendi girdiği bir değer varsa
    /// asla ezilmez. IniDocument kullanıldığı için dosyadaki yorumlar ve satır sırası korunur.
    /// </summary>
    private void AppIdiIniyeYaz()
    {
        if (_hedefKlasor is null || _appId is null) return;

        var yol = Path.Combine(_hedefKlasor, "unsteam.ini");
        if (!File.Exists(yol)) return;

        try
        {
            var belge = IniDocument.Yukle(yol);
            var mevcut = (belge.OkuVeya(AlanTanimlari.BolumGame, "real_app_id", string.Empty)).Trim();

            if (mevcut.Length > 0 && mevcut != "0")
            {
                Log($"unsteam.ini içinde real_app_id zaten {mevcut}, dokunulmadı.");
                return;
            }

            belge.Yaz(AlanTanimlari.BolumGame, "real_app_id", _appId.AppId);
            belge.Kaydet(yol);
            Log($"unsteam.ini içine real_app_id={_appId.AppId} yazıldı.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log($"unsteam.ini güncellenemedi: {ex.Message}");
        }
    }

    // --- Kurulum ---------------------------------------------------------------------------

    private void Kur()
    {
        if (_hedefKlasor is null) return;

        HedefiDogrula();
        var secilenler = SecilenDosyalar();
        if (secilenler.Count == 0)
        {
            MessageBox.Show("Kurulacak dosya seçilmedi.", Metinler.Uyari,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var dogrulama = TargetValidator.Dogrula(_hedefKlasor, secilenler);
        if (!dogrulama.Gecerli)
        {
            var hatalar = string.Join(Environment.NewLine + Environment.NewLine,
                dogrulama.Bulgular.Where(b => b.Seviye == BulguSeviyesi.Hata).Select(b => b.Mesaj));
            MessageBox.Show(hatalar, Metinler.Hata, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Yedek alınmadığı için üzerine yazma geri alınamaz. Hedefte hangi dosyaların
        // ezileceğini kuruluma başlamadan önce açıkça sor.
        var ezilecekler = PayloadInstaller.UzerineYazilacaklar(_hedefKlasor, secilenler);
        if (ezilecekler.Count > 0)
        {
            var onay = MessageBox.Show(
                string.Format(Metinler.UzerineYazmaOnayi,
                    string.Join(Environment.NewLine, ezilecekler.Select(a => "    • " + a))),
                Metinler.Uyari, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (onay != DialogResult.Yes)
            {
                DurumBildir("Kurulum iptal edildi.");
                return;
            }
        }

        Cursor = Cursors.WaitCursor;
        try
        {
            var sonuc = PayloadInstaller.Kur(_hedefKlasor, secilenler, gunluk: Log);

            if (sonuc.Basarili)
            {
                // INI kopyalandıktan sonra çalışmalı: şablon yeni kopyalandıysa real_app_id=0'dır.
                AppIdiIniyeYaz();

                DurumBildir(sonuc.Mesaj);
                MessageBox.Show(sonuc.Mesaj, Metinler.Bilgi, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DurumBildir($"Kurulum başarısız: {sonuc.Mesaj}");
                MessageBox.Show(sonuc.Mesaj, Metinler.Hata, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            Cursor = Cursors.Default;
            HedefiDogrula();
            DugmeDurumlariniTazele();
        }
    }

    private void KaldirmayiCalistir()
    {
        if (_hedefKlasor is null) return;

        var cevap = MessageBox.Show(Metinler.KaldirOnayi, Metinler.Uyari,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (cevap != DialogResult.Yes) return;

        Cursor = Cursors.WaitCursor;
        try
        {
            var sonuc = PayloadInstaller.Kaldir(_hedefKlasor, Log);
            DurumBildir(sonuc.Mesaj);

            MessageBox.Show(sonuc.Mesaj,
                sonuc.Basarili ? Metinler.Bilgi : Metinler.Hata,
                MessageBoxButtons.OK,
                sonuc.Basarili ? MessageBoxIcon.Information : MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            HedefiDogrula();
            DugmeDurumlariniTazele();
        }
    }

    // --- Log dışa aktarma ------------------------------------------------------------------

    private void LogaKaydet()
    {
        using var secici = new SaveFileDialog
        {
            Filter = "Log dosyası (*.log)|*.log|Metin dosyası (*.txt)|*.txt",
            FileName = $"rfyama-{DateTime.Now:yyyyMMdd-HHmmss}.log"
        };

        if (secici.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            File.WriteAllText(secici.FileName, _logKutusu.Text, IniDocument.BomsuzUtf8);
            DurumBildir($"Log kaydedildi: {secici.FileName}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Log kaydedilemedi: {ex.Message}", Metinler.Hata,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
