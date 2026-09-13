using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IKontoRepository
{
    IReadOnlyList<Konto> Liste(int mandantNr, bool auchGesperrte);
    Konto? Lade(int mandantNr, int kontoId);
    int Anlegen(Konto konto);
    void Aendern(Konto konto);
    void Sperren(int mandantNr, int kontoId);
    void Entsperren(int mandantNr, int kontoId);

    // Anfangsbestand plus Einnahmen, minus Ausgaben, plus eingehende, minus ausgehende Umbuchungen bis zum Stichtag.
    long Kontostand(int mandantNr, int kontoId, DateOnly stichtag);
}
