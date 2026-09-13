using Kontor.Core.Modell;

namespace Kontor.Core.Repositories;

// Wird wie die Datenbank an die Repositories durchgereicht und von jeder schreibenden Operation
// gefragt, ob der angemeldete Benutzer das darf - Oberflächenprüfungen allein sind keine Rechteprüfung,
// da ein Transaktionscode im Kommandofeld sie umgehen würde.
public interface IZugriffskontext
{
    Benutzer Benutzer { get; }

    // Nicht werfende Abfrage für die Oberfläche (Modus-Wechsel erlauben, Schaltflächen anbieten oder
    // nicht) - Systemrechte liefern immer Stufe.Voll.
    Stufe? WirksameStufe(int mandantNr, string bereich);

    // Wirft FachlicherFehler, wenn der Benutzer im Haushalt mandantNr im Bereich bereich nicht
    // mindestens die Stufe erforderlich hat (Systemrechte übersteuern das).
    void Pruefe(int mandantNr, string bereich, Stufe erforderlich);

    // Wirft FachlicherFehler, wenn der Benutzer keine Systemrechte hat - für die Benutzerverwaltung,
    // die unabhängig vom haushaltsbezogenen Rechtesystem nur Systemrechte-Inhabern vorbehalten ist.
    void PruefeSystemrechte();
}
