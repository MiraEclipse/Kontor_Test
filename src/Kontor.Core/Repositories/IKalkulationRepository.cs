using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IKalkulationRepository
{
    IReadOnlyList<Kalkulation> Liste(int mandantNr);
    Kalkulation? Lade(int mandantNr, int kalkulationId);
    int Sichern(Kalkulation kalkulation);
}
