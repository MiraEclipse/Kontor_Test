namespace Kontor.Core.Modell;

public sealed class Umbuchung
{
    public int UmbuchungId { get; set; }
    public int MandantNr { get; set; }
    public DateOnly Datum { get; set; }
    public int VonKontoId { get; set; }
    public int NachKontoId { get; set; }
    public long BetragCent { get; set; }
    public string Text { get; set; } = "";
}
