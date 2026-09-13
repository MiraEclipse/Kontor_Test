using Kontor.Core.Repositories;

namespace Kontor.App;

public sealed class Dienste
{
    public Dienste(
        IZugriffskontext zugriff,
        IMandantRepository mandanten,
        IKontoRepository konten,
        IKategorieRepository kategorien,
        IBuchungRepository buchungen,
        IUmbuchungRepository umbuchungen,
        IVertragRepository vertraege,
        IVertragspreisRepository vertragspreise,
        INotizRepository notizen,
        IBenutzerRepository benutzer,
        IRechtRepository rechte,
        IAnmeldeprotokollRepository anmeldeprotokoll)
    {
        Zugriff = zugriff;
        Mandanten = mandanten;
        Konten = konten;
        Kategorien = kategorien;
        Buchungen = buchungen;
        Umbuchungen = umbuchungen;
        Vertraege = vertraege;
        Vertragspreise = vertragspreise;
        Notizen = notizen;
        Benutzer = benutzer;
        Rechte = rechte;
        Anmeldeprotokoll = anmeldeprotokoll;
    }

    // Wird wie die Repositories durchgereicht: prüft, ob der angemeldete Benutzer eine schreibende
    // Operation ausführen darf (die Repositories fragen dieselbe Instanz noch einmal selbst).
    public IZugriffskontext Zugriff { get; }

    public IMandantRepository Mandanten { get; }

    public IKontoRepository Konten { get; }

    public IKategorieRepository Kategorien { get; }

    public IBuchungRepository Buchungen { get; }

    public IUmbuchungRepository Umbuchungen { get; }

    public IVertragRepository Vertraege { get; }

    public IVertragspreisRepository Vertragspreise { get; }

    public INotizRepository Notizen { get; }

    public IBenutzerRepository Benutzer { get; }

    public IRechtRepository Rechte { get; }

    public IAnmeldeprotokollRepository Anmeldeprotokoll { get; }
}
