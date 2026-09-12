namespace Kontor.App;

public interface IMeldungsanzeige
{
    void Erfolg(string meldung);

    void Hinweis(string meldung);

    void Fehler(string meldung);
}
