using Kontor.App.Rahmen;

namespace Kontor.App.Module;

public sealed class Systemtest : ModulFenster
{
    private readonly TextBox _mandant = new();
    private readonly TextBox _benutzer = new();
    private readonly TextBox _angemeldet = new();
    private readonly TextBox _bemerkung = new();
    private readonly Label _modusfeld = new();

    public Systemtest(Modulkontext kontext) : base("K99", "Systemtest", kontext)
    {
        ClientSize = new Size(520, 300);

        var kopf = new Label
        {
            Text = "Sitzung",
            Location = new Point(12, 12),
            Size = new Size(200, 16)
        };

        var strich = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, 30),
            Size = new Size(492, 2)
        };

        Controls.Add(kopf);
        Controls.Add(strich);

        Feld("Mandant:", _mandant, 44, false);
        Feld("Benutzer:", _benutzer, 72, false);
        Feld("Angemeldet:", _angemeldet, 100, false);
        Feld("Bemerkung:", _bemerkung, 128, true);

        _mandant.Text = $"{Sitzung.Mandant.MandantNr:0000}  {Sitzung.Mandant.Name}, {Sitzung.Mandant.Ort} " +
                        $"({Sitzung.Mandant.Waehrung})";
        _benutzer.Text = Sitzung.Benutzer;
        _angemeldet.Text = Sitzung.Angemeldet.ToString("yyyy-MM-dd HH:mm:ss");

        var modusKopf = new Label
        {
            Text = "Modus",
            Location = new Point(12, 168),
            Size = new Size(200, 16)
        };

        var modusStrich = new Label
        {
            BorderStyle = BorderStyle.Fixed3D,
            Location = new Point(12, 186),
            Size = new Size(492, 2)
        };

        _modusfeld.Location = new Point(12, 200);
        _modusfeld.Size = new Size(200, 16);

        Controls.Add(modusKopf);
        Controls.Add(modusStrich);
        Controls.Add(_modusfeld);

        Knopf("An&zeigen", 12, 224, () => SetzeModus(MaskenModus.Anzeigen));
        Knopf("Ä&ndern", 100, 224, () => SetzeModus(MaskenModus.Aendern));
        Knopf("&Erfassen", 188, 224, () => SetzeModus(MaskenModus.Erfassen));

        Knopf("&Meldung", 12, 258, () => Meldungen.Erfolg("K99 Systemtest: Meldung abgesetzt."));
        Knopf("&Hinweis", 100, 258, () => Meldungen.Hinweis("K99 Systemtest: Hinweis abgesetzt."));
        Knopf("&Fehler", 188, 258, () => Meldungen.Fehler("K99 Systemtest: Fehler abgesetzt."));

        var kontenzahl = new Button
        {
            Text = "&Konten zählen",
            Location = new Point(388, 258),
            Size = new Size(116, 24)
        };
        kontenzahl.Click += (_, _) => ZaehleKonten();
        Controls.Add(kontenzahl);
    }

    protected override void UebernehmeModus(MaskenModus modus)
    {
        _modusfeld.Text = MaskenModi.Bezeichnung(modus);
        _bemerkung.ReadOnly = !MaskenModi.IstSchreibend(modus);
        _bemerkung.BackColor = _bemerkung.ReadOnly ? SystemColors.Control : SystemColors.Window;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        SetzeModus(Modus);
    }

    private void ZaehleKonten()
    {
        var konten = Dienste.Konten.Liste(MandantNr);
        Meldungen.Erfolg($"Mandant {MandantNr:0000}: {konten.Count} Konten im Kontenrahmen.");
    }

    private void Feld(string beschriftung, TextBox feld, int oben, bool schreibbar)
    {
        var text = new Label
        {
            Text = beschriftung,
            Location = new Point(12, oben + 4),
            Size = new Size(80, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        feld.Location = new Point(96, oben);
        feld.Size = new Size(408, 20);
        feld.Font = new Font("Courier New", 9f);
        feld.ReadOnly = !schreibbar;
        feld.BackColor = feld.ReadOnly ? SystemColors.Control : SystemColors.Window;
        feld.TabStop = schreibbar;

        Controls.Add(text);
        Controls.Add(feld);
    }

    private void Knopf(string beschriftung, int links, int oben, Action aktion)
    {
        var knopf = new Button
        {
            Text = beschriftung,
            Location = new Point(links, oben),
            Size = new Size(80, 24)
        };

        knopf.Click += (_, _) => aktion();
        Controls.Add(knopf);
    }
}
