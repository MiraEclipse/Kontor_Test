using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

public interface IRechtRepository
{
    // Alle Rechte eines Benutzers, auch abgelaufene oder entzogene - Rechte werden nie gelöscht.
    IReadOnlyList<Recht> Liste(int benutzerId);
    int Erteilen(Recht recht);

    // Entziehen setzt GueltigBis auf den angegebenen Zeitpunkt, statt das Recht zu löschen.
    void Entziehen(int rechtId, DateTime zeitpunkt);
}
