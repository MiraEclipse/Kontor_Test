using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class KundeRepository : IKundeRepository
{
    private const string Spalten =
        "KundeId, MandantNr, Nummer, Name, Strasse, Plz, Ort, Telefon, UstIdNr, Gesperrt";

    private readonly Datenbank _datenbank;

    public KundeRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public IReadOnlyList<Kunde> Suche(int mandantNr, string suchbegriff, bool auchGesperrte)
    {
        var kunden = new List<Kunde>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + @" FROM Kunde
             WHERE MandantNr = @mandant
               AND (@leer = 1 OR Nummer LIKE @muster OR Name LIKE @muster OR Ort LIKE @muster)
               AND (@auchGesperrte = 1 OR Gesperrt = 0)
             ORDER BY Nummer;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@leer", string.IsNullOrWhiteSpace(suchbegriff) ? 1 : 0);
        Befehle.Setze(befehl, "@muster", Befehle.Suchmuster(suchbegriff));
        Befehle.Setze(befehl, "@auchGesperrte", auchGesperrte ? 1 : 0);
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            kunden.Add(Lies(leser));
        }

        return kunden;
    }

    public Kunde? Lade(int mandantNr, int kundeId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Kunde WHERE MandantNr = @mandant AND KundeId = @id;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kundeId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public Kunde? LadeMitNummer(int mandantNr, string nummer)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Kunde WHERE MandantNr = @mandant AND Nummer = @nummer;");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@nummer", nummer);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public string NaechsteFreieNummer(int mandantNr)
    {
        using var verbindung = _datenbank.Oeffne();
        return Nummernvorschlag.Naechste(verbindung, "Kunde", mandantNr);
    }

    public int Anlegen(Kunde kunde)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Kunde (MandantNr, Nummer, Name, Strasse, Plz, Ort, Telefon, UstIdNr, Gesperrt)
              VALUES (@mandant, @nummer, @name, @strasse, @plz, @ort, @telefon, @ustIdNr, @gesperrt);");
        SetzeStammdaten(befehl, kunde);
        Befehle.Setze(befehl, "@gesperrt", 0);
        befehl.ExecuteNonQuery();

        kunde.KundeId = Befehle.LetzteId(verbindung, null);
        kunde.Gesperrt = false;
        return kunde.KundeId;
    }

    public void Aendern(Kunde kunde)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"UPDATE Kunde
              SET Name = @name, Strasse = @strasse, Plz = @plz, Ort = @ort,
                  Telefon = @telefon, UstIdNr = @ustIdNr
              WHERE MandantNr = @mandant AND KundeId = @id;");
        SetzeStammdaten(befehl, kunde);
        Befehle.Setze(befehl, "@id", kunde.KundeId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Kunde {kunde.KundeId} ist im Mandanten {kunde.MandantNr} nicht vorhanden.");
        }
    }

    public void Sperren(int mandantNr, int kundeId) => SetzeGesperrt(mandantNr, kundeId, true);

    public void Entsperren(int mandantNr, int kundeId) => SetzeGesperrt(mandantNr, kundeId, false);

    private void SetzeGesperrt(int mandantNr, int kundeId, bool gesperrt)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Kunde SET Gesperrt = @gesperrt WHERE MandantNr = @mandant AND KundeId = @id;");
        Befehle.Setze(befehl, "@gesperrt", gesperrt ? 1 : 0);
        Befehle.Setze(befehl, "@mandant", mandantNr);
        Befehle.Setze(befehl, "@id", kundeId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Kunde {kundeId} ist im Mandanten {mandantNr} nicht vorhanden.");
        }
    }

    private static void SetzeStammdaten(SqliteCommand befehl, Kunde kunde)
    {
        Befehle.Setze(befehl, "@mandant", kunde.MandantNr);
        Befehle.Setze(befehl, "@nummer", kunde.Nummer);
        Befehle.Setze(befehl, "@name", kunde.Name);
        Befehle.Setze(befehl, "@strasse", kunde.Strasse);
        Befehle.Setze(befehl, "@plz", kunde.Plz);
        Befehle.Setze(befehl, "@ort", kunde.Ort);
        Befehle.Setze(befehl, "@telefon", kunde.Telefon);
        Befehle.Setze(befehl, "@ustIdNr", kunde.UstIdNr);
    }

    private static Kunde Lies(SqliteDataReader leser) => new()
    {
        KundeId = leser.GetInt32(0),
        MandantNr = leser.GetInt32(1),
        Nummer = leser.GetString(2),
        Name = leser.GetString(3),
        Strasse = leser.GetString(4),
        Plz = leser.GetString(5),
        Ort = leser.GetString(6),
        Telefon = leser.GetString(7),
        UstIdNr = leser.GetString(8),
        Gesperrt = leser.GetInt32(9) != 0
    };
}
