namespace Kontor.Core.Modell;

public sealed class Beleg
{
    public int BelegId { get; set; }
    public int MandantNr { get; set; }
    public string Nummer { get; set; } = "";
    public Belegart Belegart { get; set; }
    public DateTime Belegdatum { get; set; }
    public int? KundeId { get; set; }
    public int? StorniertVon { get; set; }
    public DateTime Erfasst { get; set; }

    public List<Belegposition> Positionen { get; } = new();
    public List<Buchungszeile> Zeilen { get; } = new();

    public bool IstStorno => StorniertVon.HasValue;
}
