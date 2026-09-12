using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Kontor.Data;

// Schlägt für Stammdaten (Kunde, Artikel) die nächste freie, rein numerische Nummer je Mandant vor.
// Nicht-numerische Nummern (vom Anwender frei vergeben) werden dabei ignoriert, zählen also nicht mit.
internal static class Nummernvorschlag
{
    public static string Naechste(SqliteConnection verbindung, string tabelle, int mandantNr)
    {
        using var befehl = Befehle.Neu(verbindung, null,
            $@"SELECT MAX(CAST(Nummer AS INTEGER)) FROM {tabelle}
               WHERE MandantNr = @mandant AND Nummer <> '' AND Nummer NOT GLOB '*[^0-9]*';");
        Befehle.Setze(befehl, "@mandant", mandantNr);
        var wert = befehl.ExecuteScalar();
        var bisher = wert is null || wert is DBNull ? 0L : Convert.ToInt64(wert);

        return (bisher + 1).ToString("00000", CultureInfo.InvariantCulture);
    }
}
