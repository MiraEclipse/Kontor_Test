namespace Kontor.Core.Modell;

public sealed class Artikel
{
    public int ArtikelId { get; set; }
    public int MandantNr { get; set; }
    public string Nummer { get; set; } = "";
    public string Bezeichnung { get; set; } = "";
    public string Einheit { get; set; } = "ST";
    public decimal Preis { get; set; }
    public decimal SteuerSatz { get; set; } = 19m;
    public bool Gesperrt { get; set; }
}
