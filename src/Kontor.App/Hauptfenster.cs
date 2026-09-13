using Kontor.App.Rahmen;
using Kontor.Core.Modell;

namespace Kontor.App;

public sealed class Hauptfenster : Form, IMeldungsanzeige
{
    private readonly Sitzung _sitzung;
    private readonly Modulverzeichnis _verzeichnis;
    private readonly Modulkontext _kontext;
    private readonly IReadOnlyList<Moduleintrag> _sichtbareEintraege;

    private readonly MenuStrip _menue = new();
    private readonly ToolStrip _leiste = new();
    private readonly ToolStripTextBox _kommando = new();
    private readonly StatusStrip _statusleiste = new();
    private readonly ToolStripStatusLabel _statusCode = new();
    private readonly ToolStripStatusLabel _statusMeldung = new();
    private readonly ToolStripStatusLabel _statusSitzung = new();
    private readonly ToolStripStatusLabel _statusUhr = new();
    private readonly TreeView _baum = new();
    private readonly System.Windows.Forms.Timer _uhr = new();
    private readonly System.Windows.Forms.Timer _rechteTimer = new();

    public Hauptfenster(Sitzung sitzung, Dienste dienste, Modulverzeichnis verzeichnis)
    {
        _sitzung = sitzung;
        _verzeichnis = verzeichnis;
        _kontext = new Modulkontext(sitzung, dienste, this, verzeichnis);
        _sichtbareEintraege = _verzeichnis.Sichtbare(dienste.Zugriff, sitzung.MandantNr);

        Text = $"KONTOR – {sitzung.Mandant.Name} – {sitzung.Benutzer.Anzeigename}";
        Font = new Font("MS Sans Serif", 8.25f);
        ClientSize = new Size(1000, 640);
        StartPosition = FormStartPosition.CenterScreen;
        IsMdiContainer = true;

        BaueMenuebaum();
        BaueStatusleiste();
        BaueKommandoleiste();
        BaueMenue();

        MainMenuStrip = _menue;
        MdiChildActivate += (_, _) => AktualisiereTransaktionsfeld();

        _uhr.Interval = 1000;
        _uhr.Tick += (_, _) => _statusUhr.Text = DateTime.Now.ToString("HH:mm:ss");
        _uhr.Start();

        // Prüft jede Minute, ob ein wirksames Recht für ein offenes Fenster ausgelaufen ist - eine
        // Befristung, die erst bei der nächsten Anmeldung greift, ist keine.
        _rechteTimer.Interval = 60_000;
        _rechteTimer.Tick += (_, _) => PruefeAbgelaufeneRechte();
        _rechteTimer.Start();

        Hinweis("Transaktionscode eingeben oder Modul im Menübaum wählen.");
    }

    public void Erfolg(string meldung) => ZeigeMeldung(meldung, SystemColors.ControlText);

    public void Hinweis(string meldung) => ZeigeMeldung(meldung, SystemColors.ControlText);

    public void Fehler(string meldung) => ZeigeMeldung(meldung, Color.DarkRed);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _uhr.Stop();
            _uhr.Dispose();
            _rechteTimer.Stop();
            _rechteTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ZeigeMeldung(string meldung, Color farbe)
    {
        _statusMeldung.ForeColor = farbe;
        _statusMeldung.Text = meldung;
    }

    private void BaueMenue()
    {
        _menue.RenderMode = ToolStripRenderMode.System;
        _menue.Dock = DockStyle.Top;

        var datei = new ToolStripMenuItem("&Datei");
        var beenden = new ToolStripMenuItem("&Beenden", null, (_, _) => Close())
        {
            ShortcutKeys = Keys.Alt | Keys.F4,
            ShowShortcutKeys = false
        };
        datei.DropDownItems.Add(beenden);

        var bearbeiten = new ToolStripMenuItem("&Bearbeiten");
        bearbeiten.DropDownItems.Add(Gesperrt("&Ausschneiden"));
        bearbeiten.DropDownItems.Add(Gesperrt("&Kopieren"));
        bearbeiten.DropDownItems.Add(Gesperrt("&Einfügen"));

        var springen = new ToolStripMenuItem("&Springen");
        var ersteGruppe = true;
        foreach (var gruppe in Gruppen())
        {
            if (!ersteGruppe)
            {
                springen.DropDownItems.Add(new ToolStripSeparator());
            }

            foreach (var eintrag in InGruppe(gruppe))
            {
                var moduleintrag = eintrag;
                springen.DropDownItems.Add(new ToolStripMenuItem(
                    moduleintrag.Anzeige, null, (_, _) => OeffneModul(moduleintrag)));
            }

            ersteGruppe = false;
        }

        var extras = new ToolStripMenuItem("E&xtras");
        extras.DropDownItems.Add(Gesperrt("&Einstellungen…"));
        extras.DropDownItems.Add(Gesperrt("&Nummernkreise…"));

        var hilfe = new ToolStripMenuItem("&Hilfe");
        hilfe.DropDownItems.Add(Gesperrt("&Inhalt"));
        hilfe.DropDownItems.Add(new ToolStripSeparator());
        hilfe.DropDownItems.Add(new ToolStripMenuItem("Ü&ber KONTOR", null,
            (_, _) => Hinweis($"KONTOR – Stand {DateTime.Now.Year}, angemeldet als {_sitzung.Benutzer.Anzeigename} " +
                              $"im Haushalt {_sitzung.MandantNr:0000}.")));

        _menue.Items.Add(datei);
        _menue.Items.Add(bearbeiten);
        _menue.Items.Add(springen);
        _menue.Items.Add(extras);
        _menue.Items.Add(hilfe);

        Controls.Add(_menue);
    }

    private void BaueKommandoleiste()
    {
        _leiste.RenderMode = ToolStripRenderMode.System;
        _leiste.Dock = DockStyle.Top;
        _leiste.GripStyle = ToolStripGripStyle.Hidden;

        var beschriftung = new ToolStripLabel("Kommando:");

        _kommando.Font = new Font("Courier New", 9f);
        _kommando.Size = new Size(120, 21);
        _kommando.MaxLength = 20;
        _kommando.BorderStyle = BorderStyle.Fixed3D;
        _kommando.KeyDown += KommandoTaste;

        _leiste.Items.Add(beschriftung);
        _leiste.Items.Add(_kommando);

        Controls.Add(_leiste);
    }

    private void BaueMenuebaum()
    {
        var teiler = new Splitter
        {
            Dock = DockStyle.Left,
            Width = 4,
            MinSize = 140,
            MinExtra = 320
        };

        var rahmen = new Panel
        {
            Dock = DockStyle.Left,
            Width = 216
        };

        _baum.Dock = DockStyle.Fill;
        _baum.BorderStyle = BorderStyle.Fixed3D;
        _baum.Font = new Font("MS Sans Serif", 8.25f);
        _baum.HideSelection = false;
        _baum.ShowLines = true;
        _baum.ShowPlusMinus = true;
        _baum.ShowRootLines = true;
        _baum.NodeMouseClick += BaumKlick;

        foreach (var gruppe in Gruppen())
        {
            var gruppenknoten = _baum.Nodes.Add(gruppe);

            foreach (var eintrag in InGruppe(gruppe))
            {
                var knoten = gruppenknoten.Nodes.Add(eintrag.Anzeige);
                knoten.Tag = eintrag;
            }
        }

        _baum.ExpandAll();

        rahmen.Controls.Add(_baum);

        Controls.Add(teiler);
        Controls.Add(rahmen);
    }

    private void BaueStatusleiste()
    {
        _statusleiste.RenderMode = ToolStripRenderMode.System;
        _statusleiste.Dock = DockStyle.Bottom;
        _statusleiste.SizingGrip = true;

        _statusCode.AutoSize = false;
        _statusCode.Width = 56;
        _statusCode.TextAlign = ContentAlignment.MiddleLeft;
        _statusCode.BorderSides = ToolStripStatusLabelBorderSides.All;
        _statusCode.BorderStyle = Border3DStyle.SunkenOuter;

        _statusMeldung.Spring = true;
        _statusMeldung.TextAlign = ContentAlignment.MiddleLeft;
        _statusMeldung.BorderSides = ToolStripStatusLabelBorderSides.All;
        _statusMeldung.BorderStyle = Border3DStyle.SunkenOuter;

        _statusSitzung.AutoSize = true;
        _statusSitzung.TextAlign = ContentAlignment.MiddleLeft;
        _statusSitzung.BorderSides = ToolStripStatusLabelBorderSides.All;
        _statusSitzung.BorderStyle = Border3DStyle.SunkenOuter;
        _statusSitzung.Text = $"{_sitzung.Mandant.MandantNr:0000} {_sitzung.Mandant.Name} | {_sitzung.Benutzer.Anzeigename}";

        _statusUhr.AutoSize = false;
        _statusUhr.Width = 64;
        _statusUhr.TextAlign = ContentAlignment.MiddleCenter;
        _statusUhr.BorderSides = ToolStripStatusLabelBorderSides.All;
        _statusUhr.BorderStyle = Border3DStyle.SunkenOuter;
        _statusUhr.Text = DateTime.Now.ToString("HH:mm:ss");

        _statusleiste.Items.Add(_statusCode);
        _statusleiste.Items.Add(_statusMeldung);
        _statusleiste.Items.Add(_statusSitzung);
        _statusleiste.Items.Add(_statusUhr);

        Controls.Add(_statusleiste);
    }

    private static ToolStripMenuItem Gesperrt(string text) => new(text) { Enabled = false };

    private IReadOnlyList<string> Gruppen()
    {
        var gruppen = new List<string>();

        foreach (var eintrag in _sichtbareEintraege)
        {
            if (!gruppen.Contains(eintrag.Gruppe))
            {
                gruppen.Add(eintrag.Gruppe);
            }
        }

        return gruppen;
    }

    private IReadOnlyList<Moduleintrag> InGruppe(string gruppe)
    {
        var eintraege = new List<Moduleintrag>();

        foreach (var eintrag in _sichtbareEintraege)
        {
            if (eintrag.Gruppe == gruppe)
            {
                eintraege.Add(eintrag);
            }
        }

        return eintraege;
    }

    private void KommandoTaste(object? absender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        e.Handled = true;

        FuehreKommandoAus(_kommando.Text);
        _kommando.Clear();
    }

    private void FuehreKommandoAus(string eingabe)
    {
        var transaktionscode = Kommandozeile.Normalisiere(eingabe);

        if (transaktionscode.Length == 0)
        {
            Hinweis("Kein Transaktionscode eingegeben.");
            return;
        }

        if (Kommandozeile.IstBeenden(transaktionscode))
        {
            Close();
            return;
        }

        var eintrag = _verzeichnis.Finde(transaktionscode);

        if (eintrag is null)
        {
            Fehler($"Transaktionscode {transaktionscode} ist nicht bekannt.");
            return;
        }

        OeffneModul(eintrag);
    }

    private void BaumKlick(object? absender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node?.Tag is Moduleintrag eintrag)
        {
            OeffneModul(eintrag);
        }
    }

    private void OeffneModul(Moduleintrag eintrag)
    {
        // Ein Transaktionscode ohne Recht führt zu einer Meldung in der Statuszeile, nicht zu einem
        // leeren Fenster - auch wenn er (etwa über das Kommandofeld) den Baum umgeht.
        if (!eintrag.IstSichtbarFuer(_kontext.Dienste.Zugriff, _kontext.MandantNr))
        {
            Fehler($"Kein Zugriff auf {eintrag.Transaktionscode} {eintrag.Bezeichnung}.");
            return;
        }

        foreach (var kind in MdiChildren)
        {
            if (kind is ModulFenster offen && offen.Transaktionscode == eintrag.Transaktionscode)
            {
                if (offen.WindowState == FormWindowState.Minimized)
                {
                    offen.WindowState = FormWindowState.Normal;
                }

                offen.Activate();
                Hinweis($"{eintrag.Anzeige} ist bereits geöffnet.");
                return;
            }
        }

        var fenster = eintrag.Erzeuge(_kontext);
        fenster.MdiParent = this;
        fenster.Location = NaechsterPlatz();
        fenster.Show();

        AktualisiereTransaktionsfeld();
    }

    private Point NaechsterPlatz()
    {
        var versatz = 24 * (MdiChildren.Length % 6);
        return new Point(8 + versatz, 8 + versatz);
    }

    private void AktualisiereTransaktionsfeld() =>
        _statusCode.Text = ActiveMdiChild is ModulFenster modul ? modul.Transaktionscode : "";

    private void PruefeAbgelaufeneRechte()
    {
        foreach (var kind in MdiChildren.ToArray())
        {
            if (kind is not ModulFenster modul)
            {
                continue;
            }

            if (modul.EffektiveStufe is not null)
            {
                continue;
            }

            if (modul.HatUngesicherteEingaben)
            {
                modul.SichernVersuchen();
            }

            var bezeichnung = $"{modul.Transaktionscode} {modul.Bezeichnung}";
            modul.Close();
            Fehler($"Recht für {bezeichnung} ist ausgelaufen - Fenster geschlossen.");
        }
    }
}
