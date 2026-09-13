using Kontor.Core.Repositories;

namespace Kontor.App.Rahmen;

public sealed class Moduleintrag
{
    public Moduleintrag(
        string transaktionscode, string bezeichnung, string gruppe, Func<Modulkontext, ModulFenster> fabrik,
        bool erfordertSystemrechte = false)
    {
        Transaktionscode = transaktionscode;
        Bezeichnung = bezeichnung;
        Gruppe = gruppe;
        Fabrik = fabrik;
        ErfordertSystemrechte = erfordertSystemrechte;
    }

    public string Transaktionscode { get; }

    public string Bezeichnung { get; }

    public string Gruppe { get; }

    public Func<Modulkontext, ModulFenster> Fabrik { get; }

    // K08 Benutzerverwaltung ist unabhängig vom haushaltsbezogenen Rechtesystem nur für Systemrechte-
    // Inhaber da - für alle anderen Module gilt das normale Bereich/Stufe-System.
    public bool ErfordertSystemrechte { get; }

    public string Anzeige => $"{Transaktionscode}  {Bezeichnung}";

    public ModulFenster Erzeuge(Modulkontext kontext) => Fabrik(kontext);

    public bool IstSichtbarFuer(IZugriffskontext zugriff, int mandantNr) => ErfordertSystemrechte
        ? zugriff.Benutzer.Systemrechte
        : zugriff.WirksameStufe(mandantNr, Transaktionscode) is not null;
}
