namespace Kontor.Core.Modell;

public enum Kontoart
{
    Aktiv,
    Passiv,
    Ertrag,
    Aufwand
}

public static class Kontoarten
{
    public static string Code(Kontoart art) => art switch
    {
        Kontoart.Aktiv => "A",
        Kontoart.Passiv => "P",
        Kontoart.Ertrag => "E",
        Kontoart.Aufwand => "W",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };

    public static Kontoart Aus(string code) => code switch
    {
        "A" => Kontoart.Aktiv,
        "P" => Kontoart.Passiv,
        "E" => Kontoart.Ertrag,
        "W" => Kontoart.Aufwand,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannte Kontoart")
    };

    public static string Bezeichnung(Kontoart art) => art switch
    {
        Kontoart.Aktiv => "Aktivkonto",
        Kontoart.Passiv => "Passivkonto",
        Kontoart.Ertrag => "Ertragskonto",
        Kontoart.Aufwand => "Aufwandskonto",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };
}
