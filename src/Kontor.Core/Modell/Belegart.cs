namespace Kontor.Core.Modell;

public enum Belegart
{
    Rechnung,
    Gutschrift,
    Zahlung
}

public static class Belegarten
{
    public static string Code(Belegart art) => art switch
    {
        Belegart.Rechnung => "RE",
        Belegart.Gutschrift => "GU",
        Belegart.Zahlung => "ZA",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };

    public static Belegart Aus(string code) => code switch
    {
        "RE" => Belegart.Rechnung,
        "GU" => Belegart.Gutschrift,
        "ZA" => Belegart.Zahlung,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannte Belegart")
    };

    public static string Bezeichnung(Belegart art) => art switch
    {
        Belegart.Rechnung => "Rechnung",
        Belegart.Gutschrift => "Gutschrift",
        Belegart.Zahlung => "Zahlung",
        _ => throw new ArgumentOutOfRangeException(nameof(art))
    };
}
