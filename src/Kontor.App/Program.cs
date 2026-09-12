using Kontor.App.Rahmen;
using Kontor.Data;

namespace Kontor.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetCompatibleTextRenderingDefault(false);

        var datenbank = Datenbank.Standard();

        try
        {
            datenbank.Vorbereiten();
        }
        catch (Exception fehler)
        {
            MessageBox.Show(
                $"Die Datenbank {datenbank.Dateipfad} konnte nicht geöffnet werden.\n\n{fehler.Message}",
                "KONTOR",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var dienste = new Dienste(
            new MandantRepository(datenbank),
            new KundeRepository(datenbank),
            new ArtikelRepository(datenbank),
            new BelegRepository(datenbank),
            new KontoRepository(datenbank),
            new KalkulationRepository(datenbank),
            new NotizRepository(datenbank));

        using var anmeldung = new Anmeldefenster(dienste.Mandanten);

        if (anmeldung.ShowDialog() != DialogResult.OK || anmeldung.Sitzung is null)
        {
            return;
        }

        Application.Run(new Hauptfenster(anmeldung.Sitzung, dienste, Modulverzeichnis.Standard()));
    }
}
