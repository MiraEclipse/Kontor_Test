namespace Kontor.Core.Modell;

public sealed class Benutzer
{
    public int BenutzerId { get; set; }
    public string Anmeldename { get; set; } = "";
    public string Anzeigename { get; set; } = "";
    public byte[] KennwortHash { get; set; } = Array.Empty<byte>();
    public byte[] Salz { get; set; } = Array.Empty<byte>();
    public int Durchlaeufe { get; set; }
    public bool Systemrechte { get; set; }
    public bool Gesperrt { get; set; }
    public DateTime Angelegt { get; set; }
    public DateTime? LetzteAnmeldung { get; set; }
}
