namespace Kontor.Data;

// Feldwerte rechnet mit DateTime; die neuen Fachklassen rechnen mit DateOnly (siehe Faelligkeiten).
// Dieser Adapter spart die Uhrzeit einfach weg, ohne Feldwerte selbst anzufassen.
internal static class DatumOnly
{
    public static string Text(DateOnly datum) => Feldwerte.Datum(datum.ToDateTime(TimeOnly.MinValue));

    public static DateOnly Aus(string text) => DateOnly.FromDateTime(Feldwerte.AusDatum(text));

    public static object Null(DateOnly? datum) => datum.HasValue ? Text(datum.Value) : DBNull.Value;

    public static DateOnly? AusNull(object wert) => wert is DBNull ? null : Aus((string)wert);
}
