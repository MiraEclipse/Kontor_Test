using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IBuchungRepository
{
    IReadOnlyList<Buchung> Liste(int mandantNr, DateOnly von, DateOnly bis);
    IReadOnlyList<Buchung> ListeFuerVertrag(int mandantNr, int vertragId);
    int Anlegen(Buchung buchung);
    void Loeschen(int mandantNr, int buchungId);
    bool VorhandenFuerVertragUndDatum(int mandantNr, int vertragId, DateOnly datum);
}
