using Kontor.App.Module;
using Kontor.Core.Repositories;

namespace Kontor.App.Rahmen;

public sealed class Modulverzeichnis
{
    private readonly List<Moduleintrag> _eintraege = new();

    public static Modulverzeichnis Standard()
    {
        var verzeichnis = new Modulverzeichnis();

        verzeichnis.Registriere("K01", "Haushaltsbuch", "Haushalt", kontext => new HaushaltsbuchMaske(kontext));
        verzeichnis.Registriere("K02", "Stammdaten", "Haushalt", kontext => new StammdatenMaske(kontext));
        verzeichnis.Registriere("K03", "Verträge", "Haushalt", kontext => new VertraegeMaske(kontext));
        verzeichnis.Registriere("K05", "Aufgaben und Notizen", "Haushalt", kontext => new AufgabenMaske(kontext));
        verzeichnis.Registriere("K06", "Übersicht", "Haushalt", kontext => new UebersichtMaske(kontext));
        verzeichnis.Registriere("K08", "Benutzerverwaltung", "System", kontext => new BenutzerverwaltungMaske(kontext), erfordertSystemrechte: true);
        verzeichnis.Registriere("K99", "Systemtest", "System", kontext => new Systemtest(kontext));

        return verzeichnis;
    }

    public IReadOnlyList<Moduleintrag> Alle => _eintraege;

    // Nur Module, für die mindestens Lesen vorliegt (K08 nur mit Systemrechten) - die Oberfläche bietet
    // so nichts an, was ohnehin am Repository scheitern würde.
    public IReadOnlyList<Moduleintrag> Sichtbare(IZugriffskontext zugriff, int mandantNr)
    {
        var sichtbare = new List<Moduleintrag>();

        foreach (var eintrag in _eintraege)
        {
            if (eintrag.IstSichtbarFuer(zugriff, mandantNr))
            {
                sichtbare.Add(eintrag);
            }
        }

        return sichtbare;
    }

    public void Registriere(
        string transaktionscode, string bezeichnung, string gruppe, Func<Modulkontext, ModulFenster> fabrik,
        bool erfordertSystemrechte = false)
    {
        if (Finde(transaktionscode) is not null)
        {
            throw new InvalidOperationException($"Der Transaktionscode {transaktionscode} ist bereits belegt.");
        }

        _eintraege.Add(new Moduleintrag(transaktionscode, bezeichnung, gruppe, fabrik, erfordertSystemrechte));
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
