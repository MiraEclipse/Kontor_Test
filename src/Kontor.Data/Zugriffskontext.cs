using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Core.Repositories;

namespace Kontor.Data;

public sealed class Zugriffskontext : IZugriffskontext
{
    private readonly Datenbank _datenbank;

    public Zugriffskontext(Datenbank datenbank, Benutzer benutzer)
    {
        _datenbank = datenbank;
        Benutzer = benutzer;
    }

    public Benutzer Benutzer { get; }

    // Für Vorgänge, die das Programm selbst auslöst (z.B. der Vertragslauf beim Start), nicht ein
    // konkreter angemeldeter Benutzer - Systemrechte übersteuern jede Prüfung.
    public static Zugriffskontext Systemkontext(Datenbank datenbank) => new(datenbank, new Benutzer
    {
        BenutzerId = 0,
        Anmeldename = "SYSTEM",
        Anzeigename = "System",
        Systemrechte = true
    });

    public Stufe? WirksameStufe(int mandantNr, string bereich)
    {
        if (Benutzer.Systemrechte)
        {
            return Stufe.Voll;
        }

        using var verbindung = _datenbank.Oeffne();
        var rechte = RechtRepository.Liste(verbindung, null, Benutzer.BenutzerId);
        return Berechtigungspruefer.WirksameStufe(rechte, mandantNr, bereich, DateTime.Now);
    }

    public void Pruefe(int mandantNr, string bereich, Stufe erforderlich)
    {
        var wirksameStufe = WirksameStufe(mandantNr, bereich);

        if (wirksameStufe is null || wirksameStufe.Value < erforderlich)
        {
            throw new FachlicherFehler(
                $"Keine Berechtigung: {bereich} erfordert mindestens Stufe {Stufen.Bezeichnung(erforderlich)}.");
        }
    }

    public void PruefeSystemrechte()
    {
        if (!Benutzer.Systemrechte)
        {
            throw new FachlicherFehler("Nur Benutzer mit Systemrechten dürfen das.");
        }
    }
}
