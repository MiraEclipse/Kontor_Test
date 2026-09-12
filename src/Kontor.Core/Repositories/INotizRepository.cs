using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface INotizRepository
{
    IReadOnlyList<Notiz> Liste(int mandantNr, bool nurOffene);
    Notiz? Lade(int mandantNr, int notizId);
    int Anlegen(Notiz notiz);
    void Aendern(Notiz notiz);
}
