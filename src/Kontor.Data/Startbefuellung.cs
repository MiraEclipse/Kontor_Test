using Kontor.Core.Modell;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public static class Startbefuellung
{
    public const int TestmandantNr = 1;

    public static void Ausfuehren(SqliteConnection verbindung, SqliteTransaction transaktion)
    {
        MandantAnlegen(verbindung, transaktion, new Mandant
        {
            MandantNr = TestmandantNr,
            Name = "Musterbetrieb GmbH",
            Ort = "Hamburg",
            Waehrung = "EUR"
        });
    }

    public static void MandantAnlegen(SqliteConnection verbindung, SqliteTransaction transaktion, Mandant mandant)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            "INSERT INTO Mandant (MandantNr, Name, Ort, Waehrung) VALUES (@nr, @name, @ort, @waehrung);");
        Befehle.Setze(befehl, "@nr", mandant.MandantNr);
        Befehle.Setze(befehl, "@name", mandant.Name);
        Befehle.Setze(befehl, "@ort", mandant.Ort);
        Befehle.Setze(befehl, "@waehrung", mandant.Waehrung);
        befehl.ExecuteNonQuery();

        KontenrahmenAnlegen(verbindung, transaktion, mandant.MandantNr);
    }

    public static void KontenrahmenAnlegen(SqliteConnection verbindung, SqliteTransaction transaktion, int mandantNr)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            "INSERT INTO Konto (MandantNr, KontoNr, Bezeichnung, Art) VALUES (@mandant, @konto, @bezeichnung, @art);");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@konto", "");
        Befehle.Setze(befehl, "@bezeichnung", "");
        Befehle.Setze(befehl, "@art", "");

        foreach (var konto in Kontenrahmen)
        {
            befehl.Parameters["@konto"].Value = konto.KontoNr;
            befehl.Parameters["@bezeichnung"].Value = konto.Bezeichnung;
            befehl.Parameters["@art"].Value = Kontoarten.Code(konto.Art);
            befehl.ExecuteNonQuery();
        }
    }

    public static IReadOnlyList<Konto> Kontenrahmen { get; } = new[]
    {
        Kontozeile("0400", "Technische Anlagen und Maschinen", Kontoart.Aktiv),
        Kontozeile("0420", "Büroeinrichtung", Kontoart.Aktiv),
        Kontozeile("0800", "Gezeichnetes Kapital", Kontoart.Passiv),
        Kontozeile("1000", "Kasse", Kontoart.Aktiv),
        Kontozeile("1200", "Bank", Kontoart.Aktiv),
        Kontozeile("1400", "Forderungen aus Lieferungen und Leistungen", Kontoart.Aktiv),
        Kontozeile("1571", "Abziehbare Vorsteuer 7 %", Kontoart.Aktiv),
        Kontozeile("1576", "Abziehbare Vorsteuer 19 %", Kontoart.Aktiv),
        Kontozeile("1600", "Verbindlichkeiten aus Lieferungen und Leistungen", Kontoart.Passiv),
        Kontozeile("1775", "Umsatzsteuer 7 %", Kontoart.Passiv),
        Kontozeile("1776", "Umsatzsteuer 19 %", Kontoart.Passiv),
        Kontozeile("3400", "Wareneingang 19 % Vorsteuer", Kontoart.Aufwand),
        Kontozeile("4100", "Löhne und Gehälter", Kontoart.Aufwand),
        Kontozeile("4200", "Raumkosten", Kontoart.Aufwand),
        Kontozeile("4600", "Werbekosten", Kontoart.Aufwand),
        Kontozeile("4830", "Abschreibungen auf Sachanlagen", Kontoart.Aufwand),
        Kontozeile("4910", "Porto", Kontoart.Aufwand),
        Kontozeile("4920", "Telefon", Kontoart.Aufwand),
        Kontozeile("4930", "Bürobedarf", Kontoart.Aufwand),
        Kontozeile("4980", "Sonstiger Betriebsbedarf", Kontoart.Aufwand),
        Kontozeile("8200", "Erlöse ohne Umsatzsteuer", Kontoart.Ertrag),
        Kontozeile("8300", "Erlöse 7 % Umsatzsteuer", Kontoart.Ertrag),
        Kontozeile("8400", "Erlöse 19 % Umsatzsteuer", Kontoart.Ertrag),
        Kontozeile("8736", "Gewährte Skonti 19 % Umsatzsteuer", Kontoart.Ertrag)
    };

    private static Konto Kontozeile(string kontoNr, string bezeichnung, Kontoart art) => new()
    {
        KontoNr = kontoNr,
        Bezeichnung = bezeichnung,
        Art = art
    };
}
