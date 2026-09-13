using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class VertragslaufTests
{
    private static Vertrag Vertrag(DateOnly beginn, DateOnly? gekuendigtZum = null, bool beendet = false) => new()
    {
        Bezeichnung = "Streaming-Abo",
        Turnus = Turnus.Monatlich,
        Beginn = beginn,
        GekuendigtZum = gekuendigtZum,
        Beendet = beendet
    };

    private static Vertragspreis Preis(DateOnly gueltigAb, long betragCent) => new() { GueltigAb = gueltigAb, BetragCent = betragCent };

    [Fact]
    public void BuchtNurDieSeitDemLetztenLaufNeuFaelligenTermine()
    {
        var vertrag = Vertrag(new DateOnly(2026, 1, 1));
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 1000L) };
        var bereitsGebucht = new[] { new DateOnly(2026, 1, 1) };

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(
            vertrag, preise, bereitsGebucht, new DateOnly(2026, 3, 1));

        Assert.Empty(fehler);
        Assert.Equal(new[]
        {
            new VertragslaufPosition(new DateOnly(2026, 2, 1), 1000L),
            new VertragslaufPosition(new DateOnly(2026, 3, 1), 1000L)
        }, positionen);
    }

    [Fact]
    public void ZweiterAufrufOhneNeueFaelligkeitLegtNichtsDoppeltAn()
    {
        var vertrag = Vertrag(new DateOnly(2026, 1, 1));
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 1000L) };
        var bereitsGebucht = new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 1) };

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(
            vertrag, preise, bereitsGebucht, new DateOnly(2026, 3, 1));

        Assert.Empty(fehler);
        Assert.Empty(positionen);
    }

    [Fact]
    public void FehlenderPreisFuerEineFaelligkeitErzeugtEinenFehlerStattEinerBuchung()
    {
        var vertrag = Vertrag(new DateOnly(2026, 1, 1));

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(
            vertrag, Array.Empty<Vertragspreis>(), Array.Empty<DateOnly>(), new DateOnly(2026, 1, 1));

        Assert.Empty(positionen);
        Assert.Single(fehler);
    }

    [Fact]
    public void EinBeendeterVertragBuchtNichtsMehr()
    {
        var vertrag = Vertrag(new DateOnly(2026, 1, 1), beendet: true);
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 1000L) };

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(
            vertrag, preise, Array.Empty<DateOnly>(), new DateOnly(2026, 6, 1));

        Assert.Empty(positionen);
        Assert.Empty(fehler);
    }

    [Fact]
    public void NachDerKuendigungWirdNurNochBisZumGekuendigtenTerminGebucht()
    {
        var vertrag = Vertrag(new DateOnly(2026, 1, 1), gekuendigtZum: new DateOnly(2026, 2, 1));
        var preise = new[] { Preis(new DateOnly(2026, 1, 1), 1000L) };

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(
            vertrag, preise, Array.Empty<DateOnly>(), new DateOnly(2026, 6, 1));

        Assert.Empty(fehler);
        Assert.Equal(new[]
        {
            new VertragslaufPosition(new DateOnly(2026, 1, 1), 1000L),
            new VertragslaufPosition(new DateOnly(2026, 2, 1), 1000L)
        }, positionen);
    }
}
