using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kontor.Tests;

public class KundeRepositoryTests
{
    private const int Mandant = Startbefuellung.TestmandantNr;

    [Fact]
    public void SucheOhneGesperrteBlendetGesperrteKundenAus()
    {
        using var test = new Testdatenbank();

        test.Kunden.Anlegen(NeuerKunde("00001", "Offener Kunde"));

        var gesperrt = NeuerKunde("00002", "Gesperrter Kunde");
        test.Kunden.Anlegen(gesperrt);
        test.Kunden.Sperren(Mandant, gesperrt.KundeId);

        var ohneGesperrte = test.Kunden.Suche(Mandant, "", auchGesperrte: false);
        var mitGesperrten = test.Kunden.Suche(Mandant, "", auchGesperrte: true);

        Assert.Single(ohneGesperrte);
        Assert.Equal("Offener Kunde", ohneGesperrte[0].Name);
        Assert.Equal(2, mitGesperrten.Count);
    }

    [Fact]
    public void SucheFiltertInSqlUeberNummerUndName()
    {
        using var test = new Testdatenbank();

        test.Kunden.Anlegen(NeuerKunde("00001", "Baumarkt Nord"));
        test.Kunden.Anlegen(NeuerKunde("00002", "Schreinerei Süd"));

        Assert.Single(test.Kunden.Suche(Mandant, "Nord", auchGesperrte: true));
        Assert.Single(test.Kunden.Suche(Mandant, "00002", auchGesperrte: true));
        Assert.Empty(test.Kunden.Suche(Mandant, "Ost", auchGesperrte: true));
    }

    [Fact]
    public void GesperrterKundeBleibtUeberLadeErreichbar()
    {
        using var test = new Testdatenbank();

        var kunde = NeuerKunde("00001", "Alter Kunde");
        test.Kunden.Anlegen(kunde);
        test.Kunden.Sperren(Mandant, kunde.KundeId);

        var geladen = test.Kunden.Lade(Mandant, kunde.KundeId);

        Assert.NotNull(geladen);
        Assert.True(geladen!.Gesperrt);
    }

    [Fact]
    public void EntsperrenMachtDenKundenWiederAuswaehlbar()
    {
        using var test = new Testdatenbank();

        var kunde = NeuerKunde("00001", "Kunde");
        test.Kunden.Anlegen(kunde);
        test.Kunden.Sperren(Mandant, kunde.KundeId);
        test.Kunden.Entsperren(Mandant, kunde.KundeId);

        Assert.Single(test.Kunden.Suche(Mandant, "", auchGesperrte: false));
    }

    [Fact]
    public void SperrenEinesUnbekanntenKundenWirdAbgelehnt()
    {
        using var test = new Testdatenbank();

        Assert.Throws<DatenbankFehler>(() => test.Kunden.Sperren(Mandant, 4711));
    }

    [Fact]
    public void NummerIstJeMandantEindeutig()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(2, "Zweitbetrieb");

        test.Kunden.Anlegen(NeuerKunde("00001", "Erster"));

        var fehler = Assert.Throws<SqliteException>(() => test.Kunden.Anlegen(NeuerKunde("00001", "Zweiter")));
        Assert.Contains("UNIQUE", fehler.Message);

        // Im anderen Mandanten ist dieselbe Nummer erlaubt.
        var imAnderenMandanten = NeuerKunde("00001", "Auch erster", mandantNr: 2);
        test.Kunden.Anlegen(imAnderenMandanten);
        Assert.True(imAnderenMandanten.KundeId > 0);
    }

    [Fact]
    public void NaechsteFreieNummerZaehltHochUndIgnoriertNichtnumerische()
    {
        using var test = new Testdatenbank();

        Assert.Equal("00001", test.Kunden.NaechsteFreieNummer(Mandant));

        test.Kunden.Anlegen(NeuerKunde("00001", "Erster"));
        Assert.Equal("00002", test.Kunden.NaechsteFreieNummer(Mandant));

        test.Kunden.Anlegen(NeuerKunde("SONDER-1", "Freitext-Nummer"));
        Assert.Equal("00002", test.Kunden.NaechsteFreieNummer(Mandant));
    }

    [Fact]
    public void LadeMitNummerFindetDenGenauenTreffer()
    {
        using var test = new Testdatenbank();
        test.Kunden.Anlegen(NeuerKunde("00001", "Kunde"));

        Assert.NotNull(test.Kunden.LadeMitNummer(Mandant, "00001"));
        Assert.Null(test.Kunden.LadeMitNummer(Mandant, "00002"));
    }

    [Fact]
    public void AendernLaesstDieNummerUnangetastet()
    {
        using var test = new Testdatenbank();
        var kunde = NeuerKunde("00001", "Alter Name");
        test.Kunden.Anlegen(kunde);

        kunde.Name = "Neuer Name";
        kunde.UstIdNr = "DE123456789";
        test.Kunden.Aendern(kunde);

        var geladen = test.Kunden.Lade(Mandant, kunde.KundeId);
        Assert.Equal("00001", geladen!.Nummer);
        Assert.Equal("Neuer Name", geladen.Name);
        Assert.Equal("DE123456789", geladen.UstIdNr);
    }

    private static Kunde NeuerKunde(string nummer, string name, int mandantNr = Mandant) => new()
    {
        MandantNr = mandantNr,
        Nummer = nummer,
        Name = name,
        Ort = "Hamburg"
    };
}
