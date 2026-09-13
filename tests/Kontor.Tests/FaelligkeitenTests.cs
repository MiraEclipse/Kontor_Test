using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class FaelligkeitenTests
{
    [Fact]
    public void MonatsendeWirdSauberGeklemmtUndImMaiWiederVoll()
    {
        // Vertrag ab dem 31. Januar: Februar (28, kein Schaltjahr), März (31), April (30), Mai (31) -
        // jeder Termin wird eigenständig aus dem 31. errechnet, nicht aus dem zuletzt geklemmten Tag.
        var termine = Faelligkeiten.Termine(
            Turnus.Monatlich, new DateOnly(2025, 1, 31), new DateOnly(2025, 1, 31), new DateOnly(2025, 5, 31));

        Assert.Equal(new[]
        {
            new DateOnly(2025, 1, 31),
            new DateOnly(2025, 2, 28),
            new DateOnly(2025, 3, 31),
            new DateOnly(2025, 4, 30),
            new DateOnly(2025, 5, 31)
        }, termine);
    }

    [Fact]
    public void ImSchaltjahrFaelltDerFebruarAufDen29()
    {
        var termine = Faelligkeiten.Termine(
            Turnus.Monatlich, new DateOnly(2024, 1, 31), new DateOnly(2024, 2, 1), new DateOnly(2024, 2, 29));

        Assert.Equal(new[] { new DateOnly(2024, 2, 29) }, termine);
    }

    [Fact]
    public void VierteljaehrlicherTurnusZaehltInDreiMonatsschritten()
    {
        var termine = Faelligkeiten.Termine(
            Turnus.Vierteljaehrlich, new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        Assert.Equal(new[]
        {
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 4, 15),
            new DateOnly(2026, 7, 15),
            new DateOnly(2026, 10, 15)
        }, termine);
    }

    [Fact]
    public void ZeitraumFiltertTermineVorDemVon()
    {
        var termine = Faelligkeiten.Termine(
            Turnus.Monatlich, new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 30));

        Assert.Equal(new[] { new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1) }, termine);
    }

    [Fact]
    public void LeererZeitraumVorBeginnLiefertKeineTermine()
    {
        var termine = Faelligkeiten.Termine(
            Turnus.Jaehrlich, new DateOnly(2026, 6, 1), new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(termine);
    }
}
