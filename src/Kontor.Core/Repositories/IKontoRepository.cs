using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IKontoRepository
{
    IReadOnlyList<Konto> Liste(int mandantNr);
    IReadOnlyList<KontoSaldo> Salden(int mandantNr, int jahr);
}
