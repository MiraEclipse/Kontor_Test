using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class MandantRepository : IMandantRepository
{
    private readonly Datenbank _datenbank;

    public MandantRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Mandant> Alle()
    {
        var mandanten = new List<Mandant>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT MandantNr, Name, Ort, Waehrung FROM Mandant ORDER BY MandantNr;");
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            mandanten.Add(Lies(leser));
        }

        return mandanten;
    }

    public Mandant? Lade(int mandantNr)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT MandantNr, Name, Ort, Waehrung FROM Mandant WHERE MandantNr = @mandant;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    private static Mandant Lies(SqliteDataReader leser) => new()
    {
        MandantNr = leser.GetInt32(0),
        Name = leser.GetString(1),
        Ort = leser.GetString(2),
        Waehrung = leser.GetString(3)
    };
}
