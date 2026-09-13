namespace Kontor.Core.Modell;

public sealed class Vertragspreis
{
    public int VertragspreisId { get; set; }
    public int MandantNr { get; set; }
    public int VertragId { get; set; }
    public DateOnly GueltigAb { get; set; }
    public long BetragCent { get; set; }
}
