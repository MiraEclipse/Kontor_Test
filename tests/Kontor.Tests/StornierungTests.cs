using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class StornierungTests
{
    private static List<Buchungszeile> Original() => new()
    {
        new Buchungszeile { MandantNr = 1, BelegId = 7, ZeileId = 11, KontoNr = "1400", Betrag = 238.00m, SollHaben = SollHaben.Soll },
        new Buchungszeile { MandantNr = 1, BelegId = 7, ZeileId = 12, KontoNr = "8400", Betrag = 200.00m, SollHaben = SollHaben.Haben },
        new Buchungszeile { MandantNr = 1, BelegId = 7, ZeileId = 13, KontoNr = "1776", Betrag = 38.00m, SollHaben = SollHaben.Haben }
    };

    [Fact]
    public void VertauschtSollUndHaben()
    {
        var storno = Stornierung.Spiegele(Original());

        Assert.Equal(SollHaben.Haben, storno[0].SollHaben);
        Assert.Equal(SollHaben.Soll, storno[1].SollHaben);
        Assert.Equal(SollHaben.Soll, storno[2].SollHaben);
    }

    [Fact]
    public void BehaeltKontenUndBetraege()
    {
        var storno = Stornierung.Spiegele(Original());

        Assert.Equal(new[] { "1400", "8400", "1776" }, storno.ConvertAll(z => z.KontoNr));
        Assert.Equal(238.00m, storno[0].Betrag);
        Belegpruefung.PruefeSollGleichHaben(storno);
    }

    [Fact]
    public void LoestSichVomUrsprungsbeleg()
    {
        var storno = Stornierung.Spiegele(Original());

        Assert.All(storno, z => Assert.Equal(0, z.ZeileId));
        Assert.All(storno, z => Assert.Equal(0, z.BelegId));
    }

    [Fact]
    public void HebtDenUrsprungsbelegRechnerischAuf()
    {
        var original = Original();
        var storno = Stornierung.Spiegele(original);
        var alle = new List<Buchungszeile>(original);
        alle.AddRange(storno);

        Assert.Equal(Belegpruefung.SummeSoll(alle), Belegpruefung.SummeHaben(alle));
        Assert.Equal(476.00m, Belegpruefung.SummeSoll(alle));
    }

    [Fact]
    public void WeistStornoEinesStornobelegsZurueck()
    {
        var beleg = new Beleg { BelegId = 8, MandantNr = 1, Nummer = "RE-2026-00002", StorniertVon = 7 };

        Assert.Throws<FachlicherFehler>(() => Stornierung.PruefeStornierbar(beleg, false));
    }

    [Fact]
    public void WeistZweitesStornoZurueck()
    {
        var beleg = new Beleg { BelegId = 7, MandantNr = 1, Nummer = "RE-2026-00001" };

        Assert.Throws<FachlicherFehler>(() => Stornierung.PruefeStornierbar(beleg, true));
    }
}
