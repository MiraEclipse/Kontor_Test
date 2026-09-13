using Kontor.App.Rahmen;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

// Dasselbe Gerüst wie StammdatenFenster (Suchfeld, ListView, Detailgruppe, Buttonleiste, Moduswechsel),
// aber als Panel statt als eigenes Fenster - für K02, wo Konten und Kategorien als zwei Registerkarten
// in einem Modul stecken. Rahmen/StammdatenFenster bleibt unangetastet, deshalb hier ein eigenes,
// bewusst kleines Gegenstück nur für diesen Fall.
public abstract class StammdatenPanel : Panel
{
    // Beide Registerkarten von K02 (Konten, Kategorien) teilen sich einen Bereich für die Rechteprüfung -
    // das ganze Modul wird als eine Einheit freigeschaltet.
    private const string Bereich = "K02";

    protected readonly TextBox Suchfeld = new();
    protected readonly CheckBox GesperrteAnzeigenKontrollkaestchen = new();
    protected readonly ListView Liste = new();
    protected readonly GroupBox Detailgruppe = new();
    protected readonly Button NeuKnopf = new();
    protected readonly Button AendernKnopf = new();
    protected readonly Button SichernKnopf = new();
    protected readonly Button AbbrechenKnopf = new();
    protected readonly Button SperrenKnopf = new();

    private readonly Modulkontext _kontext;
    private bool _sperreListenereignisse;

    protected StammdatenPanel(Modulkontext kontext)
    {
        _kontext = kontext;
        Dock = DockStyle.Fill;

        var suchBeschriftung = new Label
        {
            Text = "&Suchen:",
            Location = new Point(8, 10),
            Size = new Size(56, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Suchfeld.Location = new Point(66, 7);
        Suchfeld.Size = new Size(200, 20);
        Suchfeld.Font = new Font("Courier New", 9f);
        Suchfeld.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                Suchen();
            }
        };

        GesperrteAnzeigenKontrollkaestchen.Text = "&Gesperrte anzeigen";
        GesperrteAnzeigenKontrollkaestchen.Location = new Point(280, 8);
        GesperrteAnzeigenKontrollkaestchen.Size = new Size(160, 20);
        GesperrteAnzeigenKontrollkaestchen.CheckedChanged += (_, _) => Suchen();

        Liste.Location = new Point(8, 36);
        Liste.Size = new Size(548, 130);
        Liste.View = View.Details;
        Liste.FullRowSelect = true;
        Liste.GridLines = true;
        Liste.MultiSelect = false;
        Liste.HideSelection = false;
        Liste.Font = Font;
        Liste.SelectedIndexChanged += (_, _) => ListenAuswahlGeaendert();

        Detailgruppe.Text = "Details";
        Detailgruppe.Location = new Point(8, 174);
        Detailgruppe.Size = new Size(548, 140);

        var buttonY = 322;

        NeuKnopf.Text = "&Neu";
        NeuKnopf.Location = new Point(8, buttonY);
        NeuKnopf.Size = new Size(96, 24);
        NeuKnopf.Click += (_, _) => Neu();

        AendernKnopf.Text = "&Ändern";
        AendernKnopf.Location = new Point(112, buttonY);
        AendernKnopf.Size = new Size(96, 24);
        AendernKnopf.Click += (_, _) => AendernStarten();

        SichernKnopf.Text = "&Sichern";
        SichernKnopf.Location = new Point(216, buttonY);
        SichernKnopf.Size = new Size(96, 24);
        SichernKnopf.Click += (_, _) => Sichern();

        AbbrechenKnopf.Text = "Abb&rechen";
        AbbrechenKnopf.Location = new Point(320, buttonY);
        AbbrechenKnopf.Size = new Size(96, 24);
        AbbrechenKnopf.Click += (_, _) => Abbrechen();

        SperrenKnopf.Text = "&Sperren";
        SperrenKnopf.Location = new Point(424, buttonY);
        SperrenKnopf.Size = new Size(96, 24);
        SperrenKnopf.Click += (_, _) => SperrenOderEntsperren();

        Controls.Add(suchBeschriftung);
        Controls.Add(Suchfeld);
        Controls.Add(GesperrteAnzeigenKontrollkaestchen);
        Controls.Add(Liste);
        Controls.Add(Detailgruppe);
        Controls.Add(NeuKnopf);
        Controls.Add(AendernKnopf);
        Controls.Add(SichernKnopf);
        Controls.Add(AbbrechenKnopf);
        Controls.Add(SperrenKnopf);
    }

    protected Modulkontext Kontext => _kontext;

    protected Dienste Dienste => _kontext.Dienste;

    protected IMeldungsanzeige Meldungen => _kontext.Meldungen;

    protected int MandantNr => _kontext.MandantNr;

    protected MaskenModus Modus { get; private set; } = MaskenModus.Anzeigen;

    protected object? AusgewaehlterEintrag =>
        Liste.SelectedItems.Count > 0 ? Liste.SelectedItems[0].Tag : null;

    public bool HatUngesicherteEingaben => MaskenModi.IstSchreibend(Modus);

    public void SichernVersuchen() => Sichern();

    public void Initialisieren()
    {
        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    protected void SetzeModus(MaskenModus modus)
    {
        if (MaskenModi.IstSchreibend(modus) && !DarfMindestens(Stufe.Aendern))
        {
            Meldungen.Fehler($"Keine Berechtigung: {Bereich} erfordert mindestens Stufe {Stufen.Bezeichnung(Stufe.Aendern)}.");
            return;
        }

        Modus = modus;

        var schreibend = MaskenModi.IstSchreibend(modus);

        Suchfeld.Enabled = !schreibend;
        GesperrteAnzeigenKontrollkaestchen.Enabled = !schreibend;
        Liste.Enabled = !schreibend;

        NeuKnopf.Enabled = !schreibend;
        AendernKnopf.Enabled = !schreibend && AusgewaehlterEintrag is not null;
        SperrenKnopf.Enabled = !schreibend && AusgewaehlterEintrag is not null;
        SichernKnopf.Enabled = schreibend;
        AbbrechenKnopf.Enabled = schreibend;

        SetzeDetailfelderSchreibbar(modus);
    }

    private void ListenAuswahlGeaendert()
    {
        if (_sperreListenereignisse || Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        var eintrag = AusgewaehlterEintrag;

        AendernKnopf.Enabled = eintrag is not null;
        SperrenKnopf.Enabled = eintrag is not null;

        if (eintrag is null)
        {
            LeereDetailfelder();
        }
        else
        {
            ZeigeEintrag(eintrag);
        }
    }

    private void Suchen()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        _sperreListenereignisse = true;
        try
        {
            Liste.Items.Clear();
            foreach (var eintrag in SucheEintraege(Suchfeld.Text, GesperrteAnzeigenKontrollkaestchen.Checked))
            {
                Liste.Items.Add(eintrag);
            }
        }
        finally
        {
            _sperreListenereignisse = false;
        }

        AendernKnopf.Enabled = false;
        SperrenKnopf.Enabled = false;
        LeereDetailfelder();
    }

    private void Neu()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        LeereDetailfelder();
        SetzeModus(MaskenModus.Erfassen);
        FokusiereErstesFeld();
    }

    private void AendernStarten()
    {
        if (Modus != MaskenModus.Anzeigen || AusgewaehlterEintrag is null)
        {
            return;
        }

        SetzeModus(MaskenModus.Aendern);
        FokusiereErstesFeld();
    }

    private void Sichern()
    {
        if (!MaskenModi.IstSchreibend(Modus))
        {
            return;
        }

        var fehler = ValidiereUndSichere(out var erfolgsmeldung);

        if (fehler.Count > 0)
        {
            Meldungen.Fehler(fehler[0]);
            return;
        }

        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
        Meldungen.Erfolg(erfolgsmeldung);
    }

    private void Abbrechen()
    {
        if (!MaskenModi.IstSchreibend(Modus))
        {
            return;
        }

        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    private void SperrenOderEntsperren()
    {
        if (Modus != MaskenModus.Anzeigen || AusgewaehlterEintrag is null)
        {
            return;
        }

        if (!DarfMindestens(Stufe.Voll))
        {
            Meldungen.Fehler($"Keine Berechtigung: {Bereich} erfordert mindestens Stufe {Stufen.Bezeichnung(Stufe.Voll)}.");
            return;
        }

        FuehreSperrenOderEntsperrenAus(out var nachricht);
        Suchen();
        Meldungen.Erfolg(nachricht);
    }

    private bool DarfMindestens(Stufe erforderlich) =>
        Kontext.Dienste.Zugriff.WirksameStufe(MandantNr, Bereich) is { } stufe && stufe >= erforderlich;

    protected abstract IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte);
    protected abstract void ZeigeEintrag(object eintrag);
    protected abstract void LeereDetailfelder();
    protected abstract void SetzeDetailfelderSchreibbar(MaskenModus modus);
    protected abstract IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung);
    protected abstract void FokusiereErstesFeld();
    protected abstract void FuehreSperrenOderEntsperrenAus(out string nachricht);
}
