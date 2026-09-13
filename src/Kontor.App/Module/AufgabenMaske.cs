using Kontor.App.Rahmen;
using Kontor.Core.Modell;

namespace Kontor.App.Module;

public sealed class AufgabenMaske : ModulFenster
{
    private readonly ListView Liste = new();
    private readonly CheckBox NurOffeneFeld = new() { Text = "&Nur offene anzeigen", Checked = true };

    private readonly TextBox BetreffFeld = new();
    private readonly TextBox TextFeld = new();
    private readonly ComboBox PrioritaetFeld = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox HatFaelligkeitFeld = new() { Text = "&Fälligkeit:" };
    private readonly DateTimePicker FaelligkeitFeld = new() { Format = DateTimePickerFormat.Short };
    private readonly CheckBox ErledigtFeld = new() { Text = "&Erledigt" };

    private readonly Button NeuKnopf = new() { Text = "&Neu" };
    private readonly Button AendernKnopf = new() { Text = "&Ändern" };
    private readonly Button SichernKnopf = new() { Text = "&Sichern" };
    private readonly Button AbbrechenKnopf = new() { Text = "Abb&rechen" };

    private Notiz? _geladeneNotiz;

    public AufgabenMaske(Modulkontext kontext) : base("K05", "Aufgaben und Notizen", kontext)
    {
        ClientSize = new Size(560, 420);

        NurOffeneFeld.Location = new Point(12, 12);
        NurOffeneFeld.Size = new Size(160, 20);
        NurOffeneFeld.CheckedChanged += (_, _) => Suchen();

        Liste.Location = new Point(12, 36);
        Liste.Size = new Size(536, 140);
        Liste.View = View.Details;
        Liste.FullRowSelect = true;
        Liste.GridLines = true;
        Liste.MultiSelect = false;
        Liste.HideSelection = false;
        Liste.Font = Font;
        Liste.Columns.Add("Betreff", 240);
        Liste.Columns.Add("Priorität", 80);
        Liste.Columns.Add("Fälligkeit", 90);
        Liste.Columns.Add("Erledigt", 80);
        Liste.SelectedIndexChanged += (_, _) => AuswahlGeaendert();

        var detailgruppe = new GroupBox { Text = "Details", Location = new Point(12, 184), Size = new Size(536, 168) };

        var betreffBeschriftung = new Label { Text = "&Betreff:", Location = new Point(8, 22), Size = new Size(80, 16) };
        BetreffFeld.Location = new Point(96, 19);
        BetreffFeld.Size = new Size(420, 20);

        var textBeschriftung = new Label { Text = "&Text:", Location = new Point(8, 48), Size = new Size(80, 16) };
        TextFeld.Location = new Point(96, 45);
        TextFeld.Size = new Size(420, 44);
        TextFeld.Multiline = true;

        var prioritaetBeschriftung = new Label { Text = "&Priorität:", Location = new Point(8, 98), Size = new Size(80, 16) };
        PrioritaetFeld.Location = new Point(96, 95);
        PrioritaetFeld.Size = new Size(140, 21);
        foreach (Prioritaet p in Enum.GetValues<Prioritaet>())
        {
            PrioritaetFeld.Items.Add(p.ToString());
        }

        HatFaelligkeitFeld.Location = new Point(8, 124);
        HatFaelligkeitFeld.Size = new Size(88, 20);
        HatFaelligkeitFeld.CheckedChanged += (_, _) => FaelligkeitFeld.Enabled = HatFaelligkeitFeld.Checked;
        FaelligkeitFeld.Location = new Point(96, 121);
        FaelligkeitFeld.Size = new Size(120, 20);

        ErledigtFeld.Location = new Point(280, 124);
        ErledigtFeld.Size = new Size(100, 20);

        detailgruppe.Controls.Add(betreffBeschriftung);
        detailgruppe.Controls.Add(BetreffFeld);
        detailgruppe.Controls.Add(textBeschriftung);
        detailgruppe.Controls.Add(TextFeld);
        detailgruppe.Controls.Add(prioritaetBeschriftung);
        detailgruppe.Controls.Add(PrioritaetFeld);
        detailgruppe.Controls.Add(HatFaelligkeitFeld);
        detailgruppe.Controls.Add(FaelligkeitFeld);
        detailgruppe.Controls.Add(ErledigtFeld);

        var buttonY = 360;
        NeuKnopf.Location = new Point(12, buttonY);
        NeuKnopf.Size = new Size(96, 24);
        NeuKnopf.Click += (_, _) => Neu();
        AendernKnopf.Location = new Point(116, buttonY);
        AendernKnopf.Size = new Size(96, 24);
        AendernKnopf.Click += (_, _) => AendernStarten();
        SichernKnopf.Location = new Point(220, buttonY);
        SichernKnopf.Size = new Size(96, 24);
        SichernKnopf.Click += (_, _) => Sichern();
        AbbrechenKnopf.Location = new Point(324, buttonY);
        AbbrechenKnopf.Size = new Size(96, 24);
        AbbrechenKnopf.Click += (_, _) => Abbrechen();

        Controls.Add(NurOffeneFeld);
        Controls.Add(Liste);
        Controls.Add(detailgruppe);
        Controls.Add(NeuKnopf);
        Controls.Add(AendernKnopf);
        Controls.Add(SichernKnopf);
        Controls.Add(AbbrechenKnopf);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
    }

    public override bool HatUngesicherteEingaben => MaskenModi.IstSchreibend(Modus);

    public override void SichernVersuchen() => Sichern();

    protected override void UebernehmeModus(MaskenModus modus)
    {
        var schreibend = MaskenModi.IstSchreibend(modus);

        NurOffeneFeld.Enabled = !schreibend;
        Liste.Enabled = !schreibend;
        NeuKnopf.Enabled = !schreibend;
        AendernKnopf.Enabled = !schreibend && _geladeneNotiz is not null;
        SichernKnopf.Enabled = schreibend;
        AbbrechenKnopf.Enabled = schreibend;

        BetreffFeld.ReadOnly = !schreibend;
        TextFeld.ReadOnly = !schreibend;
        PrioritaetFeld.Enabled = schreibend;
        HatFaelligkeitFeld.Enabled = schreibend;
        FaelligkeitFeld.Enabled = schreibend && HatFaelligkeitFeld.Checked;
        ErledigtFeld.Enabled = schreibend;
    }

    private void Suchen()
    {
        var heute = DateOnly.FromDateTime(DateTime.Today);

        Liste.Items.Clear();
        foreach (var notiz in Dienste.Notizen.Liste(MandantNr, NurOffeneFeld.Checked))
        {
            var zeile = new ListViewItem(notiz.Betreff) { Tag = notiz };
            zeile.SubItems.Add(notiz.Prioritaet.ToString());
            zeile.SubItems.Add(notiz.Faelligkeit?.ToString("yyyy-MM-dd") ?? "");
            zeile.SubItems.Add(notiz.Erledigt ? "ja" : "");

            var ueberfaellig = !notiz.Erledigt && notiz.Faelligkeit is { } faelligkeit && faelligkeit < heute;
            if (ueberfaellig)
            {
                zeile.Font = new Font(Liste.Font, FontStyle.Bold);
            }

            Liste.Items.Add(zeile);
        }

        AendernKnopf.Enabled = false;
        LeereDetailfelder();
    }

    private void AuswahlGeaendert()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        if (Liste.SelectedItems.Count == 0)
        {
            LeereDetailfelder();
            AendernKnopf.Enabled = false;
            return;
        }

        ZeigeNotiz((Notiz)Liste.SelectedItems[0].Tag!);
        AendernKnopf.Enabled = true;
    }

    private void ZeigeNotiz(Notiz notiz)
    {
        _geladeneNotiz = notiz;

        BetreffFeld.Text = notiz.Betreff;
        TextFeld.Text = notiz.Text;
        PrioritaetFeld.SelectedIndex = Array.IndexOf(Enum.GetValues<Prioritaet>(), notiz.Prioritaet);
        HatFaelligkeitFeld.Checked = notiz.Faelligkeit.HasValue;
        FaelligkeitFeld.Value = (notiz.Faelligkeit ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue);
        ErledigtFeld.Checked = notiz.Erledigt;
    }

    private void LeereDetailfelder()
    {
        _geladeneNotiz = null;

        BetreffFeld.Text = "";
        TextFeld.Text = "";
        PrioritaetFeld.SelectedIndex = Array.IndexOf(Enum.GetValues<Prioritaet>(), Prioritaet.Normal);
        HatFaelligkeitFeld.Checked = false;
        FaelligkeitFeld.Value = DateTime.Today;
        ErledigtFeld.Checked = false;
    }

    private void Neu()
    {
        if (Modus != MaskenModus.Anzeigen)
        {
            return;
        }

        LeereDetailfelder();
        SetzeModus(MaskenModus.Erfassen);
        BetreffFeld.Focus();
    }

    private void AendernStarten()
    {
        if (Modus != MaskenModus.Anzeigen || _geladeneNotiz is null)
        {
            return;
        }

        SetzeModus(MaskenModus.Aendern);
        BetreffFeld.Focus();
    }

    private void Sichern()
    {
        if (!MaskenModi.IstSchreibend(Modus))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(BetreffFeld.Text))
        {
            Meldungen.Fehler("Der Betreff muss gesetzt sein.");
            return;
        }

        var warSchonErledigt = _geladeneNotiz?.Erledigt ?? false;
        var jetztErledigt = ErledigtFeld.Checked;

        var notiz = new Notiz
        {
            NotizId = _geladeneNotiz?.NotizId ?? 0,
            MandantNr = MandantNr,
            Betreff = BetreffFeld.Text.Trim(),
            Text = TextFeld.Text.Trim(),
            Prioritaet = Enum.GetValues<Prioritaet>()[PrioritaetFeld.SelectedIndex],
            Erledigt = jetztErledigt,
            Faelligkeit = HatFaelligkeitFeld.Checked ? DateOnly.FromDateTime(FaelligkeitFeld.Value.Date) : null,
            ErledigtAm = jetztErledigt
                ? (warSchonErledigt ? _geladeneNotiz!.ErledigtAm : DateOnly.FromDateTime(DateTime.Today))
                : null,
            Angelegt = _geladeneNotiz?.Angelegt ?? default
        };

        if (notiz.NotizId == 0)
        {
            Dienste.Notizen.Anlegen(notiz);
            Meldungen.Erfolg($"Aufgabe \"{notiz.Betreff}\" angelegt.");
        }
        else
        {
            Dienste.Notizen.Aendern(notiz);
            Meldungen.Erfolg($"Aufgabe \"{notiz.Betreff}\" gesichert.");
        }

        SetzeModus(MaskenModus.Anzeigen);
        Suchen();
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
}
