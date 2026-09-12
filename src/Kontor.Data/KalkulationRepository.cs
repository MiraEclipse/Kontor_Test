using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class KalkulationRepository : IKalkulationRepository
{
    private const string Spalten = @"
        KalkulationId, MandantNr, ArtikelId, Bezeichnung,
        MaterialeinzelkostenCent, MaterialgemeinkostenSatzBp,
        FertigungsloehneCent, FertigungsgemeinkostenSatzBp,
        VerwaltungsgemeinkostenSatzBp, VertriebsgemeinkostenSatzBp,
        GewinnzuschlagSatzBp, SkontoSatzBp, RabattSatzBp, UmsatzsteuerSatzBp, Erfasst";

    private readonly Datenbank _datenbank;

    public KalkulationRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Kalkulation> Liste(int mandantNr)
    {
        var kalkulationen = new List<Kalkulation>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Kalkulation WHERE MandantNr = @mandant ORDER BY Erfasst DESC, KalkulationId DESC;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            kalkulationen.Add(Lies(leser));
        }

        return kalkulationen;
    }

    public Kalkulation? Lade(int mandantNr, int kalkulationId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Kalkulation WHERE MandantNr = @mandant AND KalkulationId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kalkulationId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public int Sichern(Kalkulation kalkulation)
    {
        if (kalkulation.Erfasst == default)
        {
            kalkulation.Erfasst = Feldwerte.Jetzt();
        }

        using var verbindung = _datenbank.Oeffne();

        if (kalkulation.KalkulationId == 0)
        {
            using var einfuegen = Befehle.Neu(verbindung, null,
                @"INSERT INTO Kalkulation (
                      MandantNr, ArtikelId, Bezeichnung,
                      MaterialeinzelkostenCent, MaterialgemeinkostenSatzBp,
                      FertigungsloehneCent, FertigungsgemeinkostenSatzBp,
                      VerwaltungsgemeinkostenSatzBp, VertriebsgemeinkostenSatzBp,
                      GewinnzuschlagSatzBp, SkontoSatzBp, RabattSatzBp, UmsatzsteuerSatzBp, Erfasst)
                  VALUES (
                      @mandant, @artikel, @bezeichnung,
                      @mek, @mgk,
                      @fl, @fgk,
                      @vwgk, @vtgk,
                      @gewinn, @skonto, @rabatt, @ust, @erfasst);");
            Setze(einfuegen, kalkulation);
            einfuegen.ExecuteNonQuery();

            kalkulation.KalkulationId = Befehle.LetzteId(verbindung, null);
            return kalkulation.KalkulationId;
        }

        using var aendern = Befehle.Neu(verbindung, null,
            @"UPDATE Kalkulation
              SET ArtikelId = @artikel,
                  Bezeichnung = @bezeichnung,
                  MaterialeinzelkostenCent = @mek,
                  MaterialgemeinkostenSatzBp = @mgk,
                  FertigungsloehneCent = @fl,
                  FertigungsgemeinkostenSatzBp = @fgk,
                  VerwaltungsgemeinkostenSatzBp = @vwgk,
                  VertriebsgemeinkostenSatzBp = @vtgk,
                  GewinnzuschlagSatzBp = @gewinn,
                  SkontoSatzBp = @skonto,
                  RabattSatzBp = @rabatt,
                  UmsatzsteuerSatzBp = @ust,
                  Erfasst = @erfasst
              WHERE MandantNr = @mandant AND KalkulationId = @id;");
        Setze(aendern, kalkulation);
        Befehle.Setze(aendern, "@id", kalkulation.KalkulationId);

        if (aendern.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler(
                $"Kalkulation {kalkulation.KalkulationId} ist im Mandanten {kalkulation.MandantNr} nicht vorhanden.");
        }

        return kalkulation.KalkulationId;
    }

    private static void Setze(SqliteCommand befehl, Kalkulation k)
    {
        Befehle.Setze(befehl, "@mandant", k.MandantNr);
        Befehle.Setze(befehl, "@artikel", Feldwerte.Null(k.ArtikelId));
        Befehle.Setze(befehl, "@bezeichnung", k.Bezeichnung);
        Befehle.Setze(befehl, "@mek", Feldwerte.Cent(k.Materialeinzelkosten));
        Befehle.Setze(befehl, "@mgk", Feldwerte.Bp(k.MaterialgemeinkostenSatz));
        Befehle.Setze(befehl, "@fl", Feldwerte.Cent(k.Fertigungsloehne));
        Befehle.Setze(befehl, "@fgk", Feldwerte.Bp(k.FertigungsgemeinkostenSatz));
        Befehle.Setze(befehl, "@vwgk", Feldwerte.Bp(k.VerwaltungsgemeinkostenSatz));
        Befehle.Setze(befehl, "@vtgk", Feldwerte.Bp(k.VertriebsgemeinkostenSatz));
        Befehle.Setze(befehl, "@gewinn", Feldwerte.Bp(k.GewinnzuschlagSatz));
        Befehle.Setze(befehl, "@skonto", Feldwerte.Bp(k.SkontoSatz));
        Befehle.Setze(befehl, "@rabatt", Feldwerte.Bp(k.RabattSatz));
        Befehle.Setze(befehl, "@ust", Feldwerte.Bp(k.UmsatzsteuerSatz));
        Befehle.Setze(befehl, "@erfasst", Feldwerte.Zeit(k.Erfasst));
    }

    private static Kalkulation Lies(SqliteDataReader leser) => new()
    {
        KalkulationId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        ArtikelId = leser.IsDBNull(2) ? null : leser.GetInt32(2),
        Bezeichnung = leser.GetString(3),
        Materialeinzelkosten = Feldwerte.AusCent(leser.GetInt64(4)),
        MaterialgemeinkostenSatz = Feldwerte.AusBp(leser.GetInt64(5)),
        Fertigungsloehne = Feldwerte.AusCent(leser.GetInt64(6)),
        FertigungsgemeinkostenSatz = Feldwerte.AusBp(leser.GetInt64(7)),
        VerwaltungsgemeinkostenSatz = Feldwerte.AusBp(leser.GetInt64(8)),
        VertriebsgemeinkostenSatz = Feldwerte.AusBp(leser.GetInt64(9)),
        GewinnzuschlagSatz = Feldwerte.AusBp(leser.GetInt64(10)),
        SkontoSatz = Feldwerte.AusBp(leser.GetInt64(11)),
        RabattSatz = Feldwerte.AusBp(leser.GetInt64(12)),
        UmsatzsteuerSatz = Feldwerte.AusBp(leser.GetInt64(13)),
        Erfasst = Feldwerte.AusZeit(leser.GetString(14))
    };
}
