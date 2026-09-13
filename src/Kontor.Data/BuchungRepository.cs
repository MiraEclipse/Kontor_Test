using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class BuchungRepository : IBuchungRepository
{
    private const string Spalten =
        "BuchungId, MandantNr, Datum, KontoId, KategorieId, BetragCent, Text, VertragId";

    private const string Bereich = "K01";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public BuchungRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Buchung> Liste(int mandantNr, DateOnly von, DateOnly bis)
    {
        var buchungen = new List<Buchung>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Buchung
             WHERE MandantNr = @mandant AND Datum >= @von AND Datum <= @bis
             ORDER BY Datum, BuchungId;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@von", DatumOnly.Text(von));
        Befehle.Setze(befehl, "@bis", DatumOnly.Text(bis));
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            buchungen.Add(Lies(leser));
        }

        return buchungen;
    }

    public IReadOnlyList<Buchung> ListeFuerVertrag(int mandantNr, int vertragId)
    {
        var buchungen = new List<Buchung>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Buchung
             WHERE MandantNr = @mandant AND VertragId = @vertrag
             ORDER BY Datum, BuchungId;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@vertrag", vertragId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            buchungen.Add(Lies(leser));
        }

        return buchungen;
    }

    public int Anlegen(Buchung buchung)
    {
        _zugriff.Pruefe(buchung.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        var id = Anlegen(verbindung, null, buchung);
        buchung.BuchungId = id;
        return id;
    }

    public void Loeschen(int mandantNr, int buchungId)
    {
        _zugriff.Pruefe(mandantNr, Bereich, Stufe.Voll);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "DELETE FROM Buchung WHERE MandantNr = @mandant AND BuchungId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", buchungId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Buchung {buchungId} ist im Haushalt {mandantNr} nicht vorhanden.");
        }
    }

    public bool VorhandenFuerVertragUndDatum(int mandantNr, int vertragId, DateOnly datum)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT COUNT(*) FROM Buchung WHERE MandantNr = @mandant AND VertragId = @vertrag AND Datum = @datum;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@vertrag", vertragId);
        Befehle.Setze(befehl, "@datum", DatumOnly.Text(datum));
        return Convert.ToInt32(befehl.ExecuteScalar()) > 0;
    }

    internal static int Anlegen(SqliteConnection verbindung, SqliteTransaction? transaktion, Buchung buchung)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            @"INSERT INTO Buchung (MandantNr, Datum, KontoId, KategorieId, BetragCent, Text, VertragId)
              VALUES (@mandant, @datum, @konto, @kategorie, @betrag, @text, @vertrag);");
        Befehle.Setze(befehl, "@mandant", buchung.MandantNr);
        Befehle.Setze(befehl, "@datum", DatumOnly.Text(buchung.Datum));
        Befehle.Setze(befehl, "@konto", buchung.KontoId);
        Befehle.Setze(befehl, "@kategorie", buchung.KategorieId);
        Befehle.Setze(befehl, "@betrag", buchung.BetragCent);
        Befehle.Setze(befehl, "@text", buchung.Text);
        Befehle.Setze(befehl, "@vertrag", Feldwerte.Null(buchung.VertragId));
        befehl.ExecuteNonQuery();

        return Befehle.LetzteId(verbindung, transaktion);
    }

    internal static IReadOnlyList<DateOnly> DatenFuerVertrag(SqliteConnection verbindung, SqliteTransaction? transaktion, int mandantNr, int vertragId)
    {
        var daten = new List<DateOnly>();

        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT Datum FROM Buchung WHERE MandantNr = @mandant AND VertragId = @vertrag;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@vertrag", vertragId);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            daten.Add(DatumOnly.Aus(leser.GetString(0)));
        }

        return daten;
    }

    private static Buchung Lies(SqliteDataReader leser) => new()
    {
        BuchungId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Datum = DatumOnly.Aus(leser.GetString(2)),
        KontoId = leser.GetInt32(3),
        KategorieId = leser.GetInt32(4),
        BetragCent = leser.GetInt64(5),
        Text = leser.GetString(6),
        VertragId = leser.IsDBNull(7) ? null : leser.GetInt32(7)
    };
}
