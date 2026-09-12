using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class BelegnummerTests
{
    [Theory]
    [InlineData(Belegart.Rechnung, 2026, 1, "RE-2026-00001")]
    [InlineData(Belegart.Gutschrift, 2026, 42, "GU-2026-00042")]
    [InlineData(Belegart.Zahlung, 1999, 12345, "ZA-1999-12345")]
    public void BildetDieBelegnummer(Belegart art, int jahr, int lfd, string erwartet)
    {
        Assert.Equal(erwartet, Belegnummer.Bilden(art, jahr, lfd));
    }

    [Fact]
    public void WeistNummerKleinerEinsZurueck()
    {
        Assert.Throws<FachlicherFehler>(() => Belegnummer.Bilden(Belegart.Rechnung, 2026, 0));
    }

    [Theory]
    [InlineData(Belegart.Rechnung, "RE")]
    [InlineData(Belegart.Gutschrift, "GU")]
    [InlineData(Belegart.Zahlung, "ZA")]
    public void CodeUndRueckwandlungPassenZusammen(Belegart art, string code)
    {
        Assert.Equal(code, Belegarten.Code(art));
        Assert.Equal(art, Belegarten.Aus(code));
    }
}
