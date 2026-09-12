using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kontor.Tests;

public class SchemaTests
{
    [Fact]
    public void VorbereitenLegtDieDatenbankMitDerAktuellenVersionAn()
    {
        using var test = new Testdatenbank();

        Assert.True(File.Exists(test.Dateipfad));
        Assert.Equal(1L, test.Zahl("SELECT COUNT(*) FROM SchemaVersion;"));
        Assert.Equal((long)test.Datenbank.SchemaVersion, test.Zahl("SELECT Version FROM SchemaVersion WHERE Id = 1;"));
    }

    [Fact]
    public void VorbereitenLaeuftMehrfachOhneDoppelteStartbefuellung()
    {
        using var test = new Testdatenbank();

        test.Datenbank.Vorbereiten();
        test.Datenbank.Vorbereiten();

        Assert.Equal(1L, test.Zahl("SELECT COUNT(*) FROM Mandant;"));
        Assert.Equal(Startbefuellung.Kontenrahmen.Count, (int)test.Zahl("SELECT COUNT(*) FROM Konto;"));
    }

    [Fact]
    public void TestmandantUndKontenrahmenSindVorhanden()
    {
        using var test = new Testdatenbank();

        var mandant = test.Mandanten.Lade(Startbefuellung.TestmandantNr);

        Assert.NotNull(mandant);
        Assert.Equal("EUR", mandant!.Waehrung);
        Assert.NotEmpty(mandant.Name);
        Assert.Equal(Startbefuellung.Kontenrahmen.Count, test.Konten.Liste(Startbefuellung.TestmandantNr).Count);
    }

    [Fact]
    public void KontenrahmenDecktDieKontenzuordnungAb()
    {
        using var test = new Testdatenbank();

        var vorhanden = new List<string>();
        foreach (var konto in test.Konten.Liste(Startbefuellung.TestmandantNr))
        {
            vorhanden.Add(konto.KontoNr);
        }

        Assert.Contains(Kontenzuordnung.Standard.Forderungen, vorhanden);
        Assert.Contains(Kontenzuordnung.Standard.Bank, vorhanden);

        foreach (var schluessel in Kontenzuordnung.Standard.Steuer)
        {
            Assert.Contains(schluessel.Erloeskonto, vorhanden);

            if (schluessel.Steuerkonto.Length > 0)
            {
                Assert.Contains(schluessel.Steuerkonto, vorhanden);
            }
        }
    }

    [Fact]
    public void JedeVerbindungHatFremdschluesselEingeschaltet()
    {
        using var test = new Testdatenbank();

        Assert.Equal(1L, test.Zahl("PRAGMA foreign_keys;"));
    }

    [Fact]
    public void FremdschluesselverletzungWirdAbgewiesen()
    {
        using var test = new Testdatenbank();

        var fehler = Assert.Throws<SqliteException>(() => test.Kunden.Anlegen(new Kunde
        {
            MandantNr = 99,
            Nummer = "K-0001",
            Name = "Kunde ohne Mandant"
        }));

        Assert.Contains("FOREIGN KEY", fehler.Message);
        Assert.Equal(0L, test.Zahl("SELECT COUNT(*) FROM Kunde;"));
    }

    [Fact]
    public void BetraegeStehenAlsGanzzahlInCentInDerDatenbank()
    {
        using var test = new Testdatenbank();

        var artikel = new Artikel
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Nummer = "A-0001",
            Bezeichnung = "Prüfartikel",
            Einheit = "ST",
            Preis = 12.345m,
            SteuerSatz = 19m
        };

        test.Artikel.Anlegen(artikel);

        Assert.Equal("integer", test.SpaltenTyp("SELECT typeof(PreisCent) FROM Artikel LIMIT 1;"));
        Assert.Equal(1235L, test.Zahl("SELECT PreisCent FROM Artikel LIMIT 1;"));

        var gelesen = test.Artikel.Lade(Startbefuellung.TestmandantNr, artikel.ArtikelId);

        Assert.NotNull(gelesen);
        Assert.Equal(12.35m, gelesen!.Preis);
        Assert.Equal(19m, gelesen.SteuerSatz);
    }

    [Fact]
    public void DatumUndZeitstempelStehenAlsTextInDerDatenbank()
    {
        using var test = new Testdatenbank();

        test.Belege.Buchen(Testbelege.Rechnung(Startbefuellung.TestmandantNr, new DateTime(2026, 3, 5), 1m, 100m));

        Assert.Equal("text", test.SpaltenTyp("SELECT typeof(Belegdatum) FROM Beleg LIMIT 1;"));
        Assert.Equal("2026-03-05", test.SpaltenTyp("SELECT Belegdatum FROM Beleg LIMIT 1;"));
        Assert.Equal(1L, test.Zahl("SELECT COUNT(*) FROM Beleg WHERE LENGTH(Erfasst) = 19;"));
    }

    [Fact]
    public void TransaktionenSperrenDieDateiSofort()
    {
        using var test = new Testdatenbank();

        using var erste = test.Datenbank.Oeffne();
        using var transaktion = test.Datenbank.BeginneSofort(erste);

        var bauer = new SqliteConnectionStringBuilder(test.Datenbank.Verbindungszeichenfolge)
        {
            DefaultTimeout = 1
        };

        using var zweite = new SqliteConnection(bauer.ToString());
        zweite.Open();

        var fehler = Assert.Throws<SqliteException>(() => zweite.BeginTransaction(deferred: false));

        Assert.Equal(5, fehler.SqliteErrorCode);

        transaktion.Rollback();
    }

    [Fact]
    public void EindeutigeBelegnummerWirdErzwungen()
    {
        using var test = new Testdatenbank();

        Assert.Equal(1L, test.Zahl(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'UX_Beleg_Nummer';"));
    }
}
