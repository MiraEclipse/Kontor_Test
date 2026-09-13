using Kontor.Core.Fachlogik;

namespace Kontor.App.Module;

public sealed class KennwortZuruecksetzenDialog : Form
{
    private readonly TextBox _kennwort = new() { PasswordChar = '●' };
    private readonly TextBox _kennwortWiederholen = new() { PasswordChar = '●' };
    private readonly Label _meldung = new();

    public KennwortZuruecksetzenDialog(string anmeldename)
    {
        Text = $"Kennwort zurücksetzen – {anmeldename}";
        Font = new Font("MS Sans Serif", 8.25f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(320, 148);

        Feld("&Neues Kennwort:", _kennwort, 12);
        Feld("Kennwort &wiederholen:", _kennwortWiederholen, 40);

        _meldung.Location = new Point(12, 68);
        _meldung.Size = new Size(296, 40);
        _meldung.ForeColor = Color.DarkRed;

        var setzen = new Button { Text = "&Setzen", Location = new Point(156, 108), Size = new Size(76, 24) };
        setzen.Click += (_, _) => Setzen();

        var abbrechen = new Button
        {
            Text = "A&bbrechen", Location = new Point(236, 108), Size = new Size(76, 24), DialogResult = DialogResult.Cancel
        };

        Controls.Add(_meldung);
        Controls.Add(setzen);
        Controls.Add(abbrechen);

        AcceptButton = setzen;
        CancelButton = abbrechen;
    }

    public byte[]? Hash { get; private set; }
    public byte[]? Salz { get; private set; }
    public int Durchlaeufe { get; private set; }

    private void Setzen()
    {
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

        Salz = Kennwort.NeuesSalz();
        Durchlaeufe = Kennwort.StandardDurchlaeufe;
        Hash = Kennwort.Hash(_kennwort.Text, Salz, Durchlaeufe);

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
