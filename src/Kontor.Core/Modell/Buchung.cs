namespace Kontor.Core.Modell;

public sealed class Buchung
{
    public int BuchungId { get; set; }
    public int MandantNr { get; set; }
    public DateOnly Datum { get; set; }
    public int KontoId { get; set; }
    public int KategorieId { get; set; }
    public long BetragCent { get; set; }
    public string Text { get; set; } = "";
    public int? VertragId { get; set; }
}
