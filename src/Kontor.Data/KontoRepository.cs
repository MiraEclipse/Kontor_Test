using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;
using KontostandRechner = Kontor.Core.Fachlogik.Kontostand;

namespace Kontor.Data;

public sealed class KontoRepository : IKontoRepository
{
    private const string Spalten =
        "KontoId, MandantNr, Bezeichnung, Art, AnfangsbestandCent, Gesperrt";

    private const string EinnahmenAusgabenSql = @"
        SELECT COALESCE(SUM(CASE WHEN k.Richtung = 'E' THEN b.BetragCent ELSE 0 END), 0),
               COALESCE(SUM(CASE WHEN k.Richtung = 'A' THEN b.BetragCent ELSE 0 END), 0)
        FROM Buchung b
        JOIN Kategorie k ON k.KategorieId = b.KategorieId AND k.MandantNr = b.MandantNr
        WHERE b.MandantNr = @mandant AND b.KontoId = @konto AND b.Datum <= @stichtag;";

    private const string UmbuchungEinSql = @"
        SELECT COALESCE(SUM(BetragCent), 0) FROM Umbuchung
        WHERE MandantNr = @mandant AND NachKontoId = @konto AND Datum <= @stichtag;";

    private const string UmbuchungAusSql = @"
        SELECT COALESCE(SUM(BetragCent), 0) FROM Umbuchung
        WHERE MandantNr = @mandant AND VonKontoId = @konto AND Datum <= @stichtag;";

    private const string Bereich = "K02";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public KontoRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Konto> Liste(int mandantNr, bool auchGesperrte)
    {
        var konten = new List<Konto>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Konto
             WHERE MandantNr = @mandant AND (@auchGesperrte = 1 OR Gesperrt = 0)
             ORDER BY Bezeichnung;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@auchGesperrte", auchGesperrte ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            konten.Add(Lies(leser));
        }

        return konten;
    }

    public Konto? Lade(int mandantNr, int kontoId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Konto WHERE MandantNr = @mandant AND KontoId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kontoId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public int Anlegen(Konto konto)
    {
        _zugriff.Pruefe(konto.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Konto (MandantNr, Bezeichnung, Art, AnfangsbestandCent, Gesperrt)
              VALUES (@mandant, @bezeichnung, @art, @anfangsbestand, 0);");
        Befehle.Setze(befehl, "@mandant", konto.MandantNr);
        Befehle.Setze(befehl, "@bezeichnung", konto.Bezeichnung);
        Befehle.Setze(befehl, "@art", Kontoarten.Code(konto.Art));
        Befehle.Setze(befehl, "@anfangsbestand", konto.AnfangsbestandCent);
        befehl.ExecuteNonQuery();

        konto.KontoId = Befehle.LetzteId(verbindung, null);
        konto.Gesperrt = false;
        return konto.KontoId;
    }

    public void Aendern(Konto konto)
    {
        _zugriff.Pruefe(konto.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Konto SET Bezeichnung = @bezeichnung, Art = @art, AnfangsbestandCent = @anfangsbestand
              WHERE MandantNr = @mandant AND KontoId = @id;");
        Befehle.Setze(befehl, "@bezeichnung", konto.Bezeichnung);
        Befehle.Setze(befehl, "@art", Kontoarten.Code(konto.Art));
        Befehle.Setze(befehl, "@anfangsbestand", konto.AnfangsbestandCent);
        Befehle.Setze(befehl, "@mandant", konto.MandantNr);
        Befehle.Setze(befehl, "@id", konto.KontoId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Konto {konto.KontoId} ist im Haushalt {konto.MandantNr} nicht vorhanden.");
        }
    }

    public void Sperren(int mandantNr, int kontoId) => SetzeGesperrt(mandantNr, kontoId, true);

    public void Entsperren(int mandantNr, int kontoId) => SetzeGesperrt(mandantNr, kontoId, false);

    public long Kontostand(int mandantNr, int kontoId, DateOnly stichtag)
    {
        var konto = Lade(mandantNr, kontoId)
            ?? throw new DatenbankFehler($"Konto {kontoId} ist im Haushalt {mandantNr} nicht vorhanden.");

        using var verbindung = _datenbank.Oeffne();
        var stichtagText = DatumOnly.Text(stichtag);

        long einnahmenCent;
        long ausgabenCent;
        using (var befehl = Befehle.Neu(verbindung, null, EinnahmenAusgabenSql))
        {
            Befehle.Setze(befehl, "@mandant", mandantNr);
            Befehle.Setze(befehl, "@konto", kontoId);
            Befehle.Setze(befehl, "@stichtag", stichtagText);
            using var leser = befehl.ExecuteReader();
            leser.Read();
            einnahmenCent = leser.GetInt64(0);
            ausgabenCent = leser.GetInt64(1);
        }

        long umbuchungEinCent;
        using (var befehl = Befehle.Neu(verbindung, null, UmbuchungEinSql))
        {
            Befehle.Setze(befehl, "@mandant", mandantNr);
            Befehle.Setze(befehl, "@konto", kontoId);
            Befehle.Setze(befehl, "@stichtag", stichtagText);
            umbuchungEinCent = Convert.ToInt64(befehl.ExecuteScalar());
        }

        long umbuchungAusCent;
        using (var befehl = Befehle.Neu(verbindung, null, UmbuchungAusSql))
        {
            Befehle.Setze(befehl, "@mandant", mandantNr);
            Befehle.Setze(befehl, "@konto", kontoId);
            Befehle.Setze(befehl, "@stichtag", stichtagText);
            umbuchungAusCent = Convert.ToInt64(befehl.ExecuteScalar());
        }

        return KontostandRechner.Berechne(
            konto.AnfangsbestandCent, einnahmenCent, ausgabenCent, umbuchungEinCent, umbuchungAusCent);
    }

    private void SetzeGesperrt(int mandantNr, int kontoId, bool gesperrt)
    {
        _zugriff.Pruefe(mandantNr, Bereich, Stufe.Voll);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Konto SET Gesperrt = @gesperrt WHERE MandantNr = @mandant AND KontoId = @id;");
        Befehle.Setze(befehl, "@gesperrt", gesperrt ? 1 : 0);
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kontoId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Konto {kontoId} ist im Haushalt {mandantNr} nicht vorhanden.");
        }
    }

    private static Konto Lies(SqliteDataReader leser) => new()
    {
        KontoId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Bezeichnung = leser.GetString(2),
        Art = Kontoarten.Aus(leser.GetString(3)),
        AnfangsbestandCent = leser.GetInt64(4),
        Gesperrt = leser.GetInt32(5) != 0
    };
}
