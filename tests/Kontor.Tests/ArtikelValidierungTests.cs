using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class ArtikelValidierungTests
{
    [Fact]
    public void GueltigerArtikelErzeugtKeineFehler()
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "ST", Preis = 1.50m, SteuerSatz = 19m };

        Assert.Empty(ArtikelValidierung.Pruefe(artikel, false));
    }

    [Fact]
    public void FehlendeNummerWirdBemaengelt()
    {
        var artikel = new Artikel { Nummer = "", Bezeichnung = "Schraube", Einheit = "ST", SteuerSatz = 19m };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, false), f => f.Contains("Artikelnummer"));
    }

    [Fact]
    public void BereitsVergebeneNummerWirdBemaengelt()
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "ST", SteuerSatz = 19m };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, true), f => f.Contains("bereits vergeben"));
    }

    [Fact]
    public void FehlendeBezeichnungWirdBemaengelt()
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "", Einheit = "ST", SteuerSatz = 19m };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, false), f => f.Contains("Bezeichnung"));
    }

    [Fact]
    public void FehlendeEinheitWirdBemaengelt()
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "", SteuerSatz = 19m };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, false), f => f.Contains("Einheit"));
    }

    [Fact]
    public void NegativerPreisWirdBemaengelt()
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "ST", Preis = -0.01m, SteuerSatz = 19m };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, false), f => f.Contains("Preis"));
    }

    [Theory]
    [InlineData(19)]
    [InlineData(7)]
    [InlineData(0)]
    public void ZulaessigeSteuersaetzeWerdenAkzeptiert(decimal steuerSatz)
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "ST", SteuerSatz = steuerSatz };

        Assert.Empty(ArtikelValidierung.Pruefe(artikel, false));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(5)]
    [InlineData(-1)]
    public void UnzulaessigeSteuersaetzeWerdenBemaengelt(decimal steuerSatz)
    {
        var artikel = new Artikel { Nummer = "00001", Bezeichnung = "Schraube", Einheit = "ST", SteuerSatz = steuerSatz };

        Assert.Contains(ArtikelValidierung.Pruefe(artikel, false), f => f.Contains("Steuersatz"));
    }
}
