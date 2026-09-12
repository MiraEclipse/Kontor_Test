namespace Kontor.App.Rahmen;

public sealed class Moduleintrag
{
    public Moduleintrag(string transaktionscode, string bezeichnung, string gruppe, Func<Modulkontext, ModulFenster> fabrik)
    {
        Transaktionscode = transaktionscode;
        Bezeichnung = bezeichnung;
        Gruppe = gruppe;
        Fabrik = fabrik;
    }

    public string Transaktionscode { get; }

    public string Bezeichnung { get; }

    public string Gruppe { get; }

    public Func<Modulkontext, ModulFenster> Fabrik { get; }

    public string Anzeige => $"{Transaktionscode}  {Bezeichnung}";

    public ModulFenster Erzeuge(Modulkontext kontext) => Fabrik(kontext);
}
