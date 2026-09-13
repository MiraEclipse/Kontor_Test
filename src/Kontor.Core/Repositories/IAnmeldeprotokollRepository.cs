using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IAnmeldeprotokollRepository
{
    void Erfassen(string anmeldename, AnmeldeErgebnis ergebnis);

    // Anzahl der Fehlversuche seit der letzten erfolgreichen Anmeldung dieses Anmeldenamens - Grundlage
    // für die wachsende Verzögerung.
    int FehlversucheInFolge(string anmeldename);
}
