using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;

namespace Kontor.Tests;

// Läuft standardmäßig mit dem Systemkontext (Systemrechte übersteuern jede Prüfung), damit bestehende
// Tests nicht erst Benutzer und Rechte aufbauen müssen. Für Tests, die die Rechteprüfung selbst prüfen
// wollen, liefert Zugriff(benutzer) einen Kontext mit einem konkreten, nicht privilegierten Benutzer.
internal sealed class Testdatenbank : IDisposable
{
    private readonly string _ordner;
    private readonly Zugriffskontext _systemZugriff;

    public Testdatenbank()
    {
        _ordner = Path.Combine(Path.GetTempPath(), "kontor-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ordner);

        Datenbank = new Datenbank(Path.Combine(_ordner, Datenbank.StandardDateiname));
        Datenbank.Vorbereiten();

        _systemZugriff = Zugriffskontext.Systemkontext(Datenbank);
    }

    public Datenbank Datenbank { get; }

    public string Dateipfad => Datenbank.Dateipfad;

    public KontoRepository Konten => new(Datenbank, _systemZugriff);

    public KategorieRepository Kategorien => new(Datenbank, _systemZugriff);

    public BuchungRepository Buchungen => new(Datenbank, _systemZugriff);

    public UmbuchungRepository Umbuchungen => new(Datenbank, _systemZugriff);

    public VertragRepository Vertraege => new(Datenbank, _systemZugriff);

    public VertragspreisRepository Vertragspreise => new(Datenbank, _systemZugriff);

    public MandantRepository Mandanten => new(Datenbank);

    public NotizRepository Notizen => new(Datenbank, _systemZugriff);

    public BenutzerRepository Benutzer => new(Datenbank, _systemZugriff);

    public RechtRepository Rechte => new(Datenbank, _systemZugriff);

    public AnmeldeprotokollRepository Anmeldeprotokoll => new(Datenbank);

    public Zugriffskontext Zugriff(Benutzer benutzer) => new(Datenbank, benutzer);

    public void MandantAnlegen(int mandantNr, string name)
    {
        using var verbindung = Datenbank.Oeffne();
        using var transaktion = Datenbank.BeginneSofort(verbindung);
        Startbefuellung.MandantAnlegen(verbindung, transaktion, new Mandant
        {
            MandantNr = mandantNr,
            Name = name,
            Ort = "Bremen",
            Waehrung = "EUR"
        });
        transaktion.Commit();
    }

    public long Zahl(string sql)
    {
        using var verbindung = Datenbank.Oeffne();
        using var befehl = verbindung.CreateCommand();
        befehl.CommandText = sql;
        return Convert.ToInt64(befehl.ExecuteScalar());
    }

    public string SpaltenTyp(string sql)
    {
        using var verbindung = Datenbank.Oeffne();
        using var befehl = verbindung.CreateCommand();
        befehl.CommandText = sql;
        return Convert.ToString(befehl.ExecuteScalar()) ?? "";
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_ordner, true);
        }
        catch (IOException)
        {
        }
    }
}
