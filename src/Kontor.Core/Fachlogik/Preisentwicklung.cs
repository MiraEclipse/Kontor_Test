using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Preisentwicklung
{
    // Der aktuelle Preis ist der jüngste Satz der Preishistorie, dessen Gültigkeit zum Stichtag
    // erreicht ist.
    public static Vertragspreis? AktuellerPreis(IReadOnlyList<Vertragspreis> preise, DateOnly stichtag)
    {
        Vertragspreis? aktuell = null;

        foreach (var preis in preise)
        {
            if (preis.GueltigAb <= stichtag && (aktuell is null || preis.GueltigAb > aktuell.GueltigAb))
            {
                aktuell = preis;
            }
        }

        return aktuell;
    }

    public static (Preisentwicklungsergebnis? Ergebnis, IReadOnlyList<string> Fehler) Berechne(
        IReadOnlyList<Vertragspreis> preise,
        Turnus turnus,
        DateOnly stichtag,
        IReadOnlyList<Buchung> zahlungen)
    {
        if (preise.Count == 0)
        {
            return (null, new[] { "Für den Vertrag ist noch kein Preis erfasst." });
        }

        var aktuell = AktuellerPreis(preise, stichtag);
        if (aktuell is null)
        {
            return (null, new[] { $"Zum {stichtag:yyyy-MM-dd} ist noch kein Preis gültig." });
        }

        var ersterPreis = preise[0];
        foreach (var preis in preise)
        {
            if (preis.GueltigAb < ersterPreis.GueltigAb)
            {
                ersterPreis = preis;
            }
        }

        var jahreskostenCent = aktuell.BetragCent * (12 / Turnusse.Monate(turnus));

        long bisherGezahltCent = 0;
        DateOnly? ersteZahlung = null;
        foreach (var zahlung in zahlungen)
        {
            bisherGezahltCent += zahlung.BetragCent;
            if (ersteZahlung is null || zahlung.Datum < ersteZahlung.Value)
            {
                ersteZahlung = zahlung.Datum;
            }
        }

        var tageSeitErsterZahlung = ersteZahlung is null ? 0 : stichtag.DayNumber - ersteZahlung.Value.DayNumber;

        var steigerungProzent = ersterPreis.BetragCent == 0
            ? 0m
            : Betrag.Runde((aktuell.BetragCent - ersterPreis.BetragCent) * 100m / ersterPreis.BetragCent);

        return (
            new Preisentwicklungsergebnis(aktuell.BetragCent, jahreskostenCent, bisherGezahltCent, tageSeitErsterZahlung, steigerungProzent),
            Array.Empty<string>());
    }
}
