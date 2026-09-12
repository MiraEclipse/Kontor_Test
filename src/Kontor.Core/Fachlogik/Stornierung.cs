using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Stornierung
{
    public static List<Buchungszeile> Spiegele(IReadOnlyList<Buchungszeile> original)
    {
        if (original.Count == 0)
        {
            throw new FachlicherFehler("Der zu stornierende Beleg hat keine Buchungszeilen.");
        }

        var zeilen = new List<Buchungszeile>();
        foreach (var z in original)
        {
            zeilen.Add(new Buchungszeile
            {
                MandantNr = z.MandantNr,
                KontoNr = z.KontoNr,
                Betrag = z.Betrag,
                SollHaben = SollHabenCodes.Gegenseite(z.SollHaben)
            });
        }

        return zeilen;
    }

    public static void PruefeStornierbar(Beleg original, bool bereitsStorniert)
    {
        if (original.IstStorno)
        {
            throw new FachlicherFehler($"Beleg {original.Nummer} ist selbst ein Stornobeleg.");
        }

        if (bereitsStorniert)
        {
            throw new FachlicherFehler($"Beleg {original.Nummer} ist bereits storniert.");
        }
    }
}
