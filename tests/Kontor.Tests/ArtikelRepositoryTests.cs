using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kontor.Tests;

public class ArtikelRepositoryTests
{
    private const int Mandant = Startbefuellung.TestmandantNr;

    [Fact]
    public void SucheOhneGesperrteBlendetGesperrteArtikelAus()
    {
        using var test = new Testdatenbank();

        test.Artikel.Anlegen(NeuerArtikel("00001", "Schraube"));

        var gesperrt = NeuerArtikel("00002", "Auslaufartikel");
        test.Artikel.Anlegen(gesperrt);
        test.Artikel.Sperren(Mandant, gesperrt.ArtikelId);

        Assert.Single(test.Artikel.Suche(Mandant, "", auchGesperrte: false));
        Assert.Equal(2, test.Artikel.Suche(Mandant, "", auchGesperrte: true).Count);
    }

    [Fact]
    public void SucheFiltertInSqlUeberNummerUndBezeichnung()
    {
        using var test = new Testdatenbank();

        test.Artikel.Anlegen(NeuerArtikel("00001", "Holzschraube"));
        test.Artikel.Anlegen(NeuerArtikel("00002", "Metallwinkel"));

        Assert.Single(test.Artikel.Suche(Mandant, "Holz", auchGesperrte: true));
        Assert.Single(test.Artikel.Suche(Mandant, "00002", auchGesperrte: true));
        Assert.Empty(test.Artikel.Suche(Mandant, "Beton", auchGesperrte: true));
    }

    [Fact]
    public void EntsperrenMachtDenArtikelWiederAuswaehlbar()
    {
        using var test = new Testdatenbank();

        var artikel = NeuerArtikel("00001", "Schraube");
        test.Artikel.Anlegen(artikel);
        test.Artikel.Sperren(Mandant, artikel.ArtikelId);
        test.Artikel.Entsperren(Mandant, artikel.ArtikelId);

        Assert.Single(test.Artikel.Suche(Mandant, "", auchGesperrte: false));
    }

    [Fact]
    public void NummerIstJeMandantEindeutig()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(2, "Zweitbetrieb");

        test.Artikel.Anlegen(NeuerArtikel("00001", "Erster"));

        var fehler = Assert.Throws<SqliteException>(() => test.Artikel.Anlegen(NeuerArtikel("00001", "Zweiter")));
        Assert.Contains("UNIQUE", fehler.Message);

        var imAnderenMandanten = NeuerArtikel("00001", "Auch erster", mandantNr: 2);
        test.Artikel.Anlegen(imAnderenMandanten);
        Assert.True(imAnderenMandanten.ArtikelId > 0);
    }

    [Fact]
    public void NaechsteFreieNummerZaehltHoch()
    {
        using var test = new Testdatenbank();

        Assert.Equal("00001", test.Artikel.NaechsteFreieNummer(Mandant));

        test.Artikel.Anlegen(NeuerArtikel("00001", "Erster"));
        Assert.Equal("00002", test.Artikel.NaechsteFreieNummer(Mandant));
    }

    [Fact]
    public void PreisUndSteuersatzLaufenRundUmDenRepository()
    {
        using var test = new Testdatenbank();
        var artikel = NeuerArtikel("00001", "Schraube");
        artikel.Preis = 12.345m;
        artikel.SteuerSatz = 7m;
        test.Artikel.Anlegen(artikel);

        var geladen = test.Artikel.Lade(Mandant, artikel.ArtikelId);

        Assert.Equal(12.35m, geladen!.Preis);
        Assert.Equal(7m, geladen.SteuerSatz);
    }

    [Fact]
    public void AendernLaesstDieNummerUnangetastet()
    {
        using var test = new Testdatenbank();
        var artikel = NeuerArtikel("00001", "Alte Bezeichnung");
        test.Artikel.Anlegen(artikel);

        artikel.Bezeichnung = "Neue Bezeichnung";
        test.Artikel.Aendern(artikel);

        var geladen = test.Artikel.Lade(Mandant, artikel.ArtikelId);
        Assert.Equal("00001", geladen!.Nummer);
        Assert.Equal("Neue Bezeichnung", geladen.Bezeichnung);
    }

    private static Artikel NeuerArtikel(string nummer, string bezeichnung, int mandantNr = Mandant) => new()
    {
        MandantNr = mandantNr,
        Nummer = nummer,
        Bezeichnung = bezeichnung,
        Einheit = "ST",
        Preis = 1.00m,
        SteuerSatz = 19m
    };
}
