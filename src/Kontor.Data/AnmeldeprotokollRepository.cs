using Kontor.Core.Modell;
using Kontor.Core.Repositories;

namespace Kontor.Data;

public sealed class AnmeldeprotokollRepository : IAnmeldeprotokollRepository
{
    private const string FehlversucheSql = @"
        SELECT COUNT(*) FROM Anmeldeprotokoll
        WHERE Anmeldename = @name
          AND Ergebnis <> 'OK'
          AND Zeitpunkt > COALESCE(
              (SELECT MAX(Zeitpunkt) FROM Anmeldeprotokoll WHERE Anmeldename = @name AND Ergebnis = 'OK'),
              '0000-01-01 00:00:00');";

    private readonly Datenbank _datenbank;

    public AnmeldeprotokollRepository(Datenbank datenbank)
    {
        _datenbank = datenbank;
    }

    public void Erfassen(string anmeldename, AnmeldeErgebnis ergebnis)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null,
            "INSERT INTO Anmeldeprotokoll (Zeitpunkt, Anmeldename, Ergebnis) VALUES (@zeitpunkt, @name, @ergebnis);");
        Befehle.Setze(befehl, "@zeitpunkt", Feldwerte.Zeit(DateTime.Now));
        Befehle.Setze(befehl, "@name", anmeldename);
        Befehle.Setze(befehl, "@ergebnis", AnmeldeErgebnisse.Code(ergebnis));
        befehl.ExecuteNonQuery();
    }

    public int FehlversucheInFolge(string anmeldename)
    {
        using var verbindung = _datenbank.Oeffne();
        using var befehl = Befehle.Neu(verbindung, null, FehlversucheSql);
        Befehle.Setze(befehl, "@name", anmeldename);
        return Convert.ToInt32(befehl.ExecuteScalar());
    }
}
