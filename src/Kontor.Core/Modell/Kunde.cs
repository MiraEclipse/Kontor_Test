namespace Kontor.Core.Modell;

public sealed class Kunde
{
    public int KundeId { get; set; }
    public int MandantNr { get; set; }
    public string Nummer { get; set; } = "";
    public string Name { get; set; } = "";
    public string Strasse { get; set; } = "";
    public string Plz { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Telefon { get; set; } = "";
    public string UstIdNr { get; set; } = "";
    public bool Gesperrt { get; set; }
}
