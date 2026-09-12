using Kontor.Core.Modell;

namespace Kontor.App.Rahmen;

public class ModulFenster : Form
{
    protected ModulFenster(string transaktionscode, string bezeichnung, Modulkontext kontext)
    {
        Transaktionscode = transaktionscode;
        Bezeichnung = bezeichnung;
        Kontext = kontext;

        Font = new Font("MS Sans Serif", 8.25f);
        ClientSize = new Size(560, 340);
        StartPosition = FormStartPosition.Manual;
        Text = Titel(MaskenModus.Anzeigen);
    }

    public string Transaktionscode { get; }

    public string Bezeichnung { get; }

    public MaskenModus Modus { get; private set; } = MaskenModus.Anzeigen;

    protected Modulkontext Kontext { get; }

    protected Sitzung Sitzung => Kontext.Sitzung;

    protected Dienste Dienste => Kontext.Dienste;

    protected IMeldungsanzeige Meldungen => Kontext.Meldungen;

    protected int MandantNr => Kontext.MandantNr;

    public void SetzeModus(MaskenModus modus)
    {
        Modus = modus;
        Text = Titel(modus);

        UebernehmeModus(modus);

        Meldungen.Hinweis($"{Transaktionscode} {Bezeichnung} – Modus {MaskenModi.Bezeichnung(modus)}");
    }

    protected virtual void UebernehmeModus(MaskenModus modus)
    {
    }

    private string Titel(MaskenModus modus) =>
        $"{Transaktionscode} {Bezeichnung} – {MaskenModi.Bezeichnung(modus)}";
}
