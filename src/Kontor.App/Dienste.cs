using Kontor.Core.Repositories;

namespace Kontor.App;

public sealed class Dienste
{
    public Dienste(
        IMandantRepository mandanten,
        IKundeRepository kunden,
        IArtikelRepository artikel,
        IBelegRepository belege,
        IKontoRepository konten,
        IKalkulationRepository kalkulationen,
        INotizRepository notizen)
    {
        Mandanten = mandanten;
        Kunden = kunden;
        Artikel = artikel;
        Belege = belege;
        Konten = konten;
        Kalkulationen = kalkulationen;
        Notizen = notizen;
    }

    public IMandantRepository Mandanten { get; }

    public IKundeRepository Kunden { get; }

    public IArtikelRepository Artikel { get; }

    public IBelegRepository Belege { get; }

    public IKontoRepository Konten { get; }

    public IKalkulationRepository Kalkulationen { get; }

    public INotizRepository Notizen { get; }
}
