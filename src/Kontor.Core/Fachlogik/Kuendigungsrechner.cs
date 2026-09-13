using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Kuendigungsrechner
{
    // Der Vertrag verlängert sich nach Ablauf der Mindestlaufzeit stillschweigend um jeweils einen
    // weiteren Turnus. Kündbar ist er daher zum Ende der Mindestlaufzeit oder zum Ende jedes
    // folgenden Turnus - die naechste Kuendigungsmoeglichkeit ist die frueheste dieser Termine, deren
    // Kuendigungsfrist, vom Stichtag "heute" aus gesehen, noch eingehalten werden kann.
    //
    // Wie in Faelligkeiten.cs wird jeder Kandidat eigenstaendig vom urspruenglichen Beginn aus
    // errechnet (beginn.AddMonths(mindestlaufzeitMonate + n * monate)), nie vom zuletzt geklemmten
    // Kandidaten fortgeschrieben - sonst wandert ein an einem Monatsende beginnender Vertrag bei
    // jedem Treffer auf einen kuerzeren Monat dauerhaft nach vorn.
    public static Kuendigungstermin Naechste(
        DateOnly beginn,
        int mindestlaufzeitMonate,
        int kuendigungsfristMonate,
        Turnus turnus,
        DateOnly heute)
    {
        if (mindestlaufzeitMonate < 0)
        {
            throw new FachlicherFehler("Die Mindestlaufzeit darf nicht negativ sein.");
        }

        if (kuendigungsfristMonate < 0)
        {
            throw new FachlicherFehler("Die Kündigungsfrist darf nicht negativ sein.");
        }

        var monate = Turnusse.Monate(turnus);
        if (monate < 1)
        {
            throw new FachlicherFehler("Der Turnus muss mindestens einen Monat umfassen.");
        }

        var n = 0;
        while (true)
        {
            var kandidat = beginn.AddMonths(mindestlaufzeitMonate + n * monate);
            var absendetag = kandidat.AddMonths(-kuendigungsfristMonate);

            if (absendetag >= heute)
            {
                return new Kuendigungstermin(kandidat, absendetag);
            }

            n++;
        }
    }
}
