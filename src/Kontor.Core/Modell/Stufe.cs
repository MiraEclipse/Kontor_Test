namespace Kontor.Core.Modell;

public enum Stufe
{
    Lesen,
    Aendern,
    Voll
}

public static class Stufen
{
    public static string Code(Stufe stufe) => stufe switch
    {
        Stufe.Lesen => "L",
        Stufe.Aendern => "A",
        Stufe.Voll => "V",
        _ => throw new ArgumentOutOfRangeException(nameof(stufe))
    };

    public static Stufe Aus(string code) => code switch
    {
        "L" => Stufe.Lesen,
        "A" => Stufe.Aendern,
        "V" => Stufe.Voll,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannte Stufe")
    };

    public static string Bezeichnung(Stufe stufe) => stufe switch
    {
        Stufe.Lesen => "Lesen",
        Stufe.Aendern => "Ändern",
        Stufe.Voll => "Voll",
        _ => throw new ArgumentOutOfRangeException(nameof(stufe))
    };
}
