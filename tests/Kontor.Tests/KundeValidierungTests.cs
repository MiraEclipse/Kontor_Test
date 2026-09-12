using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class KundeValidierungTests
{
    [Fact]
    public void GueltigerKundeErzeugtKeineFehler()
    {
        var kunde = new Kunde { Nummer = "00001", Name = "Meier GmbH" };

        Assert.Empty(KundeValidierung.Pruefe(kunde, nummerBereitsVergeben: false));
    }

    [Fact]
    public void FehlendeNummerWirdBemaengelt()
    {
        var kunde = new Kunde { Nummer = "", Name = "Meier GmbH" };

        Assert.Contains(KundeValidierung.Pruefe(kunde, false), f => f.Contains("Kundennummer"));
    }

    [Fact]
    public void BereitsVergebeneNummerWirdBemaengelt()
    {
        var kunde = new Kunde { Nummer = "00001", Name = "Meier GmbH" };

        Assert.Contains(KundeValidierung.Pruefe(kunde, nummerBereitsVergeben: true), f => f.Contains("bereits vergeben"));
    }

    [Fact]
    public void FehlenderNameWirdBemaengelt()
    {
        var kunde = new Kunde { Nummer = "00001", Name = "" };

        Assert.Contains(KundeValidierung.Pruefe(kunde, false), f => f.Contains("Name"));
    }

    [Theory]
    [InlineData("DE123456789")]
    [InlineData("ATU12345678")]
    [InlineData("")]
    public void GueltigeUstIdNrWirdAkzeptiert(string ustIdNr)
    {
        var kunde = new Kunde { Nummer = "00001", Name = "Meier GmbH", UstIdNr = ustIdNr };

        Assert.Empty(KundeValidierung.Pruefe(kunde, false));
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("D123456789")]
    [InlineData("de123456789")]
    [InlineData("DE")]
    public void UngueltigesLaenderkuerzelFormatWirdBemaengelt(string ustIdNr)
    {
        var kunde = new Kunde { Nummer = "00001", Name = "Meier GmbH", UstIdNr = ustIdNr };

        Assert.Contains(KundeValidierung.Pruefe(kunde, false), f => f.Contains("Umsatzsteuer"));
    }
}
