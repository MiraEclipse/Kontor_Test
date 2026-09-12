using Kontor.Core.Modell;

namespace Kontor.App.Rahmen;

public sealed class Modulkontext
{
    public Modulkontext(Sitzung sitzung, Dienste dienste, IMeldungsanzeige meldungen)
    {
        Sitzung = sitzung;
        Dienste = dienste;
        Meldungen = meldungen;
    }

    public Sitzung Sitzung { get; }

    public Dienste Dienste { get; }

    public IMeldungsanzeige Meldungen { get; }

    public int MandantNr => Sitzung.MandantNr;
}
