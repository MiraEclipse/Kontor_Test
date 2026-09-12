using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Buchungssatz
{
    public static decimal Netto(Belegposition p) => Betrag.Runde(p.Menge * p.Einzelpreis);

    public static decimal Steuer(Belegposition p) => Betrag.VomHundert(Netto(p), p.SteuerSatz);

    public static decimal Brutto(Belegposition p) => Netto(p) + Steuer(p);

    public static decimal Bruttosumme(IEnumerable<Belegposition> positionen)
    {
        decimal summe = 0m;
        foreach (var p in positionen)
        {
            summe += Brutto(p);
        }

        return summe;
    }

    public static List<Buchungszeile> Erzeuge(Belegart art, IReadOnlyList<Belegposition> positionen, Kontenzuordnung konten)
    {
        if (positionen.Count == 0)
        {
            throw new FachlicherFehler("Der Beleg enthält keine Position.");
        }

        foreach (var p in positionen)
        {
            if (Netto(p) <= 0m)
            {
                throw new FachlicherFehler($"Position {p.PosNr} hat keinen positiven Betrag.");
            }

            konten.Schluessel(p.SteuerSatz);
        }

        return art switch
        {
            Belegart.Rechnung => Erloesbuchung(positionen, konten, SollHaben.Soll),
            Belegart.Gutschrift => Erloesbuchung(positionen, konten, SollHaben.Haben),
            Belegart.Zahlung => Zahlungsbuchung(positionen, konten),
            _ => throw new FachlicherFehler("Unbekannte Belegart.")
        };
    }

    private static List<Buchungszeile> Erloesbuchung(IReadOnlyList<Belegposition> positionen, Kontenzuordnung konten, SollHaben forderungsseite)
    {
        var gegenseite = SollHabenCodes.Gegenseite(forderungsseite);
        var zeilen = new List<Buchungszeile>();
        decimal brutto = 0m;

        foreach (var satz in konten.Steuersaetze)
        {
            decimal netto = 0m;
            decimal steuer = 0m;

            foreach (var p in positionen)
            {
                if (p.SteuerSatz == satz)
                {
                    netto += Netto(p);
                    steuer += Steuer(p);
                }
            }

            if (netto == 0m)
            {
                continue;
            }

            var schluessel = konten.Schluessel(satz);
            zeilen.Add(new Buchungszeile { KontoNr = schluessel.Erloeskonto, Betrag = netto, SollHaben = gegenseite });

            if (steuer > 0m)
            {
                zeilen.Add(new Buchungszeile { KontoNr = schluessel.Steuerkonto, Betrag = steuer, SollHaben = gegenseite });
            }

            brutto += netto + steuer;
        }

        zeilen.Insert(0, new Buchungszeile { KontoNr = konten.Forderungen, Betrag = brutto, SollHaben = forderungsseite });
        return zeilen;
    }

    private static List<Buchungszeile> Zahlungsbuchung(IReadOnlyList<Belegposition> positionen, Kontenzuordnung konten)
    {
        var brutto = Bruttosumme(positionen);
        return new List<Buchungszeile>
        {
            new() { KontoNr = konten.Bank, Betrag = brutto, SollHaben = SollHaben.Soll },
            new() { KontoNr = konten.Forderungen, Betrag = brutto, SollHaben = SollHaben.Haben }
        };
    }
}
