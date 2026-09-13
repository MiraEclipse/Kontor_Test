using Kontor.App.Rahmen;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class KategorienTeilmaske : StammdatenPanel
{
    private readonly TextBox BezeichnungFeld = new();
    private readonly ComboBox RichtungFeld = new();

    private Kategorie? _geladeneKategorie;

    public KategorienTeilmaske(Modulkontext kontext) : base(kontext)
    {
        Liste.Font = new Font("Courier New", 9f);
        Liste.Columns.Add("Bezeichnung", 240);
        Liste.Columns.Add("Richtung", 110);
        Liste.Columns.Add("Gesperrt", 80);

        RichtungFeld.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (Richtung richtung in Enum.GetValues<Richtung>())
        {
            RichtungFeld.Items.Add(Richtungen.Bezeichnung(richtung));
        }

        BezeichnungFeld.Size = new Size(320, 20);
        RichtungFeld.Size = new Size(160, 21);

        AddZeile("&Bezeichnung:", BezeichnungFeld, 20);
        AddZeile("&Richtung:", RichtungFeld, 44);
    }

    protected override IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte)
    {
        foreach (var kategorie in Dienste.Kategorien.Liste(MandantNr, auchGesperrte))
        {
            if (suchbegriff.Length > 0 && !kategorie.Bezeichnung.Contains(suchbegriff, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var zeile = new ListViewItem(kategorie.Bezeichnung) { Tag = kategorie };
            zeile.SubItems.Add(Richtungen.Bezeichnung(kategorie.Richtung));
            zeile.SubItems.Add(kategorie.Gesperrt ? "ja" : "");
            yield return zeile;
        }
    }

    protected override void ZeigeEintrag(object eintrag)
    {
        _geladeneKategorie = (Kategorie)eintrag;

        BezeichnungFeld.Text = _geladeneKategorie.Bezeichnung;
        RichtungFeld.SelectedItem = Richtungen.Bezeichnung(_geladeneKategorie.Richtung);

        SperrenKnopf.Text = _geladeneKategorie.Gesperrt ? "&Entsperren" : "&Sperren";
    }

    protected override void LeereDetailfelder()
    {
        _geladeneKategorie = null;

        BezeichnungFeld.Text = "";
        RichtungFeld.SelectedIndex = 1;

        SperrenKnopf.Text = "&Sperren";
    }

    protected override void SetzeDetailfelderSchreibbar(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);
        BezeichnungFeld.ReadOnly = !schreibend;
        BezeichnungFeld.BackColor = schreibend ? SystemColors.Window : SystemColors.Control;
        RichtungFeld.Enabled = schreibend;
    }

    protected override IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung)
    {
        var fehler = new List<string>();

        if (string.IsNullOrWhiteSpace(BezeichnungFeld.Text))
        {
            fehler.Add("Die Bezeichnung muss gesetzt sein.");
        }

        if (RichtungFeld.SelectedIndex < 0)
        {
            fehler.Add("Die Richtung muss gesetzt sein.");
        }

        if (fehler.Count > 0)
        {
            erfolgsmeldung = "";
            return fehler;
        }

        var kategorie = new Kategorie
        {
            KategorieId = _geladeneKategorie?.KategorieId ?? 0,
            MandantNr = MandantNr,
            Bezeichnung = BezeichnungFeld.Text.Trim(),
            Richtung = Enum.GetValues<Richtung>()[RichtungFeld.SelectedIndex],
            Gesperrt = _geladeneKategorie?.Gesperrt ?? false
        };

        if (kategorie.KategorieId == 0)
        {
            Dienste.Kategorien.Anlegen(kategorie);
            erfolgsmeldung = $"Kategorie {kategorie.Bezeichnung} angelegt.";
        }
        else
        {
            Dienste.Kategorien.Aendern(kategorie);
            erfolgsmeldung = $"Kategorie {kategorie.Bezeichnung} gesichert.";
        }

        return fehler;
    }

    protected override void FokusiereErstesFeld() => BezeichnungFeld.Focus();

    protected override void FuehreSperrenOderEntsperrenAus(out string nachricht)
    {
        var kategorie = (Kategorie)AusgewaehlterEintrag!;

        if (kategorie.Gesperrt)
        {
            Dienste.Kategorien.Entsperren(MandantNr, kategorie.KategorieId);
            nachricht = $"Kategorie {kategorie.Bezeichnung} entsperrt.";
        }
        else
        {
            Dienste.Kategorien.Sperren(MandantNr, kategorie.KategorieId);
            nachricht = $"Kategorie {kategorie.Bezeichnung} gesperrt.";
        }
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
