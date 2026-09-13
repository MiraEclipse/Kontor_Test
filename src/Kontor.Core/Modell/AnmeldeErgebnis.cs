namespace Kontor.Core.Modell;

public enum AnmeldeErgebnis
{
    Erfolg,
    UnbekannterBenutzer,
    FalschesKennwort,
    Gesperrt
}

public static class AnmeldeErgebnisse
{
    public static string Code(AnmeldeErgebnis ergebnis) => ergebnis switch
    {
        AnmeldeErgebnis.Erfolg => "OK",
        AnmeldeErgebnis.UnbekannterBenutzer => "UNBEKANNT",
        AnmeldeErgebnis.FalschesKennwort => "KENNWORT",
        AnmeldeErgebnis.Gesperrt => "GESPERRT",
        _ => throw new ArgumentOutOfRangeException(nameof(ergebnis))
    };

    public static AnmeldeErgebnis Aus(string code) => code switch
    {
        "OK" => AnmeldeErgebnis.Erfolg,
        "UNBEKANNT" => AnmeldeErgebnis.UnbekannterBenutzer,
        "KENNWORT" => AnmeldeErgebnis.FalschesKennwort,
        "GESPERRT" => AnmeldeErgebnis.Gesperrt,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unbekanntes Anmeldeergebnis")
    };

    public static string Bezeichnung(AnmeldeErgebnis ergebnis) => ergebnis switch
    {
        AnmeldeErgebnis.Erfolg => "Angemeldet",
        AnmeldeErgebnis.UnbekannterBenutzer => "Unbekannter Anmeldename",
        AnmeldeErgebnis.FalschesKennwort => "Falsches Kennwort",
        AnmeldeErgebnis.Gesperrt => "Benutzer gesperrt",
        _ => throw new ArgumentOutOfRangeException(nameof(ergebnis))
    };
}
