namespace Kontor.Core.Fachlogik;

public static class Betrag
{
    public static decimal Runde(decimal wert) => Math.Round(wert, 2, MidpointRounding.AwayFromZero);

    public static decimal VomHundert(decimal grundwert, decimal satz) => Runde(grundwert * satz / 100m);

    public static decimal ImHundert(decimal teilwert, decimal satz)
    {
        if (satz >= 100m)
        {
            throw new FachlicherFehler("Satz für die Rechnung im Hundert muss kleiner als 100 % sein.");
        }

        return Runde(teilwert / (1m - satz / 100m));
    }
}
