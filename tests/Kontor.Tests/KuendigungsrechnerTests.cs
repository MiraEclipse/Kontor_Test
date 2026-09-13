using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class KuendigungsrechnerTests
{
    [Fact]
    public void ErsteKuendigungsmoeglichkeitLiegtAmEndeDerMindestlaufzeit()
    {
        // Beginn 2025-01-01, 24 Monate Mindestlaufzeit -> Vertragsende 2027-01-01, 1 Monat Frist ->
        // spätester Absendetag 2026-12-01. Zum Stichtag 2025-01-01 ist diese Frist noch zu schaffen.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2025, 1, 1), 24, 1, Turnus.Monatlich, new DateOnly(2025, 1, 1));

        Assert.Equal(new DateOnly(2027, 1, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 12, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void IstDieErsteFristSchonVerstrichenZaehltDerNaechsteTurnusZyklus()
    {
        // Wie oben, aber "heute" liegt schon nach dem spätesten Absendetag der ersten Möglichkeit -
        // der monatliche Vertrag verlängert sich, also zählt der nächste monatliche Zyklus.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2025, 1, 1), 24, 1, Turnus.Monatlich, new DateOnly(2027, 6, 1));

        Assert.Equal(new DateOnly(2027, 7, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2027, 6, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void VierteljaehrlicherTurnusRechnetMitDreiMonatsschritten()
    {
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), 12, 3, Turnus.Vierteljaehrlich, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2027, 1, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 10, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void OhneMindestlaufzeitUndFristIstDerVertragSofortKuendbar()
    {
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 3, 5), 0, 0, Turnus.Monatlich, new DateOnly(2026, 3, 5));

        Assert.Equal(new DateOnly(2026, 3, 5), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 3, 5), termin.SpaetesterAbsendetag);
    }
}
