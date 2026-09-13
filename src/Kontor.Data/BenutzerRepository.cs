using Kontor.Core.Modell;
using Kontor.Core.Repositories;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

public sealed class BenutzerRepository : IBenutzerRepository
{
    private const string Spalten =
        "BenutzerId, Anmeldename, Anzeigename, KennwortHash, Salz, Durchlaeufe, Systemrechte, Gesperrt, Angelegt, LetzteAnmeldung";

    private readonly Datenbank _datenbank;
    private readonly Zugriffskontext _zugriff;

    public BenutzerRepository(Datenbank datenbank, Zugriffskontext zugriff)
    {
        _datenbank = datenbank;
        _zugriff = zugriff;
    }

    public IReadOnlyList<Benutzer> Alle()
    {
        var benutzer = new List<Benutzer>();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Benutzer ORDER BY Anmeldename;");
        using var leser = befehl.ExecuteReader();

        while (leser.Read())
        {
            benutzer.Add(Lies(leser));
        }

        return benutzer;
    }

    public Benutzer? Lade(int benutzerId)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Benutzer WHERE BenutzerId = @id;");
        Befehle.Setze(befehl, "@id", benutzerId);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public Benutzer? LadeMitAnmeldename(string anmeldename)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "SELECT " + Spalten + " FROM Benutzer WHERE Anmeldename = @anmeldename;");
        Befehle.Setze(befehl, "@anmeldename", anmeldename);
        using var leser = befehl.ExecuteReader();

        return leser.Read() ? Lies(leser) : null;
    }

    public int Anlegen(Benutzer benutzer)
    {
        _zugriff.PruefeSystemrechte();

        if (benutzer.Angelegt == default)
        {
            benutzer.Angelegt = Feldwerte.Jetzt();
        }

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            @"INSERT INTO Benutzer (Anmeldename, Anzeigename, KennwortHash, Salz, Durchlaeufe, Systemrechte, Gesperrt, Angelegt, LetzteAnmeldung)
              VALUES (@anmeldename, @anzeigename, @hash, @salz, @durchlaeufe, @systemrechte, 0, @angelegt, NULL);");
        Befehle.Setze(befehl, "@anmeldename", benutzer.Anmeldename);
        Befehle.Setze(befehl, "@anzeigename", benutzer.Anzeigename);
        Befehle.Setze(befehl, "@hash", benutzer.KennwortHash);
        Befehle.Setze(befehl, "@salz", benutzer.Salz);
        Befehle.Setze(befehl, "@durchlaeufe", benutzer.Durchlaeufe);
        Befehle.Setze(befehl, "@systemrechte", benutzer.Systemrechte ? 1 : 0);
        Befehle.Setze(befehl, "@angelegt", Feldwerte.Zeit(benutzer.Angelegt));
        befehl.ExecuteNonQuery();

        benutzer.BenutzerId = Befehle.LetzteId(verbindung, null);
        benutzer.Gesperrt = false;
        return benutzer.BenutzerId;
    }

    public void KennwortSetzen(int benutzerId, byte[] hash, byte[] salz, int durchlaeufe)
    {
        _zugriff.PruefeSystemrechte();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Benutzer SET KennwortHash = @hash, Salz = @salz, Durchlaeufe = @durchlaeufe WHERE BenutzerId = @id;");
        Befehle.Setze(befehl, "@hash", hash);
        Befehle.Setze(befehl, "@salz", salz);
        Befehle.Setze(befehl, "@durchlaeufe", durchlaeufe);
        Befehle.Setze(befehl, "@id", benutzerId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Benutzer {benutzerId} ist nicht vorhanden.");
        }
    }

    public void Sperren(int benutzerId) => SetzeGesperrt(benutzerId, true);

    public void Entsperren(int benutzerId) => SetzeGesperrt(benutzerId, false);

    // Kein Zugriffscheck: das Nachtragen des eigenen Anmeldezeitpunkts ist Teil des Anmeldevorgangs
    // selbst und muss für jeden Benutzer funktionieren, nicht nur für Systemrechte-Inhaber.
    public void LetzteAnmeldungSetzen(int benutzerId, DateTime zeitpunkt)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Benutzer SET LetzteAnmeldung = @zeitpunkt WHERE BenutzerId = @id;");
        Befehle.Setze(befehl, "@zeitpunkt", Feldwerte.Zeit(zeitpunkt));
        Befehle.Setze(befehl, "@id", benutzerId);
        befehl.ExecuteNonQuery();
    }

    private void SetzeGesperrt(int benutzerId, bool gesperrt)
    {
        _zugriff.PruefeSystemrechte();

        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "UPDATE Benutzer SET Gesperrt = @gesperrt WHERE BenutzerId = @id;");
        Befehle.Setze(befehl, "@gesperrt", gesperrt ? 1 : 0);
        Befehle.Setze(befehl, "@id", benutzerId);

        if (befehl.ExecuteNonQuery() == 0)
        {
            throw new DatenbankFehler($"Benutzer {benutzerId} ist nicht vorhanden.");
        }
    }

    private static Benutzer Lies(SqliteDataReader leser) => new()
    {
        BenutzerId = leser.GetInt32(0),
        Anmeldename = leser.GetString(1),
        Anzeigename = leser.GetString(2),
        KennwortHash = leser.GetFieldValue<byte[]>(3),
        Salz = leser.GetFieldValue<byte[]>(4),
        Durchlaeufe = leser.GetInt32(5),
        Systemrechte = leser.GetInt32(6) != 0,
        Gesperrt = leser.GetInt32(7) != 0,
        Angelegt = Feldwerte.AusZeit(leser.GetString(8)),
        LetzteAnmeldung = leser.IsDBNull(9) ? null : Feldwerte.AusZeit(leser.GetString(9))
    };
}
