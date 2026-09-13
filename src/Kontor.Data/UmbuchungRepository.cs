using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class UmbuchungRepository : IUmbuchungRepository
{
    private const string Spalten =
        "UmbuchungId, MandantNr, Datum, VonKontoId, NachKontoId, BetragCent, Text";

    private const string Bereich = "K01";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public UmbuchungRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Umbuchung> Liste(int mandantNr, DateOnly von, DateOnly bis)
    {
        var umbuchungen = new List<Umbuchung>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Umbuchung
             WHERE MandantNr = @mandant AND Datum >= @von AND Datum <= @bis
             ORDER BY Datum, UmbuchungId;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@von", DatumOnly.Text(von));
        Befehle.Setze(befehl, "@bis", DatumOnly.Text(bis));
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            umbuchungen.Add(Lies(leser));
        }

        return umbuchungen;
    }

    public int Anlegen(Umbuchung umbuchung)
    {
        _zugriff.Pruefe(umbuchung.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Umbuchung (MandantNr, Datum, VonKontoId, NachKontoId, BetragCent, Text)
              VALUES (@mandant, @datum, @von, @nach, @betrag, @text);");
        Befehle.Setze(befehl, "@mandant", umbuchung.MandantNr);
        Befehle.Setze(befehl, "@datum", DatumOnly.Text(umbuchung.Datum));
        Befehle.Setze(befehl, "@von", umbuchung.VonKontoId);
        Befehle.Setze(befehl, "@nach", umbuchung.NachKontoId);
        Befehle.Setze(befehl, "@betrag", umbuchung.BetragCent);
        Befehle.Setze(befehl, "@text", umbuchung.Text);
        befehl.ExecuteNonQuery();

        umbuchung.UmbuchungId = Befehle.LetzteId(verbindung, null);
        return umbuchung.UmbuchungId;
    }

    public void Loeschen(int mandantNr, int umbuchungId)
    {
        _zugriff.Pruefe(mandantNr, Bereich, Stufe.Voll);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "DELETE FROM Umbuchung WHERE MandantNr = @mandant AND UmbuchungId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", umbuchungId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Umbuchung {umbuchungId} ist im Haushalt {mandantNr} nicht vorhanden.");
        }
    }

    private static Umbuchung Lies(SqliteDataReader leser) => new()
    {
        UmbuchungId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Datum = DatumOnly.Aus(leser.GetString(2)),
        VonKontoId = leser.GetInt32(3),
        NachKontoId = leser.GetInt32(4),
        BetragCent = leser.GetInt64(5),
        Text = leser.GetString(6)
    };
}
