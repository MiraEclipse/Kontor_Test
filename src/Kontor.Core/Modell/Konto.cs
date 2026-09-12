namespace Kontor.Core.Modell;

public sealed class Konto
{
    public string KontoNr { get; set; } = "";
    public int MandantNr { get; set; }
    public string Bezeichnung { get; set; } = "";
    public Kontoart Art { get; set; }
}
