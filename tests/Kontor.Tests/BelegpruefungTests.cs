using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class BelegpruefungTests
{
    private static Buchungszeile Zeile(string konto, decimal betrag, SollHaben sh) =>
        new() { MandantNr = 1, KontoNr = konto, Betrag = betrag, SollHaben = sh };

    [Fact]
    public void LaesstAusgeglichenenBelegDurch()
    {
        var zeilen = new[]
        {
            Zeile("1400", 119.00m, SollHaben.Soll),
            Zeile("8400", 100.00m, SollHaben.Haben),
            Zeile("1776", 19.00m, SollHaben.Haben)
        };

        Belegpruefung.PruefeSollGleichHaben(zeilen);

        Assert.Equal(119.00m, Belegpruefung.SummeSoll(zeilen));
        Assert.Equal(119.00m, Belegpruefung.SummeHaben(zeilen));
    }

    [Fact]
    public void WeistUnausgeglichenenBelegZurueck()
    {
        var zeilen = new[]
        {
            Zeile("1400", 119.00m, SollHaben.Soll),
            Zeile("8400", 100.00m, SollHaben.Haben)
        };

        var fehler = Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeSollGleichHaben(zeilen));

        Assert.Contains("ungleich", fehler.Message);
    }

    [Fact]
    public void WeistBelegOhneZeilenZurueck()
    {
        Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeSollGleichHaben(Array.Empty<Buchungszeile>()));
    }

    [Fact]
    public void WeistNichtPositivenBetragZurueck()
    {
        var zeilen = new[]
        {
            Zeile("1400", 0m, SollHaben.Soll),
            Zeile("8400", 0m, SollHaben.Haben)
        };

        Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeSollGleichHaben(zeilen));
    }

    [Fact]
    public void WeistZeileOhneKontoZurueck()
    {
        var zeilen = new[]
        {
            Zeile("", 10m, SollHaben.Soll),
            Zeile("8400", 10m, SollHaben.Haben)
        };

        Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeSollGleichHaben(zeilen));
    }

    [Fact]
    public void WeistKopfOhneDatumZurueck()
    {
        Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeKopf(new Beleg { MandantNr = 1 }));
    }

    [Fact]
    public void WeistKopfOhneMandantZurueck()
    {
        Assert.Throws<FachlicherFehler>(() => Belegpruefung.PruefeKopf(new Beleg { Belegdatum = new DateTime(2026, 1, 1) }));
    }
}
