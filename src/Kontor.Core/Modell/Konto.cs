namespace Kontor.Core.Modell;

public sealed class Konto
{
    public int KontoId { get; set; }
    public int MandantNr { get; set; }
    public string Bezeichnung { get; set; } = "";
    public Kontoart Art { get; set; }
    public long AnfangsbestandCent { get; set; }
    public bool Gesperrt { get; set; }
}
