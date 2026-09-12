using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class ArtikelRepository : IArtikelRepository
{
    private const string Spalten =
        "ArtikelId, MandantNr, Nummer, Bezeichnung, Einheit, PreisCent, SteuerSatzBp, Gesperrt";

    private readonly Datenbank _datenbank;

    public ArtikelRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Artikel> Suche(int mandantNr, string suchbegriff, bool auchGesperrte)
    {
        var artikel = new List<Artikel>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Artikel
             WHERE MandantNr = @mandant
               AND (@leer = 1 OR Nummer LIKE @muster OR Bezeichnung LIKE @muster)
               AND (@auchGesperrte = 1 OR Gesperrt = 0)
             ORDER BY Nummer;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@leer", string.IsNullOrWhiteSpace(suchbegriff) ? 1 : 0);
        Befehle.Setze(befehl, "@muster", Befehle.Suchmuster(suchbegriff));
        Befehle.Setze(befehl, "@auchGesperrte", auchGesperrte ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            artikel.Add(Lies(leser));
        }

        return artikel;
    }

    public Artikel? Lade(int mandantNr, int artikelId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Artikel WHERE MandantNr = @mandant AND ArtikelId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", artikelId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public Artikel? LadeMitNummer(int mandantNr, string nummer)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Artikel WHERE MandantNr = @mandant AND Nummer = @nummer;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@nummer", nummer);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public string NaechsteFreieNummer(int mandantNr)
    {
        using var verbindung = _datenbank.Oeffne();
        return Nummernvorschlag.Naechste(verbindung, "Artikel", mandantNr);
    }

    public int Anlegen(Artikel artikel)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Artikel (MandantNr, Nummer, Bezeichnung, Einheit, PreisCent, SteuerSatzBp, Gesperrt)
              VALUES (@mandant, @nummer, @bezeichnung, @einheit, @preis, @steuer, @gesperrt);");
        SetzeStammdaten(befehl, artikel);
        Befehle.Setze(befehl, "@gesperrt", 0);
        befehl.ExecuteNonQuery();

        artikel.ArtikelId = Befehle.LetzteId(verbindung, null);
        artikel.Gesperrt = false;
        return artikel.ArtikelId;
    }

    public void Aendern(Artikel artikel)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Artikel
              SET Bezeichnung = @bezeichnung, Einheit = @einheit,
                  PreisCent = @preis, SteuerSatzBp = @steuer
              WHERE MandantNr = @mandant AND ArtikelId = @id;");
        SetzeStammdaten(befehl, artikel);
        Befehle.Setze(befehl, "@id", artikel.ArtikelId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Artikel {artikel.ArtikelId} ist im Mandanten {artikel.MandantNr} nicht vorhanden.");
        }
    }

    public void Sperren(int mandantNr, int artikelId) => SetzeGesperrt(mandantNr, artikelId, true);

    public void Entsperren(int mandantNr, int artikelId) => SetzeGesperrt(mandantNr, artikelId, false);

    private void SetzeGesperrt(int mandantNr, int artikelId, bool gesperrt)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Artikel SET Gesperrt = @gesperrt WHERE MandantNr = @mandant AND ArtikelId = @id;");
        Befehle.Setze(befehl, "@gesperrt", gesperrt ? 1 : 0);
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", artikelId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Artikel {artikelId} ist im Mandanten {mandantNr} nicht vorhanden.");
        }
    }

    private static void SetzeStammdaten(SqliteCommand befehl, Artikel artikel)
    {
        Befehle.Setze(befehl, "@mandant", artikel.MandantNr);
        Befehle.Setze(befehl, "@nummer", artikel.Nummer);
        Befehle.Setze(befehl, "@bezeichnung", artikel.Bezeichnung);
        Befehle.Setze(befehl, "@einheit", artikel.Einheit);
        Befehle.Setze(befehl, "@preis", Feldwerte.Cent(artikel.Preis));
        Befehle.Setze(befehl, "@steuer", Feldwerte.Bp(artikel.SteuerSatz));
    }

    private static Artikel Lies(SqliteDataReader leser) => new()
    {
        ArtikelId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Nummer = leser.GetString(2),
        Bezeichnung = leser.GetString(3),
        Einheit = leser.GetString(4),
        Preis = Feldwerte.AusCent(leser.GetInt64(5)),
        SteuerSatz = Feldwerte.AusBp(leser.GetInt64(6)),
        Gesperrt = leser.GetInt32(7) != 0
    };
}
