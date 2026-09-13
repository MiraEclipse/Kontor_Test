using Microsoft.Data.Sqlite;

namespace Kontor.Data;

internal static class Schema
{
    public const int Version = 4;

    public static bool Vorhanden(SqliteConnection verbindung)
    {
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name;");
        Befehle.Setze(befehl, "@name", "SchemaVersion");
        return Convert.ToInt32(befehl.ExecuteScalar()) > 0;
    }

    public static int LeseVersion(SqliteConnection verbindung)
    {
        using var befehl = Befehle.Neu(verbindung, null, "SELECT Version FROM SchemaVersion WHERE Id = 1;");
        var wert = befehl.ExecuteScalar();
        if (wert is null || wert is DBNull)
        {
            throw new DatenbankFehler("Die Tabelle SchemaVersion enthält keine Zeile.");
        }

        return Convert.ToInt32(wert);
    }

    public static void Anlegen(SqliteConnection verbindung, SqliteTransaction transaktion)
    {
        foreach (var anweisung in InfrastrukturAnweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        foreach (var anweisung in AuthAnweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        foreach (var anweisung in FachbereichAnweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "INSERT INTO SchemaVersion (Id, Version, Angelegt) VALUES (1, @version, @angelegt);");
        Befehle.Setze(befehl, "@version", Version);
        Befehle.Setze(befehl, "@angelegt", Feldwerte.Zeit(DateTime.Now));
        befehl.ExecuteNonQuery();
    }

    // Bleiben über einen Fachbereichswechsel hinweg bestehen.
    internal static readonly string[] InfrastrukturAnweisungen =
    {
        @"CREATE TABLE SchemaVersion (
            Id       INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
            Version  INTEGER NOT NULL CHECK (Version > 0),
            Angelegt TEXT    NOT NULL
        );",

        @"CREATE TABLE Mandant (
            MandantNr INTEGER NOT NULL PRIMARY KEY CHECK (MandantNr > 0),
            Name      TEXT    NOT NULL,
            Ort       TEXT    NOT NULL DEFAULT '',
            Waehrung  TEXT    NOT NULL DEFAULT 'EUR'
        );"
    };

    // Anmeldung und Rechte - bleiben wie Mandant/SchemaVersion über einen Fachbereichswechsel hinweg
    // bestehen, kamen aber erst mit Schemaversion 4 hinzu.
    internal static readonly string[] AuthAnweisungen =
    {
        @"CREATE TABLE Benutzer (
            BenutzerId      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Anmeldename     TEXT    NOT NULL,
            Anzeigename     TEXT    NOT NULL,
            KennwortHash    BLOB    NOT NULL,
            Salz            BLOB    NOT NULL,
            Durchlaeufe     INTEGER NOT NULL CHECK (Durchlaeufe > 0),
            Systemrechte    INTEGER NOT NULL DEFAULT 0 CHECK (Systemrechte IN (0, 1)),
            Gesperrt        INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1)),
            Angelegt        TEXT    NOT NULL,
            LetzteAnmeldung TEXT    NULL
        );",
        "CREATE UNIQUE INDEX UX_Benutzer_Anmeldename ON Benutzer (Anmeldename);",

        // Rechte werden nie gelöscht - Entziehen setzt nur GueltigBis, damit nachvollziehbar bleibt,
        // wer wann wofür freigeschaltet war.
        @"CREATE TABLE Recht (
            RechtId    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            BenutzerId INTEGER NOT NULL REFERENCES Benutzer (BenutzerId),
            MandantNr  INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Bereich    TEXT    NOT NULL,
            Stufe      TEXT    NOT NULL CHECK (Stufe IN ('L', 'A', 'V')),
            GueltigVon TEXT    NOT NULL,
            GueltigBis TEXT    NULL,
            ErteiltVon INTEGER NOT NULL REFERENCES Benutzer (BenutzerId),
            ErteiltAm  TEXT    NOT NULL
        );",
        "CREATE INDEX IX_Recht_Benutzer ON Recht (BenutzerId);",

        @"CREATE TABLE Anmeldeprotokoll (
            EintragId   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Zeitpunkt   TEXT    NOT NULL,
            Anmeldename TEXT    NOT NULL,
            Ergebnis    TEXT    NOT NULL CHECK (Ergebnis IN ('OK', 'UNBEKANNT', 'KENNWORT', 'GESPERRT'))
        );",
        "CREATE INDEX IX_Anmeldeprotokoll_Anmeldename ON Anmeldeprotokoll (Anmeldename, Zeitpunkt);"
    };

    // Der fachliche Teil des Schemas - wird bei einer Neuanlage direkt erzeugt und bei einem
    // Fachbereichswechsel (Migration) nach dem Verwerfen der alten Tabellen erneut ausgeführt.
    internal static readonly string[] FachbereichAnweisungen =
    {
        @"CREATE TABLE Konto (
            KontoId            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr          INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Bezeichnung        TEXT    NOT NULL,
            Art                TEXT    NOT NULL CHECK (Art IN ('G', 'B', 'S', 'K')),
            AnfangsbestandCent INTEGER NOT NULL DEFAULT 0,
            Gesperrt           INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1)),
            UNIQUE (KontoId, MandantNr)
        );",
        "CREATE INDEX IX_Konto_Mandant ON Konto (MandantNr);",

        @"CREATE TABLE Kategorie (
            KategorieId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr   INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Bezeichnung TEXT    NOT NULL,
            Richtung    TEXT    NOT NULL CHECK (Richtung IN ('E', 'A')),
            Gesperrt    INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1)),
            UNIQUE (KategorieId, MandantNr)
        );",
        "CREATE INDEX IX_Kategorie_Mandant ON Kategorie (MandantNr);",

        @"CREATE TABLE Vertrag (
            VertragId              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr              INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Bezeichnung            TEXT    NOT NULL,
            Anbieter               TEXT    NOT NULL DEFAULT '',
            Turnus                 TEXT    NOT NULL CHECK (Turnus IN ('M', 'Q', 'H', 'J')),
            Beginn                 TEXT    NOT NULL,
            MindestlaufzeitMonate  INTEGER NOT NULL DEFAULT 0 CHECK (MindestlaufzeitMonate >= 0),
            KuendigungsfristMonate INTEGER NOT NULL DEFAULT 0 CHECK (KuendigungsfristMonate >= 0),
            KategorieId            INTEGER NOT NULL,
            KontoId                INTEGER NOT NULL,
            AutomatischBuchen      INTEGER NOT NULL DEFAULT 0 CHECK (AutomatischBuchen IN (0, 1)),
            GekuendigtZum          TEXT    NULL,
            Beendet                INTEGER NOT NULL DEFAULT 0 CHECK (Beendet IN (0, 1)),
            UNIQUE (VertragId, MandantNr),
            FOREIGN KEY (KategorieId, MandantNr) REFERENCES Kategorie (KategorieId, MandantNr),
            FOREIGN KEY (KontoId, MandantNr) REFERENCES Konto (KontoId, MandantNr)
        );",
        "CREATE INDEX IX_Vertrag_Mandant ON Vertrag (MandantNr);",
        "CREATE INDEX IX_Vertrag_Kategorie ON Vertrag (KategorieId, MandantNr);",
        "CREATE INDEX IX_Vertrag_Konto ON Vertrag (KontoId, MandantNr);",

        @"CREATE TABLE Vertragspreis (
            VertragspreisId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr       INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            VertragId       INTEGER NOT NULL,
            GueltigAb       TEXT    NOT NULL,
            BetragCent      INTEGER NOT NULL CHECK (BetragCent >= 0),
            FOREIGN KEY (VertragId, MandantNr) REFERENCES Vertrag (VertragId, MandantNr)
        );",
        "CREATE INDEX IX_Vertragspreis_Vertrag ON Vertragspreis (VertragId, MandantNr);",

        @"CREATE TABLE Buchung (
            BuchungId   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr   INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Datum       TEXT    NOT NULL,
            KontoId     INTEGER NOT NULL,
            KategorieId INTEGER NOT NULL,
            BetragCent  INTEGER NOT NULL CHECK (BetragCent > 0),
            Text        TEXT    NOT NULL DEFAULT '',
            VertragId   INTEGER NULL,
            FOREIGN KEY (KontoId, MandantNr) REFERENCES Konto (KontoId, MandantNr),
            FOREIGN KEY (KategorieId, MandantNr) REFERENCES Kategorie (KategorieId, MandantNr),
            FOREIGN KEY (VertragId, MandantNr) REFERENCES Vertrag (VertragId, MandantNr)
        );",
        "CREATE INDEX IX_Buchung_Mandant_Datum ON Buchung (MandantNr, Datum);",
        "CREATE INDEX IX_Buchung_Konto ON Buchung (KontoId, MandantNr);",
        "CREATE INDEX IX_Buchung_Kategorie ON Buchung (KategorieId, MandantNr);",
        "CREATE INDEX IX_Buchung_Vertrag ON Buchung (VertragId, MandantNr);",

        @"CREATE TABLE Umbuchung (
            UmbuchungId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr   INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Datum       TEXT    NOT NULL,
            VonKontoId  INTEGER NOT NULL,
            NachKontoId INTEGER NOT NULL,
            BetragCent  INTEGER NOT NULL CHECK (BetragCent > 0),
            Text        TEXT    NOT NULL DEFAULT '',
            FOREIGN KEY (VonKontoId, MandantNr) REFERENCES Konto (KontoId, MandantNr),
            FOREIGN KEY (NachKontoId, MandantNr) REFERENCES Konto (KontoId, MandantNr)
        );",
        "CREATE INDEX IX_Umbuchung_Mandant_Datum ON Umbuchung (MandantNr, Datum);",
        "CREATE INDEX IX_Umbuchung_VonKonto ON Umbuchung (VonKontoId, MandantNr);",
        "CREATE INDEX IX_Umbuchung_NachKonto ON Umbuchung (NachKontoId, MandantNr);",

        @"CREATE TABLE Notiz (
            NotizId     INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr   INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Betreff     TEXT    NOT NULL,
            Text        TEXT    NOT NULL DEFAULT '',
            Prioritaet  INTEGER NOT NULL DEFAULT 2 CHECK (Prioritaet IN (1, 2, 3)),
            Erledigt    INTEGER NOT NULL DEFAULT 0 CHECK (Erledigt IN (0, 1)),
            Faelligkeit TEXT    NULL,
            ErledigtAm  TEXT    NULL,
            Angelegt    TEXT    NOT NULL
        );",
        "CREATE INDEX IX_Notiz_Mandant ON Notiz (MandantNr, Erledigt);",
        "CREATE INDEX IX_Notiz_Faelligkeit ON Notiz (MandantNr, Faelligkeit);"
    };
}
