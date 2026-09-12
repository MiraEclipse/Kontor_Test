using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Belegpruefung
{
    public static decimal SummeSoll(IEnumerable<Buchungszeile> zeilen)
    {
        decimal summe = 0m;
        foreach (var z in zeilen)
        {
            if (z.SollHaben == SollHaben.Soll)
            {
                summe += z.Betrag;
            }
        }

        return summe;
    }

    public static decimal SummeHaben(IEnumerable<Buchungszeile> zeilen)
    {
        decimal summe = 0m;
        foreach (var z in zeilen)
        {
            if (z.SollHaben == SollHaben.Haben)
            {
                summe += z.Betrag;
            }
        }

        return summe;
    }

    public static void PruefeSollGleichHaben(IReadOnlyList<Buchungszeile> zeilen)
    {
        if (zeilen.Count == 0)
        {
            throw new FachlicherFehler("Der Beleg enthält keine Buchungszeile.");
        }

        foreach (var z in zeilen)
        {
            if (string.IsNullOrWhiteSpace(z.KontoNr))
            {
                throw new FachlicherFehler("Buchungszeile ohne Konto.");
            }

            if (z.Betrag <= 0m)
            {
                throw new FachlicherFehler($"Buchungszeile auf Konto {z.KontoNr} hat keinen positiven Betrag.");
            }
        }

        var soll = Betrag.Runde(SummeSoll(zeilen));
        var haben = Betrag.Runde(SummeHaben(zeilen));

        if (soll != haben)
        {
            throw new FachlicherFehler($"Soll {soll:N2} ungleich Haben {haben:N2}. Beleg wird nicht gebucht.");
        }
    }

    public static void PruefeKopf(Beleg beleg)
    {
        if (beleg.MandantNr <= 0)
        {
            throw new FachlicherFehler("Beleg ohne Mandant.");
        }

        if (beleg.Belegdatum == default)
        {
            throw new FachlicherFehler("Beleg ohne Belegdatum.");
        }
    }
}
