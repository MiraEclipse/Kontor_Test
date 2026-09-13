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
            Name = "Privathaushalt",
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

        KontenAnlegen(verbindung, transaktion, mandant.MandantNr);
        KategorienAnlegen(verbindung, transaktion, mandant.MandantNr);
    }

    public static void KontenAnlegen(SqliteConnection verbindung, SqliteTransaction transaktion, int mandantNr)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Konto (MandantNr, Bezeichnung, Art, AnfangsbestandCent, Gesperrt)
              VALUES (@mandant, @bezeichnung, @art, 0, 0);");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@bezeichnung", "");
        Befehle.Setze(befehl, "@art", "");

        foreach (var bezeichnung in new[] { "Girokonto", "Bargeld" })
        {
            befehl.Parameters["@bezeichnung"].Value = bezeichnung;
            befehl.Parameters["@art"].Value = bezeichnung == "Bargeld" ? Kontoarten.Code(Kontoart.Bar) : Kontoarten.Code(Kontoart.Giro);
            befehl.ExecuteNonQuery();
        }
    }

    public static void KategorienAnlegen(SqliteConnection verbindung, SqliteTransaction transaktion, int mandantNr)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Kategorie (MandantNr, Bezeichnung, Richtung, Gesperrt)
              VALUES (@mandant, @bezeichnung, @richtung, 0);");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@bezeichnung", "");
        Befehle.Setze(befehl, "@richtung", "");

        foreach (var kategorie in Kategorien)
        {
            befehl.Parameters["@bezeichnung"].Value = kategorie.Bezeichnung;
            befehl.Parameters["@richtung"].Value = Richtungen.Code(kategorie.Richtung);
            befehl.ExecuteNonQuery();
        }
    }

    public static IReadOnlyList<Kategorie> Kategorien { get; } = new[]
    {
        Kategoriezeile("Gehalt", Richtung.Einnahme),
        Kategoriezeile("Miete", Richtung.Ausgabe),
        Kategoriezeile("Lebensmittel", Richtung.Ausgabe),
        Kategoriezeile("Abos", Richtung.Ausgabe),
        Kategoriezeile("Mobilität", Richtung.Ausgabe),
        Kategoriezeile("Freizeit", Richtung.Ausgabe),
        Kategoriezeile("Sonstiges", Richtung.Ausgabe)
    };

    private static Kategorie Kategoriezeile(string bezeichnung, Richtung richtung) => new()
    {
        Bezeichnung = bezeichnung,
        Richtung = richtung
    };
}
