using System.Globalization;
using Kontor.App.Rahmen;
using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;

namespace Kontor.App.Module;

public sealed class UebersichtMaske : ModulFenster
{
    private static readonly CultureInfo Anzeigekultur = CultureInfo.GetCultureInfo("de-DE");

    private readonly ListView KontostaendeListe = new();
    private readonly ListView AusgabenListe = new();
    private readonly ListView KuendigungenListe = new();
    private readonly ListView AufgabenListe = new();

    public UebersichtMaske(Modulkontext kontext) : base("K06", "Übersicht", kontext)
    {
        ClientSize = new Size(680, 560);

        var kontostaende = Gruppe("Kontostände", 12, 12, 656, 120);
        KontostaendeListe.Columns.Add("Konto", 260);
        var standSpalte = KontostaendeListe.Columns.Add("Stand", 120);
        standSpalte.TextAlign = HorizontalAlignment.Right;
        Fertig(kontostaende, KontostaendeListe);

        var ausgaben = Gruppe("Ausgaben je Kategorie (laufender Monat)", 12, 140, 656, 120);
        AusgabenListe.Columns.Add("Kategorie", 260);
        var summeSpalte = AusgabenListe.Columns.Add("Summe", 120);
        summeSpalte.TextAlign = HorizontalAlignment.Right;
        Fertig(ausgaben, AusgabenListe);

        var kuendigungen = Gruppe("Nächste Kündigungsfristen", 12, 268, 656, 120);
        KuendigungenListe.Columns.Add("Vertrag", 220);
        KuendigungenListe.Columns.Add("Kündigungsmöglichkeit", 130);
        KuendigungenListe.Columns.Add("Spätester Absendetag", 130);
        Fertig(kuendigungen, KuendigungenListe);

        var aufgaben = Gruppe("Offene Aufgaben", 12, 396, 656, 130);
        AufgabenListe.Columns.Add("Betreff", 300);
        AufgabenListe.Columns.Add("Fälligkeit", 100);
        AufgabenListe.Columns.Add("Priorität", 90);
        Fertig(aufgaben, AufgabenListe);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Aktualisieren();
    }

    private void Aktualisieren()
    {
        var heute = DateOnly.FromDateTime(DateTime.Today);

        KontostaendeListe.Items.Clear();
        foreach (var konto in Dienste.Konten.Liste(MandantNr, auchGesperrte: false))
        {
            var stand = Dienste.Konten.Kontostand(MandantNr, konto.KontoId, heute);
            var zeile = new ListViewItem(konto.Bezeichnung);
            zeile.SubItems.Add(BetragText(stand));
            KontostaendeListe.Items.Add(zeile);
        }

        var monatsanfang = new DateOnly(heute.Year, heute.Month, 1);
        var kategorien = Dienste.Kategorien.Liste(MandantNr, auchGesperrte: false);
        var buchungenDesMonats = Dienste.Buchungen.Liste(MandantNr, monatsanfang, heute);

        AusgabenListe.Items.Clear();
        foreach (var kategorie in kategorien)
        {
            if (kategorie.Richtung != Richtung.Ausgabe)
            {
                continue;
            }

            long summeCent = 0;
            foreach (var buchung in buchungenDesMonats)
            {
                if (buchung.KategorieId == kategorie.KategorieId)
                {
                    summeCent += buchung.BetragCent;
                }
            }

            if (summeCent == 0)
            {
                continue;
            }

            var zeile = new ListViewItem(kategorie.Bezeichnung);
            zeile.SubItems.Add(BetragText(summeCent));
            AusgabenListe.Items.Add(zeile);
        }

        var termine = new List<(Vertrag Vertrag, Kuendigungstermin Termin)>();
        foreach (var vertrag in Dienste.Vertraege.Liste(MandantNr, auchBeendete: false))
        {
            var termin = Kuendigungsrechner.Naechste(
                vertrag.Beginn, vertrag.MindestlaufzeitMonate, vertrag.KuendigungsfristMonate, vertrag.Turnus, heute);
            termine.Add((vertrag, termin));
        }
        termine.Sort((a, b) => a.Termin.SpaetesterAbsendetag.CompareTo(b.Termin.SpaetesterAbsendetag));

        KuendigungenListe.Items.Clear();
        foreach (var (vertrag, termin) in termine.Take(3))
        {
            var zeile = new ListViewItem(vertrag.Bezeichnung);
            zeile.SubItems.Add(termin.Kuendigungsmoeglichkeit.ToString("yyyy-MM-dd"));
            zeile.SubItems.Add(termin.SpaetesterAbsendetag.ToString("yyyy-MM-dd"));
            KuendigungenListe.Items.Add(zeile);
        }

        AufgabenListe.Items.Clear();
        foreach (var notiz in Dienste.Notizen.Liste(MandantNr, nurOffene: true))
        {
            var zeile = new ListViewItem(notiz.Betreff);
            zeile.SubItems.Add(notiz.Faelligkeit?.ToString("yyyy-MM-dd") ?? "");
            zeile.SubItems.Add(notiz.Prioritaet.ToString());

            if (notiz.Faelligkeit is { } faelligkeit && faelligkeit < heute)
            {
                zeile.Font = new Font(AufgabenListe.Font, FontStyle.Bold);
            }

            AufgabenListe.Items.Add(zeile);
        }
    }

    private GroupBox Gruppe(string titel, int x, int y, int breite, int hoehe)
    {
        var gruppe = new GroupBox { Text = titel, Location = new Point(x, y), Size = new Size(breite, hoehe) };
        Controls.Add(gruppe);
        return gruppe;
    }

    private static void Fertig(GroupBox gruppe, ListView liste)
    {
        liste.Location = new Point(8, 20);
        liste.Size = new Size(gruppe.Width - 16, gruppe.Height - 28);
        liste.View = View.Details;
        liste.FullRowSelect = true;
        liste.GridLines = true;
        liste.MultiSelect = false;
        liste.HideSelection = false;
        liste.Font = new Font("Courier New", 9f);
        gruppe.Controls.Add(liste);
    }

    private static string BetragText(long cent) => Feldwerte.AusCent(cent).ToString("N2", Anzeigekultur);
}
