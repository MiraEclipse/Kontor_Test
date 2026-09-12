namespace Kontor.App.Rahmen;

// Gemeinsames Gerüst für Stammdatenmasken (K01 Kunden, K02 Artikel, künftig weitere):
// Suchfeld + Kontrollkästchen oben, ListView, Detail-GroupBox, Schaltflächenreihe unten.
// Die Fachlogik (Suche, Feldabbildung, Validierung, Speichern, Sperren) bleibt bei den Subklassen;
// hier steht nur die Mechanik aus Moduswechsel, Tastatur und dem Schutz ungesicherter Änderungen.
public abstract class StammdatenFenster : ModulFenster
{
    protected readonly TextBox Suchfeld = new();
    protected readonly CheckBox GesperrteAnzeigenKontrollkaestchen = new();
    protected readonly ListView Liste = new();
    protected readonly GroupBox Detailgruppe = new();
    protected readonly Button NeuKnopf = new();
    protected readonly Button AendernKnopf = new();
    protected readonly Button SichernKnopf = new();
    protected readonly Button AbbrechenKnopf = new();
    protected readonly Button SperrenKnopf = new();

    private bool _geaendert;
    private bool _sperreListenereignisse;

    protected StammdatenFenster(string transaktionscode, string bezeichnung, Modulkontext kontext)
        : base(transaktionscode, bezeichnung, kontext)
    {
        ClientSize = new Size(600, 470);

        var suchBeschriftung = new Label
        {
            Text = "&Suchen:",
            Location = new Point(12, 14),
            Size = new Size(56, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Suchfeld.Location = new Point(70, 11);
        Suchfeld.Size = new Size(200, 20);
        Suchfeld.Font = new Font("Courier New", 9f);
        Suchfeld.TabIndex = 1;
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
        GesperrteAnzeigenKontrollkaestchen.Location = new Point(284, 12);
        GesperrteAnzeigenKontrollkaestchen.Size = new Size(160, 20);
        GesperrteAnzeigenKontrollkaestchen.TabIndex = 2;
        GesperrteAnzeigenKontrollkaestchen.CheckedChanged += (_, _) => Suchen();

        Liste.Location = new Point(12, 40);
        Liste.Size = new Size(560, 140);
        Liste.View = View.Details;
        Liste.FullRowSelect = true;
        Liste.GridLines = true;
        Liste.MultiSelect = false;
        Liste.HideSelection = false;
        Liste.Font = Font;
        Liste.TabIndex = 3;
        Liste.SelectedIndexChanged += (_, _) => ListenAuswahlGeaendert();

        Detailgruppe.Text = "Details";
        Detailgruppe.Location = new Point(12, 186);
        Detailgruppe.Size = new Size(560, 220);
        Detailgruppe.TabIndex = 4;

        var buttonY = ClientSize.Height - 36;

        NeuKnopf.Text = "&Neu";
        NeuKnopf.Location = new Point(12, buttonY);
        NeuKnopf.Size = new Size(96, 24);
        NeuKnopf.TabIndex = 90;
        NeuKnopf.Click += (_, _) => Neu();

        AendernKnopf.Text = "&Ändern";
        AendernKnopf.Location = new Point(116, buttonY);
        AendernKnopf.Size = new Size(96, 24);
        AendernKnopf.TabIndex = 91;
        AendernKnopf.Click += (_, _) => AendernStarten();

        SichernKnopf.Text = "&Sichern";
        SichernKnopf.Location = new Point(220, buttonY);
        SichernKnopf.Size = new Size(96, 24);
        SichernKnopf.TabIndex = 92;
        SichernKnopf.Click += (_, _) => Sichern();

        AbbrechenKnopf.Text = "Abb&rechen";
        AbbrechenKnopf.Location = new Point(324, buttonY);
        AbbrechenKnopf.Size = new Size(96, 24);
        AbbrechenKnopf.TabIndex = 93;
        AbbrechenKnopf.Click += (_, _) => Abbrechen();

        SperrenKnopf.Text = "&Sperren";
        SperrenKnopf.Location = new Point(428, buttonY);
        SperrenKnopf.Size = new Size(96, 24);
        SperrenKnopf.TabIndex = 94;
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

        KeyPreview = true;
        KeyDown += StammdatenTaste;
    }

    protected object? AusgewaehlterEintrag =>
        Liste.SelectedItems.Count > 0 ? Liste.SelectedItems[0].Tag : null;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    protected override void UebernehmeModus(MaskenModus modus)
    {
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

        if (!schreibend)
        {
            _geaendert = false;
        }
    }

    // Von den Detailfeldern der Subklasse bei jeder Eingabe aufzurufen (z.B. TextChanged),
    // damit Abbrechen weiß, ob wirklich etwas verworfen werden würde.
    protected void MarkiereGeaendert()
    {
        if (MaskenModi.IstSchreibend(Modus))
        {
            _geaendert = true;
        }
    }

    private void StammdatenTaste(object? absender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F5:
                e.Handled = true;
                Neu();
                break;
            case Keys.F8:
                e.Handled = true;
                Sichern();
                break;
            case Keys.F12:
                e.Handled = true;
                Abbrechen();
                break;
        }
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
        SetzeVorgeschlageneNummer();
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
            FokusiereFeldFuerFehler(fehler[0]);
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

        if (_geaendert)
        {
            var antwort = MessageBox.Show(
                this,
                "Die Änderungen sind nicht gesichert. Wirklich verwerfen?",
                "KONTOR",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (antwort != DialogResult.Yes)
            {
                return;
            }
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

        FuehreSperrenOderEntsperrenAus(out var nachricht);
        Suchen();
        Meldungen.Erfolg(nachricht);
    }

    protected abstract IEnumerable<ListViewItem> SucheEintraege(string suchbegriff, bool auchGesperrte);
    protected abstract void ZeigeEintrag(object eintrag);
    protected abstract void LeereDetailfelder();
    protected abstract void SetzeVorgeschlageneNummer();
    protected abstract void SetzeDetailfelderSchreibbar(MaskenModus modus);
    protected abstract IReadOnlyList<string> ValidiereUndSichere(out string erfolgsmeldung);
    protected abstract void FokusiereErstesFeld();
    protected abstract void FokusiereFeldFuerFehler(string fehler);
    protected abstract void FuehreSperrenOderEntsperrenAus(out string nachricht);
}
