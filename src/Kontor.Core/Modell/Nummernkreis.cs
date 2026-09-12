namespace Kontor.Core.Modell;

public sealed class Nummernkreis
{
    public int MandantNr { get; set; }
    public int Jahr { get; set; }
    public Belegart Belegart { get; set; }
    public int LetzteNummer { get; set; }
}
