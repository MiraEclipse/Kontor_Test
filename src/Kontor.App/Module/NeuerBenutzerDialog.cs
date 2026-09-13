using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class NeuerBenutzerDialog : Form
{
    private readonly TextBox _anmeldename = new();
    private readonly TextBox _anzeigename = new();
    private readonly TextBox _kennwort = new() { PasswordChar = '●' };
    private readonly TextBox _kennwortWiederholen = new() { PasswordChar = '●' };
    private readonly Label _meldung = new();

    public NeuerBenutzerDialog()
    {
        Text = "Neuer Benutzer";
        Font = new Font("MS Sans Serif", 8.25f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(320, 220);

        Feld("&Anmeldename:", _anmeldename, 12);
        Feld("An&zeigename:", _anzeigename, 40);
        Feld("&Kennwort:", _kennwort, 68);
        Feld("Kennwort &wiederholen:", _kennwortWiederholen, 96);

        _meldung.Location = new Point(12, 128);
        _meldung.Size = new Size(296, 40);
        _meldung.ForeColor = Color.DarkRed;

        var anlegen = new Button { Text = "&Anlegen", Location = new Point(156, 180), Size = new Size(76, 24) };
        anlegen.Click += (_, _) => Anlegen();

        var abbrechen = new Button
        {
            Text = "A&bbrechen", Location = new Point(236, 180), Size = new Size(76, 24), DialogResult = DialogResult.Cancel
        };

        Controls.Add(_meldung);
        Controls.Add(anlegen);
        Controls.Add(abbrechen);

        AcceptButton = anlegen;
        CancelButton = abbrechen;
    }

    public Benutzer? Ergebnis { get; private set; }

    private void Anlegen()
    {
        var anmeldename = _anmeldename.Text.Trim();
        var anzeigename = _anzeigename.Text.Trim();

        if (anmeldename.Length == 0)
        {
            _meldung.Text = "Bitte einen Anmeldenamen eingeben.";
            return;
        }

        if (anzeigename.Length == 0)
        {
            anzeigename = anmeldename;
        }

        if (_kennwort.Text.Length == 0)
        {
            _meldung.Text = "Bitte ein Kennwort eingeben.";
            return;
        }

        if (_kennwort.Text != _kennwortWiederholen.Text)
        {
            _meldung.Text = "Die beiden Kennwörter stimmen nicht überein.";
            return;
        }

        var salz = Kennwort.NeuesSalz();
        var durchlaeufe = Kennwort.StandardDurchlaeufe;

        Ergebnis = new Benutzer
        {
            Anmeldename = anmeldename,
            Anzeigename = anzeigename,
            KennwortHash = Kennwort.Hash(_kennwort.Text, salz, durchlaeufe),
            Salz = salz,
            Durchlaeufe = durchlaeufe
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private void Feld(string beschriftung, TextBox feld, int oben)
    {
        var text = new Label
        {
            Text = beschriftung, Location = new Point(12, oben + 3), Size = new Size(120, 16), TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(136, oben);
        feld.Size = new Size(172, 20);

        Controls.Add(text);
        Controls.Add(feld);
    }
}
