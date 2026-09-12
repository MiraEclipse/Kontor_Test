using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IBelegRepository
{
    IReadOnlyList<Beleg> Liste(int mandantNr, int jahr);
    Beleg? Lade(int mandantNr, int belegId);
    Beleg Buchen(Beleg beleg);
    Beleg Stornieren(int mandantNr, int belegId, DateTime belegdatum);
}
