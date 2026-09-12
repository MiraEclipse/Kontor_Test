namespace Kontor.Data;

public sealed class DatenbankFehler : Exception
{
    public DatenbankFehler(string meldung) : base(meldung)
    {
    }
}
