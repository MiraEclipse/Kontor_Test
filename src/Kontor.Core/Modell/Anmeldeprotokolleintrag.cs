namespace Kontor.Core.Modell;

public sealed class Anmeldeprotokolleintrag
{
    public int EintragId { get; set; }
    public DateTime Zeitpunkt { get; set; }
    public string Anmeldename { get; set; } = "";
    public AnmeldeErgebnis Ergebnis { get; set; }
}
