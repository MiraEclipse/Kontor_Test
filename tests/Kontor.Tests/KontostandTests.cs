using Kontor.Core.Fachlogik;
using Xunit;

namespace Kontor.Tests;

public class KontostandTests
{
    [Fact]
    public void AnfangsbestandPlusEinnahmenMinusAusgabenPlusEinMinusAusUmbuchungen()
    {
        // 1000,00 Anfangsbestand + 500,00 Einnahmen - 200,00 Ausgaben + 300,00 eingehende Umbuchung
        // - 150,00 ausgehende Umbuchung = 1450,00
        Assert.Equal(145000L, Kontostand.Berechne(100000L, 50000L, 20000L, 30000L, 15000L));
    }

    [Fact]
    public void OhneBuchungenBleibtDerAnfangsbestand()
    {
        Assert.Equal(50000L, Kontostand.Berechne(50000L, 0L, 0L, 0L, 0L));
    }

    [Fact]
    public void KannInsNegativeRutschen()
    {
        // 0,00 Anfangsbestand + 0,00 Einnahmen - 100,00 Ausgaben = -100,00 (z.B. Kreditkarte)
        Assert.Equal(-10000L, Kontostand.Berechne(0L, 0L, 10000L, 0L, 0L));
    }
}
