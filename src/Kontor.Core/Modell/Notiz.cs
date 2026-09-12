namespace Kontor.Core.Modell;

public sealed class Notiz
{
    public int NotizId { get; set; }
    public int MandantNr { get; set; }
    public string Betreff { get; set; } = "";
    public string Text { get; set; } = "";
    public Prioritaet Prioritaet { get; set; } = Prioritaet.Normal;
    public bool Erledigt { get; set; }
    public int? BelegId { get; set; }
    public int? KundeId { get; set; }
    public DateTime Angelegt { get; set; }
}
