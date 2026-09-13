using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class VertragspreisRepository : IVertragspreisRepository
{
    private const string Spalten = "VertragspreisId, MandantNr, VertragId, GueltigAb, BetragCent";

    private const string Bereich = "K03";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public VertragspreisRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Vertragspreis> Liste(int mandantNr, int vertragId)
    {
        using var verbindung = _datenbank.Oeffne();
        return Liste(verbindung, null, mandantNr, vertragId);
    }

    public int Anlegen(Vertragspreis preis)
    {
        _zugriff.Pruefe(preis.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        var id = Anlegen(verbindung, null, preis);
        preis.VertragspreisId = id;
        return id;
    }

    internal static IReadOnlyList<Vertragspreis> Liste(
        SqliteConnection verbindung, SqliteTransaction? transaktion, int mandantNr, int vertragId)
    {
        var preise = new List<Vertragspreis>();

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT " + Spalten + @" FROM Vertragspreis
             WHERE MandantNr = @mandant AND VertragId = @vertrag ORDER BY GueltigAb;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@vertrag", vertragId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            preise.Add(Lies(leser));
        }

        return preise;
    }

    internal static int Anlegen(SqliteConnection verbindung, SqliteTransaction? transaktion, Vertragspreis preis)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Vertragspreis (MandantNr, VertragId, GueltigAb, BetragCent)
              VALUES (@mandant, @vertrag, @gueltigAb, @betrag);");
        Befehle.Setze(befehl, "@mandant", preis.MandantNr);
        Befehle.Setze(befehl, "@vertrag", preis.VertragId);
        Befehle.Setze(befehl, "@gueltigAb", DatumOnly.Text(preis.GueltigAb));
        Befehle.Setze(befehl, "@betrag", preis.BetragCent);
        befehl.ExecuteNonQuery();

        return Befehle.LetzteId(verbindung, transaktion);
    }

    private static Vertragspreis Lies(SqliteDataReader leser) => new()
    {
        VertragspreisId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        VertragId = leser.GetInt32(2),
        GueltigAb = DatumOnly.Aus(leser.GetString(3)),
        BetragCent = leser.GetInt64(4)
    };
}
