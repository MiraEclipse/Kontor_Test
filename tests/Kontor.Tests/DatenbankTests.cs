using Kontor.Data;
using Xunit;

namespace Kontor.Tests;

public class DatenbankTests
{
    [Fact]
    public void StandardLiefertEinenPfadUnterhalbVonApplicationDataUndErzeugtDabeiNichtsImDateisystem()
    {
        // Ob der Ordner schon existiert, hängt vom Rechner ab (z.B. durch einen früheren echten
        // Programmstart) - geprüft wird nur, dass der Aufruf von Standard() daran nichts ändert.
        var applicationData = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        var ordner = Path.Combine(applicationData, "Kontor");
        var ordnerBestandVorher = Directory.Exists(ordner);
        var dateiBestandVorher = File.Exists(Path.Combine(ordner, "kontor.db"));

        var datenbank = Datenbank.Standard();

        Assert.StartsWith(applicationData + Path.DirectorySeparatorChar, datenbank.Dateipfad);
        Assert.Equal(ordnerBestandVorher, Directory.Exists(ordner));
        Assert.Equal(dateiBestandVorher, File.Exists(datenbank.Dateipfad));
    }

    [Fact]
    public void DasUeberschreibendeArgumentGewinntGegenueberDemStandardpfad()
    {
        var ueberschreibenderPfad = Path.Combine(Path.GetTempPath(), "kontor-ueberschrieben-" + Guid.NewGuid().ToString("N"), "kontor.db");

        var datenbank = Datenbank.Standard(ueberschreibenderPfad);

        Assert.Equal(Path.GetFullPath(ueberschreibenderPfad), datenbank.Dateipfad);
    }
}
