using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IKategorieRepository
{
    IReadOnlyList<Kategorie> Liste(int mandantNr, bool auchGesperrte);
    Kategorie? Lade(int mandantNr, int kategorieId);
    int Anlegen(Kategorie kategorie);
    void Aendern(Kategorie kategorie);
    void Sperren(int mandantNr, int kategorieId);
    void Entsperren(int mandantNr, int kategorieId);
}
