using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Belegnummer
{
    public static string Bilden(Belegart art, int jahr, int laufendeNummer)
    {
        if (laufendeNummer < 1)
        {
            throw new FachlicherFehler("Laufende Belegnummer muss größer als null sein.");
        }

        return $"{Belegarten.Code(art)}-{jahr:0000}-{laufendeNummer:00000}";
    }
}
