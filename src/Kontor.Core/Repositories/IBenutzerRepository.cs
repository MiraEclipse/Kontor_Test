using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IBenutzerRepository
{
    IReadOnlyList<Benutzer> Alle();
    Benutzer? Lade(int benutzerId);
    Benutzer? LadeMitAnmeldename(string anmeldename);
    int Anlegen(Benutzer benutzer);
    void KennwortSetzen(int benutzerId, byte[] hash, byte[] salz, int durchlaeufe);
    void Sperren(int benutzerId);
    void Entsperren(int benutzerId);
    void LetzteAnmeldungSetzen(int benutzerId, DateTime zeitpunkt);
}
