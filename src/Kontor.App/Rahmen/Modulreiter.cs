namespace Kontor.App.Rahmen;

// Selbst gezeichnete Reiterleiste für die MDI-Arbeitsfläche. WinForms bietet keine eingebaute
// Tab-MDI; der tragfähige Weg ohne externe Bibliothek ist, jedes Kindfenster maximiert und ohne
// eigene Titelleiste zu zeigen und darüber diese Leiste zu legen, die nur anzeigt, welches Fenster
// gerade sichtbar sein soll. Docked Top in Hauptfenster, direkt oberhalb der (automatisch auf den
// Rest der Fläche schrumpfenden) MDI-Arbeitsfläche.
public sealed class Modulreiter : Control
{
    private const int Pfeilbreite = 16;
    private const int Seitenpolsterung = 10;
    private const int Schliessengroesse = 8;
    private const int AbstandVorSchliessen = 8;

    private sealed record Eintrag(string? Code, string Anzeige, bool Schliessbar);

    private readonly List<Eintrag> _eintraege = new() { new Eintrag(null, "Start", false) };
    private readonly List<(Eintrag Eintrag, Rectangle Tab, Rectangle? Schliessen)> _layout = new();
    private readonly ContextMenuStrip _kontextmenue = new();
    private readonly ToolStripMenuItem _schliessenEintrag;

    // Durchgehend GDI+ (Graphics) statt eines Gemischs aus Graphics und TextRenderer verwenden -
    // TextRenderer.DrawText kann bei Control.DrawToBitmap (WM_PRINT) die falsche Textfarbe liefern.
    private readonly StringFormat _textformat = new()
    {
        LineAlignment = StringAlignment.Center,
        Alignment = StringAlignment.Near,
        Trimming = StringTrimming.EllipsisCharacter,
        FormatFlags = StringFormatFlags.NoWrap
    };

    private string? _aktiverCode;
    private string? _kontextmenueCode;
    private int _ersterSichtbarerIndex;
    private bool _linkerPfeilAktiv;
    private bool _rechterPfeilAktiv;
    private Rectangle _linkerPfeil;
    private Rectangle _rechterPfeil;

    public event EventHandler<string?>? TabAktiviert;
    public event EventHandler<string>? TabSchliessenAngefordert;
    public event EventHandler<string?>? AlleAusserDiesemSchliessenAngefordert;

    public string? AktiverCode => _aktiverCode;

    public Modulreiter()
    {
        Height = 24;
        Dock = DockStyle.Top;
        DoubleBuffered = true;
        Font = new Font("MS Sans Serif", 8f);
        SetStyle(ControlStyles.ResizeRedraw, true);

        _schliessenEintrag = new ToolStripMenuItem("&Schließen", null, (_, _) =>
        {
            if (_kontextmenueCode is { } code)
            {
                TabSchliessenAngefordert?.Invoke(this, code);
            }
        });
        var alleAusserEintrag = new ToolStripMenuItem("&Alle außer diesem schließen", null,
            (_, _) => AlleAusserDiesemSchliessenAngefordert?.Invoke(this, _kontextmenueCode));

        _kontextmenue.Items.Add(_schliessenEintrag);
        _kontextmenue.Items.Add(alleAusserEintrag);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kontextmenue.Dispose();
            _textformat.Dispose();
        }

        base.Dispose(disposing);
    }

    public void Oeffnen(string code, string anzeige)
    {
        if (_eintraege.Any(e => e.Code == code))
        {
            return;
        }

        _eintraege.Add(new Eintrag(code, anzeige, true));
        Invalidate();
    }

    public void Schliessen(string code)
    {
        _eintraege.RemoveAll(e => e.Code == code);

        if (_aktiverCode == code)
        {
            _aktiverCode = null;
        }

        Invalidate();
    }

    public void Aktiviere(string? code)
    {
        _aktiverCode = code;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        BerechneLayout(e.Graphics);
        Zeichne(e.Graphics);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (_linkerPfeilAktiv && _linkerPfeil.Contains(e.Location))
        {
            _ersterSichtbarerIndex = Math.Max(0, _ersterSichtbarerIndex - 1);
            Invalidate();
            return;
        }

        if (_rechterPfeilAktiv && _rechterPfeil.Contains(e.Location))
        {
            _ersterSichtbarerIndex = Math.Min(_eintraege.Count - 1, _ersterSichtbarerIndex + 1);
            Invalidate();
            return;
        }

        foreach (var (eintrag, tab, schliessen) in _layout)
        {
            if (!tab.Contains(e.Location))
            {
                continue;
            }

            if (e.Button == MouseButtons.Right)
            {
                _kontextmenueCode = eintrag.Code;
                _schliessenEintrag.Enabled = eintrag.Schliessbar;
                _kontextmenue.Show(this, e.Location);
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            if (schliessen is { } schliessenRechteck && schliessenRechteck.Contains(e.Location) && eintrag.Code is { } schliessbarerCode)
            {
                TabSchliessenAngefordert?.Invoke(this, schliessbarerCode);
                return;
            }

            _aktiverCode = eintrag.Code;
            Invalidate();
            TabAktiviert?.Invoke(this, eintrag.Code);
            return;
        }
    }

    private void BerechneLayout(Graphics g)
    {
        _layout.Clear();

        if (_ersterSichtbarerIndex >= _eintraege.Count)
        {
            _ersterSichtbarerIndex = 0;
        }

        var breiten = _eintraege.Select(e => Breite(g, e)).ToList();

        if (breiten.Sum() <= ClientSize.Width)
        {
            _ersterSichtbarerIndex = 0;
            _linkerPfeilAktiv = false;
            _rechterPfeilAktiv = false;
        }
        else
        {
            _linkerPfeilAktiv = _ersterSichtbarerIndex > 0;

            var restOhnePfeil = ClientSize.Width - (_linkerPfeilAktiv ? Pfeilbreite : 0);
            var restSumme = breiten.Skip(_ersterSichtbarerIndex).Sum();
            _rechterPfeilAktiv = restSumme > restOhnePfeil;
        }

        var x = _linkerPfeilAktiv ? Pfeilbreite : 0;
        var grenze = ClientSize.Width - (_rechterPfeilAktiv ? Pfeilbreite : 0);

        for (var i = _ersterSichtbarerIndex; i < _eintraege.Count; i++)
        {
            var breite = breiten[i];
            if (x + breite > grenze && _layout.Count > 0)
            {
                break;
            }

            var tab = new Rectangle(x, 0, breite, Height);
            Rectangle? schliessen = null;
            if (_eintraege[i].Schliessbar)
            {
                schliessen = new Rectangle(
                    tab.Right - Seitenpolsterung - Schliessengroesse, (Height - Schliessengroesse) / 2,
                    Schliessengroesse, Schliessengroesse);
            }

            _layout.Add((_eintraege[i], tab, schliessen));
            x += breite;
        }

        _linkerPfeil = _linkerPfeilAktiv ? new Rectangle(0, 0, Pfeilbreite, Height) : Rectangle.Empty;
        _rechterPfeil = _rechterPfeilAktiv
            ? new Rectangle(ClientSize.Width - Pfeilbreite, 0, Pfeilbreite, Height)
            : Rectangle.Empty;
    }

    private int Breite(Graphics g, Eintrag eintrag)
    {
        var textbreite = (int)Math.Ceiling(g.MeasureString(eintrag.Anzeige, Font, PointF.Empty, _textformat).Width);
        var breite = Seitenpolsterung + textbreite + Seitenpolsterung;

        if (eintrag.Schliessbar)
        {
            breite += AbstandVorSchliessen + Schliessengroesse;
        }

        return breite;
    }

    private void Zeichne(Graphics g)
    {
        // Kein ClearType-Subpixel-Rendering - passt zum klassischen, kantenscharfen Erscheinungsbild
        // der übrigen Oberfläche und vermeidet Farbsäume an den Buchstaben.
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
        g.Clear(SystemColors.Control);

        using var inaktivPinsel = new SolidBrush(SystemColors.Control);
        using var aktivPinsel = new SolidBrush(SystemColors.Window);
        using var textPinsel = new SolidBrush(SystemColors.ControlText);
        using var randStift = new Pen(SystemColors.ControlDark);

        g.DrawLine(randStift, 0, Height - 1, ClientSize.Width, Height - 1);

        var ersterGezeichnet = true;
        foreach (var (eintrag, tab, schliessen) in _layout)
        {
            var istAktiv = eintrag.Code == _aktiverCode;
            var fuellung = istAktiv ? new Rectangle(tab.X, 0, tab.Width, Height) : new Rectangle(tab.X, 0, tab.Width, Height - 1);
            g.FillRectangle(istAktiv ? aktivPinsel : inaktivPinsel, fuellung);

            g.DrawLine(randStift, tab.Left, 0, tab.Right, 0);
            g.DrawLine(randStift, tab.Right - 1, 0, tab.Right - 1, Height - 1);
            if (ersterGezeichnet)
            {
                g.DrawLine(randStift, tab.Left, 0, tab.Left, Height - 1);
            }

            ersterGezeichnet = false;

            var textbreite = tab.Width - 2 * Seitenpolsterung - (schliessen is null ? 0 : AbstandVorSchliessen + Schliessengroesse);
            var textRechteck = new RectangleF(tab.Left + Seitenpolsterung, 0, Math.Max(0, textbreite), Height);
            g.DrawString(eintrag.Anzeige, Font, textPinsel, textRechteck, _textformat);

            if (schliessen is { } schliessenRechteck)
            {
                g.DrawLine(randStift, schliessenRechteck.Left, schliessenRechteck.Top, schliessenRechteck.Right, schliessenRechteck.Bottom);
                g.DrawLine(randStift, schliessenRechteck.Left, schliessenRechteck.Bottom, schliessenRechteck.Right, schliessenRechteck.Top);
            }
        }

        if (_linkerPfeilAktiv)
        {
            ZeichnePfeil(g, _linkerPfeil, nachLinks: true);
        }

        if (_rechterPfeilAktiv)
        {
            ZeichnePfeil(g, _rechterPfeil, nachLinks: false);
        }
    }

    private void ZeichnePfeil(Graphics g, Rectangle feld, bool nachLinks)
    {
        using var pinsel = new SolidBrush(SystemColors.ControlDarkDark);

        var mitteY = feld.Top + feld.Height / 2;
        var mitteX = feld.Left + feld.Width / 2;
        const int groesse = 4;

        Point[] spitze = nachLinks
            ? new[] { new Point(mitteX + groesse / 2, mitteY - groesse), new Point(mitteX + groesse / 2, mitteY + groesse), new Point(mitteX - groesse / 2, mitteY) }
            : new[] { new Point(mitteX - groesse / 2, mitteY - groesse), new Point(mitteX - groesse / 2, mitteY + groesse), new Point(mitteX + groesse / 2, mitteY) };

        g.FillPolygon(pinsel, spitze);
    }
}
