namespace Kontor.Core.Fachlogik;

public static class Kontostand
{
    // Anfangsbestand plus Einnahmen, minus Ausgaben, plus eingehende, minus ausgehende Umbuchungen.
    // Die Summenbildung selbst läuft in SQL (SUM(BetragCent)); diese Funktion verrechnet nur die
    // bereits summierten Cent-Beträge.
    public static long Berechne(
        long anfangsbestandCent,
        long einnahmenCent,
        long ausgabenCent,
        long umbuchungEinCent,
        long umbuchungAusCent) =>
        anfangsbestandCent + einnahmenCent - ausgabenCent + umbuchungEinCent - umbuchungAusCent;
}
