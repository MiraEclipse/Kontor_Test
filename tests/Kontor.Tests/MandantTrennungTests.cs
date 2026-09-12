using Kontor.Core.Modell;
using Kontor.Data;
using Xunit;

namespace Kontor.Tests;

public class MandantTrennungTests
{
    private const int Eigener = Startbefuellung.TestmandantNr;
    private const int Fremder = 2;

    private static readonly DateTime Belegdatum = new(2026, 3, 5);

    [Fact]
    public void KundenlisteBlendetFremdeMandantenAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "Zweitbetrieb OHG");

        test.Kunden.Anlegen(Kunde(Eigener, "K-0001", "Eigener Kunde"));
        var fremder = Kunde(Fremder, "K-0001", "Fremder Kunde");
        test.Kunden.Anlegen(fremder);

        var liste = test.Kunden.Suche(Eigener, "", auchGesperrte: true);

        Assert.Single(liste);
        Assert.Equal("Eigener Kunde", liste[0].Name);
        Assert.Null(test.Kunden.Lade(Eigener, fremder.KundeId));
        Assert.NotNull(test.Kunden.Lade(Fremder, fremder.KundeId));
    }

    [Fact]
    public void SuchtextFindetNurDenEigenenMandanten()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "Zweitbetrieb OHG");

        test.Kunden.Anlegen(Kunde(Eigener, "K-0001", "Meier"));
        test.Kunden.Anlegen(Kunde(Fremder, "K-0002", "Meier"));

        Assert.Single(test.Kunden.Suche(Eigener, "Meier", auchGesperrte: true));
        Assert.Empty(test.Kunden.Suche(Eigener, "Schulze", auchGesperrte: true));
    }

    [Fact]
    public void BeleglisteUndSaldenBlendenFremdeMandantenAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "Zweitbetrieb OHG");

        test.Belege.Buchen(Testbelege.Rechnung(Eigener, Belegdatum, 1m, 100m));
        test.Belege.Buchen(Testbelege.Rechnung(Fremder, Belegdatum, 1m, 500m));
        test.Belege.Buchen(Testbelege.Rechnung(Fremder, Belegdatum, 1m, 500m));

        Assert.Single(test.Belege.Liste(Eigener, 2026));
        Assert.Equal(2, test.Belege.Liste(Fremder, 2026).Count);
        Assert.Equal(3L, test.AnzahlBelege());

        Assert.Equal(119.00m, Saldo(test, Eigener, "1400"));
        Assert.Equal(1190.00m, Saldo(test, Fremder, "1400"));

        Assert.Equal("RE-2026-00001", test.Belege.Liste(Eigener, 2026)[0].Nummer);
        Assert.Equal("RE-2026-00001", test.Belege.Liste(Fremder, 2026)[0].Nummer);
    }

    [Fact]
    public void NotizenUndKalkulationenBlendenFremdeMandantenAus()
    {
        using var test = new Testdatenbank();
        test.MandantAnlegen(Fremder, "Zweitbetrieb OHG");

        test.Notizen.Anlegen(new Notiz { MandantNr = Eigener, Betreff = "Eigene Notiz" });
        var fremdeNotiz = new Notiz { MandantNr = Fremder, Betreff = "Fremde Notiz" };
        test.Notizen.Anlegen(fremdeNotiz);

        test.Kalkulationen.Sichern(new Kalkulation
        {
            MandantNr = Eigener,
            Bezeichnung = "Eigene Kalkulation",
            Materialeinzelkosten = 100m,
            Fertigungsloehne = 50m
        });
        var fremdeKalkulation = new Kalkulation
        {
            MandantNr = Fremder,
            Bezeichnung = "Fremde Kalkulation",
            Materialeinzelkosten = 200m,
            Fertigungsloehne = 80m
        };
        test.Kalkulationen.Sichern(fremdeKalkulation);

        Assert.Single(test.Notizen.Liste(Eigener, false));
        Assert.Null(test.Notizen.Lade(Eigener, fremdeNotiz.NotizId));
        Assert.Single(test.Kalkulationen.Liste(Eigener));
        Assert.Null(test.Kalkulationen.Lade(Eigener, fremdeKalkulation.KalkulationId));
    }

    private static Kunde Kunde(int mandantNr, string nummer, string name) => new()
    {
        MandantNr = mandantNr,
        Nummer = nummer,
        Name = name,
        Ort = "Hamburg"
    };

    private static decimal Saldo(Testdatenbank test, int mandantNr, string kontoNr)
    {
        foreach (var saldo in test.Konten.Salden(mandantNr, 2026))
        {
            if (saldo.KontoNr == kontoNr)
            {
                return saldo.Saldo;
            }
        }

        return -1m;
    }
}
