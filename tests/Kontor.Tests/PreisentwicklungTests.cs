using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class PreisentwicklungTests
{
    private static Vertragspreis Preis(DateOnly gueltigAb, long betragCent) => new()
    {
        GueltigAb = gueltigAb,
        BetragCent = betragCent
    };

    private static Buchung Zahlung(DateOnly datum, long betragCent) => new()
    {
        Datum = datum,
        BetragCent = betragCent
    };

    [Fact]
    public void RechnetAktuellenPreisJahreskostenSummeUndSteigerungDurch()
    {
        var preise = new[]
        {
            Preis(new DateOnly(2025, 1, 1), 1000L),
            Preis(new DateOnly(2026, 1, 1), 1200L)
        };

        var zahlungen = new[]
        {
            Zahlung(new DateOnly(2025, 1, 1), 1000L),
            Zahlung(new DateOnly(2025, 2, 1), 1000L),
            Zahlung(new DateOnly(2026, 1, 1), 1200L)
        };

        var (ergebnis, fehler) = Preisentwicklung.Berechne(
            preise, Turnus.Monatlich, new DateOnly(2026, 6, 1), zahlungen);

        Assert.Empty(fehler);
        Assert.NotNull(ergebnis);
        Assert.Equal(1200L, ergebnis!.AktuellerPreisCent);
        Assert.Equal(14400L, ergebnis.JahreskostenCent);
        Assert.Equal(3200L, ergebnis.BisherGezahltCent);
        Assert.Equal(516, ergebnis.TageSeitErsterZahlung);
        Assert.Equal(20.00m, ergebnis.SteigerungProzent);
    }

    [Theory]
    [InlineData(Turnus.Monatlich, 12)]
    [InlineData(Turnus.Vierteljaehrlich, 4)]
    [InlineData(Turnus.Halbjaehrlich, 2)]
    [InlineData(Turnus.Jaehrlich, 1)]
    public void JahreskostenRechnenDenTurnusAufZwoelfMonateHoch(Turnus turnus, int erwarteterFaktor)
    {
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 500L) };

        var (ergebnis, _) = Preisentwicklung.Berechne(preise, turnus, new DateOnly(2026, 1, 1), Array.Empty<Buchung>());

        Assert.Equal(500L * erwarteterFaktor, ergebnis!.JahreskostenCent);
    }

    [Fact]
    public void OhneZahlungenSindSummeUndZeitraumNull()
    {
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 500L) };

        var (ergebnis, fehler) = Preisentwicklung.Berechne(
            preise, Turnus.Jaehrlich, new DateOnly(2026, 6, 1), Array.Empty<Buchung>());

        Assert.Empty(fehler);
        Assert.Equal(0L, ergebnis!.BisherGezahltCent);
        Assert.Equal(0, ergebnis.TageSeitErsterZahlung);
        Assert.Equal(0m, ergebnis.SteigerungProzent);
    }

    [Fact]
    public void OhnePreishistorieGibtEsEinenFehlerStattEinerAusnahme()
    {
        var (ergebnis, fehler) = Preisentwicklung.Berechne(
            Array.Empty<Vertragspreis>(), Turnus.Monatlich, new DateOnly(2026, 1, 1), Array.Empty<Buchung>());

        Assert.Null(ergebnis);
        Assert.Single(fehler);
    }

    [Fact]
    public void VorDemErstenGueltigAbGibtEsEinenFehler()
    {
        var preise = new[] { Preis(new DateOnly(2026, 6, 1), 500L) };

        var (ergebnis, fehler) = Preisentwicklung.Berechne(
            preise, Turnus.Monatlich, new DateOnly(2026, 1, 1), Array.Empty<Buchung>());

        Assert.Null(ergebnis);
        Assert.Single(fehler);
    }

    [Fact]
    public void AktuellerPreisIstDerJuengsteBereitsGueltigeSatz()
    {
        var preise = new[]
        {
            Preis(new DateOnly(2025, 1, 1), 900L),
            Preis(new DateOnly(2025, 6, 1), 950L),
            Preis(new DateOnly(2026, 1, 1), 1000L)
        };

        var aktuell = Preisentwicklung.AktuellerPreis(preise, new DateOnly(2025, 12, 31));

        Assert.NotNull(aktuell);
        Assert.Equal(950L, aktuell!.BetragCent);
    }
}
