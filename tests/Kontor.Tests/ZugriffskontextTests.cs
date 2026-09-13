using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;
using Xunit;

namespace Kontor.Tests;

public class ZugriffskontextTests
{
    private static Benutzer NeuerBenutzer(string anmeldename)
    {
        var salz = Kennwort.NeuesSalz();
        return new Benutzer
        {
            Anmeldename = anmeldename,
            Anzeigename = anmeldename,
            KennwortHash = Kennwort.Hash("geheim123", salz, Kennwort.StandardDurchlaeufe),
            Salz = salz,
            Durchlaeufe = Kennwort.StandardDurchlaeufe,
            Systemrechte = false
        };
    }

    [Fact]
    public void SchreibenderZugriffOhneRechtWirdImRepositoryAbgewiesenNichtNurInDerMaske()
    {
        using var test = new Testdatenbank();

        var benutzer = NeuerBenutzer("gast");
        test.Benutzer.Anlegen(benutzer);

        var eingeschraenkterZugriff = test.Zugriff(benutzer);
        var kategorien = new KategorieRepository(test.Datenbank, eingeschraenkterZugriff);

        var fehler = Assert.Throws<FachlicherFehler>(() => kategorien.Anlegen(new Kategorie
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Bezeichnung = "Urlaub",
            Richtung = Richtung.Ausgabe
        }));

        Assert.Contains("K02", fehler.Message);
        Assert.DoesNotContain(test.Kategorien.Liste(Startbefuellung.TestmandantNr, auchGesperrte: true),
            k => k.Bezeichnung == "Urlaub");
    }

    [Fact]
    public void MitAusreichenderStufeGelingtDerSchreibendeZugriff()
    {
        using var test = new Testdatenbank();

        var benutzer = NeuerBenutzer("kind");
        test.Benutzer.Anlegen(benutzer);

        test.Rechte.Erteilen(new Recht
        {
            BenutzerId = benutzer.BenutzerId,
            MandantNr = Startbefuellung.TestmandantNr,
            Bereich = "K02",
            Stufe = Stufe.Aendern,
            GueltigVon = DateTime.Now.AddMinutes(-1),
            GueltigBis = null,
            ErteiltVon = benutzer.BenutzerId,
            ErteiltAm = DateTime.Now
        });

        var zugriff = test.Zugriff(benutzer);
        var kategorien = new KategorieRepository(test.Datenbank, zugriff);

        var id = kategorien.Anlegen(new Kategorie
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Bezeichnung = "Urlaub",
            Richtung = Richtung.Ausgabe
        });

        Assert.True(id > 0);
    }

    [Fact]
    public void EineNurLesendeStufeReichtFuerEinenSchreibendenZugriffNicht()
    {
        using var test = new Testdatenbank();

        var benutzer = NeuerBenutzer("kind");
        test.Benutzer.Anlegen(benutzer);

        test.Rechte.Erteilen(new Recht
        {
            BenutzerId = benutzer.BenutzerId,
            MandantNr = Startbefuellung.TestmandantNr,
            Bereich = "K02",
            Stufe = Stufe.Lesen,
            GueltigVon = DateTime.Now.AddMinutes(-1),
            GueltigBis = null,
            ErteiltVon = benutzer.BenutzerId,
            ErteiltAm = DateTime.Now
        });

        var zugriff = test.Zugriff(benutzer);
        var kategorien = new KategorieRepository(test.Datenbank, zugriff);

        Assert.Throws<FachlicherFehler>(() => kategorien.Anlegen(new Kategorie
        {
            MandantNr = Startbefuellung.TestmandantNr,
            Bezeichnung = "Urlaub",
            Richtung = Richtung.Ausgabe
        }));
    }

    [Fact]
    public void EntzugSetztDasEndeUndLoeschtNichts()
    {
        using var test = new Testdatenbank();

        var benutzer = NeuerBenutzer("kind");
        test.Benutzer.Anlegen(benutzer);

        var recht = new Recht
        {
            BenutzerId = benutzer.BenutzerId,
            MandantNr = Startbefuellung.TestmandantNr,
            Bereich = "K01",
            Stufe = Stufe.Lesen,
            GueltigVon = DateTime.Now.AddDays(-1),
            GueltigBis = null,
            ErteiltVon = benutzer.BenutzerId,
            ErteiltAm = DateTime.Now
        };
        test.Rechte.Erteilen(recht);

        var entzugsZeitpunkt = DateTime.Now;
        test.Rechte.Entziehen(recht.RechtId, entzugsZeitpunkt);

        var rechte = test.Rechte.Liste(benutzer.BenutzerId);

        Assert.Single(rechte);
        Assert.NotNull(rechte[0].GueltigBis);
        Assert.Equal(
            entzugsZeitpunkt.ToString("yyyy-MM-dd HH:mm:ss"),
            rechte[0].GueltigBis!.Value.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void NurSystemrechteDuerfenBenutzerVerwalten()
    {
        using var test = new Testdatenbank();

        var admin = NeuerBenutzer("elternteil");
        admin.Systemrechte = true;
        test.Benutzer.Anlegen(admin);

        var normalerBenutzer = NeuerBenutzer("kind");
        test.Benutzer.Anlegen(normalerBenutzer);

        var eingeschraenkterZugriff = test.Zugriff(normalerBenutzer);
        var benutzerVerwaltung = new BenutzerRepository(test.Datenbank, eingeschraenkterZugriff);

        Assert.Throws<FachlicherFehler>(() => benutzerVerwaltung.Anlegen(NeuerBenutzer("neu")));
    }
}
