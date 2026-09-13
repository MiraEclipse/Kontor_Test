namespace Kontor.Core.Modell;

public enum Kontoart
{
    Giro,
    Bar,
    Sparen,
    Kreditkarte
}

public static class Kontoarten
{
    public static string Code(Kontoart art) => art switch
    {
        Kontoart.Giro => "G",
        Kontoart.Bar => "B",
        Kontoart.Sparen => "S",
        Kontoart.Kreditkarte => "K",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };

    public static Kontoart Aus(string code) => code switch
    {
        "G" => Kontoart.Giro,
        "B" => Kontoart.Bar,
        "S" => Kontoart.Sparen,
        "K" => Kontoart.Kreditkarte,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannte Kontoart")
    };

    public static string Bezeichnung(Kontoart art) => art switch
    {
        Kontoart.Giro => "Girokonto",
        Kontoart.Bar => "Bargeld",
        Kontoart.Sparen => "Sparkonto",
        Kontoart.Kreditkarte => "Kreditkarte",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };
}
