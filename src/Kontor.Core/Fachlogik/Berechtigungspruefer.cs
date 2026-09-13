using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Berechtigungspruefer
{
    // Mehrere Rechte auf denselben Bereich verschmelzen zur höchsten zum Zeitpunkt wirksamen Stufe.
    // Ein Recht ist wirksam, wenn GueltigVon erreicht und GueltigBis (falls gesetzt) noch nicht erreicht ist.
    public static Stufe? WirksameStufe(IReadOnlyList<Recht> rechte, int mandantNr, string bereich, DateTime zeitpunkt)
    {
        Stufe? hoechste = null;

        foreach (var recht in rechte)
        {
            if (recht.MandantNr != mandantNr)
            {
                continue;
            }

            if (recht.Bereich != bereich && recht.Bereich != Recht.AlleBereiche)
            {
                continue;
            }

            if (recht.GueltigVon > zeitpunkt)
            {
                continue;
            }

            if (recht.GueltigBis is { } gueltigBis && gueltigBis <= zeitpunkt)
            {
                continue;
            }

            if (hoechste is null || recht.Stufe > hoechste.Value)
            {
                hoechste = recht.Stufe;
            }
        }

        return hoechste;
    }

    // Systemrechte übersteuern alles - sie werden nicht über einzelne Recht-Einträge vergeben.
    public static bool DarfArbeiten(
        Benutzer benutzer, IReadOnlyList<Recht> rechte, int mandantNr, string bereich, Stufe erforderlich, DateTime zeitpunkt)
    {
        if (benutzer.Systemrechte)
        {
            return true;
        }

        var wirksameStufe = WirksameStufe(rechte, mandantNr, bereich, zeitpunkt);
        return wirksameStufe is { } stufe && stufe >= erforderlich;
    }
}
