using System.Text;

namespace Kontor.App;

public static class Kommandozeile
{
    public const string Beenden = "END";

    public static string Normalisiere(string? eingabe)
    {
        if (string.IsNullOrWhiteSpace(eingabe))
        {
            return "";
        }

        var bau = new StringBuilder();
        foreach (var zeichen in eingabe)
        {
            if (!char.IsWhiteSpace(zeichen))
            {
                bau.Append(char.ToUpperInvariant(zeichen));
            }
        }

        var text = bau.ToString();

        if (text.StartsWith("/N", StringComparison.Ordinal))
        {
            text = text.Substring(2);
        }

        return text;
    }

    public static bool IstBeenden(string transaktionscode) =>
        string.Equals(transaktionscode, Beenden, StringComparison.Ordinal);
}
