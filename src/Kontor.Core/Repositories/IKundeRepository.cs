using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IKundeRepository
{
    IReadOnlyList<Kunde> Suche(int mandantNr, string suchbegriff, bool auchGesperrte);
    Kunde? Lade(int mandantNr, int kundeId);
    Kunde? LadeMitNummer(int mandantNr, string nummer);
    string NaechsteFreieNummer(int mandantNr);
    int Anlegen(Kunde kunde);
    void Aendern(Kunde kunde);
    void Sperren(int mandantNr, int kundeId);
    void Entsperren(int mandantNr, int kundeId);
}
