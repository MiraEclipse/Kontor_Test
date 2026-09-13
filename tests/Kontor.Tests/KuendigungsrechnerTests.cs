using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class KuendigungsrechnerTests
{
    [Fact]
    public void ErsteKuendigungsmoeglichkeitLiegtAmEndeDerMindestlaufzeit()
    {
        // Beginn 2025-01-01, 24 Monate Mindestlaufzeit -> Vertragsende 2027-01-01, 1 Monat Frist ->
        // spätester Absendetag 2026-12-01. Zum Stichtag 2025-01-01 ist diese Frist noch zu schaffen.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2025, 1, 1), 24, 1, Turnus.Monatlich, new DateOnly(2025, 1, 1));

        Assert.Equal(new DateOnly(2027, 1, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 12, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void IstDieErsteFristSchonVerstrichenZaehltDerNaechsteTurnusZyklus()
    {
        // Wie oben, aber "heute" liegt schon nach dem spätesten Absendetag der ersten Möglichkeit -
        // der monatliche Vertrag verlängert sich, also zählt der nächste monatliche Zyklus.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2025, 1, 1), 24, 1, Turnus.Monatlich, new DateOnly(2027, 6, 1));

        Assert.Equal(new DateOnly(2027, 7, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2027, 6, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void VierteljaehrlicherTurnusRechnetMitDreiMonatsschritten()
    {
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), 12, 3, Turnus.Vierteljaehrlich, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2027, 1, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 10, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void OhneMindestlaufzeitUndFristIstDerVertragSofortKuendbar()
    {
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 3, 5), 0, 0, Turnus.Monatlich, new DateOnly(2026, 3, 5));

        Assert.Equal(new DateOnly(2026, 3, 5), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 3, 5), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void MonatsendeBeginnRechnetJedenKandidatenVomBeginnAusStattVomVorigenFortzuschreiben()
    {
        // Beginn am 31.01., 1 Monat Mindestlaufzeit, keine Frist, monatlicher Turnus: der erste
        // Kandidat ist 28.02.2026 (Mindestlaufzeit ab Beginn geklemmt). Ist der Stichtag 01.03.2026,
        // ist dieser Kandidat schon verstrichen, also zählt der nächste - und der muss wieder vom
        // ursprünglichen Beginn aus gerechnet werden: 31.01. + 2 Monate = 31.03., nicht 28.02. + 1
        // Monat = 28.03. (der frühere Fehler).
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 31), 1, 0, Turnus.Monatlich, new DateOnly(2026, 3, 1));

        Assert.Equal(new DateOnly(2026, 3, 31), termin.Kuendigungsmoeglichkeit);
    }

    [Fact]
    public void MonatsendeBeginnKlemmtAuchDenUebernaechstenKandidatenWiederVomBeginnAus()
    {
        // Wie oben, aber Stichtag 01.04.2026: der Kandidat 31.03.2026 ist dann schon verstrichen,
        // der nächste ist 31.01. + 3 Monate = 30.04., nicht 31.03. + 1 Monat = 30.04. zufällig richtig,
        // sondern weil April nur 30 Tage hat, unabhängig vom Ausgangspunkt - das ist genau der Fall,
        // der die fehlerhafte Fortschreibung vom vorigen Kandidaten aus nicht mehr verdeckt hätte.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 31), 1, 0, Turnus.Monatlich, new DateOnly(2026, 4, 1));

        Assert.Equal(new DateOnly(2026, 4, 30), termin.Kuendigungsmoeglichkeit);
    }

    [Fact]
    public void SchaltjahresBeginnKlemmtAufDen28FebruarNichtAufDenErstenMaerz()
    {
        // Beginn 29.02.2024 (Schaltjahr), jährlicher Turnus, 3 Monate Frist. Die Kandidaten liegen
        // bei 29.02.2024, 28.02.2025, 28.02.2026, 28.02.2027 - jeweils vom Beginn aus geklemmt. Zum
        // Stichtag 15.06.2026 ist erst der Kandidat 28.02.2027 erreichbar (Absendetag 28.11.2026).
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2024, 2, 29), 0, 3, Turnus.Jaehrlich, new DateOnly(2026, 6, 15));

        Assert.Equal(new DateOnly(2027, 2, 28), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 11, 28), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void IstDieFristLaengerAlsDieMindestlaufzeitWirdDerErsteKandidatUebersprungen()
    {
        // Mindestlaufzeit 1 Monat, Frist 3 Monate, monatlicher Turnus: der erste Kandidat (Ende der
        // Mindestlaufzeit) hätte schon vor Vertragsbeginn abgesendet werden müssen, ist also nie
        // erreichbar. Erst der dritte Kandidat (01.04.2026) hat einen Absendetag, der nicht mehr in
        // der Vergangenheit liegt.
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), 1, 3, Turnus.Monatlich, new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 4, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2026, 1, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void FaelltDerAbsendetagGenauAufDenStichtagGiltDerTerminNochAlsErreichbar()
    {
        var termin = Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), 0, 1, Turnus.Monatlich, new DateOnly(2025, 12, 1));

        Assert.Equal(new DateOnly(2026, 1, 1), termin.Kuendigungsmoeglichkeit);
        Assert.Equal(new DateOnly(2025, 12, 1), termin.SpaetesterAbsendetag);
    }

    [Fact]
    public void OhneMindestlaufzeitUndFristStimmenDieKuendigungstermineMitDenFaelligkeitenUeberein()
    {
        // Für Mindestlaufzeit 0 und Frist 0 ist jeder Kündigungstermin genau ein Fälligkeitstermin -
        // driften Kuendigungsrechner und Faelligkeiten hier je auseinander, schlägt dieser Test an.
        var beginn = new DateOnly(2026, 1, 31);
        var bis = beginn.AddMonths(6);

        var erwartet = Faelligkeiten.Termine(Turnus.Monatlich, beginn, beginn, bis);

        var tatsaechlich = new List<DateOnly>();
        var heute = beginn;
        while (true)
        {
            var termin = Kuendigungsrechner.Naechste(beginn, 0, 0, Turnus.Monatlich, heute);
            if (termin.Kuendigungsmoeglichkeit > bis)
            {
                break;
            }

            tatsaechlich.Add(termin.Kuendigungsmoeglichkeit);
            heute = termin.Kuendigungsmoeglichkeit.AddDays(1);
        }

        Assert.Equal(erwartet, tatsaechlich);
    }

    [Fact]
    public void EineNegativeMindestlaufzeitWirdAbgewiesen()
    {
        Assert.Throws<FachlicherFehler>(() => Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), -1, 0, Turnus.Monatlich, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void EineNegativeKuendigungsfristWirdAbgewiesen()
    {
        Assert.Throws<FachlicherFehler>(() => Kuendigungsrechner.Naechste(
            new DateOnly(2026, 1, 1), 0, -1, Turnus.Monatlich, new DateOnly(2026, 1, 1)));
    }
}
