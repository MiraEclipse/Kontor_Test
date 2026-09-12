using Kontor.Core.Fachlogik;
using Xunit;

namespace Kontor.Tests;

public class BetragTests
{
    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("2.345", "2.35")]
    [InlineData("2.344", "2.34")]
    [InlineData("-0.005", "-0.01")]
    public void RundetKaufmaennisch(string eingabe, string erwartet)
    {
        Assert.Equal(decimal.Parse(erwartet, System.Globalization.CultureInfo.InvariantCulture),
            Betrag.Runde(decimal.Parse(eingabe, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void VomHundertRechnetAufDenGrundwert()
    {
        Assert.Equal(19.00m, Betrag.VomHundert(100m, 19m));
    }

    [Fact]
    public void ImHundertRechnetAufDenVermindertenGrundwert()
    {
        Assert.Equal(102.04m, Betrag.ImHundert(100m, 2m));
    }

    [Fact]
    public void ImHundertWeistHundertProzentZurueck()
    {
        Assert.Throws<FachlicherFehler>(() => Betrag.ImHundert(100m, 100m));
    }
}
