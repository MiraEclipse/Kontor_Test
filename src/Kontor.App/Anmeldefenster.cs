using Kontor.Core.Modell;
using Kontor.Core.Repositories;

namespace Kontor.App;

public sealed class Anmeldefenster : Form
{
    private readonly IMandantRepository _mandanten;
    private readonly ComboBox _mandantenliste = new();
    private readonly TextBox _benutzer = new();
    private readonly Label _meldung = new();
    private readonly Button _anmelden = new();

    public Anmeldefenster(IMandantRepository mandanten)
    {
        _mandanten = mandanten;

        Text = "KONTOR – Anmeldung";
        Font = new Font("MS Sans Serif", 8.25f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(344, 176);

        var titel = new Label
        {
            Text = "KONTOR  Kaufmännische Verwaltung",
            Location = new Point(12, 12),
            Size = new Size(320, 16)
        };

        var strich = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, 32),
            Size = new Size(320, 2)
        };

        var mandantBeschriftung = new Label
        {
            Text = "&Mandant:",
            Location = new Point(12, 48),
            Size = new Size(72, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _mandantenliste.DropDownStyle = ComboBoxStyle.DropDownList;
        _mandantenliste.Location = new Point(88, 44);
        _mandantenliste.Size = new Size(244, 21);
        _mandantenliste.DisplayMember = nameof(Mandanteintrag.Anzeige);

        var benutzerBeschriftung = new Label
        {
            Text = "&Benutzer:",
            Location = new Point(12, 76),
            Size = new Size(72, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _benutzer.Location = new Point(88, 72);
        _benutzer.Size = new Size(244, 20);
        _benutzer.MaxLength = 20;
        _benutzer.Text = Environment.UserName;

        _meldung.Location = new Point(12, 136);
        _meldung.Size = new Size(320, 32);
        _meldung.ForeColor = Color.DarkRed;

        _anmelden.Text = "&Anmelden";
        _anmelden.Location = new Point(172, 104);
        _anmelden.Size = new Size(76, 24);
        _anmelden.Click += (_, _) => Anmelden();

        var abbrechen = new Button
        {
            Text = "A&bbrechen",
            Location = new Point(256, 104),
            Size = new Size(76, 24),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(titel);
        Controls.Add(strich);
        Controls.Add(mandantBeschriftung);
        Controls.Add(_mandantenliste);
        Controls.Add(benutzerBeschriftung);
        Controls.Add(_benutzer);
        Controls.Add(_meldung);
        Controls.Add(_anmelden);
        Controls.Add(abbrechen);

        AcceptButton = _anmelden;
        CancelButton = abbrechen;

        LadeMandanten();
    }

    public Sitzung? Sitzung { get; private set; }

    private void LadeMandanten()
    {
        var mandanten = _mandanten.Alle();

        foreach (var mandant in mandanten)
        {
            _mandantenliste.Items.Add(new Mandanteintrag(mandant));
        }

        if (_mandantenliste.Items.Count == 0)
        {
            _meldung.Text = "Kein Mandant vorhanden. Die Datenbank ist nicht eingerichtet.";
            _anmelden.Enabled = false;
            return;
        }

        _mandantenliste.SelectedIndex = 0;
    }

    private void Anmelden()
    {
        if (_mandantenliste.SelectedItem is not Mandanteintrag eintrag)
        {
            _meldung.Text = "Bitte einen Mandanten auswählen.";
            return;
        }

        var benutzer = _benutzer.Text.Trim();
        if (benutzer.Length == 0)
        {
            _meldung.Text = "Bitte einen Benutzernamen eingeben.";
            _benutzer.Focus();
            return;
        }

        Sitzung = new Sitzung(eintrag.Mandant, benutzer);
        DialogResult = DialogResult.OK;
        Close();
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
