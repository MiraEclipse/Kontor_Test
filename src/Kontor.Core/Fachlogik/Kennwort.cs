using System.Security.Cryptography;

namespace Kontor.Core.Fachlogik;

public static class Kennwort
{
    public const int StandardDurchlaeufe = 210_000;
    public const int SalzLaenge = 16;
    private const int HashLaenge = 32;

    public static byte[] NeuesSalz() => RandomNumberGenerator.GetBytes(SalzLaenge);

    public static byte[] Hash(string kennwort, byte[] salz, int durchlaeufe)
    {
        using var ableitung = new Rfc2898DeriveBytes(kennwort, salz, durchlaeufe, HashAlgorithmName.SHA256);
        return ableitung.GetBytes(HashLaenge);
    }

    // Zeitkonstanter Vergleich, damit die Laufzeit der Prüfung nicht verrät, an welcher Stelle der
    // Hash abweicht.
    public static bool Pruefe(string kennwort, byte[] hash, byte[] salz, int durchlaeufe) =>
        CryptographicOperations.FixedTimeEquals(Hash(kennwort, salz, durchlaeufe), hash);
}
