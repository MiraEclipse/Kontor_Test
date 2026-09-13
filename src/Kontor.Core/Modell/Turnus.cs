namespace Kontor.Core.Modell;

public enum Turnus
{
    Monatlich,
    Vierteljaehrlich,
    Halbjaehrlich,
    Jaehrlich
}

public static class Turnusse
{
    public static string Code(Turnus turnus) => turnus switch
    {
        Turnus.Monatlich => "M",
        Turnus.Vierteljaehrlich => "Q",
        Turnus.Halbjaehrlich => "H",
        Turnus.Jaehrlich => "J",
        _ => throw new ArgumentOutOfRangeException(nameof(turnus))
    };

    public static Turnus Aus(string code) => code switch
    {
        "M" => Turnus.Monatlich,
        "Q" => Turnus.Vierteljaehrlich,
        "H" => Turnus.Halbjaehrlich,
        "J" => Turnus.Jaehrlich,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekannter Turnus")
    };

    public static string Bezeichnung(Turnus turnus) => turnus switch
    {
        Turnus.Monatlich => "monatlich",
        Turnus.Vierteljaehrlich => "vierteljährlich",
        Turnus.Halbjaehrlich => "halbjährlich",
        Turnus.Jaehrlich => "jährlich",
        _ => throw new ArgumentOutOfRangeException(nameof(turnus))
    };

    // Anzahl der Monate zwischen zwei Fälligkeiten desselben Vertrags.
    public static int Monate(Turnus turnus) => turnus switch
    {
        Turnus.Monatlich => 1,
        Turnus.Vierteljaehrlich => 3,
        Turnus.Halbjaehrlich => 6,
        Turnus.Jaehrlich => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(turnus))
    };
}
