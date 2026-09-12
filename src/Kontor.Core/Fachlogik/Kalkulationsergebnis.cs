namespace Kontor.Core.Fachlogik;

public sealed class Kalkulationsergebnis
{
    public decimal Materialeinzelkosten { get; init; }
    public decimal Materialgemeinkosten { get; init; }
    public decimal Materialkosten { get; init; }
    public decimal Fertigungsloehne { get; init; }
    public decimal Fertigungsgemeinkosten { get; init; }
    public decimal Fertigungskosten { get; init; }
    public decimal Herstellkosten { get; init; }
    public decimal Verwaltungsgemeinkosten { get; init; }
    public decimal Vertriebsgemeinkosten { get; init; }
    public decimal Selbstkosten { get; init; }
    public decimal Gewinn { get; init; }
    public decimal Barverkaufspreis { get; init; }
    public decimal Kundenskonto { get; init; }
    public decimal Zielverkaufspreis { get; init; }
    public decimal Kundenrabatt { get; init; }
    public decimal Listenverkaufspreis { get; init; }
    public decimal Umsatzsteuer { get; init; }
    public decimal Bruttoverkaufspreis { get; init; }
}
