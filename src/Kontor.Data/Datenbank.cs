using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class Datenbank
{
    public const string StandardDateiname = "kontor.db";
    public const string StandardUnterordner = "Kontor";

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

    // Liefert nur den Pfad, legt aber nichts im Dateisystem an - das geschieht ausschließlich in
    // Vorbereiten(). Ein Konstruktor bzw. eine Standard()-Methode, die nebenbei ins Dateisystem
    // schreibt, macht Tests unberechenbar.
    //
    // ueberschreibenderPfad kommt vom Kommandozeilenargument --datenbank und gewinnt, wenn gesetzt -
    // %AppData% gehört zu genau einem Windows-Konto, mehrere Benutzer am selben Rechner brauchen
    // einen gemeinsamen, frei wählbaren Ablageort.
    public static Datenbank Standard(string? ueberschreibenderPfad = null) =>
        new(!string.IsNullOrWhiteSpace(ueberschreibenderPfad) ? ueberschreibenderPfad : StandardPfad());

    public static string StandardPfad() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), StandardUnterordner, StandardDateiname);

    // Der frühere Standardpfad, mitten im Build-Ordner - wird beim ersten Start am neuen Ort einmalig
    // übernommen (siehe Vorbereiten()).
    public static string AlterStandardpfad() => Path.Combine(AppContext.BaseDirectory, StandardDateiname);

    public string Dateipfad { get; }

    public string Verbindungszeichenfolge { get; }

    public int SchemaVersion => Schema.Version;

    // Ist beim Start eine Datenbank am alten Speicherort übernommen worden, steht hier ihr alter Pfad -
    // damit kann der Aufrufer das in der Statuszeile melden. Bleibt null, wenn nichts verschoben wurde.
    public string? AusAltemSpeicherortUebernommenVon { get; private set; }

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
        var ordner = Path.GetDirectoryName(Dateipfad);
        if (!string.IsNullOrEmpty(ordner))
        {
            Directory.CreateDirectory(ordner);
        }

        UebernehmeAusAltemSpeicherortFallsVorhanden();

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

    private void UebernehmeAusAltemSpeicherortFallsVorhanden()
    {
        var alterPfad = AlterStandardpfad();

        if (string.Equals(alterPfad, Dateipfad, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (File.Exists(Dateipfad) || !File.Exists(alterPfad))
        {
            return;
        }

        File.Move(alterPfad, Dateipfad);
        AusAltemSpeicherortUebernommenVon = alterPfad;
    }
}
