using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Faelligkeiten
{
    // Liefert alle Fälligkeitstermine eines Vertrags im Zeitraum [von, bis] (beide Grenzen eingeschlossen).
    // Monatsende sauber: ein Vertrag ab dem 31. fällt im Februar auf den 28./29., im April auf den 30.
    // und im Mai wieder auf den 31. - jeder Termin wird eigenständig aus Beginn errechnet, nicht aus
    // dem zuletzt geklemmten Tag fortgeschrieben.
    public static IReadOnlyList<DateOnly> Termine(Turnus turnus, DateOnly beginn, DateOnly von, DateOnly bis)
    {
        if (bis < beginn || bis < von)
        {
            return Array.Empty<DateOnly>();
        }

        var monate = Turnusse.Monate(turnus);
        var termine = new List<DateOnly>();

        var i = 0;
        while (true)
        {
            var termin = AddMonateGeklemmtAnBeginn(beginn, i * monate);

            if (termin > bis)
            {
                break;
            }

            if (termin >= von)
            {
                termine.Add(termin);
            }

            i++;
        }

        return termine;
    }

    private static DateOnly AddMonateGeklemmtAnBeginn(DateOnly beginn, int monate)
    {
        var gesamtmonat = beginn.Month - 1 + monate;
        var jahr = beginn.Year + gesamtmonat / 12;
        var monat = gesamtmonat % 12 + 1;
        var tag = Math.Min(beginn.Day, DateTime.DaysInMonth(jahr, monat));
        return new DateOnly(jahr, monat, tag);
    }
}
