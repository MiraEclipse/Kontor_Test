namespace Kontor.Core.Modell;

public sealed class KontoSaldo
{
    public string KontoNr { get; set; } = "";
    public string Bezeichnung { get; set; } = "";
    public Kontoart Art { get; set; }
    public decimal Soll { get; set; }
    public decimal Haben { get; set; }

    public decimal Saldo => Soll - Haben;
}
