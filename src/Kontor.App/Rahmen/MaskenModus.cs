namespace Kontor.App.Rahmen;

public enum MaskenModus
{
    Anzeigen,
    Aendern,
    Erfassen
}

public static class MaskenModi
{
    public static string Bezeichnung(MaskenModus modus) => modus switch
    {
        MaskenModus.Anzeigen => "Anzeigen",
        MaskenModus.Aendern => "Ändern",
        MaskenModus.Erfassen => "Erfassen",
        _ => throw new ArgumentOutOfRangeException(nameof(modus))
    };

    public static bool IstSchreibend(MaskenModus modus) => modus != MaskenModus.Anzeigen;
}
