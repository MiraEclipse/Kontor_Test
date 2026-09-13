using Kontor.Core.Modell;

namespace Kontor.App.Rahmen;

public sealed class Modulkontext
{
    public Modulkontext(Sitzung sitzung, Dienste dienste, IMeldungsanzeige meldungen, Modulverzeichnis verzeichnis)
    {
        Sitzung = sitzung;
        Dienste = dienste;
        Meldungen = meldungen;
        Verzeichnis = verzeichnis;
    }

    public Sitzung Sitzung { get; }

    public Dienste Dienste { get; }

    public IMeldungsanzeige Meldungen { get; }

    // Für K08: die Liste aller registrierten Module, aus der ein Bereich für eine Freigabe gewählt wird.
    public Modulverzeichnis Verzeichnis { get; }

    public int MandantNr => Sitzung.MandantNr;
}
