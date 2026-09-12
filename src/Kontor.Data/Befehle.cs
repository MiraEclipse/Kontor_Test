using Microsoft.Data.Sqlite;

namespace Kontor.Data;

internal static class Befehle
{
    public static SqliteCommand Neu(SqliteConnection verbindung, SqliteTransaction? transaktion, string text)
    {
        var befehl = verbindung.CreateCommand();
        befehl.CommandText = text;
        befehl.Transaction = transaktion;
        return befehl;
    }

    public static void Setze(SqliteCommand befehl, string name, object? wert)
    {
        var p = befehl.CreateParameter();
        p.ParameterName = name;
        p.Value = wert ?? DBNull.Value;
        befehl.Parameters.Add(p);
    }

    public static void Fuehre(SqliteConnection verbindung, SqliteTransaction? transaktion, string text)
    {
        using var befehl = Neu(verbindung, transaktion, text);
        befehl.ExecuteNonQuery();
    }

    public static int LetzteId(SqliteConnection verbindung, SqliteTransaction? transaktion)
    {
        using var befehl = Neu(verbindung, transaktion, "SELECT last_insert_rowid();");
        return Convert.ToInt32(befehl.ExecuteScalar());
    }

    public static string Suchmuster(string suchtext) => "%" + suchtext.Trim() + "%";
}
