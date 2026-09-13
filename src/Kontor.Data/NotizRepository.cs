using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class NotizRepository : INotizRepository
{
    private const string Spalten =
        "NotizId, MandantNr, Betreff, Text, Prioritaet, Erledigt, Faelligkeit, ErledigtAm, Angelegt";

    private const string Bereich = "K05";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public NotizRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Notiz> Liste(int mandantNr, bool nurOffene)
    {
        var notizen = new List<Notiz>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Notiz
             WHERE MandantNr = @mandant
               AND (@nurOffene = 0 OR Erledigt = 0)
             ORDER BY Erledigt, Prioritaet DESC, Faelligkeit, Angelegt DESC;");
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
        _zugriff.Pruefe(notiz.MandantNr, Bereich, Stufe.Aendern);

        if (notiz.Angelegt == default)
        {
            notiz.Angelegt = Feldwerte.Jetzt();
        }

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Notiz (MandantNr, Betreff, Text, Prioritaet, Erledigt, Faelligkeit, ErledigtAm, Angelegt)
              VALUES (@mandant, @betreff, @text, @prioritaet, @erledigt, @faelligkeit, @erledigtAm, @angelegt);");
        Setze(befehl, notiz);
        befehl.ExecuteNonQuery();

        notiz.NotizId = Befehle.LetzteId(verbindung, null);
        return notiz.NotizId;
    }

    public void Aendern(Notiz notiz)
    {
        _zugriff.Pruefe(notiz.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Notiz
              SET Betreff = @betreff, Text = @text, Prioritaet = @prioritaet, Erledigt = @erledigt,
                  Faelligkeit = @faelligkeit, ErledigtAm = @erledigtAm, Angelegt = @angelegt
              WHERE MandantNr = @mandant AND NotizId = @id;");
        Setze(befehl, notiz);
        Befehle.Setze(befehl, "@id", notiz.NotizId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Notiz {notiz.NotizId} ist im Haushalt {notiz.MandantNr} nicht vorhanden.");
        }
    }

    private static void Setze(SqliteCommand befehl, Notiz notiz)
    {
        Befehle.Setze(befehl, "@mandant", notiz.MandantNr);
        Befehle.Setze(befehl, "@betreff", notiz.Betreff);
        Befehle.Setze(befehl, "@text", notiz.Text);
        Befehle.Setze(befehl, "@prioritaet", (int)notiz.Prioritaet);
        Befehle.Setze(befehl, "@erledigt", notiz.Erledigt ? 1 : 0);
        Befehle.Setze(befehl, "@faelligkeit", DatumOnly.Null(notiz.Faelligkeit));
        Befehle.Setze(befehl, "@erledigtAm", DatumOnly.Null(notiz.ErledigtAm));
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
        Faelligkeit = leser.IsDBNull(6) ? null : DatumOnly.Aus(leser.GetString(6)),
        ErledigtAm = leser.IsDBNull(7) ? null : DatumOnly.Aus(leser.GetString(7)),
        Angelegt = Feldwerte.AusZeit(leser.GetString(8))
    };
}
