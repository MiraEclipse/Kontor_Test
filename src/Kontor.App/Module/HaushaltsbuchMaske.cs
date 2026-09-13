using System.Globalization;
using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;

namespace Kontor.App.Module;

public sealed class HaushaltsbuchMaske : ModulFenster
{
    private static readonly CultureInfo Anzeigekultur = CultureInfo.GetCultureInfo("de-DE");

    private readonly DateTimePicker MonatFeld = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM yyyy",
        ShowUpDown = true,
        Location = new Point(70, 10),
        Size = new Size(160, 20)
    };

    private readonly ListView Liste = new();

    private readonly RadioButton BuchungRadio = new() { Text = "&Buchung", Checked = true };
    private readonly RadioButton UmbuchungRadio = new() { Text = "&Umbuchung" };
    private readonly Label KontoBeschriftung = new() { Text = "&Konto:" };
    private readonly ComboBox KontoFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label GegenBeschriftung = new() { Text = "&Kategorie:" };
    private readonly ComboBox GegenFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker DatumFeld = new() { Format = DateTimePickerFormat.Short };
    private readonly TextBox BetragFeld = new();
    private readonly TextBox TextFeld = new();
    private readonly Button BuchenKnopf = new() { Text = "&Buchen" };
    private readonly Button LoeschenKnopf = new() { Text = "&Löschen", Enabled = false };

    private readonly Label EinnahmenLabel = new();
    private readonly Label AusgabenLabel = new();
    private readonly Label SaldoLabel = new();

    private List<Konto> _konten = new();
    private List<Kategorie> _kategorien = new();

    public HaushaltsbuchMaske(Modulkontext kontext) : base("K01", "Haushaltsbuch", kontext)
    {
        ClientSize = new Size(640, 480);

        var monatBeschriftung = new Label
        {
            Text = "&Monat:",
            Location = new Point(12, 13),
            Size = new Size(56, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        MonatFeld.Location = new Point(70, 10);
        MonatFeld.ValueChanged += (_, _) => Aktualisieren();

        Liste.Location = new Point(12, 38);
        Liste.Size = new Size(612, 160);
        Liste.View = View.Details;
        Liste.FullRowSelect = true;
        Liste.GridLines = true;
        Liste.MultiSelect = false;
        Liste.HideSelection = false;
        Liste.Font = new Font("Courier New", 9f);
        Liste.Columns.Add("Datum", 80);
        Liste.Columns.Add("Art", 80);
        Liste.Columns.Add("Konto", 110);
        Liste.Columns.Add("Kategorie / Gegenkonto", 140);
        var betragSpalte = Liste.Columns.Add("Betrag", 90);
        betragSpalte.TextAlign = HorizontalAlignment.Right;
        Liste.Columns.Add("Text", 108);
        Liste.SelectedIndexChanged += (_, _) =>
            LoeschenKnopf.Enabled = Liste.SelectedItems.Count > 0 && DarfMindestens(Stufe.Voll);

        var erfassungsgruppe = new GroupBox
        {
            Text = "Erfassen",
            Location = new Point(12, 204),
            Size = new Size(612, 168)
        };

        BuchungRadio.Location = new Point(10, 20);
        BuchungRadio.Size = new Size(96, 20);
        BuchungRadio.CheckedChanged += (_, _) => AktualisiereArt();

        UmbuchungRadio.Location = new Point(110, 20);
        UmbuchungRadio.Size = new Size(112, 20);

        var datumBeschriftung = new Label { Text = "&Datum:", Location = new Point(10, 48), Size = new Size(96, 16) };
        DatumFeld.Location = new Point(110, 45);
        DatumFeld.Size = new Size(120, 20);

        KontoBeschriftung.Location = new Point(10, 76);
        KontoBeschriftung.Size = new Size(96, 16);
        KontoFeld.Location = new Point(110, 73);
        KontoFeld.Size = new Size(200, 21);

        GegenBeschriftung.Location = new Point(10, 104);
        GegenBeschriftung.Size = new Size(96, 16);
        GegenFeld.Location = new Point(110, 101);
        GegenFeld.Size = new Size(200, 21);

        var betragBeschriftung = new Label { Text = "&Betrag:", Location = new Point(10, 132), Size = new Size(96, 16) };
        BetragFeld.Location = new Point(110, 129);
        BetragFeld.Size = new Size(120, 20);
        BetragFeld.Font = new Font("Courier New", 9f);
        BetragFeld.TextAlign = HorizontalAlignment.Right;

        var textBeschriftung = new Label { Text = "&Text:", Location = new Point(340, 48), Size = new Size(56, 16) };
        TextFeld.Location = new Point(400, 45);
        TextFeld.Size = new Size(200, 20);

        BuchenKnopf.Location = new Point(400, 129);
        BuchenKnopf.Size = new Size(96, 24);
        BuchenKnopf.Click += (_, _) => Buchen();

        LoeschenKnopf.Location = new Point(504, 129);
        LoeschenKnopf.Size = new Size(96, 24);
        LoeschenKnopf.Click += (_, _) => LoescheAusgewaehlte();

        erfassungsgruppe.Controls.Add(BuchungRadio);
        erfassungsgruppe.Controls.Add(UmbuchungRadio);
        erfassungsgruppe.Controls.Add(datumBeschriftung);
        erfassungsgruppe.Controls.Add(DatumFeld);
        erfassungsgruppe.Controls.Add(KontoBeschriftung);
        erfassungsgruppe.Controls.Add(KontoFeld);
        erfassungsgruppe.Controls.Add(GegenBeschriftung);
        erfassungsgruppe.Controls.Add(GegenFeld);
        erfassungsgruppe.Controls.Add(betragBeschriftung);
        erfassungsgruppe.Controls.Add(BetragFeld);
        erfassungsgruppe.Controls.Add(textBeschriftung);
        erfassungsgruppe.Controls.Add(TextFeld);
        erfassungsgruppe.Controls.Add(BuchenKnopf);
        erfassungsgruppe.Controls.Add(LoeschenKnopf);

        EinnahmenLabel.Location = new Point(12, 380);
        EinnahmenLabel.Size = new Size(200, 16);
        AusgabenLabel.Location = new Point(220, 380);
        AusgabenLabel.Size = new Size(200, 16);
        SaldoLabel.Location = new Point(428, 380);
        SaldoLabel.Size = new Size(196, 16);
        SaldoLabel.Font = new Font(Font, FontStyle.Bold);

        Controls.Add(monatBeschriftung);
        Controls.Add(MonatFeld);
        Controls.Add(Liste);
        Controls.Add(erfassungsgruppe);
        Controls.Add(EinnahmenLabel);
        Controls.Add(AusgabenLabel);
        Controls.Add(SaldoLabel);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LadeStammdaten();
        AktualisiereArt();
        Aktualisieren();

        var darfErfassen = DarfMindestens(Stufe.Aendern);
        BuchenKnopf.Enabled = darfErfassen;
        BuchungRadio.Enabled = darfErfassen;
        UmbuchungRadio.Enabled = darfErfassen;
        KontoFeld.Enabled = darfErfassen;
        GegenFeld.Enabled = darfErfassen;
        DatumFeld.Enabled = darfErfassen;
        BetragFeld.Enabled = darfErfassen;
        TextFeld.Enabled = darfErfassen;
    }

    public override bool HatUngesicherteEingaben =>
        !string.IsNullOrWhiteSpace(BetragFeld.Text) || !string.IsNullOrWhiteSpace(TextFeld.Text);

    public override void SichernVersuchen() => Buchen();

    private bool DarfMindestens(Stufe erforderlich) => EffektiveStufe is { } stufe && stufe >= erforderlich;

    private void LadeStammdaten()
    {
        _konten = new List<Konto>(Dienste.Konten.Liste(MandantNr, auchGesperrte: false));
        _kategorien = new List<Kategorie>(Dienste.Kategorien.Liste(MandantNr, auchGesperrte: false));

        KontoFeld.Items.Clear();
        foreach (var konto in _konten)
        {
            KontoFeld.Items.Add(konto.Bezeichnung);
        }

        if (KontoFeld.Items.Count > 0)
        {
            KontoFeld.SelectedIndex = 0;
        }
    }

    private void AktualisiereArt()
    {
        var istBuchung = BuchungRadio.Checked;

        GegenBeschriftung.Text = istBuchung ? "&Kategorie:" : "&Nach-Konto:";
        KontoBeschriftung.Text = istBuchung ? "&Konto:" : "&Von-Konto:";

        GegenFeld.Items.Clear();
        if (istBuchung)
        {
            foreach (var kategorie in _kategorien)
            {
                GegenFeld.Items.Add(kategorie.Bezeichnung);
            }
        }
        else
        {
            foreach (var konto in _konten)
            {
                GegenFeld.Items.Add(konto.Bezeichnung);
            }
        }

        if (GegenFeld.Items.Count > 0)
        {
            GegenFeld.SelectedIndex = 0;
        }
    }

    private (DateOnly Von, DateOnly Bis) Monatsspanne()
    {
        var von = new DateOnly(MonatFeld.Value.Year, MonatFeld.Value.Month, 1);
        var bis = von.AddMonths(1).AddDays(-1);
        return (von, bis);
    }

    private void Aktualisieren()
    {
        var (von, bis) = Monatsspanne();

        var buchungen = Dienste.Buchungen.Liste(MandantNr, von, bis);
        var umbuchungen = Dienste.Umbuchungen.Liste(MandantNr, von, bis);

        Liste.Items.Clear();

        long einnahmenCent = 0;
        long ausgabenCent = 0;

        foreach (var buchung in buchungen)
        {
            var konto = KontoName(buchung.KontoId);
            var kategorie = _kategorien.Find(k => k.KategorieId == buchung.KategorieId);
            var richtung = kategorie?.Richtung ?? Richtung.Ausgabe;

            if (richtung == Richtung.Einnahme)
            {
                einnahmenCent += buchung.BetragCent;
            }
            else
            {
                ausgabenCent += buchung.BetragCent;
            }

            var zeile = new ListViewItem(buchung.Datum.ToString("yyyy-MM-dd")) { Tag = buchung };
            zeile.SubItems.Add("Buchung");
            zeile.SubItems.Add(konto);
            zeile.SubItems.Add(kategorie?.Bezeichnung ?? "");
            zeile.SubItems.Add((richtung == Richtung.Einnahme ? "+" : "-") + BetragText(buchung.BetragCent));
            zeile.SubItems.Add(buchung.Text);
            Liste.Items.Add(zeile);
        }

        foreach (var umbuchung in umbuchungen)
        {
            var zeile = new ListViewItem(umbuchung.Datum.ToString("yyyy-MM-dd")) { Tag = umbuchung };
            zeile.SubItems.Add("Umbuchung");
            zeile.SubItems.Add(KontoName(umbuchung.VonKontoId));
            zeile.SubItems.Add(KontoName(umbuchung.NachKontoId));
            zeile.SubItems.Add(BetragText(umbuchung.BetragCent));
            zeile.SubItems.Add(umbuchung.Text);
            Liste.Items.Add(zeile);
        }

        EinnahmenLabel.Text = $"Einnahmen: {BetragText(einnahmenCent)}";
        AusgabenLabel.Text = $"Ausgaben: {BetragText(ausgabenCent)}";
        SaldoLabel.Text = $"Saldo: {BetragText(einnahmenCent - ausgabenCent)}";
        LoeschenKnopf.Enabled = false;
    }

    private void Buchen()
    {
        if (!TryParseBetrag(BetragFeld.Text, out var betrag) || betrag <= 0m)
        {
            Meldungen.Fehler($"Der Betrag \"{BetragFeld.Text}\" ist kein gültiger, positiver Betrag.");
            return;
        }

        if (KontoFeld.SelectedIndex < 0)
        {
            Meldungen.Fehler("Bitte ein Konto auswählen.");
            return;
        }

        if (GegenFeld.SelectedIndex < 0)
        {
            Meldungen.Fehler(BuchungRadio.Checked ? "Bitte eine Kategorie auswählen." : "Bitte ein Nach-Konto auswählen.");
            return;
        }

        var datum = DateOnly.FromDateTime(DatumFeld.Value.Date);

        try
        {
            if (BuchungRadio.Checked)
            {
                var konto = _konten[KontoFeld.SelectedIndex];
                var kategorie = _kategorien[GegenFeld.SelectedIndex];

                Dienste.Buchungen.Anlegen(new Buchung
                {
                    MandantNr = MandantNr,
                    Datum = datum,
                    KontoId = konto.KontoId,
                    KategorieId = kategorie.KategorieId,
                    BetragCent = Feldwerte.Cent(betrag),
                    Text = TextFeld.Text.Trim()
                });

                Meldungen.Erfolg($"Buchung über {betrag:N2} auf {konto.Bezeichnung} erfasst.");
            }
            else
            {
                var vonKonto = _konten[KontoFeld.SelectedIndex];
                var nachKonto = _konten[GegenFeld.SelectedIndex];

                if (vonKonto.KontoId == nachKonto.KontoId)
                {
                    Meldungen.Fehler("Von-Konto und Nach-Konto dürfen nicht gleich sein.");
                    return;
                }

                Dienste.Umbuchungen.Anlegen(new Umbuchung
                {
                    MandantNr = MandantNr,
                    Datum = datum,
                    VonKontoId = vonKonto.KontoId,
                    NachKontoId = nachKonto.KontoId,
                    BetragCent = Feldwerte.Cent(betrag),
                    Text = TextFeld.Text.Trim()
                });

                Meldungen.Erfolg($"Umbuchung über {betrag:N2} von {vonKonto.Bezeichnung} nach {nachKonto.Bezeichnung} erfasst.");
            }
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        BetragFeld.Text = "";
        TextFeld.Text = "";
        Aktualisieren();
    }

    private void LoescheAusgewaehlte()
    {
        if (Liste.SelectedItems.Count == 0)
        {
            return;
        }

        try
        {
            switch (Liste.SelectedItems[0].Tag)
            {
                case Buchung buchung:
                    Dienste.Buchungen.Loeschen(MandantNr, buchung.BuchungId);
                    Meldungen.Erfolg("Buchung gelöscht.");
                    break;
                case Umbuchung umbuchung:
                    Dienste.Umbuchungen.Loeschen(MandantNr, umbuchung.UmbuchungId);
                    Meldungen.Erfolg("Umbuchung gelöscht.");
                    break;
            }
        }
        catch (FachlicherFehler fehler)
        {
            Meldungen.Fehler(fehler.Message);
            return;
        }

        Aktualisieren();
    }

    private string KontoName(int kontoId) => _konten.Find(k => k.KontoId == kontoId)?.Bezeichnung ?? $"#{kontoId}";

    private static string BetragText(long cent) => Feldwerte.AusCent(cent).ToString("N2", Anzeigekultur);

    private static bool TryParseBetrag(string text, out decimal wert) =>
        decimal.TryParse(text, NumberStyles.Number, Anzeigekultur, out wert) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out wert);
}
