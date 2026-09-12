namespace Kontor.Core.Modell;

public sealed class Mandant
{
    public int MandantNr { get; set; }
    public string Name { get; set; } = "";
    public string Ort { get; set; } = "";
    public string Waehrung { get; set; } = "EUR";
}
