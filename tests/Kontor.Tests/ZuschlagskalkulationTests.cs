using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class ZuschlagskalkulationTests
{
    private static Kalkulation Beispiel() => new()
    {
        MandantNr = 1,
        Bezeichnung = "Beispiel",
        Materialeinzelkosten = 1000.00m,
        MaterialgemeinkostenSatz = 10m,
        Fertigungsloehne = 500.00m,
        FertigungsgemeinkostenSatz = 80m,
        VerwaltungsgemeinkostenSatz = 12m,
        VertriebsgemeinkostenSatz = 8m,
        GewinnzuschlagSatz = 10m,
        SkontoSatz = 2m,
        RabattSatz = 10m,
        UmsatzsteuerSatz = 19m
    };

    [Fact]
    public void RechnetDasVollstaendigeSchema()
    {
        var e = Zuschlagskalkulation.Rechne(Beispiel());

        Assert.Equal(100.00m, e.Materialgemeinkosten);
        Assert.Equal(1100.00m, e.Materialkosten);
        Assert.Equal(400.00m, e.Fertigungsgemeinkosten);
        Assert.Equal(900.00m, e.Fertigungskosten);
        Assert.Equal(2000.00m, e.Herstellkosten);
        Assert.Equal(240.00m, e.Verwaltungsgemeinkosten);
        Assert.Equal(160.00m, e.Vertriebsgemeinkosten);
        Assert.Equal(2400.00m, e.Selbstkosten);
        Assert.Equal(240.00m, e.Gewinn);
        Assert.Equal(2640.00m, e.Barverkaufspreis);
        Assert.Equal(2693.88m, e.Zielverkaufspreis);
        Assert.Equal(53.88m, e.Kundenskonto);
        Assert.Equal(2993.20m, e.Listenverkaufspreis);
        Assert.Equal(299.32m, e.Kundenrabatt);
        Assert.Equal(568.71m, e.Umsatzsteuer);
        Assert.Equal(3561.91m, e.Bruttoverkaufspreis);
    }

    [Fact]
    public void RechnetSkontoImHundertNichtAufHundert()
    {
        var k = Beispiel();
        k.RabattSatz = 0m;

        var e = Zuschlagskalkulation.Rechne(k);

        Assert.Equal(2693.88m, e.Zielverkaufspreis);
        Assert.NotEqual(2692.80m, e.Zielverkaufspreis);
    }

    [Fact]
    public void RechnetRabattImHundertNichtAufHundert()
    {
        var k = Beispiel();
        k.SkontoSatz = 0m;

        var e = Zuschlagskalkulation.Rechne(k);

        Assert.Equal(2640.00m, e.Zielverkaufspreis);
        Assert.Equal(2933.33m, e.Listenverkaufspreis);
        Assert.NotEqual(2904.00m, e.Listenverkaufspreis);
    }

    [Fact]
    public void OhneZuschlaegeBleibtDerBarverkaufspreisGleichDenEinzelkosten()
    {
        var k = new Kalkulation { Materialeinzelkosten = 120.50m, Fertigungsloehne = 79.50m };

        var e = Zuschlagskalkulation.Rechne(k);

        Assert.Equal(200.00m, e.Herstellkosten);
        Assert.Equal(200.00m, e.Selbstkosten);
        Assert.Equal(200.00m, e.Barverkaufspreis);
        Assert.Equal(200.00m, e.Bruttoverkaufspreis);
    }

    [Fact]
    public void RundetKaufmaennischAufZweiStellen()
    {
        var k = new Kalkulation { Materialeinzelkosten = 10.005m, Fertigungsloehne = 0m };

        var e = Zuschlagskalkulation.Rechne(k);

        Assert.Equal(10.01m, e.Materialeinzelkosten);
    }

    [Fact]
    public void MeldetSkontoVonHundertProzent()
    {
        var k = Beispiel();
        k.SkontoSatz = 100m;

        var fehler = Zuschlagskalkulation.Pruefe(k);

        Assert.Contains(fehler, f => f.Contains("Kundenskonto"));
        Assert.Throws<FachlicherFehler>(() => Zuschlagskalkulation.Rechne(k));
    }

    [Fact]
    public void MeldetFehlendeEinzelkosten()
    {
        var k = new Kalkulation { Materialeinzelkosten = 0m, Fertigungsloehne = 0m };

        Assert.NotEmpty(Zuschlagskalkulation.Pruefe(k));
        Assert.Throws<FachlicherFehler>(() => Zuschlagskalkulation.Rechne(k));
    }

    [Fact]
    public void MeldetNegativeSaetze()
    {
        var k = Beispiel();
        k.MaterialgemeinkostenSatz = -1m;

        Assert.Contains(Zuschlagskalkulation.Pruefe(k), f => f.Contains("Materialgemeinkostensatz"));
    }

    [Fact]
    public void SchemaZeigtJedeStufeUndEndetBeimBruttopreis()
    {
        var k = Beispiel();
        var e = Zuschlagskalkulation.Rechne(k);

        var schema = Zuschlagskalkulation.Schema(k, e);

        Assert.Equal(18, schema.Count);
        Assert.Equal(3561.91m, schema[^1].Betrag);
        Assert.Equal(2m, schema[12].Satz);
    }
}
