namespace Kontor.Core.Modell;

public sealed class Buchungszeile
{
    public int ZeileId { get; set; }
    public int MandantNr { get; set; }
    public int BelegId { get; set; }
    public string KontoNr { get; set; } = "";
    public decimal Betrag { get; set; }
    public SollHaben SollHaben { get; set; }
}
