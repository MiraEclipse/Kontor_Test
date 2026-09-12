namespace Kontor.Core.Modell;

public sealed class Belegposition
{
    public int PositionId { get; set; }
    public int MandantNr { get; set; }
    public int BelegId { get; set; }
    public int PosNr { get; set; }
    public int? ArtikelId { get; set; }
    public string Bezeichnung { get; set; } = "";
    public string Einheit { get; set; } = "ST";
    public decimal Menge { get; set; }
    public decimal Einzelpreis { get; set; }
    public decimal SteuerSatz { get; set; }
}
