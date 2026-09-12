using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class ArtikelValidierung
{
    private static readonly decimal[] GueltigeSteuersaetze = { 19m, 7m, 0m };

    public static IReadOnlyList<string> Pruefe(Artikel artikel, bool nummerBereitsVergeben)
    {
        var fehler = new List<string>();

        if (string.IsNullOrWhiteSpace(artikel.Nummer))
        {
            fehler.Add("Die Artikelnummer muss gesetzt sein.");
        }
        else if (nummerBereitsVergeben)
        {
            fehler.Add($"Die Artikelnummer {artikel.Nummer} ist in diesem Mandanten bereits vergeben.");
        }

        if (string.IsNullOrWhiteSpace(artikel.Bezeichnung))
        {
            fehler.Add("Die Bezeichnung muss gesetzt sein.");
        }

        if (string.IsNullOrWhiteSpace(artikel.Einheit))
        {
            fehler.Add("Die Einheit muss gesetzt sein.");
        }

        if (artikel.Preis < 0m)
        {
            fehler.Add("Der Preis darf nicht negativ sein.");
        }

        if (Array.IndexOf(GueltigeSteuersaetze, artikel.SteuerSatz) < 0)
        {
            fehler.Add($"Der Steuersatz {artikel.SteuerSatz:0.##} % ist nicht zulässig. Erlaubt sind 19, 7 oder 0 Prozent.");
        }

        return fehler;
    }
}
