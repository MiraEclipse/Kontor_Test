using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Vertragslauf
{
    // Liefert je seit dem letzten Lauf fällig gewordenem Termin eine zu buchende Position, mit dem
    // zum Fälligkeitstag gültigen Preis. Termine, die laut bereitsGebucht schon eine Buchung haben,
    // werden übersprungen - mehrfacher Aufruf legt so nichts doppelt an. Fehlt zu einem fälligen
    // Termin ein gültiger Preis, wird dafür ein Fehler statt einer Position zurückgegeben.
    public static (IReadOnlyList<VertragslaufPosition> Positionen, IReadOnlyList<string> Fehler) ZuBuchendeTermine(
        Vertrag vertrag,
        IReadOnlyList<Vertragspreis> preise,
        IReadOnlyList<DateOnly> bereitsGebucht,
        DateOnly stichtag)
    {
        if (vertrag.Beendet)
        {
            return (Array.Empty<VertragslaufPosition>(), Array.Empty<string>());
        }

        var bis = vertrag.GekuendigtZum is { } gekuendigtZum && gekuendigtZum < stichtag ? gekuendigtZum : stichtag;
        var termine = Faelligkeiten.Termine(vertrag.Turnus, vertrag.Beginn, vertrag.Beginn, bis);

        var positionen = new List<VertragslaufPosition>();
        var fehler = new List<string>();

        foreach (var termin in termine)
        {
            if (bereitsGebucht.Contains(termin))
            {
                continue;
            }

            var preis = Preisentwicklung.AktuellerPreis(preise, termin);

            if (preis is null)
            {
                fehler.Add($"Für die Fälligkeit {termin:yyyy-MM-dd} des Vertrags \"{vertrag.Bezeichnung}\" ist kein Preis hinterlegt.");
                continue;
            }

            positionen.Add(new VertragslaufPosition(termin, preis.BetragCent));
        }

        return (positionen, fehler);
    }
}
