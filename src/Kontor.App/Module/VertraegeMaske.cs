using System.Globalization;
using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;

namespace Kontor.App.Module;

public sealed class VertraegeMaske : ModulFenster
{
    private static readonly CultureInfo Anzeigekultur = CultureInfo.GetCultureInfo("de-DE");

    private readonly ListView Liste = new();
    private readonly ListView PreishistorieListe = new();

    private readonly TextBox BezeichnungFeld = new();
    private readonly TextBox AnbieterFeld = new();
    private readonly ComboBox TurnusFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker BeginnFeld = new() { Format = DateTimePickerFormat.Short };
    private readonly NumericUpDown MindestlaufzeitFeld = new() { Minimum = 0, Maximum = 240 };
    private readonly NumericUpDown KuendigungsfristFeld = new() { Minimum = 0, Maximum = 24 };
    private readonly ComboBox KategorieFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox KontoFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox AutomatischFeld = new() { Text = "&Automatisch buchen" };
    private readonly CheckBox BeendetFeld = new() { Text = "&Beendet" };
    private readonly CheckBox GekuendigtFeld = new() { Text = "&Gekündigt zum:" };
    private readonly DateTimePicker GekuendigtZumFeld = new() { Format = DateTimePickerFormat.Short };

    private readonly DateTimePicker NeuerPreisAbFeld = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox NeuerPreisBetragFeld = new();
    private readonly Button PreisErfassenKnopf = new() { Text = "Preisänderung &erfassen" };

    private readonly Button NeuKnopf = new() { Text = "&Neu" };
    private readonly Button AendernKnopf = new() { Text = "&Ändern" };
    private readonly Button SichernKnopf = new() { Text = "&Sichern" };
    private readonly Button AbbrechenKnopf = new() { Text = "Abb&rechen" };
    private readonly Button FaelligeJetztKnopf = new() { Text = "&Fällige jetzt buchen" };

    private List<Kategorie> _kategorien = new();
    private List<Konto> _konten = new();
    private Vertrag? _geladenerVertrag;
    private bool _sperreAutomatischEreignis;

    public VertraegeMaske(Modulkontext kontext) : base("K03", "Verträge", kontext)
    {
        ClientSize = new Size(680, 560);

        Liste.Location = new Point(12, 12);
        Liste.Size = new Size(656, 130);
        Liste.View = View.Details;
        Liste.FullRowSelect = true;
        Liste.GridLines = true;
        Liste.MultiSelect = false;
        Liste.HideSelection = false;
        Liste.Font = new Font("Courier New", 9f);
        Liste.Columns.Add("Bezeichnung", 160);
        Liste.Columns.Add("Anbieter", 120);
        Liste.Columns.Add("Turnus", 90);
        var preisSpalte = Liste.Columns.Add("Akt. Preis", 80);
        preisSpalte.TextAlign = HorizontalAlignment.Right;
        var jahresSpalte = Liste.Columns.Add("Jahreskosten", 90);
        jahresSpalte.TextAlign = HorizontalAlignment.Right;
        Liste.Columns.Add("Nächste Kündigung", 100);
        Liste.SelectedIndexChanged += (_, _) => AuswahlGeaendert();

        var detailgruppe = new GroupBox { Text = "Vertrag", Location = new Point(12, 148), Size = new Size(656, 190) };
        Zeile(detailgruppe, "&Bezeichnung:", BezeichnungFeld, 8, 20, 220);
        Zeile(detailgruppe, "&Anbieter:", AnbieterFeld, 8, 46, 220);
        Zeile(detailgruppe, "&Turnus:", TurnusFeld, 8, 72, 140);
        Zeile(detailgruppe, "&Beginn:", BeginnFeld, 8, 98, 120);

        foreach (Turnus t in Enum.GetValues<Turnus>())
        {
            TurnusFeld.Items.Add(Turnusse.Bezeichnung(t));
        }

        Zeile(detailgruppe, "&Mindestlaufzeit (Monate):", MindestlaufzeitFeld, 260, 20, 80);
        Zeile(detailgruppe, "&Kündigungsfrist (Monate):", KuendigungsfristFeld, 260, 46, 80);
        Zeile(detailgruppe, "K&ategorie:", KategorieFeld, 260, 72, 200);
        Zeile(detailgruppe, "K&onto:", KontoFeld, 260, 98, 200);

        AutomatischFeld.Location = new Point(8, 130);
        AutomatischFeld.Size = new Size(160, 20);
        AutomatischFeld.CheckedChanged += (_, _) => AutomatischGeaendert();
        BeendetFeld.Location = new Point(180, 130);
        BeendetFeld.Size = new Size(80, 20);

        GekuendigtFeld.Location = new Point(8, 156);
        GekuendigtFeld.Size = new Size(120, 20);
        GekuendigtFeld.CheckedChanged += (_, _) => GekuendigtZumFeld.Enabled = GekuendigtFeld.Checked;
        GekuendigtZumFeld.Location = new Point(132, 154);
        GekuendigtZumFeld.Size = new Size(120, 20);

        detailgruppe.Controls.Add(AutomatischFeld);
        detailgruppe.Controls.Add(BeendetFeld);
        detailgruppe.Controls.Add(GekuendigtFeld);
        detailgruppe.Controls.Add(GekuendigtZumFeld);

        FaelligeJetztKnopf.Location = new Point(280, 152);
        FaelligeJetztKnopf.Size = new Size(180, 24);
        FaelligeJetztKnopf.Click += (_, _) => FaelligeJetztBuchen();
        detailgruppe.Controls.Add(FaelligeJetztKnopf);

        var preisgruppe = new GroupBox { Text = "Preishistorie", Location = new Point(12, 344), Size = new Size(656, 140) };

        PreishistorieListe.Location = new Point(8, 20);
        PreishistorieListe.Size = new Size(400, 108);
        PreishistorieListe.View = View.Details;
        PreishistorieListe.FullRowSelect = true;
        PreishistorieListe.GridLines = true;
        PreishistorieListe.Font = new Font("Courier New", 9f);
        PreishistorieListe.Columns.Add("Gültig ab", 100);
        var betragSpalte = PreishistorieListe.Columns.Add("Betrag", 100);
        betragSpalte.TextAlign = HorizontalAlignment.Right;

        var abBeschriftung = new Label { Text = "&Gültig ab:", Location = new Point(420, 24), Size = new Size(80, 16) };
        NeuerPreisAbFeld.Location = new Point(500, 21);
        NeuerPreisAbFeld.Size = new Size(120, 20);

        var betragBeschriftung = new Label { Text = "&Betrag:", Location = new Point(420, 52), Size = new Size(80, 16) };
        NeuerPreisBetragFeld.Location = new Point(500, 49);
        NeuerPreisBetragFeld.Size = new Size(120, 20);
        NeuerPreisBetragFeld.Font = new Font("Courier New", 9f);
        NeuerPreisBetragFeld.TextAlign = HorizontalAlignment.Right;

        PreisErfassenKnopf.Location = new Point(420, 80);
        PreisErfassenKnopf.Size = new Size(200, 24);
        PreisErfassenKnopf.Click += (_, _) => PreisErfassen();

        preisgruppe.Controls.Add(PreishistorieListe);
        preisgruppe.Controls.Add(abBeschriftung);
        preisgruppe.Controls.Add(NeuerPreisAbFeld);
        preisgruppe.Controls.Add(betragBeschriftung);
        preisgruppe.Controls.Add(NeuerPreisBetragFeld);
        preisgruppe.Controls.Add(PreisErfassenKnopf);

        var buttonY = 494;
        NeuKnopf.Location = new Point(12, buttonY);
        NeuKnopf.Size = new Size(96, 24);
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Location = new Point(116, buttonY);
        AendernKnopf.Size = new Size(96, 24);
        AendernKnopf.Click += (_, _) => AendernStarten();
        SichernKnopf.Location = new Point(220, buttonY);
        SichernKnopf.Size = new Size(96, 24);
        SichernKnopf.Click += (_, _) => Sichern();
        AbbrechenKnopf.Location = new Point(324, buttonY);
        AbbrechenKnopf.Size = new Size(96, 24);
        AbbrechenKnopf.Click += (_, _) => Abbrechen();

        Controls.Add(Liste);
        Controls.Add(detailgruppe);
        Controls.Add(preisgruppe);
        Controls.Add(NeuKnopf);
        Controls.Add(AendernKnopf);
        Controls.Add(SichernKnopf);
        Controls.Add(AbbrechenKnopf);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LadeStammdaten();
        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    public override bool HatUngesicherteEingaben => MaskenModi.IstSchreibend(Modus);

    public override void SichernVersuchen() => Sichern();

    protected override void UebernehmeModus(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);

        Liste.Enabled = !schreibend;
        NeuKnopf.Enabled = !schreibend;
        AendernKnopf.Enabled = !schreibend && _geladenerVertrag is not null;
        SichernKnopf.Enabled = schreibend;
        AbbrechenKnopf.Enabled = schreibend;
        FaelligeJetztKnopf.Enabled = !schreibend && _geladenerVertrag is { Beendet: false, AutomatischBuchen: false };
        PreisErfassenKnopf.Enabled = !schreibend && _geladenerVertrag is not null;

        foreach (Control feld in new Control[]
                 {
                     BezeichnungFeld, AnbieterFeld, TurnusFeld, BeginnFeld, MindestlaufzeitFeld,
                     KuendigungsfristFeld, KategorieFeld, KontoFeld, BeendetFeld, GekuendigtFeld
                 })
        {
            feld.Enabled = schreibend;
        }

        GekuendigtZumFeld.Enabled = schreibend && GekuendigtFeld.Checked;
    }

    private void LadeStammdaten()
    {
        _kategorien = new List<Kategorie>(Dienste.Kategorien.Liste(MandantNr, auchGesperrte: false));
        _konten = new List<Konto>(Dienste.Konten.Liste(MandantNr, auchGesperrte: false));

        KategorieFeld.Items.Clear();
        foreach (var kategorie in _kategorien)
        {
            KategorieFeld.Items.Add(kategorie.Bezeichnung);
        }

        KontoFeld.Items.Clear();
        foreach (var konto in _konten)
        {
            KontoFeld.Items.Add(konto.Bezeichnung);
        }
    }

    private void Suchen()
    {
        Liste.Items.Clear();

        foreach (var vertrag in Dienste.Vertraege.Liste(MandantNr, auchBeendete: true))
        {
            var preise = Dienste.Vertragspreise.Liste(MandantNr, vertrag.VertragId);
            var zahlungen = Dienste.Buchungen.ListeFuerVertrag(MandantNr, vertrag.VertragId);
            var heute = DateOnly.FromDateTime(DateTime.Today);

            var (ergebnis, _) = Preisentwicklung.Berechne(preise, vertrag.Turnus, heute, zahlungen);

            var zeile = new ListViewItem(vertrag.Bezeichnung) { Tag = vertrag };
            zeile.SubItems.Add(vertrag.Anbieter);
            zeile.SubItems.Add(Turnusse.Bezeichnung(vertrag.Turnus));
            zeile.SubItems.Add(ergebnis is null ? "" : BetragText(ergebnis.AktuellerPreisCent));
            zeile.SubItems.Add(ergebnis is null ? "" : BetragText(ergebnis.JahreskostenCent));

            if (!vertrag.Beendet)
            {
                var termin = Kuendigungsrechner.Naechste(
                    vertrag.Beginn, vertrag.MindestlaufzeitMonate, vertrag.KuendigungsfristMonate, vertrag.Turnus, heute);
                zeile.SubItems.Add(termin.Kuendigungsmoeglichkeit.ToString("yyyy-MM-dd"));
            }
            else
            {
                zeile.SubItems.Add("beendet");
            }

            Liste.Items.Add(zeile);
        }

        AendernKnopf.Enabled = false;
        FaelligeJetztKnopf.Enabled = false;
        PreisErfassenKnopf.Enabled = false;
        LeereDetailfelder();
    }

    private void AuswahlGeaendert()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        if (Liste.SelectedItems.Count == 0)
        {
            LeereDetailfelder();
            AendernKnopf.Enabled = false;
            FaelligeJetztKnopf.Enabled = false;
            PreisErfassenKnopf.Enabled = false;
            return;
        }

        ZeigeVertrag((Vertrag)Liste.SelectedItems[0].Tag!);
        AendernKnopf.Enabled = true;
        FaelligeJetztKnopf.Enabled = _geladenerVertrag is { Beendet: false, AutomatischBuchen: false };
        PreisErfassenKnopf.Enabled = true;
    }

    private void ZeigeVertrag(Vertrag vertrag)
    {
        _geladenerVertrag = vertrag;

        BezeichnungFeld.Text = vertrag.Bezeichnung;
        AnbieterFeld.Text = vertrag.Anbieter;
        TurnusFeld.SelectedIndex = Array.IndexOf(Enum.GetValues<Turnus>(), vertrag.Turnus);
        BeginnFeld.Value = vertrag.Beginn.ToDateTime(TimeOnly.MinValue);
        MindestlaufzeitFeld.Value = vertrag.MindestlaufzeitMonate;
        KuendigungsfristFeld.Value = vertrag.KuendigungsfristMonate;
        KategorieFeld.SelectedIndex = _kategorien.FindIndex(k => k.KategorieId == vertrag.KategorieId);
        KontoFeld.SelectedIndex = _konten.FindIndex(k => k.KontoId == vertrag.KontoId);
        AutomatischFeld.Checked = vertrag.AutomatischBuchen;
        BeendetFeld.Checked = vertrag.Beendet;
        GekuendigtFeld.Checked = vertrag.GekuendigtZum.HasValue;
        GekuendigtZumFeld.Value = (vertrag.GekuendigtZum ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);

        PreishistorieListe.Items.Clear();
        foreach (var preis in Dienste.Vertragspreise.Liste(MandantNr, vertrag.VertragId))
        {
            var zeile = new ListViewItem(preis.GueltigAb.ToString("yyyy-MM-dd"));
            zeile.SubItems.Add(BetragText(preis.BetragCent));
            PreishistorieListe.Items.Add(zeile);
        }
    }

    private void LeereDetailfelder()
    {
        _geladenerVertrag = null;

        BezeichnungFeld.Text = "";
        AnbieterFeld.Text = "";
        TurnusFeld.SelectedIndex = 0;
        BeginnFeld.Value = DateTime.Today;
        MindestlaufzeitFeld.Value = 0;
        KuendigungsfristFeld.Value = 0;
        KategorieFeld.SelectedIndex = KategorieFeld.Items.Count > 0 ? 0 : -1;
        KontoFeld.SelectedIndex = KontoFeld.Items.Count > 0 ? 0 : -1;
        AutomatischFeld.Checked = false;
        BeendetFeld.Checked = false;
        GekuendigtFeld.Checked = false;
        GekuendigtZumFeld.Value = DateTime.Today;
        PreishistorieListe.Items.Clear();
    }

    private void Neu()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        LeereDetailfelder();
        SetzeModus(MaskenModus.Erfassen);
        BezeichnungFeld.Focus();
    }

    private void AendernStarten()
    {
        if (Modus != MaskenModus.Anzeigen || _geladenerVertrag is null)
        {
            return;
        }

        SetzeModus(MaskenModus.Aendern);
        BezeichnungFeld.Focus();
    }

    private void Sichern()
    {
        if (!MaskenModi.IstSchreibend(Modus))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BezeichnungFeld.Text))
        {
            Meldungen.Fehler("Die Bezeichnung muss gesetzt sein.");
            return;
        }

        if (KategorieFeld.SelectedIndex < 0 || KontoFeld.SelectedIndex < 0)
        {
            Meldungen.Fehler("Kategorie und Konto müssen gesetzt sein.");
            return;
        }

        var vertrag = new Vertrag
        {
            VertragId = _geladenerVertrag?.VertragId ?? 0,
            MandantNr = MandantNr,
            Bezeichnung = BezeichnungFeld.Text.Trim(),
            Anbieter = AnbieterFeld.Text.Trim(),
            Turnus = Enum.GetValues<Turnus>()[TurnusFeld.SelectedIndex],
            Beginn = DateOnly.FromDateTime(BeginnFeld.Value.Date),
            MindestlaufzeitMonate = (int)MindestlaufzeitFeld.Value,
            KuendigungsfristMonate = (int)KuendigungsfristFeld.Value,
            KategorieId = _kategorien[KategorieFeld.SelectedIndex].KategorieId,
            KontoId = _konten[KontoFeld.SelectedIndex].KontoId,
            AutomatischBuchen = AutomatischFeld.Checked,
            GekuendigtZum = GekuendigtFeld.Checked ? DateOnly.FromDateTime(GekuendigtZumFeld.Value.Date) : null,
            Beendet = BeendetFeld.Checked
        };

        if (vertrag.VertragId == 0)
        {
            Dienste.Vertraege.Anlegen(vertrag);
            Meldungen.Erfolg($"Vertrag {vertrag.Bezeichnung} angelegt.");
        }
        else
        {
            Dienste.Vertraege.Aendern(vertrag);
            Meldungen.Erfolg($"Vertrag {vertrag.Bezeichnung} gesichert.");
        }

        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    private void Abbrechen()
    {
        if (!MaskenModi.IstSchreibend(Modus))
        {
            return;
        }

        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    // Der Schalter wirkt sofort, unabhängig vom Anzeigen/Ändern-Modus, da er kein Stammdatenfeld
    // im eigentlichen Sinn ist, sondern eine laufende Einstellung je Vertrag.
    private void AutomatischGeaendert()
    {
        if (_sperreAutomatischEreignis || _geladenerVertrag is null || Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        if (_geladenerVertrag.AutomatischBuchen == AutomatischFeld.Checked)
        {
            return;
        }

        var vorherigerWert = _geladenerVertrag.AutomatischBuchen;
        _geladenerVertrag.AutomatischBuchen = AutomatischFeld.Checked;

        try
        {
            Dienste.Vertraege.Aendern(_geladenerVertrag);
        }
        catch (FachlicherFehler fehler)
        {
            _geladenerVertrag.AutomatischBuchen = vorherigerWert;

            _sperreAutomatischEreignis = true;
            AutomatischFeld.Checked = vorherigerWert;
            _sperreAutomatischEreignis = false;

            Meldungen.Fehler(fehler.Message);
            return;
        }

        FaelligeJetztKnopf.Enabled = _geladenerVertrag is { Beendet: false, AutomatischBuchen: false };
        Meldungen.Erfolg(AutomatischFeld.Checked
            ? $"{_geladenerVertrag.Bezeichnung} wird jetzt automatisch gebucht."
            : $"{_geladenerVertrag.Bezeichnung} wird nicht mehr automatisch gebucht.");
    }

    private void PreisErfassen()
    {
        if (_geladenerVertrag is null)
        {
            return;
        }

        if (!TryParseBetrag(NeuerPreisBetragFeld.Text, out var betrag) || betrag < 0m)
        {
            Meldungen.Fehler($"Der Betrag \"{NeuerPreisBetragFeld.Text}\" ist kein gültiger Betrag.");
            return;
        }

        try
        {
            Dienste.Vertragspreise.Anlegen(new Vertragspreis
            {
                MandantNr = MandantNr,
                VertragId = _geladenerVertrag.VertragId,
                GueltigAb = DateOnly.FromDateTime(NeuerPreisAbFeld.Value.Date),
                BetragCent = Feldwerte.Cent(betrag)
            });
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Meldungen.Erfolg($"Preisänderung für {_geladenerVertrag.Bezeichnung} erfasst.");
        NeuerPreisBetragFeld.Text = "";
        ZeigeVertrag(_geladenerVertrag);
        Suchen();
    }

    private void FaelligeJetztBuchen()
    {
        if (_geladenerVertrag is null)
        {
            return;
        }

        try
        {
            var anzahl = Dienste.Vertraege.VertragslaufAusfuehren(
                MandantNr, _geladenerVertrag.VertragId, DateOnly.FromDateTime(DateTime.Today));

            Meldungen.Erfolg(anzahl > 0
                ? $"{anzahl} fällige Buchung(en) für {_geladenerVertrag.Bezeichnung} angelegt."
                : $"Für {_geladenerVertrag.Bezeichnung} sind keine fälligen Buchungen offen.");
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
        }

        Suchen();
    }

    private static void Zeile(GroupBox gruppe, string beschriftung, Control feld, int links, int oben, int breite)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(links, oben + 3),
            Size = new Size(150, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(links + 152, oben);
        feld.Size = new Size(breite, feld.Height);

        gruppe.Controls.Add(text);
        gruppe.Controls.Add(feld);
    }

    private static string BetragText(long cent) => Feldwerte.AusCent(cent).ToString("N2", Anzeigekultur);

    private static bool TryParseBetrag(string text, out decimal wert) =>
        decimal.TryParse(text, NumberStyles.Number, Anzeigekultur, out wert) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out wert);
}
