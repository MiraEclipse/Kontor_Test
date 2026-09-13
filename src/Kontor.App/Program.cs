using Kontor.App.Rahmen;
using Kontor.Core.Modell;
using Kontor.Data;

namespace Kontor.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetCompatibleTextRenderingDefault(false);

        var datenbank = Datenbank.Standard();

        try
        {
            datenbank.Vorbereiten();
        }
        catch (Exception fehler)
        {
            MessageBox.Show(
                $"Die Datenbank {datenbank.Dateipfad} konnte nicht geöffnet werden.\n\n{fehler.Message}",
                "KONTOR",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        // Für Anmeldung und Ersteinrichtung gibt es noch keinen angemeldeten Benutzer - diese
        // Repositories laufen daher mit dem Systemkontext (Systemrechte übersteuern jede Prüfung).
        var systemZugriff = Zugriffskontext.Systemkontext(datenbank);
        var systemBenutzer = new BenutzerRepository(datenbank, systemZugriff);
        var systemRechte = new RechtRepository(datenbank, systemZugriff);
        var mandanten = new MandantRepository(datenbank);
        var anmeldeprotokoll = new AnmeldeprotokollRepository(datenbank);

        if (systemBenutzer.Alle().Count == 0)
        {
            using var einrichtung = new Ersteinrichtungsfenster(systemBenutzer, mandanten);
            if (einrichtung.ShowDialog() != DialogResult.OK || einrichtung.Sitzung is null)
            {
                return;
            }

            StarteHauptfenster(datenbank, einrichtung.Sitzung, systemZugriff);
            return;
        }

        using var anmeldung = new Anmeldefenster(systemBenutzer, systemRechte, mandanten, anmeldeprotokoll);

        if (anmeldung.ShowDialog() != DialogResult.OK || anmeldung.Sitzung is null)
        {
            return;
        }

        StarteHauptfenster(datenbank, anmeldung.Sitzung, systemZugriff);
    }

    private static void StarteHauptfenster(Datenbank datenbank, Sitzung sitzung, Zugriffskontext systemZugriff)
    {
        // Der Vertragslauf beim Programmstart ist eine Wartungsaufgabe des Programms, keine Aktion des
        // angemeldeten Benutzers - er läuft daher immer mit dem Systemkontext, unabhängig davon, welche
        // Rechte auf K03 gerade angemeldet sind.
        var systemVertraege = new VertragRepository(datenbank, systemZugriff);
        var gebucht = FuehreFaelligeVertraegeAus(systemVertraege, sitzung.MandantNr);

        var zugriff = new Zugriffskontext(datenbank, sitzung.Benutzer);
        var dienste = new Dienste(
            zugriff,
            new MandantRepository(datenbank),
            new KontoRepository(datenbank, zugriff),
            new KategorieRepository(datenbank, zugriff),
            new BuchungRepository(datenbank, zugriff),
            new UmbuchungRepository(datenbank, zugriff),
            new VertragRepository(datenbank, zugriff),
            new VertragspreisRepository(datenbank, zugriff),
            new NotizRepository(datenbank, zugriff),
            new BenutzerRepository(datenbank, zugriff),
            new RechtRepository(datenbank, zugriff),
            new AnmeldeprotokollRepository(datenbank));

        var hauptfenster = new Hauptfenster(sitzung, dienste, Modulverzeichnis.Standard());

        if (gebucht > 0)
        {
            hauptfenster.Hinweis($"{gebucht} fällige Buchung(en) aus automatisch geführten Verträgen angelegt.");
        }

        Application.Run(hauptfenster);
    }

    // Beim Programmstart werden für alle automatisch geführten, nicht beendeten Verträge die seit dem
    // letzten Lauf fällig gewordenen Buchungen erzeugt. Mehrfacher Programmstart legt nichts doppelt an.
    private static int FuehreFaelligeVertraegeAus(VertragRepository vertraege, int mandantNr)
    {
        var heute = DateOnly.FromDateTime(DateTime.Today);
        var gebucht = 0;

        foreach (var vertrag in vertraege.Liste(mandantNr, auchBeendete: false))
        {
            if (!vertrag.AutomatischBuchen)
            {
                continue;
            }

            gebucht += vertraege.VertragslaufAusfuehren(mandantNr, vertrag.VertragId, heute);
        }

        return gebucht;
    }
}
