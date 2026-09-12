using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class BuchungssatzTests
{
    private static Belegposition Position(int pos, decimal menge, decimal preis, decimal steuer) =>
        new() { MandantNr = 1, PosNr = pos, Bezeichnung = $"Position {pos}", Menge = menge, Einzelpreis = preis, SteuerSatz = steuer };

    [Fact]
    public void RechnungBuchtForderungGegenErloesUndSteuer()
    {
        var positionen = new[] { Position(1, 2m, 100.00m, 19m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Rechnung, positionen, Kontenzuordnung.Standard);

        Assert.Equal(3, zeilen.Count);
        Assert.Equal("1400", zeilen[0].KontoNr);
        Assert.Equal(238.00m, zeilen[0].Betrag);
        Assert.Equal(SollHaben.Soll, zeilen[0].SollHaben);
        Assert.Equal("8400", zeilen[1].KontoNr);
        Assert.Equal(200.00m, zeilen[1].Betrag);
        Assert.Equal("1776", zeilen[2].KontoNr);
        Assert.Equal(38.00m, zeilen[2].Betrag);
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void TrenntDieSteuersaetze()
    {
        var positionen = new[] { Position(1, 2m, 100.00m, 19m), Position(2, 1m, 50.00m, 7m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Rechnung, positionen, Kontenzuordnung.Standard);

        Assert.Equal(5, zeilen.Count);
        Assert.Equal(291.50m, zeilen[0].Betrag);
        Assert.Contains(zeilen, z => z.KontoNr == "8300" && z.Betrag == 50.00m);
        Assert.Contains(zeilen, z => z.KontoNr == "1775" && z.Betrag == 3.50m);
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void BuchtSteuerfreiOhneSteuerzeile()
    {
        var positionen = new[] { Position(1, 1m, 80.00m, 0m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Rechnung, positionen, Kontenzuordnung.Standard);

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("8200", zeilen[1].KontoNr);
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void GutschriftDrehtDieSeitenUm()
    {
        var positionen = new[] { Position(1, 2m, 100.00m, 19m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Gutschrift, positionen, Kontenzuordnung.Standard);

        Assert.Equal(SollHaben.Haben, zeilen[0].SollHaben);
        Assert.All(zeilen.GetRange(1, 2), z => Assert.Equal(SollHaben.Soll, z.SollHaben));
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void ZahlungBuchtBankGegenForderung()
    {
        var positionen = new[] { Position(1, 1m, 291.50m, 0m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Zahlung, positionen, Kontenzuordnung.Standard);

        Assert.Equal(2, zeilen.Count);
        Assert.Equal("1200", zeilen[0].KontoNr);
        Assert.Equal(SollHaben.Soll, zeilen[0].SollHaben);
        Assert.Equal("1400", zeilen[1].KontoNr);
        Assert.Equal(291.50m, zeilen[1].Betrag);
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void RundetJedePositionEinzeln()
    {
        var positionen = new[] { Position(1, 3m, 0.335m, 19m) };

        var zeilen = Buchungssatz.Erzeuge(Belegart.Rechnung, positionen, Kontenzuordnung.Standard);

        Assert.Equal(1.01m, zeilen[1].Betrag);
        Assert.Equal(0.19m, zeilen[2].Betrag);
        Assert.Equal(1.20m, zeilen[0].Betrag);
        Belegpruefung.PruefeSollGleichHaben(zeilen);
    }

    [Fact]
    public void WeistUnbekanntenSteuersatzZurueck()
    {
        var positionen = new[] { Position(1, 1m, 100.00m, 16m) };

        Assert.Throws<FachlicherFehler>(() => Buchungssatz.Erzeuge(Belegart.Rechnung, positionen, Kontenzuordnung.Standard));
    }

    [Fact]
    public void WeistBelegOhnePositionZurueck()
    {
        Assert.Throws<FachlicherFehler>(() =>
            Buchungssatz.Erzeuge(Belegart.Rechnung, Array.Empty<Belegposition>(), Kontenzuordnung.Standard));
    }
}
