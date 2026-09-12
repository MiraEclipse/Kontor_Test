using Kontor.Core.Modell;

namespace Kontor.Tests;

internal static class Testbelege
{
    public static Beleg Rechnung(int mandantNr, DateTime belegdatum, decimal menge, decimal einzelpreis, decimal steuerSatz = 19m)
    {
        var beleg = new Beleg
        {
            MandantNr = mandantNr,
            Belegart = Belegart.Rechnung,
            Belegdatum = belegdatum
        };

        beleg.Positionen.Add(new Belegposition
        {
            PosNr = 1,
            Bezeichnung = "Beratungsleistung",
            Einheit = "STD",
            Menge = menge,
            Einzelpreis = einzelpreis,
            SteuerSatz = steuerSatz
        });

        return beleg;
    }

    public static Beleg MitZeilen(int mandantNr, DateTime belegdatum, params Buchungszeile[] zeilen)
    {
        var beleg = Rechnung(mandantNr, belegdatum, 1m, 100m);
        beleg.Zeilen.Clear();

        foreach (var zeile in zeilen)
        {
            beleg.Zeilen.Add(zeile);
        }

        return beleg;
    }

    public static Buchungszeile Zeile(string kontoNr, decimal betrag, SollHaben sollHaben) => new()
    {
        KontoNr = kontoNr,
        Betrag = betrag,
        SollHaben = sollHaben
    };
}
