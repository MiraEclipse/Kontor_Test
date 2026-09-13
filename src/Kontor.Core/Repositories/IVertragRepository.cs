using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IVertragRepository
{
    IReadOnlyList<Vertrag> Liste(int mandantNr, bool auchBeendete);
    Vertrag? Lade(int mandantNr, int vertragId);
    int Anlegen(Vertrag vertrag);
    void Aendern(Vertrag vertrag);

    // Bucht für den Vertrag alle bis zum Stichtag fälligen, noch nicht gebuchten Termine in einer
    // Transaktion und liefert die Anzahl der neu angelegten Buchungen zurück. Mehrfacher Aufruf legt
    // nichts doppelt an.
    int VertragslaufAusfuehren(int mandantNr, int vertragId, DateOnly stichtag);
}
