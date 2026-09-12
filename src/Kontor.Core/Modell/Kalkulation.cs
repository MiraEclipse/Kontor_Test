namespace Kontor.Core.Modell;

public sealed class Kalkulation
{
    public int KalkulationId { get; set; }
    public int MandantNr { get; set; }
    public int? ArtikelId { get; set; }
    public string Bezeichnung { get; set; } = "";

    public decimal Materialeinzelkosten { get; set; }
    public decimal MaterialgemeinkostenSatz { get; set; }
    public decimal Fertigungsloehne { get; set; }
    public decimal FertigungsgemeinkostenSatz { get; set; }
    public decimal VerwaltungsgemeinkostenSatz { get; set; }
    public decimal VertriebsgemeinkostenSatz { get; set; }
    public decimal GewinnzuschlagSatz { get; set; }
    public decimal SkontoSatz { get; set; }
    public decimal RabattSatz { get; set; }
    public decimal UmsatzsteuerSatz { get; set; }

    public DateTime Erfasst { get; set; }
}
