using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Xunit;

namespace Kontor.Tests;

public class BerechtigungspruefeTests
{
    private static readonly DateTime Jetzt = new(2026, 3, 5, 12, 0, 0);

    private static Recht MachRecht(int mandantNr, string bereich, Stufe stufe, DateTime von, DateTime? bis) => new()
    {
        MandantNr = mandantNr,
        Bereich = bereich,
        Stufe = stufe,
        GueltigVon = von,
        GueltigBis = bis
    };

    [Fact]
    public void EinRechtVorSeinemBeginnIstNochNichtWirksam()
    {
        var rechte = new[] { MachRecht(1, "K01", Stufe.Voll, Jetzt.AddHours(1), null) };

        Assert.Null(Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void EinRechtImGueltigenZeitraumIstWirksam()
    {
        var rechte = new[] { MachRecht(1, "K01", Stufe.Aendern, Jetzt.AddHours(-1), Jetzt.AddHours(1)) };

        Assert.Equal(Stufe.Aendern, Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void EinRechtNachAblaufIstNichtMehrWirksam()
    {
        var rechte = new[] { MachRecht(1, "K01", Stufe.Voll, Jetzt.AddHours(-2), Jetzt.AddHours(-1)) };

        Assert.Null(Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void GueltigBisIstDieExakteGrenzeSelbstSchonAbgelaufen()
    {
        var rechte = new[] { MachRecht(1, "K01", Stufe.Voll, Jetzt.AddHours(-2), Jetzt) };

        Assert.Null(Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void MehrereRechteVerschmelzenZurHoechstenWirksamenStufe()
    {
        var rechte = new[]
        {
            MachRecht(1, "K01", Stufe.Lesen, Jetzt.AddHours(-1), null),
            MachRecht(1, "K01", Stufe.Voll, Jetzt.AddHours(-1), null),
            MachRecht(1, "K01", Stufe.Aendern, Jetzt.AddHours(-1), null)
        };

        Assert.Equal(Stufe.Voll, Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void EinAbgelaufenesRechtZaehltBeiDerVerschmelzungNichtMehrMit()
    {
        var rechte = new[]
        {
            MachRecht(1, "K01", Stufe.Voll, Jetzt.AddHours(-2), Jetzt.AddHours(-1)),
            MachRecht(1, "K01", Stufe.Lesen, Jetzt.AddHours(-1), null)
        };

        Assert.Equal(Stufe.Lesen, Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void DerBereichSternGiltFuerJedenBereich()
    {
        var rechte = new[] { MachRecht(1, Recht.AlleBereiche, Stufe.Aendern, Jetzt.AddHours(-1), null) };

        Assert.Equal(Stufe.Aendern, Berechtigungspruefer.WirksameStufe(rechte, 1, "K06", Jetzt));
    }

    [Fact]
    public void HaushaltsfilterBlendetFremdeHaushalteAus()
    {
        var rechte = new[] { MachRecht(2, "K01", Stufe.Voll, Jetzt.AddHours(-1), null) };

        Assert.Null(Berechtigungspruefer.WirksameStufe(rechte, 1, "K01", Jetzt));
    }

    [Fact]
    public void SystemrechteUebersteuernJedePruefungAuchOhneEinEinzigesRecht()
    {
        var systemBenutzer = new Benutzer { Systemrechte = true };

        Assert.True(Berechtigungspruefer.DarfArbeiten(systemBenutzer, Array.Empty<Recht>(), 1, "K08", Stufe.Voll, Jetzt));
    }

    [Fact]
    public void OhneSystemrechteUndOhneRechtDarfNichtGearbeitetWerden()
    {
        var benutzer = new Benutzer { Systemrechte = false };

        Assert.False(Berechtigungspruefer.DarfArbeiten(benutzer, Array.Empty<Recht>(), 1, "K01", Stufe.Lesen, Jetzt));
    }

    [Fact]
    public void DarfArbeitenPrueftDieMindeststufe()
    {
        var benutzer = new Benutzer { Systemrechte = false };
        var rechte = new[] { MachRecht(1, "K01", Stufe.Lesen, Jetzt.AddHours(-1), null) };

        Assert.True(Berechtigungspruefer.DarfArbeiten(benutzer, rechte, 1, "K01", Stufe.Lesen, Jetzt));
        Assert.False(Berechtigungspruefer.DarfArbeiten(benutzer, rechte, 1, "K01", Stufe.Aendern, Jetzt));
    }
}
