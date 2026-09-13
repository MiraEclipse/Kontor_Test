using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IVertragspreisRepository
{
    // Preishistorie eines Vertrags, aufsteigend nach GueltigAb.
    IReadOnlyList<Vertragspreis> Liste(int mandantNr, int vertragId);
    int Anlegen(Vertragspreis preis);
}
