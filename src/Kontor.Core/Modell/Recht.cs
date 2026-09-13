namespace Kontor.Core.Modell;

public sealed class Recht
{
    // Bereich-Wert, der für alle Module gilt statt für einen einzelnen Transaktionscode.
    public const string AlleBereiche = "*";

    public int RechtId { get; set; }
    public int BenutzerId { get; set; }
    public int MandantNr { get; set; }
    public string Bereich { get; set; } = "";
    public Stufe Stufe { get; set; }
    public DateTime GueltigVon { get; set; }
    public DateTime? GueltigBis { get; set; }
    public int ErteiltVon { get; set; }
    public DateTime ErteiltAm { get; set; }
}
