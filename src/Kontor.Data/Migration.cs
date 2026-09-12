using Microsoft.Data.Sqlite;

namespace Kontor.Data;

internal static class Migration
{
    public static void Fuehre(SqliteConnection verbindung, int vonVersion)
    {
        var version = vonVersion;

        while (version < Schema.Version)
        {
            using var transaktion = verbindung.BeginTransaction(deferred: false);
            version = Schritt(verbindung, transaktion, version);
            Schreibe(verbindung, transaktion, version);
            transaktion.Commit();
        }
    }

    private static int Schritt(SqliteConnection verbindung, SqliteTransaction transaktion, int version) => version switch
    {
        1 => AufVersion2(verbindung, transaktion),
        _ => throw new DatenbankFehler($"Für die Schemaversion {version} ist keine Migration hinterlegt.")
    };

    // Version 1 -> 2: Kunde und Artikel werden nie gelöscht, weil Belege an ihnen hängen.
    // Statt eines Löschens bekommen beide eine Spalte Gesperrt, die das bisherige Aktiv-Flag
    // invertiert ablöst (Aktiv = 0 entsprach "nicht mehr verwendbar", also Gesperrt = 1).
    // Kunde bekommt zusätzlich die USt-IdNr für die Validierung im Core.
    private static int AufVersion2(SqliteConnection verbindung, SqliteTransaction transaktion)
    {
        Befehle.Fuehre(verbindung, transaktion,
            "ALTER TABLE Kunde ADD COLUMN UstIdNr TEXT NOT NULL DEFAULT '';");
        Befehle.Fuehre(verbindung, transaktion,
            "ALTER TABLE Kunde ADD COLUMN Gesperrt INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1));");
        Befehle.Fuehre(verbindung, transaktion,
            "UPDATE Kunde SET Gesperrt = 1 - Aktiv;");
        Befehle.Fuehre(verbindung, transaktion,
            "ALTER TABLE Kunde DROP COLUMN Aktiv;");

        Befehle.Fuehre(verbindung, transaktion,
            "ALTER TABLE Artikel ADD COLUMN Gesperrt INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1));");
        Befehle.Fuehre(verbindung, transaktion,
            "UPDATE Artikel SET Gesperrt = 1 - Aktiv;");
        Befehle.Fuehre(verbindung, transaktion,
            "ALTER TABLE Artikel DROP COLUMN Aktiv;");

        return 2;
    }

    private static void Schreibe(SqliteConnection verbindung, SqliteTransaction transaktion, int version)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            "UPDATE SchemaVersion SET Version = @version, Angelegt = @angelegt WHERE Id = 1;");
        Befehle.Setze(befehl, "@version", version);
        Befehle.Setze(befehl, "@angelegt", Feldwerte.Zeit(DateTime.Now));
        befehl.ExecuteNonQuery();
    }
}
