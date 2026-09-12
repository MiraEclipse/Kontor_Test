using Kontor.Core.Modell;

namespace Kontor.Core.Fachlogik;

public sealed class Steuerschluessel
{
    public Steuerschluessel(decimal satz, string erloeskonto, string steuerkonto)
    {
        Satz = satz;
        Erloeskonto = erloeskonto;
        Steuerkonto = steuerkonto;
    }

    public decimal Satz { get; }
    public string Erloeskonto { get; }
    public string Steuerkonto { get; }
}

public sealed class Kontenzuordnung
{
    public static readonly Kontenzuordnung Standard = new(
        "1400",
        "1200",
        new[]
        {
            new Steuerschluessel(19m, "8400", "1776"),
            new Steuerschluessel(7m, "8300", "1775"),
            new Steuerschluessel(0m, "8200", "")
        });

    public Kontenzuordnung(string forderungen, string bank, IReadOnlyList<Steuerschluessel> steuer)
    {
        Forderungen = forderungen;
        Bank = bank;
        Steuer = steuer;
    }

    public string Forderungen { get; }
    public string Bank { get; }
    public IReadOnlyList<Steuerschluessel> Steuer { get; }

    public Steuerschluessel Schluessel(decimal satz)
    {
        foreach (var s in Steuer)
        {
            if (s.Satz == satz)
            {
                return s;
            }
        }

        throw new FachlicherFehler($"Für den Steuersatz {satz:0.##} % ist kein Erlöskonto hinterlegt.");
    }

    public IReadOnlyList<decimal> Steuersaetze
    {
        get
        {
            var saetze = new List<decimal>();
            foreach (var s in Steuer)
            {
                saetze.Add(s.Satz);
            }

            return saetze;
        }
    }
}
