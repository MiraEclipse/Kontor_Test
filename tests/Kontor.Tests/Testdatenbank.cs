using Kontor.Core.Modell;
using Kontor.Data;
using Microsoft.Data.Sqlite;

namespace Kontor.Tests;

internal sealed class Testdatenbank : IDisposable
{
    private readonly string _ordner;

    public Testdatenbank()
    {
        _ordner = Path.Combine(Path.GetTempPath(), "kontor-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ordner);

        Datenbank = new Datenbank(Path.Combine(_ordner, Datenbank.StandardDateiname));
        Datenbank.Vorbereiten();
    }

    public Datenbank Datenbank { get; }

    public string Dateipfad => Datenbank.Dateipfad;

    public BelegRepository Belege => new(Datenbank);

    public KundeRepository Kunden => new(Datenbank);

    public ArtikelRepository Artikel => new(Datenbank);

    public KontoRepository Konten => new(Datenbank);

    public MandantRepository Mandanten => new(Datenbank);

    public NotizRepository Notizen => new(Datenbank);

    public KalkulationRepository Kalkulationen => new(Datenbank);

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

    public long AnzahlBelege() => Zahl("SELECT COUNT(*) FROM Beleg;");

    public long AnzahlPositionen() => Zahl("SELECT COUNT(*) FROM Belegposition;");

    public long AnzahlZeilen() => Zahl("SELECT COUNT(*) FROM Buchungszeile;");

    public long LetzteNummer(int mandantNr, int jahr, Belegart belegart)
    {
        using var verbindung = Datenbank.Oeffne();
        using var befehl = verbindung.CreateCommand();
        befehl.CommandText =
            @"SELECT COALESCE(MAX(LetzteNummer), 0) FROM Nummernkreis
              WHERE MandantNr = @mandant AND Jahr = @jahr AND Belegart = @art;";
        befehl.Parameters.AddWithValue("@mandant", mandantNr);
        befehl.Parameters.AddWithValue("@jahr", jahr);
        befehl.Parameters.AddWithValue("@art", Belegarten.Code(belegart));
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
