using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kontor.Tests;

public class BelegRepositoryTests
{
    private const int Mandant = Startbefuellung.TestmandantNr;

    private static readonly DateTime Belegdatum = new(2026, 3, 5);

    [Fact]
    public void RechnungWirdMitNummerUndBuchungssatzGebucht()
    {
        using var test = new Testdatenbank();

        var gebucht = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 2m, 50m));

        Assert.Equal("RE-2026-00001", gebucht.Nummer);
        Assert.True(gebucht.BelegId > 0);
        Assert.False(gebucht.IstStorno);

        var gelesen = test.Belege.Lade(Mandant, gebucht.BelegId);

        Assert.NotNull(gelesen);
        Assert.Equal("RE-2026-00001", gelesen!.Nummer);
        Assert.Equal(Belegdatum, gelesen.Belegdatum);
        Assert.Single(gelesen.Positionen);
        Assert.Equal(3, gelesen.Zeilen.Count);
        Assert.Equal(119.00m, Belegpruefung.SummeSoll(gelesen.Zeilen));
        Assert.Equal(119.00m, Belegpruefung.SummeHaben(gelesen.Zeilen));
        Assert.Equal(100.00m, Betrag(gelesen, "8400", SollHaben.Haben));
        Assert.Equal(19.00m, Betrag(gelesen, "1776", SollHaben.Haben));
        Assert.Equal(119.00m, Betrag(gelesen, "1400", SollHaben.Soll));
    }

    [Fact]
    public void NummernkreisZaehltJeMandantJahrUndBelegartHoch()
    {
        using var test = new Testdatenbank();

        var ersteRechnung = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));
        var zweiteRechnung = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));

        Assert.Equal("RE-2026-00001", ersteRechnung.Nummer);
        Assert.Equal("RE-2026-00002", zweiteRechnung.Nummer);
        Assert.Equal(2L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
        Assert.Equal(0L, test.LetzteNummer(Mandant, 2027, Belegart.Rechnung));
        Assert.Equal(0L, test.LetzteNummer(Mandant, 2026, Belegart.Gutschrift));
    }

    [Fact]
    public void UnausgeglichenerBelegHinterlaesstKeineSpurInDerDatenbank()
    {
        using var test = new Testdatenbank();

        var beleg = Testbelege.MitZeilen(Mandant, Belegdatum,
            Testbelege.Zeile("1400", 100.00m, SollHaben.Soll),
            Testbelege.Zeile("8400", 90.00m, SollHaben.Haben));

        var fehler = Assert.Throws<FachlicherFehler>(() => test.Belege.Buchen(beleg));

        Assert.Contains("ungleich Haben", fehler.Message);
        Assert.Equal(0L, test.AnzahlBelege());
        Assert.Equal(0L, test.AnzahlPositionen());
        Assert.Equal(0L, test.AnzahlZeilen());
        Assert.Equal(0L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
    }

    [Fact]
    public void BuchungAufUnbekanntesKontoWirdAbgewiesen()
    {
        using var test = new Testdatenbank();

        var beleg = Testbelege.MitZeilen(Mandant, Belegdatum,
            Testbelege.Zeile("9999", 100.00m, SollHaben.Soll),
            Testbelege.Zeile("8400", 100.00m, SollHaben.Haben));

        var fehler = Assert.Throws<SqliteException>(() => test.Belege.Buchen(beleg));

        Assert.Contains("FOREIGN KEY", fehler.Message);
        Assert.Equal(0L, test.AnzahlBelege());
        Assert.Equal(0L, test.AnzahlPositionen());
        Assert.Equal(0L, test.AnzahlZeilen());
    }

    [Fact]
    public void NummernkreisBleibtNachEinemRollbackLueckenlos()
    {
        using var test = new Testdatenbank();

        var erste = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));

        var kaputt = Testbelege.MitZeilen(Mandant, Belegdatum,
            Testbelege.Zeile("1400", 100.00m, SollHaben.Soll),
            Testbelege.Zeile("8400", 90.00m, SollHaben.Haben));
        Assert.Throws<FachlicherFehler>(() => test.Belege.Buchen(kaputt));

        var zweite = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));

        Assert.Equal("RE-2026-00001", erste.Nummer);
        Assert.Equal("RE-2026-00002", zweite.Nummer);
        Assert.Equal(2L, test.AnzahlBelege());
        Assert.Equal(2L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
    }

    [Fact]
    public void StornoSpiegeltDieBuchungUndHebtDieSaldenAuf()
    {
        using var test = new Testdatenbank();

        var original = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));
        var storno = test.Belege.Stornieren(Mandant, original.BelegId, new DateTime(2026, 3, 31));

        Assert.Equal("RE-2026-00002", storno.Nummer);
        Assert.True(storno.IstStorno);
        Assert.Equal(original.BelegId, storno.StorniertVon);
        Assert.Equal(new DateTime(2026, 3, 31), storno.Belegdatum);
        Assert.Equal(original.Positionen.Count, storno.Positionen.Count);
        Assert.Equal(original.Zeilen.Count, storno.Zeilen.Count);
        Assert.Equal(119.00m, Betrag(storno, "1400", SollHaben.Haben));
        Assert.Equal(100.00m, Betrag(storno, "8400", SollHaben.Soll));

        foreach (var saldo in test.Konten.Salden(Mandant, 2026))
        {
            Assert.Equal(0m, saldo.Saldo);
        }
    }

    [Fact]
    public void ZweitesStornoWirdAbgelehnt()
    {
        using var test = new Testdatenbank();

        var original = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));
        test.Belege.Stornieren(Mandant, original.BelegId, Belegdatum);

        var fehler = Assert.Throws<FachlicherFehler>(() => test.Belege.Stornieren(Mandant, original.BelegId, Belegdatum));

        Assert.Contains("bereits storniert", fehler.Message);
        Assert.Equal(2L, test.AnzahlBelege());
        Assert.Equal(2L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
    }

    [Fact]
    public void StornoEinesStornosWirdAbgelehnt()
    {
        using var test = new Testdatenbank();

        var original = test.Belege.Buchen(Testbelege.Rechnung(Mandant, Belegdatum, 1m, 100m));
        var storno = test.Belege.Stornieren(Mandant, original.BelegId, Belegdatum);

        var fehler = Assert.Throws<FachlicherFehler>(() => test.Belege.Stornieren(Mandant, storno.BelegId, Belegdatum));

        Assert.Contains("selbst ein Stornobeleg", fehler.Message);
        Assert.Equal(2L, test.AnzahlBelege());
        Assert.Equal(2L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
    }

    [Fact]
    public void StornoEinesUnbekanntenBelegsWirdAbgelehnt()
    {
        using var test = new Testdatenbank();

        var fehler = Assert.Throws<FachlicherFehler>(() => test.Belege.Stornieren(Mandant, 4711, Belegdatum));

        Assert.Contains("nicht vorhanden", fehler.Message);
        Assert.Equal(0L, test.AnzahlBelege());
    }

    [Fact]
    public void BelegOhnePositionWirdNichtGebucht()
    {
        using var test = new Testdatenbank();

        var beleg = new Beleg
        {
            MandantNr = Mandant,
            Belegart = Belegart.Rechnung,
            Belegdatum = Belegdatum
        };

        Assert.Throws<FachlicherFehler>(() => test.Belege.Buchen(beleg));
        Assert.Equal(0L, test.AnzahlBelege());
        Assert.Equal(0L, test.LetzteNummer(Mandant, 2026, Belegart.Rechnung));
    }

    [Fact]
    public void ListeZeigtNurDasAngefragteJahr()
    {
        using var test = new Testdatenbank();

        test.Belege.Buchen(Testbelege.Rechnung(Mandant, new DateTime(2026, 12, 31), 1m, 100m));
        test.Belege.Buchen(Testbelege.Rechnung(Mandant, new DateTime(2027, 1, 1), 1m, 100m));

        Assert.Single(test.Belege.Liste(Mandant, 2026));
        Assert.Single(test.Belege.Liste(Mandant, 2027));
        Assert.Equal("RE-2027-00001", test.Belege.Liste(Mandant, 2027)[0].Nummer);
    }

    private static decimal Betrag(Beleg beleg, string kontoNr, SollHaben sollHaben)
    {
        decimal summe = 0m;

        foreach (var zeile in beleg.Zeilen)
        {
            if (zeile.KontoNr == kontoNr && zeile.SollHaben == sollHaben)
            {
                summe += zeile.Betrag;
            }
        }

        return summe;
    }
}
