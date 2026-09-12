using System.Globalization;

namespace Kontor.Data;

public static class Feldwerte
{
    private const string DatumsMuster = "yyyy-MM-dd";
    private const string ZeitMuster = "yyyy-MM-dd HH:mm:ss";

    public static long Cent(decimal betrag) => (long)Math.Round(betrag * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal AusCent(long cent) => cent / 100m;

    public static long Tausendstel(decimal menge) => (long)Math.Round(menge * 1000m, 0, MidpointRounding.AwayFromZero);

    public static decimal AusTausendstel(long tausendstel) => tausendstel / 1000m;

    public static long Bp(decimal prozentsatz) => (long)Math.Round(prozentsatz * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal AusBp(long bp) => bp / 100m;

    public static string Datum(DateTime wert) => wert.ToString(DatumsMuster, CultureInfo.InvariantCulture);

    public static DateTime AusDatum(string wert) =>
        DateTime.ParseExact(wert, DatumsMuster, CultureInfo.InvariantCulture, DateTimeStyles.None);

    public static string Zeit(DateTime wert) => wert.ToString(ZeitMuster, CultureInfo.InvariantCulture);

    public static DateTime AusZeit(string wert) =>
        DateTime.ParseExact(wert, ZeitMuster, CultureInfo.InvariantCulture, DateTimeStyles.None);

    public static DateTime Jetzt() => AusZeit(Zeit(DateTime.Now));

    public static string Jahr(int jahr) => jahr.ToString("0000", CultureInfo.InvariantCulture);

    public static object Null(int? wert) => wert.HasValue ? wert.Value : DBNull.Value;
}
