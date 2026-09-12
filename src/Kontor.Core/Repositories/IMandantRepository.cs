using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IMandantRepository
{
    IReadOnlyList<Mandant> Alle();
    Mandant? Lade(int mandantNr);
}
