namespace Kontor.Core.Fachlogik;

// Bei Fehlversuchen keine Kontosperre (die sperrt in einer Familie nur gegenseitig aus), sondern eine
// mit jedem Versuch wachsende Verzögerung.
public static class Anmeldeverzoegerung
{
    private static readonly TimeSpan Schritt = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan Obergrenze = TimeSpan.FromSeconds(30);

    public static TimeSpan Fuer(int aufeinanderfolgendeFehlversuche)
    {
        if (aufeinanderfolgendeFehlversuche <= 0)
        {
            return TimeSpan.Zero;
        }

        var wartezeit = Schritt * aufeinanderfolgendeFehlversuche;
        return wartezeit > Obergrenze ? Obergrenze : wartezeit;
    }
}
