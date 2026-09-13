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
        Assert.Equal(2L, test.Zahl("SELECT COUNT(*) FROM Konto;"));
        Assert.Equal(Startbefuellung.Kategorien.Count, (int)test.Zahl("SELECT COUNT(*) FROM Kategorie;"));
    }

    [Fact]
    public void HaushaltGirokontoBargeldUndKategorienSindVorhanden()
    {
        using var test = new Testdatenbank();

        var mandant = test.Mandanten.Lade(Startbefuellung.TestmandantNr);

        Assert.NotNull(mandant);
        Assert.Equal("EUR", mandant!.Waehrung);
        Assert.NotEmpty(mandant.Name);

        var konten = test.Konten.Liste(Startbefuellung.TestmandantNr, auchGesperrte: true);
        Assert.Equal(2, konten.Count);
        Assert.Contains(konten, k => k.Bezeichnung == "Girokonto" && k.Art == Kontoart.Giro);
        Assert.Contains(konten, k => k.Bezeichnung == "Bargeld" && k.Art == Kontoart.Bar);

        var kategorien = test.Kategorien.Liste(Startbefuellung.TestmandantNr, auchGesperrte: true);
        Assert.Equal(Startbefuellung.Kategorien.Count, kategorien.Count);
        Assert.Contains(kategorien, k => k.Bezeichnung == "Gehalt" && k.Richtung == Richtung.Einnahme);
        Assert.Contains(kategorien, k => k.Bezeichnung == "Miete" && k.Richtung == Richtung.Ausgabe);
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

        var fehler = Assert.Throws<SqliteException>(() => test.Konten.Anlegen(new Konto
        {
            MandantNr = 99,
            Bezeichnung = "Konto ohne Haushalt",
            Art = Kontoart.Giro
        }));

        Assert.Contains("FOREIGN KEY", fehler.Message);
        Assert.Equal(2L, test.Zahl("SELECT COUNT(*) FROM Konto;"));
    }

    [Fact]
    public void BetraegeStehenAlsGanzzahlInCentInDerDatenbank()
    {
        using var test = new Testdatenbank();

        var konto = new Konto
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Bezeichnung = "Sparkonto",
            Art = Kontoart.Sparen,
            AnfangsbestandCent = 123456L
        };

        test.Konten.Anlegen(konto);

        Assert.Equal("integer", test.SpaltenTyp($"SELECT typeof(AnfangsbestandCent) FROM Konto WHERE KontoId = {konto.KontoId};"));
        Assert.Equal(123456L, test.Zahl($"SELECT AnfangsbestandCent FROM Konto WHERE KontoId = {konto.KontoId};"));

        var gelesen = test.Konten.Lade(Startbefuellung.TestmandantNr, konto.KontoId);

        Assert.NotNull(gelesen);
        Assert.Equal(123456L, gelesen!.AnfangsbestandCent);
    }

    [Fact]
    public void DatumStehtAlsTextInDerDatenbank()
    {
        using var test = new Testdatenbank();

        var konten = test.Konten.Liste(Startbefuellung.TestmandantNr, auchGesperrte: true);
        var kategorien = test.Kategorien.Liste(Startbefuellung.TestmandantNr, auchGesperrte: true);

        test.Buchungen.Anlegen(new Buchung
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Datum = new DateOnly(2026, 3, 5),
            KontoId = konten[0].KontoId,
            KategorieId = kategorien[0].KategorieId,
            BetragCent = 100L,
            Text = "Test"
        });

        Assert.Equal("text", test.SpaltenTyp("SELECT typeof(Datum) FROM Buchung LIMIT 1;"));
        Assert.Equal("2026-03-05", test.SpaltenTyp("SELECT Datum FROM Buchung LIMIT 1;"));
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
}
