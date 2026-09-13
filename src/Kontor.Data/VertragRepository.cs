using Kontor.Core.Fachlogik;
using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class VertragRepository : IVertragRepository
{
    private const string Spalten =
        @"VertragId, MandantNr, Bezeichnung, Anbieter, Turnus, Beginn, MindestlaufzeitMonate,
          KuendigungsfristMonate, KategorieId, KontoId, AutomatischBuchen, GekuendigtZum, Beendet";

    private const string Bereich = "K03";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public VertragRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Vertrag> Liste(int mandantNr, bool auchBeendete)
    {
        var vertraege = new List<Vertrag>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Vertrag
             WHERE MandantNr = @mandant AND (@auchBeendete = 1 OR Beendet = 0)
             ORDER BY Bezeichnung;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@auchBeendete", auchBeendete ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            vertraege.Add(Lies(leser));
        }

        return vertraege;
    }

    public Vertrag? Lade(int mandantNr, int vertragId)
    {
        using var verbindung = _datenbank.Oeffne();
        return Lade(verbindung, null, mandantNr, vertragId);
    }

    public int Anlegen(Vertrag vertrag)
    {
        _zugriff.Pruefe(vertrag.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Vertrag (MandantNr, Bezeichnung, Anbieter, Turnus, Beginn, MindestlaufzeitMonate,
                                   KuendigungsfristMonate, KategorieId, KontoId, AutomatischBuchen, GekuendigtZum, Beendet)
              VALUES (@mandant, @bezeichnung, @anbieter, @turnus, @beginn, @mindestlaufzeit,
                      @kuendigungsfrist, @kategorie, @konto, @automatisch, @gekuendigtZum, @beendet);");
        Setze(befehl, vertrag);
        befehl.ExecuteNonQuery();

        vertrag.VertragId = Befehle.LetzteId(verbindung, null);
        return vertrag.VertragId;
    }

    public void Aendern(Vertrag vertrag)
    {
        _zugriff.Pruefe(vertrag.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Vertrag SET
                Bezeichnung = @bezeichnung, Anbieter = @anbieter, Turnus = @turnus, Beginn = @beginn,
                MindestlaufzeitMonate = @mindestlaufzeit, KuendigungsfristMonate = @kuendigungsfrist,
                KategorieId = @kategorie, KontoId = @konto, AutomatischBuchen = @automatisch,
                GekuendigtZum = @gekuendigtZum, Beendet = @beendet
              WHERE MandantNr = @mandant AND VertragId = @id;");
        Setze(befehl, vertrag);
        Befehle.Setze(befehl, "@id", vertrag.VertragId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Vertrag {vertrag.VertragId} ist im Haushalt {vertrag.MandantNr} nicht vorhanden.");
        }
    }

    public int VertragslaufAusfuehren(int mandantNr, int vertragId, DateOnly stichtag)
    {
        _zugriff.Pruefe(mandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var transaktion = _datenbank.BeginneSofort(verbindung);

        var vertrag = Lade(verbindung, transaktion, mandantNr, vertragId)
            ?? throw new DatenbankFehler($"Vertrag {vertragId} ist im Haushalt {mandantNr} nicht vorhanden.");

        var preise = VertragspreisRepository.Liste(verbindung, transaktion, mandantNr, vertragId);
        var bereitsGebucht = BuchungRepository.DatenFuerVertrag(verbindung, transaktion, mandantNr, vertragId);

        var (positionen, fehler) = Vertragslauf.ZuBuchendeTermine(vertrag, preise, bereitsGebucht, stichtag);

        if (fehler.Count > 0)
        {
            throw new FachlicherFehler(string.Join(" ", fehler));
        }

        foreach (var position in positionen)
        {
            BuchungRepository.Anlegen(verbindung, transaktion, new Buchung
            {
                MandantNr = mandantNr,
                Datum = position.Datum,
                KontoId = vertrag.KontoId,
                KategorieId = vertrag.KategorieId,
                BetragCent = position.BetragCent,
                Text = vertrag.Bezeichnung,
                VertragId = vertrag.VertragId
            });
        }

        transaktion.Commit();
        return positionen.Count;
    }

    private static void Setze(SqliteCommand befehl, Vertrag vertrag)
    {
        Befehle.Setze(befehl, "@mandant", vertrag.MandantNr);
        Befehle.Setze(befehl, "@bezeichnung", vertrag.Bezeichnung);
        Befehle.Setze(befehl, "@anbieter", vertrag.Anbieter);
        Befehle.Setze(befehl, "@turnus", Turnusse.Code(vertrag.Turnus));
        Befehle.Setze(befehl, "@beginn", DatumOnly.Text(vertrag.Beginn));
        Befehle.Setze(befehl, "@mindestlaufzeit", vertrag.MindestlaufzeitMonate);
        Befehle.Setze(befehl, "@kuendigungsfrist", vertrag.KuendigungsfristMonate);
        Befehle.Setze(befehl, "@kategorie", vertrag.KategorieId);
        Befehle.Setze(befehl, "@konto", vertrag.KontoId);
        Befehle.Setze(befehl, "@automatisch", vertrag.AutomatischBuchen ? 1 : 0);
        Befehle.Setze(befehl, "@gekuendigtZum", DatumOnly.Null(vertrag.GekuendigtZum));
        Befehle.Setze(befehl, "@beendet", vertrag.Beendet ? 1 : 0);
    }

    private static Vertrag? Lade(SqliteConnection verbindung, SqliteTransaction? transaktion, int mandantNr, int vertragId)
    {
        using var befehl = Befehle.Neu(verbindung, transaktion,
            "SELECT " + Spalten + " FROM Vertrag WHERE MandantNr = @mandant AND VertragId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", vertragId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    private static Vertrag Lies(SqliteDataReader leser) => new()
    {
        VertragId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Bezeichnung = leser.GetString(2),
        Anbieter = leser.GetString(3),
        Turnus = Turnusse.Aus(leser.GetString(4)),
        Beginn = DatumOnly.Aus(leser.GetString(5)),
        MindestlaufzeitMonate = leser.GetInt32(6),
        KuendigungsfristMonate = leser.GetInt32(7),
        KategorieId = leser.GetInt32(8),
        KontoId = leser.GetInt32(9),
        AutomatischBuchen = leser.GetInt32(10) != 0,
        GekuendigtZum = leser.IsDBNull(11) ? null : DatumOnly.Aus(leser.GetString(11)),
        Beendet = leser.GetInt32(12) != 0
    };
}
