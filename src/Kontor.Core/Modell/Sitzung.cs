namespace Kontor.Core.Modell;

public sealed class Sitzung
{
    public Sitzung(Mandant mandant, string benutzer)
    {
        Mandant = mandant;
        Benutzer = benutzer;
        Angemeldet = DateTime.Now;
    }

    public Mandant Mandant { get; }
    public string Benutzer { get; }
    public DateTime Angemeldet { get; }

    public int MandantNr => Mandant.MandantNr;
}
