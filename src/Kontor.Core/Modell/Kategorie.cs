namespace Kontor.Core.Modell;

public sealed class Kategorie
{
    public int KategorieId { get; set; }
    public int MandantNr { get; set; }
    public string Bezeichnung { get; set; } = "";
    public Richtung Richtung { get; set; }
    public bool Gesperrt { get; set; }
}
