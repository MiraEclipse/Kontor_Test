using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class Datenbank
{
    public const string StandardDateiname = "kontor.db";

    public Datenbank(string dateipfad)
    {
        if (string.IsNullOrWhiteSpace(dateipfad))
        {
            throw new DatenbankFehler("Kein Pfad für die Datenbankdatei angegeben.");
        }

        Dateipfad = Path.GetFullPath(dateipfad);
        Verbindungszeichenfolge = new SqliteConnectionStringBuilder
        {
            DataSource = Dateipfad,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();
    }

    public static Datenbank Standard() =>
        new(Path.Combine(AppContext.BaseDirectory, StandardDateiname));

    public string Dateipfad { get; }

    public string Verbindungszeichenfolge { get; }

    public int SchemaVersion => Schema.Version;

    public SqliteConnection Oeffne()
    {
        var verbindung = new SqliteConnection(Verbindungszeichenfolge);
        verbindung.Open();
        Befehle.Fuehre(verbindung, null, "PRAGMA foreign_keys = ON;");
        return verbindung;
    }

    public SqliteTransaction BeginneSofort(SqliteConnection verbindung) =>
        verbindung.BeginTransaction(deferred: false);

    public void Vorbereiten()
    {
        using var verbindung = Oeffne();

        if (!Schema.Vorhanden(verbindung))
        {
            using var transaktion = BeginneSofort(verbindung);
            Schema.Anlegen(verbindung, transaktion);
            Startbefuellung.Ausfuehren(verbindung, transaktion);
            transaktion.Commit();
            return;
        }

        var version = Schema.LeseVersion(verbindung);
        if (version == Schema.Version)
        {
            return;
        }

        if (version > Schema.Version)
        {
            throw new DatenbankFehler(
                $"Die Datenbank {Dateipfad} hat Schemaversion {version}, dieses Programm kennt nur {Schema.Version}.");
        }

        Migration.Fuehre(verbindung, version);
    }
}
