namespace Kontor.Core.Modell;

public sealed class Vertrag
{
    public int VertragId { get; set; }
    public int MandantNr { get; set; }
    public string Bezeichnung { get; set; } = "";
    public string Anbieter { get; set; } = "";
    public Turnus Turnus { get; set; }
    public DateOnly Beginn { get; set; }
    public int MindestlaufzeitMonate { get; set; }
    public int KuendigungsfristMonate { get; set; }
    public int KategorieId { get; set; }
    public int KontoId { get; set; }
    public bool AutomatischBuchen { get; set; }
    public DateOnly? GekuendigtZum { get; set; }
    public bool Beendet { get; set; }
}
