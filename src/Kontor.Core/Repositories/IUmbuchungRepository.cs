using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IUmbuchungRepository
{
    IReadOnlyList<Umbuchung> Liste(int mandantNr, DateOnly von, DateOnly bis);
    int Anlegen(Umbuchung umbuchung);
    void Loeschen(int mandantNr, int umbuchungId);
}
