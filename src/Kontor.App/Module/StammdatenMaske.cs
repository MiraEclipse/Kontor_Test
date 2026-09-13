using Kontor.App.Rahmen;

namespace Kontor.App.Module;

public sealed class StammdatenMaske : ModulFenster
{
    private readonly KontenTeilmaske _konten;
    private readonly KategorienTeilmaske _kategorien;

    public StammdatenMaske(Modulkontext kontext) : base("K02", "Stammdaten", kontext)
    {
        ClientSize = new Size(600, 470);

        var reiter = new TabControl { Dock = DockStyle.Fill };

        var kontenReiter = new TabPage("Konten");
        _konten = new KontenTeilmaske(kontext);
        kontenReiter.Controls.Add(_konten);

        var kategorienReiter = new TabPage("Kategorien");
        _kategorien = new KategorienTeilmaske(kontext);
        kategorienReiter.Controls.Add(_kategorien);

        reiter.TabPages.Add(kontenReiter);
        reiter.TabPages.Add(kategorienReiter);

        Controls.Add(reiter);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _konten.Initialisieren();
        _kategorien.Initialisieren();
    }

    public override bool HatUngesicherteEingaben => _konten.HatUngesicherteEingaben || _kategorien.HatUngesicherteEingaben;

    public override void SichernVersuchen()
    {
        _konten.SichernVersuchen();
        _kategorien.SichernVersuchen();
    }
}
