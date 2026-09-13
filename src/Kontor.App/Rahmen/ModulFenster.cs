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

        // Kein frei schwebendes MDI-Kindfenster mehr mit eigener Titelleiste - das Hauptfenster zeigt
        // jeweils ein maximiertes Modul und legt darüber eine eigene Reiterleiste (Modulreiter), die
        // dessen Aufgabe (Titel, Schließen-Kreuz) übernimmt.
        FormBorderStyle = FormBorderStyle.None;
        ControlBox = false;
    }

    public string Transaktionscode { get; }

    public string Bezeichnung { get; }

    public MaskenModus Modus { get; private set; } = MaskenModus.Anzeigen;

    protected Modulkontext Kontext { get; }

    protected Sitzung Sitzung => Kontext.Sitzung;

    protected Dienste Dienste => Kontext.Dienste;

    protected IMeldungsanzeige Meldungen => Kontext.Meldungen;

    protected int MandantNr => Kontext.MandantNr;

    // Die zum jetzigen Zeitpunkt wirksame Stufe dieses Fensters im aktuellen Haushalt - null, wenn gar
    // kein Recht (mehr) vorliegt.
    public Stufe? EffektiveStufe => Dienste.Zugriff.WirksameStufe(MandantNr, Transaktionscode);

    // Für den Zeitgeber in Hauptfenster: ob beim erzwungenen Schließen (Recht abgelaufen) noch
    // ungesicherte Eingaben bestehen, die vorher zum Sichern angeboten werden sollen.
    public virtual bool HatUngesicherteEingaben => false;

    public virtual void SichernVersuchen()
    {
    }

    public void SetzeModus(MaskenModus modus)
    {
        if (MaskenModi.IstSchreibend(modus) && (EffektiveStufe is null || EffektiveStufe.Value < Stufe.Aendern))
        {
            Meldungen.Fehler(
                $"Keine Berechtigung: {Transaktionscode} erfordert mindestens Stufe {Stufen.Bezeichnung(Stufe.Aendern)}.");
            return;
        }

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
