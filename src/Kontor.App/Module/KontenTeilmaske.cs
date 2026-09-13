using System.Globalization;
using Kontor.App.Rahmen;
using Kontor.Core.Modell;
using Kontor.Data;

namespace Kontor.App.Module;

public sealed class KontenTeilmaske : StammdatenPanel
{
    private static readonly CultureInfo Anzeigekultur = CultureInfo.GetCultureInfo("de-DE");

    private readonly TextBox BezeichnungFeld = new();
    private readonly ComboBox ArtFeld = new();
    private readonly TextBox AnfangsbestandFeld = new();

    private Konto? _geladenesKonto;

    public KontenTeilmaske(Modulkontext kontext) : base(kontext)
    {
        Liste.Font = new Font("Courier New", 9f);
        Liste.Columns.Add("Bezeichnung", 220);
        Liste.Columns.Add("Art", 110);
        var standSpalte = Liste.Columns.Add("Anfangsbestand", 120);
        standSpalte.TextAlign = HorizontalAlignment.Right;
        Liste.Columns.Add("Gesperrt", 80);

        ArtFeld.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (Kontoart art in Enum.GetValues<Kontoart>())
        {
            ArtFeld.Items.Add(Kontoarten.Bezeichnung(art));
        }

        BezeichnungFeld.Size = new Size(320, 20);
        ArtFeld.Size = new Size(160, 21);
        AnfangsbestandFeld.Size = new Size(120, 20);
        AnfangsbestandFeld.Font = new Font("Courier New", 9f);
        AnfangsbestandFeld.TextAlign = HorizontalAlignment.Right;

        AddZeile("&Bezeichnung:", BezeichnungFeld, 20);
        AddZeile("&Art:", ArtFeld, 44);
        AddZeile("&Anfangsbestand:", AnfangsbestandFeld, 68);
    }

    protected override IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte)
    {
        foreach (var konto in Dienste.Konten.Liste(MandantNr, auchGesperrte))
        {
            if (suchbegriff.Length > 0 && !konto.Bezeichnung.Contains(suchbegriff, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var zeile = new ListViewItem(konto.Bezeichnung) { Tag = konto };
            zeile.SubItems.Add(Kontoarten.Bezeichnung(konto.Art));
            zeile.SubItems.Add(Feldwerte.AusCent(konto.AnfangsbestandCent).ToString("N2", Anzeigekultur));
            zeile.SubItems.Add(konto.Gesperrt ? "ja" : "");
            yield return zeile;
        }
    }

    protected override void ZeigeEintrag(object eintrag)
    {
        _geladenesKonto = (Konto)eintrag;

        BezeichnungFeld.Text = _geladenesKonto.Bezeichnung;
        ArtFeld.SelectedItem = Kontoarten.Bezeichnung(_geladenesKonto.Art);
        AnfangsbestandFeld.Text = Feldwerte.AusCent(_geladenesKonto.AnfangsbestandCent).ToString("N2", Anzeigekultur);

        SperrenKnopf.Text = _geladenesKonto.Gesperrt ? "&Entsperren" : "&Sperren";
    }

    protected override void LeereDetailfelder()
    {
        _geladenesKonto = null;

        BezeichnungFeld.Text = "";
        ArtFeld.SelectedIndex = 0;
        AnfangsbestandFeld.Text = "0,00";

        SperrenKnopf.Text = "&Sperren";
    }

    protected override void SetzeDetailfelderSchreibbar(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);
        SetzeFeld(BezeichnungFeld, schreibend);
        ArtFeld.Enabled = schreibend;
        SetzeFeld(AnfangsbestandFeld, schreibend);
    }

    protected override IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung)
    {
        var fehler = new List<string>();

        if (string.IsNullOrWhiteSpace(BezeichnungFeld.Text))
        {
            fehler.Add("Die Bezeichnung muss gesetzt sein.");
        }

        if (!TryParseBetrag(AnfangsbestandFeld.Text, out var anfangsbestand))
        {
            fehler.Add($"Der Anfangsbestand \"{AnfangsbestandFeld.Text}\" ist kein gültiger Betrag.");
        }

        if (ArtFeld.SelectedIndex < 0)
        {
            fehler.Add("Die Kontoart muss gesetzt sein.");
        }

        if (fehler.Count > 0)
        {
            erfolgsmeldung = "";
            return fehler;
        }

        var konto = new Konto
        {
            KontoId = _geladenesKonto?.KontoId ?? 0,
            MandantNr = MandantNr,
            Bezeichnung = BezeichnungFeld.Text.Trim(),
            Art = Enum.GetValues<Kontoart>()[ArtFeld.SelectedIndex],
            AnfangsbestandCent = Feldwerte.Cent(anfangsbestand),
            Gesperrt = _geladenesKonto?.Gesperrt ?? false
        };

        if (konto.KontoId == 0)
        {
            Dienste.Konten.Anlegen(konto);
            erfolgsmeldung = $"Konto {konto.Bezeichnung} angelegt.";
        }
        else
        {
            Dienste.Konten.Aendern(konto);
            erfolgsmeldung = $"Konto {konto.Bezeichnung} gesichert.";
        }

        return fehler;
    }

    protected override void FokusiereErstesFeld() => BezeichnungFeld.Focus();

    protected override void FuehreSperrenOderEntsperrenAus(out string nachricht)
    {
        var konto = (Konto)AusgewaehlterEintrag!;

        if (konto.Gesperrt)
        {
            Dienste.Konten.Entsperren(MandantNr, konto.KontoId);
            nachricht = $"Konto {konto.Bezeichnung} entsperrt.";
        }
        else
        {
            Dienste.Konten.Sperren(MandantNr, konto.KontoId);
            nachricht = $"Konto {konto.Bezeichnung} gesperrt.";
        }
    }

    private static bool TryParseBetrag(string text, out decimal wert) =>
        decimal.TryParse(text, NumberStyles.Number, Anzeigekultur, out wert) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out wert);

    private static void SetzeFeld(TextBox feld, bool schreibbar)
    {
        feld.ReadOnly = !schreibbar;
        feld.BackColor = schreibbar ? SystemColors.Window : SystemColors.Control;
    }

    private void AddZeile(string beschriftung, Control feld, int oben)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(8, oben + 3),
            Size = new Size(110, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(126, oben);

        Detailgruppe.Controls.Add(text);
        Detailgruppe.Controls.Add(feld);
    }
}
