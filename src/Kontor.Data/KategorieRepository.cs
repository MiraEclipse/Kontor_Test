using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class KategorieRepository : IKategorieRepository
{
    private const string Spalten = "KategorieId, MandantNr, Bezeichnung, Richtung, Gesperrt";

    private const string Bereich = "K02";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public KategorieRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Kategorie> Liste(int mandantNr, bool auchGesperrte)
    {
        var kategorien = new List<Kategorie>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Kategorie
             WHERE MandantNr = @mandant AND (@auchGesperrte = 1 OR Gesperrt = 0)
             ORDER BY Bezeichnung;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@auchGesperrte", auchGesperrte ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            kategorien.Add(Lies(leser));
        }

        return kategorien;
    }

    public Kategorie? Lade(int mandantNr, int kategorieId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Kategorie WHERE MandantNr = @mandant AND KategorieId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kategorieId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public int Anlegen(Kategorie kategorie)
    {
        _zugriff.Pruefe(kategorie.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Kategorie (MandantNr, Bezeichnung, Richtung, Gesperrt)
              VALUES (@mandant, @bezeichnung, @richtung, 0);");
        Befehle.Setze(befehl, "@mandant", kategorie.MandantNr);
        Befehle.Setze(befehl, "@bezeichnung", kategorie.Bezeichnung);
        Befehle.Setze(befehl, "@richtung", Richtungen.Code(kategorie.Richtung));
        befehl.ExecuteNonQuery();

        kategorie.KategorieId = Befehle.LetzteId(verbindung, null);
        kategorie.Gesperrt = false;
        return kategorie.KategorieId;
    }

    public void Aendern(Kategorie kategorie)
    {
        _zugriff.Pruefe(kategorie.MandantNr, Bereich, Stufe.Aendern);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Kategorie SET Bezeichnung = @bezeichnung, Richtung = @richtung
              WHERE MandantNr = @mandant AND KategorieId = @id;");
        Befehle.Setze(befehl, "@bezeichnung", kategorie.Bezeichnung);
        Befehle.Setze(befehl, "@richtung", Richtungen.Code(kategorie.Richtung));
        Befehle.Setze(befehl, "@mandant", kategorie.MandantNr);
        Befehle.Setze(befehl, "@id", kategorie.KategorieId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Kategorie {kategorie.KategorieId} ist im Haushalt {kategorie.MandantNr} nicht vorhanden.");
        }
    }

    public void Sperren(int mandantNr, int kategorieId) => SetzeGesperrt(mandantNr, kategorieId, true);

    public void Entsperren(int mandantNr, int kategorieId) => SetzeGesperrt(mandantNr, kategorieId, false);

    private void SetzeGesperrt(int mandantNr, int kategorieId, bool gesperrt)
    {
        _zugriff.Pruefe(mandantNr, Bereich, Stufe.Voll);

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Kategorie SET Gesperrt = @gesperrt WHERE MandantNr = @mandant AND KategorieId = @id;");
        Befehle.Setze(befehl, "@gesperrt", gesperrt ? 1 : 0);
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kategorieId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Kategorie {kategorieId} ist im Haushalt {mandantNr} nicht vorhanden.");
        }
    }

    private static Kategorie Lies(SqliteDataReader leser) => new()
    {
        KategorieId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Bezeichnung = leser.GetString(2),
        Richtung = Richtungen.Aus(leser.GetString(3)),
        Gesperrt = leser.GetInt32(4) != 0
    };
}
