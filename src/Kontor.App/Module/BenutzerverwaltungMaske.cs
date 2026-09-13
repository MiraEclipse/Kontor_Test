using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class BenutzerverwaltungMaske : ModulFenster
{
    private readonly ListView BenutzerListe = new();

    // Stammdaten-Reiter
    private readonly TextBox AnmeldenameFeld = new();
    private readonly TextBox AnzeigenameFeld = new();
    private readonly TextBox SystemrechteFeld = new();
    private readonly TextBox AngelegtFeld = new();
    private readonly TextBox LetzteAnmeldungFeld = new();
    private readonly Button AnlegenKnopf = new() { Text = "&Anlegen…" };
    private readonly Button KennwortZuruecksetzenKnopf = new() { Text = "&Kennwort zurücksetzen…", Enabled = false };
    private readonly Button SperrenKnopf = new() { Text = "&Sperren", Enabled = false };

    // Rechte-Reiter
    private readonly ListView RechteListe = new();
    private readonly ComboBox BereichFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox StufeFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox DauerFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker EigenesEndeFeld = new() { Format = DateTimePickerFormat.Short, Enabled = false };
    private readonly Button BestaetigenKnopf = new() { Text = "&Bestätigen", Enabled = false };
    private readonly Button EntziehenKnopf = new() { Text = "Jetzt &entziehen", Enabled = false };

    private List<Benutzer> _benutzer = new();
    private Benutzer? _ausgewaehlterBenutzer;
    private List<Recht> _rechteDesBenutzers = new();

    private static readonly (string Anzeige, TimeSpan? Dauer, bool EigenesEnde)[] Dauervorgaben =
    {
        ("2 Stunden", TimeSpan.FromHours(2), false),
        ("8 Stunden", TimeSpan.FromHours(8), false),
        ("Bis Mitternacht", null, false),
        ("7 Tage", TimeSpan.FromDays(7), false),
        ("Unbefristet", null, false),
        ("Eigenes Enddatum", null, true)
    };

    public BenutzerverwaltungMaske(Modulkontext kontext) : base("K08", "Benutzerverwaltung", kontext)
    {
        ClientSize = new Size(760, 480);

        BenutzerListe.Location = new Point(12, 12);
        BenutzerListe.Size = new Size(220, 440);
        BenutzerListe.View = View.Details;
        BenutzerListe.FullRowSelect = true;
        BenutzerListe.GridLines = true;
        BenutzerListe.MultiSelect = false;
        BenutzerListe.HideSelection = false;
        BenutzerListe.Font = new Font("Courier New", 9f);
        BenutzerListe.Columns.Add("Anmeldename", 110);
        BenutzerListe.Columns.Add("Anzeigename", 110);
        BenutzerListe.Columns.Add("Sys.", 40);
        BenutzerListe.Columns.Add("Gesp.", 40);
        BenutzerListe.SelectedIndexChanged += (_, _) => AuswahlGeaendert();

        var reiter = new TabControl { Location = new Point(240, 12), Size = new Size(508, 440) };

        var stammdatenReiter = new TabPage("Stammdaten");
        BaueStammdatenReiter(stammdatenReiter);

        var rechteReiter = new TabPage("Rechte");
        BaueRechteReiter(rechteReiter);

        reiter.TabPages.Add(stammdatenReiter);
        reiter.TabPages.Add(rechteReiter);

        Controls.Add(BenutzerListe);
        Controls.Add(reiter);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LadeBereiche();
        LadeDauervorgaben();
        Suchen();
    }

    private void BaueStammdatenReiter(TabPage reiter)
    {
        Feld(reiter, "&Anmeldename:", AnmeldenameFeld, 12);
        Feld(reiter, "An&zeigename:", AnzeigenameFeld, 40);
        Feld(reiter, "&Systemrechte:", SystemrechteFeld, 68);
        Feld(reiter, "A&ngelegt:", AngelegtFeld, 96);
        Feld(reiter, "&Letzte Anmeldung:", LetzteAnmeldungFeld, 124);

        AnlegenKnopf.Location = new Point(12, 164);
        AnlegenKnopf.Size = new Size(160, 24);
        AnlegenKnopf.Click += (_, _) => BenutzerAnlegen();

        KennwortZuruecksetzenKnopf.Location = new Point(12, 196);
        KennwortZuruecksetzenKnopf.Size = new Size(160, 24);
        KennwortZuruecksetzenKnopf.Click += (_, _) => KennwortZuruecksetzen();

        SperrenKnopf.Location = new Point(12, 228);
        SperrenKnopf.Size = new Size(160, 24);
        SperrenKnopf.Click += (_, _) => SperrenOderEntsperren();

        reiter.Controls.Add(AnlegenKnopf);
        reiter.Controls.Add(KennwortZuruecksetzenKnopf);
        reiter.Controls.Add(SperrenKnopf);
    }

    private void BaueRechteReiter(TabPage reiter)
    {
        RechteListe.Location = new Point(12, 12);
        RechteListe.Size = new Size(468, 190);
        RechteListe.View = View.Details;
        RechteListe.FullRowSelect = true;
        RechteListe.GridLines = true;
        RechteListe.MultiSelect = false;
        RechteListe.HideSelection = false;
        RechteListe.Font = new Font("Courier New", 9f);
        RechteListe.Columns.Add("Bereich", 100);
        RechteListe.Columns.Add("Stufe", 70);
        RechteListe.Columns.Add("Gültig von", 110);
        RechteListe.Columns.Add("Gültig bis", 110);
        RechteListe.Columns.Add("Status", 70);
        RechteListe.SelectedIndexChanged += (_, _) => RechteAuswahlGeaendert();

        var freigabe = new GroupBox { Text = "Freigabe erteilen", Location = new Point(12, 210), Size = new Size(468, 130) };

        var bereichBeschriftung = new Label { Text = "&Bereich:", Location = new Point(8, 24), Size = new Size(80, 16) };
        BereichFeld.Location = new Point(96, 21);
        BereichFeld.Size = new Size(180, 21);
        BereichFeld.SelectedIndexChanged += (_, _) => AktualisiereBestaetigenKnopf();

        var stufeBeschriftung = new Label { Text = "&Stufe:", Location = new Point(288, 24), Size = new Size(56, 16) };
        StufeFeld.Location = new Point(348, 21);
        StufeFeld.Size = new Size(112, 21);
        foreach (Stufe stufe in Enum.GetValues<Stufe>())
        {
            StufeFeld.Items.Add(Stufen.Bezeichnung(stufe));
        }
        StufeFeld.SelectedIndex = 0;

        var dauerBeschriftung = new Label { Text = "&Dauer:", Location = new Point(8, 52), Size = new Size(80, 16) };
        DauerFeld.Location = new Point(96, 49);
        DauerFeld.Size = new Size(180, 21);
        DauerFeld.SelectedIndexChanged += (_, _) =>
            EigenesEndeFeld.Enabled = Dauervorgaben[DauerFeld.SelectedIndex].EigenesEnde;

        EigenesEndeFeld.Location = new Point(288, 49);
        EigenesEndeFeld.Size = new Size(172, 20);

        BestaetigenKnopf.Location = new Point(8, 80);
        BestaetigenKnopf.Size = new Size(140, 24);
        BestaetigenKnopf.Click += (_, _) => FreigabeBestaetigen();

        EntziehenKnopf.Location = new Point(156, 80);
        EntziehenKnopf.Size = new Size(140, 24);
        EntziehenKnopf.Click += (_, _) => Entziehen();

        freigabe.Controls.Add(bereichBeschriftung);
        freigabe.Controls.Add(BereichFeld);
        freigabe.Controls.Add(stufeBeschriftung);
        freigabe.Controls.Add(StufeFeld);
        freigabe.Controls.Add(dauerBeschriftung);
        freigabe.Controls.Add(DauerFeld);
        freigabe.Controls.Add(EigenesEndeFeld);
        freigabe.Controls.Add(BestaetigenKnopf);
        freigabe.Controls.Add(EntziehenKnopf);

        reiter.Controls.Add(RechteListe);
        reiter.Controls.Add(freigabe);
    }

    private void LadeBereiche()
    {
        BereichFeld.Items.Add("* (alle Module)");

        foreach (var eintrag in Kontext.Verzeichnis.Alle)
        {
            BereichFeld.Items.Add($"{eintrag.Transaktionscode} {eintrag.Bezeichnung}");
        }
    }

    private void LadeDauervorgaben()
    {
        foreach (var vorgabe in Dauervorgaben)
        {
            DauerFeld.Items.Add(vorgabe.Anzeige);
        }

        DauerFeld.SelectedIndex = 0;
    }

    private void Suchen()
    {
        _benutzer = new List<Benutzer>(Dienste.Benutzer.Alle());

        BenutzerListe.Items.Clear();
        foreach (var benutzer in _benutzer)
        {
            var zeile = new ListViewItem(benutzer.Anmeldename) { Tag = benutzer };
            zeile.SubItems.Add(benutzer.Anzeigename);
            zeile.SubItems.Add(benutzer.Systemrechte ? "ja" : "");
            zeile.SubItems.Add(benutzer.Gesperrt ? "ja" : "");
            BenutzerListe.Items.Add(zeile);
        }

        LeereAuswahl();
    }

    private void AuswahlGeaendert()
    {
        if (BenutzerListe.SelectedItems.Count == 0)
        {
            LeereAuswahl();
            return;
        }

        _ausgewaehlterBenutzer = (Benutzer)BenutzerListe.SelectedItems[0].Tag!;

        AnmeldenameFeld.Text = _ausgewaehlterBenutzer.Anmeldename;
        AnzeigenameFeld.Text = _ausgewaehlterBenutzer.Anzeigename;
        SystemrechteFeld.Text = _ausgewaehlterBenutzer.Systemrechte ? "ja" : "nein";
        AngelegtFeld.Text = _ausgewaehlterBenutzer.Angelegt.ToString("yyyy-MM-dd HH:mm:ss");
        LetzteAnmeldungFeld.Text = _ausgewaehlterBenutzer.LetzteAnmeldung?.ToString("yyyy-MM-dd HH:mm:ss") ?? "noch nie";

        KennwortZuruecksetzenKnopf.Enabled = true;
        SperrenKnopf.Enabled = true;
        SperrenKnopf.Text = _ausgewaehlterBenutzer.Gesperrt ? "&Entsperren" : "&Sperren";

        LadeRechte();
    }

    private void LeereAuswahl()
    {
        _ausgewaehlterBenutzer = null;

        AnmeldenameFeld.Text = "";
        AnzeigenameFeld.Text = "";
        SystemrechteFeld.Text = "";
        AngelegtFeld.Text = "";
        LetzteAnmeldungFeld.Text = "";

        KennwortZuruecksetzenKnopf.Enabled = false;
        SperrenKnopf.Enabled = false;
        SperrenKnopf.Text = "&Sperren";

        _rechteDesBenutzers = new List<Recht>();
        RechteListe.Items.Clear();
        AktualisiereBestaetigenKnopf();
        EntziehenKnopf.Enabled = false;
    }

    private void LadeRechte()
    {
        if (_ausgewaehlterBenutzer is null)
        {
            return;
        }

        _rechteDesBenutzers = new List<Recht>(Dienste.Rechte.Liste(_ausgewaehlterBenutzer.BenutzerId));
        var jetzt = DateTime.Now;

        RechteListe.Items.Clear();
        foreach (var recht in _rechteDesBenutzers)
        {
            var status = StatusVon(recht, jetzt);

            var zeile = new ListViewItem(BereichAnzeige(recht.Bereich)) { Tag = recht };
            zeile.SubItems.Add(Stufen.Bezeichnung(recht.Stufe));
            zeile.SubItems.Add(recht.GueltigVon.ToString("yyyy-MM-dd HH:mm"));
            zeile.SubItems.Add(recht.GueltigBis?.ToString("yyyy-MM-dd HH:mm") ?? "unbefristet");
            zeile.SubItems.Add(status);

            if (status != "aktiv")
            {
                zeile.ForeColor = SystemColors.GrayText;
            }

            RechteListe.Items.Add(zeile);
        }

        AktualisiereBestaetigenKnopf();
        EntziehenKnopf.Enabled = false;
    }

    private void RechteAuswahlGeaendert()
    {
        if (RechteListe.SelectedItems.Count == 0 || _ausgewaehlterBenutzer is null)
        {
            EntziehenKnopf.Enabled = false;
            return;
        }

        var recht = (Recht)RechteListe.SelectedItems[0].Tag!;
        EntziehenKnopf.Enabled = StatusVon(recht, DateTime.Now) == "aktiv";
    }

    private void AktualisiereBestaetigenKnopf() =>
        BestaetigenKnopf.Enabled = _ausgewaehlterBenutzer is not null && BereichFeld.SelectedIndex >= 0;

    private void BenutzerAnlegen()
    {
        using var dialog = new NeuerBenutzerDialog();

        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Ergebnis is null)
        {
            return;
        }

        try
        {
            Dienste.Benutzer.Anlegen(dialog.Ergebnis);
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Meldungen.Erfolg($"Benutzer {dialog.Ergebnis.Anmeldename} angelegt.");
        Suchen();
    }

    private void KennwortZuruecksetzen()
    {
        if (_ausgewaehlterBenutzer is null)
        {
            return;
        }

        using var dialog = new KennwortZuruecksetzenDialog(_ausgewaehlterBenutzer.Anmeldename);

        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Hash is null || dialog.Salz is null)
        {
            return;
        }

        try
        {
            Dienste.Benutzer.KennwortSetzen(_ausgewaehlterBenutzer.BenutzerId, dialog.Hash, dialog.Salz, dialog.Durchlaeufe);
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Meldungen.Erfolg($"Kennwort für {_ausgewaehlterBenutzer.Anmeldename} zurückgesetzt.");
    }

    private void SperrenOderEntsperren()
    {
        if (_ausgewaehlterBenutzer is null)
        {
            return;
        }

        try
        {
            if (_ausgewaehlterBenutzer.Gesperrt)
            {
                Dienste.Benutzer.Entsperren(_ausgewaehlterBenutzer.BenutzerId);
                Meldungen.Erfolg($"Benutzer {_ausgewaehlterBenutzer.Anmeldename} entsperrt.");
            }
            else
            {
                Dienste.Benutzer.Sperren(_ausgewaehlterBenutzer.BenutzerId);
                Meldungen.Erfolg($"Benutzer {_ausgewaehlterBenutzer.Anmeldename} gesperrt.");
            }
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Suchen();
    }

    private void FreigabeBestaetigen()
    {
        if (_ausgewaehlterBenutzer is null || BereichFeld.SelectedIndex < 0)
        {
            return;
        }

        var bereich = BereichFeld.SelectedIndex == 0 ? Recht.AlleBereiche : Kontext.Verzeichnis.Alle[BereichFeld.SelectedIndex - 1].Transaktionscode;
        var stufe = Enum.GetValues<Stufe>()[StufeFeld.SelectedIndex];
        var jetzt = DateTime.Now;
        var gueltigBis = BerechneGueltigBis(jetzt);

        var recht = new Recht
        {
            BenutzerId = _ausgewaehlterBenutzer.BenutzerId,
            MandantNr = MandantNr,
            Bereich = bereich,
            Stufe = stufe,
            GueltigVon = jetzt,
            GueltigBis = gueltigBis,
            ErteiltVon = Sitzung.Benutzer.BenutzerId,
            ErteiltAm = jetzt
        };

        try
        {
            Dienste.Rechte.Erteilen(recht);
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Meldungen.Erfolg($"{BereichAnzeige(bereich)} für {_ausgewaehlterBenutzer.Anmeldename} freigegeben " +
                         $"({Stufen.Bezeichnung(stufe)}, {(gueltigBis is null ? "unbefristet" : "bis " + gueltigBis.Value.ToString("yyyy-MM-dd HH:mm"))}).");

        LadeRechte();
    }

    private DateTime? BerechneGueltigBis(DateTime jetzt)
    {
        var vorgabe = Dauervorgaben[DauerFeld.SelectedIndex];

        if (vorgabe.EigenesEnde)
        {
            return EigenesEndeFeld.Value;
        }

        if (vorgabe.Dauer is { } spanne)
        {
            return jetzt + spanne;
        }

        if (vorgabe.Anzeige == "Bis Mitternacht")
        {
            return jetzt.Date.AddDays(1);
        }

        return null; // Unbefristet
    }

    private void Entziehen()
    {
        if (RechteListe.SelectedItems.Count == 0)
        {
            return;
        }

        var recht = (Recht)RechteListe.SelectedItems[0].Tag!;

        try
        {
            Dienste.Rechte.Entziehen(recht.RechtId, DateTime.Now);
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Meldungen.Erfolg($"{BereichAnzeige(recht.Bereich)} entzogen.");
        LadeRechte();
    }

    private string BereichAnzeige(string bereich)
    {
        if (bereich == Recht.AlleBereiche)
        {
            return "* (alle Module)";
        }

        var eintrag = Kontext.Verzeichnis.Finde(bereich);
        return eintrag is null ? bereich : $"{eintrag.Transaktionscode} {eintrag.Bezeichnung}";
    }

    private static string StatusVon(Recht recht, DateTime jetzt)
    {
        if (recht.GueltigVon > jetzt)
        {
            return "zukünftig";
        }

        if (recht.GueltigBis is { } bis && bis <= jetzt)
        {
            return "abgelaufen";
        }

        return "aktiv";
    }

    private static void Feld(Control eltern, string beschriftung, TextBox feld, int oben)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(8, oben + 3),
            Size = new Size(140, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(148, oben);
        feld.Size = new Size(340, 20);
        feld.ReadOnly = true;
        feld.BackColor = SystemColors.Control;

        eltern.Controls.Add(text);
        eltern.Controls.Add(feld);
    }
}
