using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IArtikelRepository
{
    IReadOnlyList<Artikel> Suche(int mandantNr, string suchbegriff, bool auchGesperrte);
    Artikel? Lade(int mandantNr, int artikelId);
    Artikel? LadeMitNummer(int mandantNr, string nummer);
    string NaechsteFreieNummer(int mandantNr);
    int Anlegen(Artikel artikel);
    void Aendern(Artikel artikel);
    void Sperren(int mandantNr, int artikelId);
    void Entsperren(int mandantNr, int artikelId);
}
