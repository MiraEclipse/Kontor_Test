using Microsoft.Data.Sqlite;

namespace Kontor.Data;

internal static class Schema
{
    public const int Version = 2;

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
        foreach (var anweisung in Anweisungen)
        {
            Befehle.Fuehre(verbindung, transaktion, anweisung);
        }

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "INSERT INTO SchemaVersion (Id, Version, Angelegt) VALUES (1, @version, @angelegt);");
        Befehle.Setze(befehl, "@version", Version);
        Befehle.Setze(befehl, "@angelegt", Feldwerte.Zeit(DateTime.Now));
        befehl.ExecuteNonQuery();
    }

    private static readonly string[] Anweisungen =
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
        );",

        @"CREATE TABLE Konto (
            MandantNr   INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            KontoNr     TEXT    NOT NULL,
            Bezeichnung TEXT    NOT NULL,
            Art         TEXT    NOT NULL CHECK (Art IN ('A', 'P', 'E', 'W')),
            PRIMARY KEY (MandantNr, KontoNr)
        );",

        @"CREATE TABLE Kunde (
            KundeId   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Nummer    TEXT    NOT NULL,
            Name      TEXT    NOT NULL,
            Strasse   TEXT    NOT NULL DEFAULT '',
            Plz       TEXT    NOT NULL DEFAULT '',
            Ort       TEXT    NOT NULL DEFAULT '',
            Telefon   TEXT    NOT NULL DEFAULT '',
            UstIdNr   TEXT    NOT NULL DEFAULT '',
            Gesperrt  INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1)),
            UNIQUE (KundeId, MandantNr)
        );",
        "CREATE INDEX IX_Kunde_Mandant ON Kunde (MandantNr);",
        "CREATE UNIQUE INDEX UX_Kunde_Nummer ON Kunde (MandantNr, Nummer);",

        @"CREATE TABLE Artikel (
            ArtikelId    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr    INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Nummer       TEXT    NOT NULL,
            Bezeichnung  TEXT    NOT NULL,
            Einheit      TEXT    NOT NULL DEFAULT 'ST',
            PreisCent    INTEGER NOT NULL CHECK (PreisCent >= 0),
            SteuerSatzBp INTEGER NOT NULL CHECK (SteuerSatzBp >= 0),
            Gesperrt     INTEGER NOT NULL DEFAULT 0 CHECK (Gesperrt IN (0, 1)),
            UNIQUE (ArtikelId, MandantNr)
        );",
        "CREATE INDEX IX_Artikel_Mandant ON Artikel (MandantNr);",
        "CREATE UNIQUE INDEX UX_Artikel_Nummer ON Artikel (MandantNr, Nummer);",

        @"CREATE TABLE Nummernkreis (
            MandantNr    INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Jahr         INTEGER NOT NULL CHECK (Jahr > 0),
            Belegart     TEXT    NOT NULL CHECK (Belegart IN ('RE', 'GU', 'ZA')),
            LetzteNummer INTEGER NOT NULL DEFAULT 0 CHECK (LetzteNummer >= 0),
            PRIMARY KEY (MandantNr, Jahr, Belegart)
        );",

        @"CREATE TABLE Beleg (
            BelegId      INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr    INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Nummer       TEXT    NOT NULL,
            Belegart     TEXT    NOT NULL CHECK (Belegart IN ('RE', 'GU', 'ZA')),
            Belegdatum   TEXT    NOT NULL,
            KundeId      INTEGER NULL,
            StorniertVon INTEGER NULL,
            Erfasst      TEXT    NOT NULL,
            UNIQUE (BelegId, MandantNr),
            FOREIGN KEY (KundeId, MandantNr) REFERENCES Kunde (KundeId, MandantNr),
            FOREIGN KEY (StorniertVon, MandantNr) REFERENCES Beleg (BelegId, MandantNr)
        );",
        "CREATE UNIQUE INDEX UX_Beleg_Nummer ON Beleg (MandantNr, Nummer);",
        "CREATE UNIQUE INDEX UX_Beleg_StorniertVon ON Beleg (StorniertVon, MandantNr);",
        "CREATE INDEX IX_Beleg_Mandant_Datum ON Beleg (MandantNr, Belegdatum);",
        "CREATE INDEX IX_Beleg_Kunde ON Beleg (KundeId, MandantNr);",

        @"CREATE TABLE Belegposition (
            PositionId       INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr        INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            BelegId          INTEGER NOT NULL,
            PosNr            INTEGER NOT NULL CHECK (PosNr > 0),
            ArtikelId        INTEGER NULL,
            Bezeichnung      TEXT    NOT NULL,
            Einheit          TEXT    NOT NULL DEFAULT 'ST',
            MengeTausendstel INTEGER NOT NULL CHECK (MengeTausendstel > 0),
            EinzelpreisCent  INTEGER NOT NULL CHECK (EinzelpreisCent >= 0),
            SteuerSatzBp     INTEGER NOT NULL CHECK (SteuerSatzBp >= 0),
            FOREIGN KEY (BelegId, MandantNr) REFERENCES Beleg (BelegId, MandantNr),
            FOREIGN KEY (ArtikelId, MandantNr) REFERENCES Artikel (ArtikelId, MandantNr)
        );",
        "CREATE INDEX IX_Belegposition_Beleg ON Belegposition (BelegId, MandantNr);",
        "CREATE INDEX IX_Belegposition_Artikel ON Belegposition (ArtikelId, MandantNr);",
        "CREATE UNIQUE INDEX UX_Belegposition_PosNr ON Belegposition (BelegId, PosNr);",

        @"CREATE TABLE Buchungszeile (
            ZeileId    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr  INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            BelegId    INTEGER NOT NULL,
            KontoNr    TEXT    NOT NULL,
            BetragCent INTEGER NOT NULL CHECK (BetragCent > 0),
            SollHaben  TEXT    NOT NULL CHECK (SollHaben IN ('S', 'H')),
            FOREIGN KEY (BelegId, MandantNr) REFERENCES Beleg (BelegId, MandantNr),
            FOREIGN KEY (MandantNr, KontoNr) REFERENCES Konto (MandantNr, KontoNr)
        );",
        "CREATE INDEX IX_Buchungszeile_Beleg ON Buchungszeile (BelegId, MandantNr);",
        "CREATE INDEX IX_Buchungszeile_Konto ON Buchungszeile (MandantNr, KontoNr);",

        @"CREATE TABLE Kalkulation (
            KalkulationId                 INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr                     INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            ArtikelId                     INTEGER NULL,
            Bezeichnung                   TEXT    NOT NULL,
            MaterialeinzelkostenCent      INTEGER NOT NULL CHECK (MaterialeinzelkostenCent >= 0),
            MaterialgemeinkostenSatzBp    INTEGER NOT NULL CHECK (MaterialgemeinkostenSatzBp >= 0),
            FertigungsloehneCent          INTEGER NOT NULL CHECK (FertigungsloehneCent >= 0),
            FertigungsgemeinkostenSatzBp  INTEGER NOT NULL CHECK (FertigungsgemeinkostenSatzBp >= 0),
            VerwaltungsgemeinkostenSatzBp INTEGER NOT NULL CHECK (VerwaltungsgemeinkostenSatzBp >= 0),
            VertriebsgemeinkostenSatzBp   INTEGER NOT NULL CHECK (VertriebsgemeinkostenSatzBp >= 0),
            GewinnzuschlagSatzBp          INTEGER NOT NULL CHECK (GewinnzuschlagSatzBp >= 0),
            SkontoSatzBp                  INTEGER NOT NULL CHECK (SkontoSatzBp >= 0 AND SkontoSatzBp < 10000),
            RabattSatzBp                  INTEGER NOT NULL CHECK (RabattSatzBp >= 0 AND RabattSatzBp < 10000),
            UmsatzsteuerSatzBp            INTEGER NOT NULL CHECK (UmsatzsteuerSatzBp >= 0),
            Erfasst                       TEXT    NOT NULL,
            FOREIGN KEY (ArtikelId, MandantNr) REFERENCES Artikel (ArtikelId, MandantNr)
        );",
        "CREATE INDEX IX_Kalkulation_Mandant ON Kalkulation (MandantNr);",
        "CREATE INDEX IX_Kalkulation_Artikel ON Kalkulation (ArtikelId, MandantNr);",

        @"CREATE TABLE Notiz (
            NotizId    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            MandantNr  INTEGER NOT NULL REFERENCES Mandant (MandantNr),
            Betreff    TEXT    NOT NULL,
            Text       TEXT    NOT NULL DEFAULT '',
            Prioritaet INTEGER NOT NULL DEFAULT 2 CHECK (Prioritaet IN (1, 2, 3)),
            Erledigt   INTEGER NOT NULL DEFAULT 0 CHECK (Erledigt IN (0, 1)),
            BelegId    INTEGER NULL,
            KundeId    INTEGER NULL,
            Angelegt   TEXT    NOT NULL,
            FOREIGN KEY (BelegId, MandantNr) REFERENCES Beleg (BelegId, MandantNr),
            FOREIGN KEY (KundeId, MandantNr) REFERENCES Kunde (KundeId, MandantNr)
        );",
        "CREATE INDEX IX_Notiz_Mandant ON Notiz (MandantNr, Erledigt);",
        "CREATE INDEX IX_Notiz_Beleg ON Notiz (BelegId, MandantNr);",
        "CREATE INDEX IX_Notiz_Kunde ON Notiz (KundeId, MandantNr);"
    };
}
