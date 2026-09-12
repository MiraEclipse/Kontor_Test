using System.Globalization;
using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class ArtikelMaske : StammdatenFenster
{
    private static readonly CultureInfo Anzeigekultur = CultureInfo.GetCultureInfo("de-DE");

    private readonly TextBox NummerFeld = new();
    private readonly TextBox BezeichnungFeld = new();
    private readonly TextBox EinheitFeld = new();
    private readonly TextBox PreisFeld = new();
    private readonly ComboBox SteuerSatzFeld = new();

    private Artikel? _geladenerArtikel;

    public ArtikelMaske(Modulkontext kontext) : base("K02", "Artikel", kontext)
    {
        Liste.Font = new Font("Courier New", 9f);
        Liste.Columns.Add("Nummer", 90);
        Liste.Columns.Add("Bezeichnung", 210);
        Liste.Columns.Add("Einheit", 60);
        var preisSpalte = Liste.Columns.Add("Preis", 90);
        preisSpalte.TextAlign = HorizontalAlignment.Right;
        Liste.Columns.Add("Steuersatz", 80);

        SteuerSatzFeld.DropDownStyle = ComboBoxStyle.DropDownList;
        SteuerSatzFeld.Items.AddRange(new object[] { "19", "7", "0" });

        NummerFeld.Size = new Size(200, 20);
        BezeichnungFeld.Size = new Size(320, 20);
        EinheitFeld.Size = new Size(80, 20);
        PreisFeld.Size = new Size(120, 20);
        PreisFeld.Font = new Font("Courier New", 9f);
        PreisFeld.TextAlign = HorizontalAlignment.Right;
        SteuerSatzFeld.Size = new Size(80, 21);

        AddZeile("&Nummer:", NummerFeld, 20, 10);
        AddZeile("&Bezeichnung:", BezeichnungFeld, 44, 11);
        AddZeile("&Einheit:", EinheitFeld, 68, 12);
        AddZeile("&Preis:", PreisFeld, 92, 13);
        AddZeile("&Steuersatz:", SteuerSatzFeld, 116, 14);
    }

    protected override IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte)
    {
        foreach (var artikel in Dienste.Artikel.Suche(MandantNr, suchbegriff, auchGesperrte))
        {
            var zeile = new ListViewItem(artikel.Nummer) { Tag = artikel };
            zeile.SubItems.Add(artikel.Bezeichnung);
            zeile.SubItems.Add(artikel.Einheit);
            zeile.SubItems.Add(artikel.Preis.ToString("N2", Anzeigekultur));
            zeile.SubItems.Add(artikel.SteuerSatz.ToString("0", CultureInfo.InvariantCulture) + " %");
            yield return zeile;
        }
    }

    protected override void ZeigeEintrag(object eintrag)
    {
        _geladenerArtikel = (Artikel)eintrag;

        NummerFeld.Text = _geladenerArtikel.Nummer;
        BezeichnungFeld.Text = _geladenerArtikel.Bezeichnung;
        EinheitFeld.Text = _geladenerArtikel.Einheit;
        PreisFeld.Text = _geladenerArtikel.Preis.ToString("N2", Anzeigekultur);
        SteuerSatzFeld.Text = _geladenerArtikel.SteuerSatz.ToString("0", CultureInfo.InvariantCulture);

        SperrenKnopf.Text = _geladenerArtikel.Gesperrt ? "&Entsperren" : "&Sperren";
    }

    protected override void LeereDetailfelder()
    {
        _geladenerArtikel = null;

        NummerFeld.Text = "";
        BezeichnungFeld.Text = "";
        EinheitFeld.Text = "ST";
        PreisFeld.Text = "";
        SteuerSatzFeld.Text = "19";

        SperrenKnopf.Text = "&Sperren";
    }

    protected override void SetzeVorgeschlageneNummer() =>
        NummerFeld.Text = Dienste.Artikel.NaechsteFreieNummer(MandantNr);

    protected override void SetzeDetailfelderSchreibbar(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);

        SetzeFeld(NummerFeld, modus == MaskenModus.Erfassen);
        SetzeFeld(BezeichnungFeld, schreibend);
        SetzeFeld(EinheitFeld, schreibend);
        SetzeFeld(PreisFeld, schreibend);
        SteuerSatzFeld.Enabled = schreibend;
    }

    protected override IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung)
    {
        if (!TryParsePreis(PreisFeld.Text, out var preis))
        {
            erfolgsmeldung = "";
            return new[] { $"Der Preis \"{PreisFeld.Text}\" ist keine gültige Zahl." };
        }

        var steuerSatz = SteuerSatzFeld.SelectedItem is string text
            ? decimal.Parse(text, CultureInfo.InvariantCulture)
            : -1m;

        var artikel = new Artikel
        {
            ArtikelId = _geladenerArtikel?.ArtikelId ?? 0,
            MandantNr = MandantNr,
            Nummer = NummerFeld.Text.Trim(),
            Bezeichnung = BezeichnungFeld.Text.Trim(),
            Einheit = EinheitFeld.Text.Trim(),
            Preis = preis,
            SteuerSatz = steuerSatz,
            Gesperrt = _geladenerArtikel?.Gesperrt ?? false
        };

        var vorhandener = Dienste.Artikel.LadeMitNummer(MandantNr, artikel.Nummer);
        var nummerBereitsVergeben = vorhandener is not null && vorhandener.ArtikelId != artikel.ArtikelId;

        var fehler = ArtikelValidierung.Pruefe(artikel, nummerBereitsVergeben);

        if (fehler.Count > 0)
        {
            erfolgsmeldung = "";
            return fehler;
        }

        if (artikel.ArtikelId == 0)
        {
            Dienste.Artikel.Anlegen(artikel);
            erfolgsmeldung = $"Artikel {artikel.Nummer} angelegt.";
        }
        else
        {
            Dienste.Artikel.Aendern(artikel);
            erfolgsmeldung = $"Artikel {artikel.Nummer} gesichert.";
        }

        return fehler;
    }

    protected override void FokusiereErstesFeld() =>
        (Modus == MaskenModus.Erfassen ? NummerFeld : BezeichnungFeld).Focus();

    protected override void FokusiereFeldFuerFehler(string fehler)
    {
        Control ziel = fehler switch
        {
            _ when fehler.Contains("Artikelnummer") => NummerFeld,
            _ when fehler.Contains("Bezeichnung") => BezeichnungFeld,
            _ when fehler.Contains("Einheit") => EinheitFeld,
            _ when fehler.Contains("Preis") => PreisFeld,
            _ when fehler.Contains("Steuersatz") => SteuerSatzFeld,
            _ => BezeichnungFeld
        };

        ziel.Focus();
        if (ziel is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    protected override void FuehreSperrenOderEntsperrenAus(out string nachricht)
    {
        var artikel = (Artikel)AusgewaehlterEintrag!;

        if (artikel.Gesperrt)
        {
            Dienste.Artikel.Entsperren(MandantNr, artikel.ArtikelId);
            nachricht = $"Artikel {artikel.Nummer} entsperrt.";
        }
        else
        {
            Dienste.Artikel.Sperren(MandantNr, artikel.ArtikelId);
            nachricht = $"Artikel {artikel.Nummer} gesperrt.";
        }
    }

    private static bool TryParsePreis(string text, out decimal wert) =>
        decimal.TryParse(text, NumberStyles.Number, Anzeigekultur, out wert) ||
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out wert);

    private static void SetzeFeld(TextBox feld, bool schreibbar)
    {
        feld.ReadOnly = !schreibbar;
        feld.BackColor = schreibbar ? SystemColors.Window : SystemColors.Control;
    }

    private void AddZeile(string beschriftung, Control feld, int oben, int tabIndex)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(12, oben + 3),
            Size = new Size(80, 16),
            TextAlign = ContentAlignment.MiddleLeft,
            TabIndex = tabIndex - 1
        };

        feld.Location = new Point(96, oben);
        feld.TabIndex = tabIndex;

        if (feld is TextBox textBox)
        {
            textBox.TextChanged += (_, _) => MarkiereGeaendert();
        }
        else if (feld is ComboBox comboBox)
        {
            comboBox.SelectedIndexChanged += (_, _) => MarkiereGeaendert();
        }

        Detailgruppe.Controls.Add(text);
        Detailgruppe.Controls.Add(feld);
    }
}
