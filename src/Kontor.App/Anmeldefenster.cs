using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Core.Repositories;

namespace Kontor.App;

public sealed class Anmeldefenster : Form
{
    private readonly IBenutzerRepository _benutzer;
    private readonly IRechtRepository _rechte;
    private readonly IMandantRepository _mandanten;
    private readonly IAnmeldeprotokollRepository _anmeldeprotokoll;

    private readonly TextBox _anmeldename = new();
    private readonly TextBox _kennwort = new() { PasswordChar = '●' };
    private readonly ComboBox _haushaltsliste = new();
    private readonly Label _meldung = new();
    private readonly Button _anmelden = new();

    public Anmeldefenster(
        IBenutzerRepository benutzer, IRechtRepository rechte, IMandantRepository mandanten, IAnmeldeprotokollRepository anmeldeprotokoll)
    {
        _benutzer = benutzer;
        _rechte = rechte;
        _mandanten = mandanten;
        _anmeldeprotokoll = anmeldeprotokoll;

        Text = "KONTOR – Anmeldung";
        Font = new Font("MS Sans Serif", 8.25f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(344, 208);

        var titel = new Label
        {
            Text = "KONTOR  Private Haushaltsführung",
            Location = new Point(12, 12),
            Size = new Size(320, 16)
        };

        var strich = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, 32),
            Size = new Size(320, 2)
        };

        var anmeldenameBeschriftung = new Label
        {
            Text = "&Anmeldename:",
            Location = new Point(12, 48),
            Size = new Size(96, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _anmeldename.Location = new Point(112, 44);
        _anmeldename.Size = new Size(220, 20);
        _anmeldename.MaxLength = 40;
        _anmeldename.Leave += (_, _) => LadeHaushalte();

        var kennwortBeschriftung = new Label
        {
            Text = "&Kennwort:",
            Location = new Point(12, 76),
            Size = new Size(96, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _kennwort.Location = new Point(112, 72);
        _kennwort.Size = new Size(220, 20);

        var haushaltBeschriftung = new Label
        {
            Text = "&Haushalt:",
            Location = new Point(12, 104),
            Size = new Size(96, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _haushaltsliste.DropDownStyle = ComboBoxStyle.DropDownList;
        _haushaltsliste.Location = new Point(112, 100);
        _haushaltsliste.Size = new Size(220, 21);
        _haushaltsliste.DisplayMember = nameof(Mandanteintrag.Anzeige);

        _meldung.Location = new Point(12, 168);
        _meldung.Size = new Size(320, 32);
        _meldung.ForeColor = Color.DarkRed;

        _anmelden.Text = "&Anmelden";
        _anmelden.Location = new Point(172, 132);
        _anmelden.Size = new Size(76, 24);
        _anmelden.Click += (_, _) => Anmelden();

        var abbrechen = new Button
        {
            Text = "A&bbrechen",
            Location = new Point(256, 132),
            Size = new Size(76, 24),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(titel);
        Controls.Add(strich);
        Controls.Add(anmeldenameBeschriftung);
        Controls.Add(_anmeldename);
        Controls.Add(kennwortBeschriftung);
        Controls.Add(_kennwort);
        Controls.Add(haushaltBeschriftung);
        Controls.Add(_haushaltsliste);
        Controls.Add(_meldung);
        Controls.Add(_anmelden);
        Controls.Add(abbrechen);

        AcceptButton = _anmelden;
        CancelButton = abbrechen;
    }

    public Sitzung? Sitzung { get; private set; }

    // Der Haushalt wird auf die beschränkt, für die (zum jetzigen Zeitpunkt wirksam) Rechte vorliegen -
    // bei Systemrechten sind das alle. Wird nachgeladen, sobald ein bekannter Anmeldename eingegeben wurde.
    private void LadeHaushalte()
    {
        _haushaltsliste.Items.Clear();

        var anmeldename = _anmeldename.Text.Trim();
        if (anmeldename.Length == 0)
        {
            return;
        }

        var benutzer = _benutzer.LadeMitAnmeldename(anmeldename);
        if (benutzer is null)
        {
            return;
        }

        var alleHaushalte = _mandanten.Alle();

        if (benutzer.Systemrechte)
        {
            foreach (var mandant in alleHaushalte)
            {
                _haushaltsliste.Items.Add(new Mandanteintrag(mandant));
            }
        }
        else
        {
            var rechte = _rechte.Liste(benutzer.BenutzerId);
            var jetzt = DateTime.Now;

            foreach (var mandant in alleHaushalte)
            {
                if (HatIrgendeinWirksamesRecht(rechte, mandant.MandantNr, jetzt))
                {
                    _haushaltsliste.Items.Add(new Mandanteintrag(mandant));
                }
            }
        }

        if (_haushaltsliste.Items.Count > 0)
        {
            _haushaltsliste.SelectedIndex = 0;
        }
    }

    private static bool HatIrgendeinWirksamesRecht(IReadOnlyList<Recht> rechte, int mandantNr, DateTime jetzt)
    {
        foreach (var recht in rechte)
        {
            if (recht.MandantNr != mandantNr)
            {
                continue;
            }

            if (recht.GueltigVon > jetzt)
            {
                continue;
            }

            if (recht.GueltigBis is { } gueltigBis && gueltigBis <= jetzt)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void Anmelden()
    {
        var anmeldename = _anmeldename.Text.Trim();

        if (anmeldename.Length == 0)
        {
            _meldung.Text = "Bitte einen Anmeldenamen eingeben.";
            _anmeldename.Focus();
            return;
        }

        Verzoegern(anmeldename);

        var benutzer = _benutzer.LadeMitAnmeldename(anmeldename);
        var ergebnis = Pruefen(benutzer);
        _anmeldeprotokoll.Erfassen(anmeldename, ergebnis);

        if (ergebnis != AnmeldeErgebnis.Erfolg)
        {
            _meldung.Text = AnmeldeErgebnisse.Bezeichnung(ergebnis) + ".";
            _kennwort.Clear();
            _kennwort.Focus();
            return;
        }

        if (_haushaltsliste.SelectedItem is not Mandanteintrag eintrag)
        {
            _meldung.Text = "Bitte einen Haushalt auswählen.";
            return;
        }

        _benutzer.LetzteAnmeldungSetzen(benutzer!.BenutzerId, DateTime.Now);

        Sitzung = new Sitzung(eintrag.Mandant, benutzer);
        DialogResult = DialogResult.OK;
        Close();
    }

    private AnmeldeErgebnis Pruefen(Benutzer? benutzer)
    {
        if (benutzer is null)
        {
            return AnmeldeErgebnis.UnbekannterBenutzer;
        }

        if (benutzer.Gesperrt)
        {
            return AnmeldeErgebnis.Gesperrt;
        }

        if (!Kennwort.Pruefe(_kennwort.Text, benutzer.KennwortHash, benutzer.Salz, benutzer.Durchlaeufe))
        {
            return AnmeldeErgebnis.FalschesKennwort;
        }

        return AnmeldeErgebnis.Erfolg;
    }

    // Keine Kontosperre bei Fehlversuchen, sondern eine mit jedem Versuch wachsende Verzögerung -
    // eine Sperre führt in einer Familie nur dazu, dass man sich gegenseitig aussperrt.
    private void Verzoegern(string anmeldename)
    {
        var fehlversuche = _anmeldeprotokoll.FehlversucheInFolge(anmeldename);
        var wartezeit = Anmeldeverzoegerung.Fuer(fehlversuche);

        if (wartezeit <= TimeSpan.Zero)
        {
            return;
        }

        _meldung.ForeColor = Color.DarkRed;
        _meldung.Text = $"Zu viele Fehlversuche. Bitte {wartezeit.TotalSeconds:0} Sekunden warten…";
        _meldung.Refresh();

        var vorherigerCursor = Cursor;
        Cursor = Cursors.WaitCursor;
        try
        {
            Thread.Sleep(wartezeit);
        }
        finally
        {
            Cursor = vorherigerCursor;
        }
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
