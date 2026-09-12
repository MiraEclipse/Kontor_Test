using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class NotizRepository : INotizRepository
{
    private const string Spalten =
        "NotizId, MandantNr, Betreff, Text, Prioritaet, Erledigt, BelegId, KundeId, Angelegt";

    private readonly Datenbank _datenbank;

    public NotizRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Notiz> Liste(int mandantNr, bool nurOffene)
    {
        var notizen = new List<Notiz>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Notiz
             WHERE MandantNr = @mandant
               AND (@nurOffene = 0 OR Erledigt = 0)
             ORDER BY Erledigt, Prioritaet DESC, Angelegt DESC;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@nurOffene", nurOffene ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            notizen.Add(Lies(leser));
        }

        return notizen;
    }

    public Notiz? Lade(int mandantNr, int notizId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Notiz WHERE MandantNr = @mandant AND NotizId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", notizId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public int Anlegen(Notiz notiz)
    {
        if (notiz.Angelegt == default)
        {
            notiz.Angelegt = Feldwerte.Jetzt();
        }

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Notiz (MandantNr, Betreff, Text, Prioritaet, Erledigt, BelegId, KundeId, Angelegt)
              VALUES (@mandant, @betreff, @text, @prioritaet, @erledigt, @beleg, @kunde, @angelegt);");
        Setze(befehl, notiz);
        befehl.ExecuteNonQuery();

        notiz.NotizId = Befehle.LetzteId(verbindung, null);
        return notiz.NotizId;
    }

    public void Aendern(Notiz notiz)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Notiz
              SET Betreff = @betreff, Text = @text, Prioritaet = @prioritaet, Erledigt = @erledigt,
                  BelegId = @beleg, KundeId = @kunde, Angelegt = @angelegt
              WHERE MandantNr = @mandant AND NotizId = @id;");
        Setze(befehl, notiz);
        Befehle.Setze(befehl, "@id", notiz.NotizId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Notiz {notiz.NotizId} ist im Mandanten {notiz.MandantNr} nicht vorhanden.");
        }
    }

    private static void Setze(SqliteCommand befehl, Notiz notiz)
    {
        Befehle.Setze(befehl, "@mandant", notiz.MandantNr);
        Befehle.Setze(befehl, "@betreff", notiz.Betreff);
        Befehle.Setze(befehl, "@text", notiz.Text);
        Befehle.Setze(befehl, "@prioritaet", (int)notiz.Prioritaet);
        Befehle.Setze(befehl, "@erledigt", notiz.Erledigt ? 1 : 0);
        Befehle.Setze(befehl, "@beleg", Feldwerte.Null(notiz.BelegId));
        Befehle.Setze(befehl, "@kunde", Feldwerte.Null(notiz.KundeId));
        Befehle.Setze(befehl, "@angelegt", Feldwerte.Zeit(notiz.Angelegt));
    }

    private static Notiz Lies(SqliteDataReader leser) => new()
    {
        NotizId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Betreff = leser.GetString(2),
        Text = leser.GetString(3),
        Prioritaet = (Prioritaet)leser.GetInt32(4),
        Erledigt = leser.GetInt32(5) != 0,
        BelegId = leser.IsDBNull(6) ? null : leser.GetInt32(6),
        KundeId = leser.IsDBNull(7) ? null : leser.GetInt32(7),
        Angelegt = Feldwerte.AusZeit(leser.GetString(8))
    };
}
