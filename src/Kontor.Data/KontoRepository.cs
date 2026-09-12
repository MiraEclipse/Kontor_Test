using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class KontoRepository : IKontoRepository
{
    private const string SaldenSql = @"
        SELECT k.KontoNr,
               k.Bezeichnung,
               k.Art,
               COALESCE(b.SollCent, 0)  AS SollCent,
               COALESCE(b.HabenCent, 0) AS HabenCent
        FROM Konto k
        LEFT JOIN (
            SELECT z.KontoNr,
                   SUM(CASE WHEN z.SollHaben = 'S' THEN z.BetragCent ELSE 0 END) AS SollCent,
                   SUM(CASE WHEN z.SollHaben = 'H' THEN z.BetragCent ELSE 0 END) AS HabenCent
            FROM Buchungszeile z
            JOIN Beleg g ON g.BelegId = z.BelegId AND g.MandantNr = z.MandantNr
            WHERE z.MandantNr = @mandant
              AND g.Belegdatum >= @von
              AND g.Belegdatum <= @bis
            GROUP BY z.KontoNr
        ) b ON b.KontoNr = k.KontoNr
        WHERE k.MandantNr = @mandant
        ORDER BY k.KontoNr;";

    private readonly Datenbank _datenbank;

    public KontoRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Konto> Liste(int mandantNr)
    {
        var konten = new List<Konto>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT MandantNr, KontoNr, Bezeichnung, Art FROM Konto WHERE MandantNr = @mandant ORDER BY KontoNr;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            konten.Add(new Konto
            {
                MandantNr = leser.GetInt32(0),
                KontoNr = leser.GetString(1),
                Bezeichnung = leser.GetString(2),
                Art = Kontoarten.Aus(leser.GetString(3))
            });
        }

        return konten;
    }

    public IReadOnlyList<KontoSaldo> Salden(int mandantNr, int jahr)
    {
        var salden = new List<KontoSaldo>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null, SaldenSql);
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@von", Feldwerte.Datum(new DateTime(jahr, 1, 1)));
        Befehle.Setze(befehl, "@bis", Feldwerte.Datum(new DateTime(jahr, 12, 31)));
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            salden.Add(new KontoSaldo
            {
                KontoNr = leser.GetString(0),
                Bezeichnung = leser.GetString(1),
                Art = Kontoarten.Aus(leser.GetString(2)),
                Soll = Feldwerte.AusCent(leser.GetInt64(3)),
                Haben = Feldwerte.AusCent(leser.GetInt64(4))
            });
        }

        return salden;
    }
}
