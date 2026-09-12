namespace Kontor.Core.Fachlogik;

public sealed class Schemazeile
{
    public Schemazeile(string bezeichnung, decimal? satz, decimal betrag, bool zwischensumme)
    {
        Bezeichnung = bezeichnung;
        Satz = satz;
        Betrag = betrag;
        Zwischensumme = zwischensumme;
    }

    public string Bezeichnung { get; }
    public decimal? Satz { get; }
    public decimal Betrag { get; }
    public bool Zwischensumme { get; }
}
