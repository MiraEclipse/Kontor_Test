using Kontor.App.Module;

namespace Kontor.App.Rahmen;

public sealed class Modulverzeichnis
{
    private readonly List<Moduleintrag> _eintraege = new();

    public static Modulverzeichnis Standard()
    {
        var verzeichnis = new Modulverzeichnis();

        verzeichnis.Registriere("K01", "Kunden", "Stammdaten", kontext => new KundenMaske(kontext));
        verzeichnis.Registriere("K02", "Artikel", "Stammdaten", kontext => new ArtikelMaske(kontext));
        verzeichnis.Registriere("K99", "Systemtest", "System", kontext => new Systemtest(kontext));

        return verzeichnis;
    }

    public IReadOnlyList<Moduleintrag> Alle => _eintraege;

    public void Registriere(string transaktionscode, string bezeichnung, string gruppe, Func<Modulkontext, ModulFenster> fabrik)
    {
        if (Finde(transaktionscode) is not null)
        {
            throw new InvalidOperationException($"Der Transaktionscode {transaktionscode} ist bereits belegt.");
        }

        _eintraege.Add(new Moduleintrag(transaktionscode, bezeichnung, gruppe, fabrik));
    }

    public Moduleintrag? Finde(string transaktionscode)
    {
        foreach (var eintrag in _eintraege)
        {
            if (string.Equals(eintrag.Transaktionscode, transaktionscode, StringComparison.OrdinalIgnoreCase))
            {
                return eintrag;
            }
        }

        return null;
    }

    public IReadOnlyList<string> Gruppen()
    {
        var gruppen = new List<string>();

        foreach (var eintrag in _eintraege)
        {
            if (!gruppen.Contains(eintrag.Gruppe))
            {
                gruppen.Add(eintrag.Gruppe);
            }
        }

        return gruppen;
    }

    public IReadOnlyList<Moduleintrag> InGruppe(string gruppe)
    {
        var eintraege = new List<Moduleintrag>();

        foreach (var eintrag in _eintraege)
        {
            if (eintrag.Gruppe == gruppe)
            {
                eintraege.Add(eintrag);
            }
        }

        return eintraege;
    }
}
