namespace Kontor.Core.Fachlogik;

public sealed class FachlicherFehler : Exception
{
    public FachlicherFehler(string meldung) : base(meldung)
    {
    }
}
