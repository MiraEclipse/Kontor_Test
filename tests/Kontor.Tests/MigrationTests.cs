using Kontor.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kontor.Tests;

public class MigrationTests
{
    [Fact]
    public void MigrationVonVersion1AufVersion2ErhaeltDatenUndInvertiertAktivZuGesperrt()
    {
        var ordner = Path.Combine(Path.GetTempPath(), "kontor-migration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(ordner);
        var pfad = Path.Combine(ordner, "alt.db");

        try
        {
            ErstelleVersion1Datenbank(pfad);

            var datenbank = new Datenbank(pfad);
            datenbank.Vorbereiten();

            using (var verbindung = datenbank.Oeffne())
            {
                using (var version = verbindung.CreateCommand())
                {
                    version.CommandText = "SELECT Version FROM SchemaVersion WHERE Id = 1;";
                    Assert.Equal((long)datenbank.SchemaVersion, Convert.ToInt64(version.ExecuteScalar()));
                }

                using (var mandant = verbindung.CreateCommand())
                {
                    mandant.CommandText = "SELECT Name FROM Mandant WHERE MandantNr = 1;";
                    Assert.Equal("Alter Mandant", Convert.ToString(mandant.ExecuteScalar()));
                }

                using (var kunde = verbindung.CreateCommand())
                {
                    kunde.CommandText = "SELECT Nummer, Name, Gesperrt, UstIdNr FROM Kunde ORDER BY KundeId;";
                    using var leser = kunde.ExecuteReader();

                    Assert.True(leser.Read());
                    Assert.Equal("K-0001", leser.GetString(0));
                    Assert.Equal("Aktiver Kunde", leser.GetString(1));
                    Assert.Equal(0L, leser.GetInt64(2));
                    Assert.Equal("", leser.GetString(3));

                    Assert.True(leser.Read());
                    Assert.Equal("K-0002", leser.GetString(0));
                    Assert.Equal("Inaktiver Kunde", leser.GetString(1));
                    Assert.Equal(1L, leser.GetInt64(2));

                    Assert.False(leser.Read());
                }

                using (var artikel = verbindung.CreateCommand())
                {
                    artikel.CommandText = "SELECT Nummer, Bezeichnung, Gesperrt FROM Artikel ORDER BY ArtikelId;";
                    using var leser = artikel.ExecuteReader();

                    Assert.True(leser.Read());
                    Assert.Equal("A-0001", leser.GetString(0));
                    Assert.Equal(0L, leser.GetInt64(2));

                    Assert.True(leser.Read());
                    Assert.Equal("A-0002", leser.GetString(0));
                    Assert.Equal(1L, leser.GetInt64(2));

                    Assert.False(leser.Read());
                }
            }

            // Nach der Migration verhält sich die Datenbank wie jede andere: Repositories funktionieren normal.
            var kunden = new KundeRepository(datenbank);
            var geladen = kunden.LadeMitNummer(1, "K-0001");
            Assert.NotNull(geladen);
            Assert.False(geladen!.Gesperrt);

            SqliteConnection.ClearAllPools();
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            try
            {
                Directory.Delete(ordner, true);
            }
            catch (IOException)
            {
            }
        }
    }

    // Baut eine Datenbank exakt so, wie Schritt 2 sie erzeugt hätte (Schemaversion 1, Aktiv statt Gesperrt,
    // ohne UstIdNr) - unabhängig vom aktuellen Schema.cs, damit der Test einen echten Migrationsschritt prüft.
    private static void ErstelleVersion1Datenbank(string pfad)
    {
        using var verbindung = new SqliteConnection($"Data Source={pfad}");
        verbindung.Open();

        void Exec(string sql)
        {
            using var befehl = verbindung.CreateCommand();
            befehl.CommandText = sql;
            befehl.ExecuteNonQuery();
        }

        Exec(@"CREATE TABLE SchemaVersion (
                    Id       INTEGER NOT NULL PRIMARY KEY CHECK (Id = 1),
                    Version  INTEGER NOT NULL CHECK (Version > 0),
                    Angelegt TEXT    NOT NULL
                );");
        Exec("INSERT INTO SchemaVersion (Id, Version, Angelegt) VALUES (1, 1, '2026-01-01 00:00:00');");

        Exec(@"CREATE TABLE Mandant (
                    MandantNr INTEGER NOT NULL PRIMARY KEY CHECK (MandantNr > 0),
                    Name      TEXT    NOT NULL,
                    Ort       TEXT    NOT NULL DEFAULT '',
                    Waehrung  TEXT    NOT NULL DEFAULT 'EUR'
                );");
        Exec("INSERT INTO Mandant (MandantNr, Name, Ort, Waehrung) VALUES (1, 'Alter Mandant', 'Bremen', 'EUR');");

        Exec(@"CREATE TABLE Kunde (
                    KundeId   INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    MandantNr INTEGER NOT NULL REFERENCES Mandant (MandantNr),
                    Nummer    TEXT    NOT NULL,
                    Name      TEXT    NOT NULL,
                    Strasse   TEXT    NOT NULL DEFAULT '',
                    Plz       TEXT    NOT NULL DEFAULT '',
                    Ort       TEXT    NOT NULL DEFAULT '',
                    Telefon   TEXT    NOT NULL DEFAULT '',
                    Aktiv     INTEGER NOT NULL DEFAULT 1 CHECK (Aktiv IN (0, 1)),
                    UNIQUE (KundeId, MandantNr)
                );");
        Exec("CREATE UNIQUE INDEX UX_Kunde_Nummer ON Kunde (MandantNr, Nummer);");
        Exec("INSERT INTO Kunde (MandantNr, Nummer, Name, Aktiv) VALUES (1, 'K-0001', 'Aktiver Kunde', 1);");
        Exec("INSERT INTO Kunde (MandantNr, Nummer, Name, Aktiv) VALUES (1, 'K-0002', 'Inaktiver Kunde', 0);");

        Exec(@"CREATE TABLE Artikel (
                    ArtikelId    INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    MandantNr    INTEGER NOT NULL REFERENCES Mandant (MandantNr),
                    Nummer       TEXT    NOT NULL,
                    Bezeichnung  TEXT    NOT NULL,
                    Einheit      TEXT    NOT NULL DEFAULT 'ST',
                    PreisCent    INTEGER NOT NULL CHECK (PreisCent >= 0),
                    SteuerSatzBp INTEGER NOT NULL CHECK (SteuerSatzBp >= 0),
                    Aktiv        INTEGER NOT NULL DEFAULT 1 CHECK (Aktiv IN (0, 1)),
                    UNIQUE (ArtikelId, MandantNr)
                );");
        Exec("CREATE UNIQUE INDEX UX_Artikel_Nummer ON Artikel (MandantNr, Nummer);");
        Exec("INSERT INTO Artikel (MandantNr, Nummer, Bezeichnung, PreisCent, SteuerSatzBp, Aktiv) VALUES (1, 'A-0001', 'Aktiver Artikel', 1000, 1900, 1);");
        Exec("INSERT INTO Artikel (MandantNr, Nummer, Bezeichnung, PreisCent, SteuerSatzBp, Aktiv) VALUES (1, 'A-0002', 'Inaktiver Artikel', 2000, 700, 0);");

        verbindung.Close();
        SqliteConnection.ClearAllPools();
    }
}
