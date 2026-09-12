using System.Globalization;
using Kontor.Data;
using Xunit;

namespace Kontor.Tests;

public class FeldwerteTests
{
    [Theory]
    [InlineData("0.005", 1L, "0.01")]
    [InlineData("1.005", 101L, "1.01")]
    [InlineData("2.344", 234L, "2.34")]
    [InlineData("2.345", 235L, "2.35")]
    [InlineData("2.346", 235L, "2.35")]
    [InlineData("-2.345", -235L, "-2.35")]
    [InlineData("119.00", 11900L, "119.00")]
    [InlineData("0", 0L, "0")]
    public void BetragLaeuftUeberCentHinUndZurueck(string eingabe, long cent, string erwartet)
    {
        var wert = Dez(eingabe);

        Assert.Equal(cent, Feldwerte.Cent(wert));
        Assert.Equal(Dez(erwartet), Feldwerte.AusCent(cent));
        Assert.Equal(Dez(erwartet), Feldwerte.AusCent(Feldwerte.Cent(wert)));
    }

    [Theory]
    [InlineData("1.234", 1234L)]
    [InlineData("2.5", 2500L)]
    [InlineData("0.001", 1L)]
    public void MengeLaeuftUeberTausendstelHinUndZurueck(string eingabe, long tausendstel)
    {
        var wert = Dez(eingabe);

        Assert.Equal(tausendstel, Feldwerte.Tausendstel(wert));
        Assert.Equal(wert, Feldwerte.AusTausendstel(tausendstel));
    }

    [Theory]
    [InlineData("19", 1900L)]
    [InlineData("7", 700L)]
    [InlineData("2.5", 250L)]
    [InlineData("0", 0L)]
    public void SatzLaeuftUeberBasispunkteHinUndZurueck(string eingabe, long bp)
    {
        var wert = Dez(eingabe);

        Assert.Equal(bp, Feldwerte.Bp(wert));
        Assert.Equal(wert, Feldwerte.AusBp(bp));
    }

    [Fact]
    public void DatumUndZeitstempelSindKulturunabhaengig()
    {
        Assert.Equal("2026-03-05", Feldwerte.Datum(new DateTime(2026, 3, 5, 17, 42, 13)));
        Assert.Equal("2026-03-05 17:42:13", Feldwerte.Zeit(new DateTime(2026, 3, 5, 17, 42, 13)));
        Assert.Equal(new DateTime(2026, 3, 5), Feldwerte.AusDatum("2026-03-05"));
        Assert.Equal(new DateTime(2026, 3, 5, 17, 42, 13), Feldwerte.AusZeit("2026-03-05 17:42:13"));
    }

    private static decimal Dez(string wert) => decimal.Parse(wert, CultureInfo.InvariantCulture);
}
