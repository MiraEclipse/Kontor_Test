using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public static class Zuschlagskalkulation
{
    public static IReadOnlyList<string> Pruefe(Kalkulation k)
    {
        var fehler = new List<string>();

        if (k.Materialeinzelkosten < 0m)
        {
            fehler.Add("Materialeinzelkosten dürfen nicht negativ sein.");
        }

        if (k.Fertigungsloehne < 0m)
        {
            fehler.Add("Fertigungslöhne dürfen nicht negativ sein.");
        }

        if (k.Materialeinzelkosten + k.Fertigungsloehne <= 0m)
        {
            fehler.Add("Materialeinzelkosten und Fertigungslöhne sind beide null.");
        }

        PruefeSatz(fehler, k.MaterialgemeinkostenSatz, "Materialgemeinkostensatz");
        PruefeSatz(fehler, k.FertigungsgemeinkostenSatz, "Fertigungsgemeinkostensatz");
        PruefeSatz(fehler, k.VerwaltungsgemeinkostenSatz, "Verwaltungsgemeinkostensatz");
        PruefeSatz(fehler, k.VertriebsgemeinkostenSatz, "Vertriebsgemeinkostensatz");
        PruefeSatz(fehler, k.GewinnzuschlagSatz, "Gewinnzuschlag");
        PruefeSatz(fehler, k.UmsatzsteuerSatz, "Umsatzsteuersatz");

        PruefeImHundertSatz(fehler, k.SkontoSatz, "Kundenskonto");
        PruefeImHundertSatz(fehler, k.RabattSatz, "Kundenrabatt");

        return fehler;
    }

    public static Kalkulationsergebnis Rechne(Kalkulation k)
    {
        var fehler = Pruefe(k);
        if (fehler.Count > 0)
        {
            throw new FachlicherFehler(fehler[0]);
        }

        var mek = Betrag.Runde(k.Materialeinzelkosten);
        var mgk = Betrag.VomHundert(mek, k.MaterialgemeinkostenSatz);
        var materialkosten = mek + mgk;

        var fl = Betrag.Runde(k.Fertigungsloehne);
        var fgk = Betrag.VomHundert(fl, k.FertigungsgemeinkostenSatz);
        var fertigungskosten = fl + fgk;

        var herstellkosten = materialkosten + fertigungskosten;

        var vwgk = Betrag.VomHundert(herstellkosten, k.VerwaltungsgemeinkostenSatz);
        var vtgk = Betrag.VomHundert(herstellkosten, k.VertriebsgemeinkostenSatz);
        var selbstkosten = herstellkosten + vwgk + vtgk;

        var gewinn = Betrag.VomHundert(selbstkosten, k.GewinnzuschlagSatz);
        var barverkaufspreis = selbstkosten + gewinn;

        var zielverkaufspreis = Betrag.ImHundert(barverkaufspreis, k.SkontoSatz);
        var skonto = zielverkaufspreis - barverkaufspreis;

        var listenverkaufspreis = Betrag.ImHundert(zielverkaufspreis, k.RabattSatz);
        var rabatt = listenverkaufspreis - zielverkaufspreis;

        var umsatzsteuer = Betrag.VomHundert(listenverkaufspreis, k.UmsatzsteuerSatz);

        return new Kalkulationsergebnis
        {
            Materialeinzelkosten = mek,
            Materialgemeinkosten = mgk,
            Materialkosten = materialkosten,
            Fertigungsloehne = fl,
            Fertigungsgemeinkosten = fgk,
            Fertigungskosten = fertigungskosten,
            Herstellkosten = herstellkosten,
            Verwaltungsgemeinkosten = vwgk,
            Vertriebsgemeinkosten = vtgk,
            Selbstkosten = selbstkosten,
            Gewinn = gewinn,
            Barverkaufspreis = barverkaufspreis,
            Kundenskonto = skonto,
            Zielverkaufspreis = zielverkaufspreis,
            Kundenrabatt = rabatt,
            Listenverkaufspreis = listenverkaufspreis,
            Umsatzsteuer = umsatzsteuer,
            Bruttoverkaufspreis = listenverkaufspreis + umsatzsteuer
        };
    }

    public static IReadOnlyList<Schemazeile> Schema(Kalkulation k, Kalkulationsergebnis e) => new[]
    {
        new Schemazeile("Materialeinzelkosten", null, e.Materialeinzelkosten, false),
        new Schemazeile("+ Materialgemeinkosten", k.MaterialgemeinkostenSatz, e.Materialgemeinkosten, false),
        new Schemazeile("= Materialkosten", null, e.Materialkosten, true),
        new Schemazeile("Fertigungslöhne", null, e.Fertigungsloehne, false),
        new Schemazeile("+ Fertigungsgemeinkosten", k.FertigungsgemeinkostenSatz, e.Fertigungsgemeinkosten, false),
        new Schemazeile("= Fertigungskosten", null, e.Fertigungskosten, true),
        new Schemazeile("= Herstellkosten", null, e.Herstellkosten, true),
        new Schemazeile("+ Verwaltungsgemeinkosten", k.VerwaltungsgemeinkostenSatz, e.Verwaltungsgemeinkosten, false),
        new Schemazeile("+ Vertriebsgemeinkosten", k.VertriebsgemeinkostenSatz, e.Vertriebsgemeinkosten, false),
        new Schemazeile("= Selbstkosten", null, e.Selbstkosten, true),
        new Schemazeile("+ Gewinnzuschlag", k.GewinnzuschlagSatz, e.Gewinn, false),
        new Schemazeile("= Barverkaufspreis", null, e.Barverkaufspreis, true),
        new Schemazeile("+ Kundenskonto (i.H.)", k.SkontoSatz, e.Kundenskonto, false),
        new Schemazeile("= Zielverkaufspreis", null, e.Zielverkaufspreis, true),
        new Schemazeile("+ Kundenrabatt (i.H.)", k.RabattSatz, e.Kundenrabatt, false),
        new Schemazeile("= Listenverkaufspreis netto", null, e.Listenverkaufspreis, true),
        new Schemazeile("+ Umsatzsteuer", k.UmsatzsteuerSatz, e.Umsatzsteuer, false),
        new Schemazeile("= Listenverkaufspreis brutto", null, e.Bruttoverkaufspreis, true)
    };

    private static void PruefeSatz(List<string> fehler, decimal satz, string bezeichnung)
    {
        if (satz < 0m)
        {
            fehler.Add($"{bezeichnung} darf nicht negativ sein.");
        }
    }

    private static void PruefeImHundertSatz(List<string> fehler, decimal satz, string bezeichnung)
    {
        if (satz < 0m)
        {
            fehler.Add($"{bezeichnung} darf nicht negativ sein.");
        }
        else if (satz >= 100m)
        {
            fehler.Add($"{bezeichnung} muss kleiner als 100 % sein.");
        }
    }
}
