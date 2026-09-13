using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Core.Repositories;

namespace Kontor.App;

// Läuft genau einmal, solange noch kein Benutzer existiert: Anmeldename, Kennwort zweifach, Haushalt.
// Dieser erste Benutzer bekommt Systemrechte - kein fest eingebautes Standardkennwort, kein Hintertürchen.
public sealed class Ersteinrichtungsfenster : Form
{
    private readonly IBenutzerRepository _benutzer;
    private readonly IMandantRepository _mandanten;

    private readonly TextBox _anmeldename = new();
    private readonly TextBox _kennwort = new() { PasswordChar = '●' };
    private readonly TextBox _kennwortWiederholen = new() { PasswordChar = '●' };
    private readonly ComboBox _haushaltsliste = new();
    private readonly Label _meldung = new();
    private readonly Button _einrichten = new();

    public Ersteinrichtungsfenster(IBenutzerRepository benutzer, IMandantRepository mandanten)
    {
        _benutzer = benutzer;
        _mandanten = mandanten;

        Text = "KONTOR – Ersteinrichtung";
        Font = new Font("MS Sans Serif", 8.25f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(360, 264);

        var titel = new Label
        {
            Text = "Willkommen bei KONTOR. Richten Sie den ersten Zugang ein.",
            Location = new Point(12, 12),
            Size = new Size(336, 32)
        };

        var strich = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, 48),
            Size = new Size(336, 2)
        };

        Feld("&Anmeldename:", _anmeldename, 60);
        Feld("&Kennwort:", _kennwort, 88);
        Feld("Kennwort &wiederholen:", _kennwortWiederholen, 116);

        var haushaltBeschriftung = new Label
        {
            Text = "&Haushalt:",
            Location = new Point(12, 148),
            Size = new Size(120, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _haushaltsliste.DropDownStyle = ComboBoxStyle.DropDownList;
        _haushaltsliste.Location = new Point(136, 144);
        _haushaltsliste.Size = new Size(212, 21);
        _haushaltsliste.DisplayMember = nameof(Mandanteintrag.Anzeige);

        _meldung.Location = new Point(12, 176);
        _meldung.Size = new Size(336, 48);
        _meldung.ForeColor = Color.DarkRed;

        _einrichten.Text = "&Einrichten";
        _einrichten.Location = new Point(196, 228);
        _einrichten.Size = new Size(76, 24);
        _einrichten.Click += (_, _) => Einrichten();

        var abbrechen = new Button
        {
            Text = "A&bbrechen",
            Location = new Point(276, 228),
            Size = new Size(76, 24),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(titel);
        Controls.Add(strich);
        Controls.Add(haushaltBeschriftung);
        Controls.Add(_haushaltsliste);
        Controls.Add(_meldung);
        Controls.Add(_einrichten);
        Controls.Add(abbrechen);

        AcceptButton = _einrichten;
        CancelButton = abbrechen;

        LadeHaushalte();
    }

    public Sitzung? Sitzung { get; private set; }

    private void LadeHaushalte()
    {
        foreach (var mandant in _mandanten.Alle())
        {
            _haushaltsliste.Items.Add(new Mandanteintrag(mandant));
        }

        if (_haushaltsliste.Items.Count > 0)
        {
            _haushaltsliste.SelectedIndex = 0;
        }
    }

    private void Einrichten()
    {
        var anmeldename = _anmeldename.Text.Trim();

        if (anmeldename.Length == 0)
        {
            _meldung.Text = "Bitte einen Anmeldenamen eingeben.";
            _anmeldename.Focus();
            return;
        }

        if (_kennwort.Text.Length == 0)
        {
            _meldung.Text = "Bitte ein Kennwort eingeben.";
            _kennwort.Focus();
            return;
        }

        if (_kennwort.Text != _kennwortWiederholen.Text)
        {
            _meldung.Text = "Die beiden Kennwörter stimmen nicht überein.";
            _kennwortWiederholen.Focus();
            return;
        }

        if (_haushaltsliste.SelectedItem is not Mandanteintrag eintrag)
        {
            _meldung.Text = "Bitte einen Haushalt auswählen.";
            return;
        }

        var salz = Kennwort.NeuesSalz();
        var durchlaeufe = Kennwort.StandardDurchlaeufe;
        var hash = Kennwort.Hash(_kennwort.Text, salz, durchlaeufe);

        var benutzer = new Benutzer
        {
            Anmeldename = anmeldename,
            Anzeigename = anmeldename,
            KennwortHash = hash,
            Salz = salz,
            Durchlaeufe = durchlaeufe,
            Systemrechte = true
        };

        _benutzer.Anlegen(benutzer);

        Sitzung = new Sitzung(eintrag.Mandant, benutzer);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void Feld(string beschriftung, TextBox feld, int oben)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(12, oben + 3),
            Size = new Size(120, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(136, oben);
        feld.Size = new Size(212, 20);

        Controls.Add(text);
        Controls.Add(feld);
    }

    private sealed class Mandanteintrag
    {
        public Mandanteintrag(Mandant mandant)
        {
            Mandant = mandant;
        }

        public Mandant Mandant { get; }

        public string Anzeige => $"{Mandant.MandantNr:0000}  {Mandant.Name}";

        public override string ToString() => Anzeige;
    }
}
