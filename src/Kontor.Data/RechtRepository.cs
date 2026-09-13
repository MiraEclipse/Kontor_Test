using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class RechtRepository : IRechtRepository
{
    private const string Spalten =
        "RechtId, BenutzerId, MandantNr, Bereich, Stufe, GueltigVon, GueltigBis, ErteiltVon, ErteiltAm";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public RechtRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Recht> Liste(int benutzerId)
    {
        using var verbindung = _datenbank.Oeffne();
        return Liste(verbindung, null, benutzerId);
    }

    public int Erteilen(Recht recht)
    {
        _zugriff.PruefeSystemrechte();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Recht (BenutzerId, MandantNr, Bereich, Stufe, GueltigVon, GueltigBis, ErteiltVon, ErteiltAm)
              VALUES (@benutzer, @mandant, @bereich, @stufe, @von, @bis, @erteiltVon, @erteiltAm);");
        Befehle.Setze(befehl, "@benutzer", recht.BenutzerId);
        Befehle.Setze(befehl, "@mandant", recht.MandantNr);
        Befehle.Setze(befehl, "@bereich", recht.Bereich);
        Befehle.Setze(befehl, "@stufe", Stufen.Code(recht.Stufe));
        Befehle.Setze(befehl, "@von", Feldwerte.Zeit(recht.GueltigVon));
        Befehle.Setze(befehl, "@bis", recht.GueltigBis.HasValue ? Feldwerte.Zeit(recht.GueltigBis.Value) : DBNull.Value);
        Befehle.Setze(befehl, "@erteiltVon", recht.ErteiltVon);
        Befehle.Setze(befehl, "@erteiltAm", Feldwerte.Zeit(recht.ErteiltAm));
        befehl.ExecuteNonQuery();

        recht.RechtId = Befehle.LetzteId(verbindung, null);
        return recht.RechtId;
    }

    public void Entziehen(int rechtId, DateTime zeitpunkt)
    {
        _zugriff.PruefeSystemrechte();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Recht SET GueltigBis = @bis WHERE RechtId = @id;");
        Befehle.Setze(befehl, "@bis", Feldwerte.Zeit(zeitpunkt));
        Befehle.Setze(befehl, "@id", rechtId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Recht {rechtId} ist nicht vorhanden.");
        }
    }

    internal static IReadOnlyList<Recht> Liste(SqliteConnection verbindung, SqliteTransaction? transaktion, int benutzerId)
    {
        var rechte = new List<Recht>();

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT " + Spalten + " FROM Recht WHERE BenutzerId = @benutzer ORDER BY GueltigVon;");
        Befehle.Setze(befehl, "@benutzer", benutzerId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            rechte.Add(Lies(leser));
        }

        return rechte;
    }

    private static Recht Lies(SqliteDataReader leser) => new()
    {
        RechtId = leser.GetInt32(0),
        BenutzerId = leser.GetInt32(1),
        MandantNr = leser.GetInt32(2),
        Bereich = leser.GetString(3),
        Stufe = Stufen.Aus(leser.GetString(4)),
        GueltigVon = Feldwerte.AusZeit(leser.GetString(5)),
        GueltigBis = leser.IsDBNull(6) ? null : Feldwerte.AusZeit(leser.GetString(6)),
        ErteiltVon = leser.GetInt32(7),
        ErteiltAm = Feldwerte.AusZeit(leser.GetString(8))
    };
}
