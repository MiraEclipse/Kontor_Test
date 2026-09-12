using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class KundenMaske : StammdatenFenster
{
    private readonly TextBox NummerFeld = new();
    private readonly TextBox NameFeld = new();
    private readonly TextBox StrasseFeld = new();
    private readonly TextBox PlzFeld = new();
    private readonly TextBox OrtFeld = new();
    private readonly TextBox TelefonFeld = new();
    private readonly TextBox UstIdNrFeld = new();

    private Kunde? _geladenerKunde;

    public KundenMaske(Modulkontext kontext) : base("K01", "Kunden", kontext)
    {
        Liste.Columns.Add("Nummer", 90);
        Liste.Columns.Add("Name", 220);
        Liste.Columns.Add("Ort", 140);
        Liste.Columns.Add("Gesperrt", 90);

        AddFeld("&Nummer:", NummerFeld, 20, 10);
        AddFeld("N&ame:", NameFeld, 44, 11);
        AddFeld("&Straße:", StrasseFeld, 68, 12);
        AddFeld("&PLZ:", PlzFeld, 92, 13);
        AddFeld("&Ort:", OrtFeld, 116, 14);
        AddFeld("&Telefon:", TelefonFeld, 140, 15);
        AddFeld("&USt-IdNr.:", UstIdNrFeld, 164, 16);
    }

    protected override IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte)
    {
        foreach (var kunde in Dienste.Kunden.Suche(MandantNr, suchbegriff, auchGesperrte))
        {
            var zeile = new ListViewItem(kunde.Nummer) { Tag = kunde };
            zeile.SubItems.Add(kunde.Name);
            zeile.SubItems.Add(kunde.Ort);
            zeile.SubItems.Add(kunde.Gesperrt ? "Ja" : "");
            yield return zeile;
        }
    }

    protected override void ZeigeEintrag(object eintrag)
    {
        _geladenerKunde = (Kunde)eintrag;

        NummerFeld.Text = _geladenerKunde.Nummer;
        NameFeld.Text = _geladenerKunde.Name;
        StrasseFeld.Text = _geladenerKunde.Strasse;
        PlzFeld.Text = _geladenerKunde.Plz;
        OrtFeld.Text = _geladenerKunde.Ort;
        TelefonFeld.Text = _geladenerKunde.Telefon;
        UstIdNrFeld.Text = _geladenerKunde.UstIdNr;

        SperrenKnopf.Text = _geladenerKunde.Gesperrt ? "&Entsperren" : "&Sperren";
    }

    protected override void LeereDetailfelder()
    {
        _geladenerKunde = null;

        NummerFeld.Text = "";
        NameFeld.Text = "";
        StrasseFeld.Text = "";
        PlzFeld.Text = "";
        OrtFeld.Text = "";
        TelefonFeld.Text = "";
        UstIdNrFeld.Text = "";

        SperrenKnopf.Text = "&Sperren";
    }

    protected override void SetzeVorgeschlageneNummer() =>
        NummerFeld.Text = Dienste.Kunden.NaechsteFreieNummer(MandantNr);

    protected override void SetzeDetailfelderSchreibbar(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);

        SetzeFeld(NummerFeld, modus == MaskenModus.Erfassen);
        SetzeFeld(NameFeld, schreibend);
        SetzeFeld(StrasseFeld, schreibend);
        SetzeFeld(PlzFeld, schreibend);
        SetzeFeld(OrtFeld, schreibend);
        SetzeFeld(TelefonFeld, schreibend);
        SetzeFeld(UstIdNrFeld, schreibend);
    }

    protected override IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung)
    {
        var kunde = new Kunde
        {
            KundeId = _geladenerKunde?.KundeId ?? 0,
            MandantNr = MandantNr,
            Nummer = NummerFeld.Text.Trim(),
            Name = NameFeld.Text.Trim(),
            Strasse = StrasseFeld.Text.Trim(),
            Plz = PlzFeld.Text.Trim(),
            Ort = OrtFeld.Text.Trim(),
            Telefon = TelefonFeld.Text.Trim(),
            UstIdNr = UstIdNrFeld.Text.Trim(),
            Gesperrt = _geladenerKunde?.Gesperrt ?? false
        };

        var vorhandener = Dienste.Kunden.LadeMitNummer(MandantNr, kunde.Nummer);
        var nummerBereitsVergeben = vorhandener is not null && vorhandener.KundeId != kunde.KundeId;

        var fehler = KundeValidierung.Pruefe(kunde, nummerBereitsVergeben);

        if (fehler.Count > 0)
        {
            erfolgsmeldung = "";
            return fehler;
        }

        if (kunde.KundeId == 0)
        {
            Dienste.Kunden.Anlegen(kunde);
            erfolgsmeldung = $"Kunde {kunde.Nummer} angelegt.";
        }
        else
        {
            Dienste.Kunden.Aendern(kunde);
            erfolgsmeldung = $"Kunde {kunde.Nummer} gesichert.";
        }

        return fehler;
    }

    protected override void FokusiereErstesFeld() =>
        (Modus == MaskenModus.Erfassen ? NummerFeld : NameFeld).Focus();

    protected override void FokusiereFeldFuerFehler(string fehler)
    {
        var ziel = fehler switch
        {
            _ when fehler.Contains("Kundennummer") => NummerFeld,
            _ when fehler.Contains("Umsatzsteuer") => UstIdNrFeld,
            _ when fehler.Contains("Name") => NameFeld,
            _ => NameFeld
        };

        ziel.Focus();
        ziel.SelectAll();
    }

    protected override void FuehreSperrenOderEntsperrenAus(out string nachricht)
    {
        var kunde = (Kunde)AusgewaehlterEintrag!;

        if (kunde.Gesperrt)
        {
            Dienste.Kunden.Entsperren(MandantNr, kunde.KundeId);
            nachricht = $"Kunde {kunde.Nummer} entsperrt.";
        }
        else
        {
            Dienste.Kunden.Sperren(MandantNr, kunde.KundeId);
            nachricht = $"Kunde {kunde.Nummer} gesperrt.";
        }
    }

    private static void SetzeFeld(TextBox feld, bool schreibbar)
    {
        feld.ReadOnly = !schreibbar;
        feld.BackColor = schreibbar ? SystemColors.Window : SystemColors.Control;
    }

    private void AddFeld(string beschriftung, TextBox feld, int oben, int tabIndex)
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
        feld.Size = new Size(440, 20);
        feld.TabIndex = tabIndex;
        feld.TextChanged += (_, _) => MarkiereGeaendert();

        Detailgruppe.Controls.Add(text);
        Detailgruppe.Controls.Add(feld);
    }
}
