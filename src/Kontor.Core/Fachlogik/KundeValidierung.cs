using System.Text.RegularExpressions;
using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class KundeValidierung
{
    private static readonly Regex UstIdMuster = new(@"^[A-Z]{2}[A-Z0-9]{2,12}$", RegexOptions.Compiled);

    public static IReadOnlyList<string> Pruefe(Kunde kunde, bool nummerBereitsVergeben)
    {
        var fehler = new List<string>();

        if (string.IsNullOrWhiteSpace(kunde.Nummer))
        {
            fehler.Add("Die Kundennummer muss gesetzt sein.");
        }
        else if (nummerBereitsVergeben)
        {
            fehler.Add($"Die Kundennummer {kunde.Nummer} ist in diesem Mandanten bereits vergeben.");
        }

        if (string.IsNullOrWhiteSpace(kunde.Name))
        {
            fehler.Add("Der Name muss gesetzt sein.");
        }

        if (!string.IsNullOrWhiteSpace(kunde.UstIdNr) && !UstIdMuster.IsMatch(kunde.UstIdNr.Trim()))
        {
            fehler.Add($"Die Umsatzsteuer-Identifikationsnummer {kunde.UstIdNr} entspricht nicht dem Länderkürzel-Format (z. B. DE123456789).");
        }

        return fehler;
    }
}
