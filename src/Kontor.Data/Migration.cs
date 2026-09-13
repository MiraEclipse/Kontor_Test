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
        2 => AufVersion3(verbindung, transaktion),
        3 => AufVersion4(verbindung, transaktion),
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

    // Version 2 -> 3: Fachbereichswechsel von der Firmenbuchhaltung zur privaten Haushaltsführung.
    // Es gibt keine erhaltenswerten Daten in den alten Fachtabellen - sie werden verworfen und durch
    // die neuen Tabellen aus Schema.FachbereichAnweisungen ersetzt. Mandant und SchemaVersion bleiben.
    private static int AufVersion3(SqliteConnection verbindung, SqliteTransaction transaktion)
    {
        foreach (var tabelle in AlteFachtabellen)
        {
            Befehle.Fuehre(verbindung, transaktion, $"DROP TABLE IF EXISTS {tabelle};");
        }

        foreach (var anweisung in Schema.FachbereichAnweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        return 3;
    }

    private static readonly string[] AlteFachtabellen =
    {
        "Buchungszeile", "Belegposition", "Notiz", "Beleg", "Kalkulation", "Nummernkreis", "Artikel", "Kunde", "Konto"
    };

    // Version 3 -> 4: Anmeldung und Rechte kommen hinzu - reine Ergänzung, nichts wird verworfen.
    private static int AufVersion4(SqliteConnection verbindung, SqliteTransaction transaktion)
    {
        foreach (var anweisung in Schema.AuthAnweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        return 4;
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
