namespace Kontor.Core.Modell;

public enum Richtung
{
    Einnahme,
    Ausgabe
}

public static class Richtungen
{
    public static string Code(Richtung richtung) => richtung switch
    {
        Richtung.Einnahme => "E",
        Richtung.Ausgabe => "A",
        _ => throw new ArgumentOutOfRangeException(nameof(richtung))
    };

    public static Richtung Aus(string code) => code switch
    {
        "E" => Richtung.Einnahme,
        "A" => Richtung.Ausgabe,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannte Richtung")
    };

    public static string Bezeichnung(Richtung richtung) => richtung switch
    {
        Richtung.Einnahme => "Einnahme",
        Richtung.Ausgabe => "Ausgabe",
        _ => throw new ArgumentOutOfRangeException(nameof(richtung))
    };
}
