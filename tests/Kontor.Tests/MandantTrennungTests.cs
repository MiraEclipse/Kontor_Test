using Kontor.Core.Modell;
using Kontor.Data;
using Xunit;

namespace Kontor.Tests;

public class MandantTrennungTests
{
    private const int Eigener = Startbefuellung.TestmandantNr;
    private const int Fremder = 2;

    private static readonly DateOnly Buchungsdatum = new(2026, 3, 5);

    [Fact]
    public void KontenlisteBlendetFremdeHaushalteAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "WG-Kasse");

        var eigeneKonten = test.Konten.Liste(Eigener, auchGesperrte: true);
        var fremdeKonten = test.Konten.Liste(Fremder, auchGesperrte: true);

        Assert.Equal(2, eigeneKonten.Count);
        Assert.Equal(2, fremdeKonten.Count);
        Assert.All(eigeneKonten, k => Assert.Equal(Eigener, k.MandantNr));
        Assert.Null(test.Konten.Lade(Eigener, fremdeKonten[0].KontoId));
        Assert.NotNull(test.Konten.Lade(Fremder, fremdeKonten[0].KontoId));
    }

    [Fact]
    public void KategorienlisteBlendetFremdeHaushalteAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "WG-Kasse");

        Assert.Equal(Startbefuellung.Kategorien.Count, test.Kategorien.Liste(Eigener, auchGesperrte: true).Count);
        Assert.Equal(Startbefuellung.Kategorien.Count, test.Kategorien.Liste(Fremder, auchGesperrte: true).Count);
        Assert.All(test.Kategorien.Liste(Eigener, auchGesperrte: true), k => Assert.Equal(Eigener, k.MandantNr));
    }

    [Fact]
    public void BuchungslisteUndKontostandBlendenFremdeHaushalteAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "WG-Kasse");

        var eigenesKonto = test.Konten.Liste(Eigener, auchGesperrte: true)[0];
        var eigeneKategorie = test.Kategorien.Liste(Eigener, auchGesperrte: true)[0];
        var fremdesKonto = test.Konten.Liste(Fremder, auchGesperrte: true)[0];
        var fremdeKategorie = test.Kategorien.Liste(Fremder, auchGesperrte: true)[0];

        test.Buchungen.Anlegen(Buchung(Eigener, eigenesKonto.KontoId, eigeneKategorie.KategorieId, 10000L));
        test.Buchungen.Anlegen(Buchung(Fremder, fremdesKonto.KontoId, fremdeKategorie.KategorieId, 50000L));
        test.Buchungen.Anlegen(Buchung(Fremder, fremdesKonto.KontoId, fremdeKategorie.KategorieId, 50000L));

        Assert.Single(test.Buchungen.Liste(Eigener, Buchungsdatum, Buchungsdatum));
        Assert.Equal(2, test.Buchungen.Liste(Fremder, Buchungsdatum, Buchungsdatum).Count);

        var eigenerStand = test.Konten.Kontostand(Eigener, eigenesKonto.KontoId, Buchungsdatum);
        var fremderStand = test.Konten.Kontostand(Fremder, fremdesKonto.KontoId, Buchungsdatum);

        Assert.Equal(eigenesKonto.AnfangsbestandCent + (eigeneKategorie.Richtung == Richtung.Einnahme ? 10000L : -10000L), eigenerStand);
        Assert.Equal(fremdesKonto.AnfangsbestandCent + (fremdeKategorie.Richtung == Richtung.Einnahme ? 100000L : -100000L), fremderStand);
    }

    [Fact]
    public void NotizenBlendenFremdeHaushalteAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "WG-Kasse");

        test.Notizen.Anlegen(new Notiz { MandantNr = Eigener, Betreff = "Eigene Notiz" });
        var fremdeNotiz = new Notiz { MandantNr = Fremder, Betreff = "Fremde Notiz" };
        test.Notizen.Anlegen(fremdeNotiz);

        Assert.Single(test.Notizen.Liste(Eigener, false));
        Assert.Null(test.Notizen.Lade(Eigener, fremdeNotiz.NotizId));
        Assert.NotNull(test.Notizen.Lade(Fremder, fremdeNotiz.NotizId));
    }

    [Fact]
    public void VertraegeUndVertragspreiseBlendenFremdeHaushalteAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "WG-Kasse");

        var eigenesKonto = test.Konten.Liste(Eigener, auchGesperrte: true)[0];
        var eigeneKategorie = test.Kategorien.Liste(Eigener, auchGesperrte: true)[0];
        var fremdesKonto = test.Konten.Liste(Fremder, auchGesperrte: true)[0];
        var fremdeKategorie = test.Kategorien.Liste(Fremder, auchGesperrte: true)[0];

        var eigenerVertrag = new Vertrag
        {
            MandantNr = Eigener,
            Bezeichnung = "Eigener Vertrag",
            Turnus = Turnus.Monatlich,
            Beginn = new DateOnly(2026, 1, 1),
            KategorieId = eigeneKategorie.KategorieId,
            KontoId = eigenesKonto.KontoId
        };
        test.Vertraege.Anlegen(eigenerVertrag);

        var fremderVertrag = new Vertrag
        {
            MandantNr = Fremder,
            Bezeichnung = "Fremder Vertrag",
            Turnus = Turnus.Monatlich,
            Beginn = new DateOnly(2026, 1, 1),
            KategorieId = fremdeKategorie.KategorieId,
            KontoId = fremdesKonto.KontoId
        };
        test.Vertraege.Anlegen(fremderVertrag);

        test.Vertragspreise.Anlegen(new Vertragspreis { MandantNr = Eigener, VertragId = eigenerVertrag.VertragId, GueltigAb = new DateOnly(2026, 1, 1), BetragCent = 500L });
        test.Vertragspreise.Anlegen(new Vertragspreis { MandantNr = Fremder, VertragId = fremderVertrag.VertragId, GueltigAb = new DateOnly(2026, 1, 1), BetragCent = 900L });

        Assert.Single(test.Vertraege.Liste(Eigener, auchBeendete: true));
        Assert.Null(test.Vertraege.Lade(Eigener, fremderVertrag.VertragId));
        Assert.NotNull(test.Vertraege.Lade(Fremder, fremderVertrag.VertragId));

        Assert.Single(test.Vertragspreise.Liste(Eigener, eigenerVertrag.VertragId));
    }

    private static Buchung Buchung(int mandantNr, int kontoId, int kategorieId, long betragCent) => new()
    {
        MandantNr = mandantNr,
        Datum = Buchungsdatum,
        KontoId = kontoId,
        KategorieId = kategorieId,
        BetragCent = betragCent,
        Text = "Testbuchung"
    };
}
