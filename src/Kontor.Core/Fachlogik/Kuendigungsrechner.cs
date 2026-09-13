using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Kuendigungsrechner
{
    // Der Vertrag verlängert sich nach Ablauf der Mindestlaufzeit stillschweigend um jeweils einen
    // weiteren Turnus. Kündbar ist er daher zum Ende der Mindestlaufzeit oder zum Ende jedes
    // folgenden Turnus - die naechste Kuendigungsmoeglichkeit ist die frueheste dieser Termine, deren
    // Kuendigungsfrist, vom Stichtag "heute" aus gesehen, noch eingehalten werden kann.
    public static Kuendigungstermin Naechste(
        DateOnly beginn,
        int mindestlaufzeitMonate,
        int kuendigungsfristMonate,
        Turnus turnus,
        DateOnly heute)
    {
        var monate = Turnusse.Monate(turnus);
        var kandidat = beginn.AddMonths(mindestlaufzeitMonate);

        while (true)
        {
            var absendetag = kandidat.AddMonths(-kuendigungsfristMonate);

            if (absendetag >= heute)
            {
                return new Kuendigungstermin(kandidat, absendetag);
            }

            kandidat = kandidat.AddMonths(monate);
        }
    }
}
