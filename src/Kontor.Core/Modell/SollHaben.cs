namespace Kontor.Core.Modell;

public enum SollHaben
{
    Soll,
    Haben
}

public static class SollHabenCodes
{
    public static string Code(SollHaben sh) => sh == SollHaben.Soll ? "S" : "H";

    public static SollHaben Aus(string code) => code switch
    {
        "S" => SollHaben.Soll,
        "H" => SollHaben.Haben,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekanntes Soll/Haben-Kennzeichen")
    };

    public static SollHaben Gegenseite(SollHaben sh) => sh == SollHaben.Soll ? SollHaben.Haben : SollHaben.Soll;
}
