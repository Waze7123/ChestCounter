using System.Globalization;

namespace ContadorBaus.Ui;

/// <summary>Barras com os baús de cada dia; crescem ao mudar o período.</summary>
internal sealed class DayBars : Canvas
{
    private List<DayStats> _days = new();
    private float _grow = 1;
    private int _hover = -1;

    public void SetData(List<DayStats> days)
    {
        var changed = days.Count != _days.Count || !days.Select(d => d.Chests).SequenceEqual(_days.Select(d => d.Chests));
        _days = days;
        _hover = -1;
        if (changed) Anim.To(this, "grow", 0.15f, 1, 520, v => { _grow = v; Invalidate(); });
        else Invalidate();
    }

    private RectangleF Plot => new(4, 30, Width - 8, Height - 30 - 24);

    private int IndexAt(Point p)
    {
        if (_days.Count == 0) return -1;
        var slot = Plot.Width / _days.Count;
        var i = (int)((p.X - Plot.X) / slot);
        return i >= 0 && i < _days.Count ? i : -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var i = IndexAt(e.Location);
        if (i != _hover) { _hover = i; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        Theme.Text_(g, Lang.T("chart.title"), Theme.Heading, Theme.Text, new Rectangle(0, 0, 200, 22));

        if (_days.Count == 0 || _days.All(d => d.Matches == 0))
        {
            Theme.Text_(g, Lang.T("matches.noneInPeriod"), Theme.Body, Theme.Faint, ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var hoverText = _hover >= 0
            ? $"{_days[_hover].Day.ToString("dddd", Lang.Culture)}, {Lang.DM(_days[_hover].Day)} · {Lang.Plural(_days[_hover].Chests, "u.chest")} · {Lang.Plural(_days[_hover].Matches, "u.match")}"
            : "";
        Theme.Text_(g, hoverText, Theme.Small, Theme.Muted, new Rectangle(200, 0, Width - 200, 22), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

        var plot = Plot;
        Theme.HLine(g, plot.Left, plot.Right, plot.Bottom + 0.5f);
        var max = Math.Max(1, _days.Max(d => d.Chests));
        var slot = plot.Width / _days.Count;
        var barW = Math.Clamp(slot * 0.56f, 3, 34);
        var every = Math.Max(1, (int)Math.Ceiling(44 / slot));
        for (var i = 0; i < _days.Count; i++)
        {
            var d = _days[i];
            var x = plot.X + slot * i + (slot - barW) / 2;
            var h = d.Chests == 0 ? 0 : Math.Max(3, (plot.Height - 16) * d.Chests / max * _grow);
            var hot = i == _hover;
            var today = d.Day == DateTime.Today;
            if (h > 0)
            {
                var bar = new RectangleF(x, plot.Bottom - h, barW, h);
                var color = hot ? Theme.GoldBright : today ? Theme.Gold : Theme.Mix(Theme.GoldDeep, Theme.Gold, 0.72f);
                Theme.Fill(g, color, bar, Math.Min(3, barW / 3));
                if (slot >= 20 || hot)
                {
                    var s = Lang.N(d.Chests);
                    Theme.Text_(g, s, Theme.Number(8.5f, false), hot ? Theme.GoldBright : Theme.Muted,
                        new Rectangle((int)(x + barW / 2 - 30), (int)(bar.Y - 18), 60, 16), TextFormatFlags.HorizontalCenter | TextFormatFlags.Bottom);
                }
            }
            if (i % every == 0 || i == _days.Count - 1 || hot)
            {
                var label = today ? Lang.T("period.today") : Lang.DM(d.Day);
                Theme.Text_(g, label, Theme.Small, hot || today ? Theme.Text : Theme.Faint,
                    new Rectangle((int)(x + barW / 2 - 45), (int)plot.Bottom + 4, 90, 18), TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            }
        }
    }
}

/// <summary>
/// O "ritmo" de uma partida: uma linha do tempo com um losango dourado por baú e uma marca vazia por contentor extra.
/// Em modo compacto (linhas da lista) não tem eixo nem etiquetas.
/// </summary>
internal sealed class LootStrip : Canvas
{
    private MatchRecord? _match;
    private CountingOptions _options = CountingOptions.ChestsOnly;
    private float _reveal = 1;
    private int _hover = -1;
    private List<(ChestOpen Open, PointF At)> _marks = new();

    public bool Compact { get; set; }
    public string EmptyText { get; set; } = Lang.T("strip.none");

    public void SetMatch(MatchRecord? match, CountingOptions options)
    {
        var changed = !ReferenceEquals(match, _match);
        _match = match;
        _options = options;
        _hover = -1;
        if (changed) Anim.To(this, "reveal", 0, 1, 700, v => { _reveal = v; Invalidate(); });
        else Invalidate();
    }

    private RectangleF Track => Compact ? new RectangleF(0, 0, Width, Height) : new RectangleF(10, 26, Width - 20, Height - 26 - 22);

    /// <summary>Desenha o ritmo num retângulo qualquer (usado pelas linhas da lista de partidas).</summary>
    public static void Draw(Graphics g, RectangleF track, MatchRecord m, CountingOptions o, Color lineColor, float reveal = 1, List<(ChestOpen, PointF)>? marks = null, int hover = -1)
    {
        var duration = Math.Max(60f, Math.Max(m.DurationSeconds, m.Chests.Concat(m.Extras).Select(c => c.TimeSeconds).DefaultIfEmpty(0).Max() + 5));
        var cy = track.Y + track.Height / 2;
        using (var pen = new Pen(lineColor, 1)) g.DrawLine(pen, track.X, cy, track.Right, cy);

        var compact = track.Height < 24;
        var r = compact ? 3.2f : Math.Clamp(track.Height * 0.32f, 3, 6.5f);
        var extras = m.Extras.Select(x => (x, counted: o.Counts(x.Kind)));
        var all = m.Chests.Select(c => (c, counted: true)).Concat(extras).OrderBy(x => x.Item1.TimeSeconds).ToList();
        var lastX = float.MinValue;
        var stack = 0;
        var index = 0;
        foreach (var (open, counted) in all)
        {
            var frac = open.TimeSeconds / duration;
            if (frac > reveal) break;
            var x = track.X + track.Width * frac;
            stack = !compact && x - lastX < r * 1.4f ? stack + 1 : 0;
            lastX = x;
            var dy = stack == 0 ? 0 : (stack % 2 == 1 ? -1 : 1) * r * 2.3f * ((stack + 1) / 2);
            var at = new PointF(x, Math.Clamp(cy + dy, track.Y + r, track.Bottom - r));
            marks?.Add((open, at));
            var hot = index++ == hover;
            var size = hot ? r * 1.5f : r;
            if (ChestKinds.IsChest(open.Kind))
            {
                using var b = new SolidBrush(hot ? Theme.GoldBright : Theme.KindColor(open.Kind));
                Icons.FillDiamond(g, b, at.X, at.Y, size);
            }
            else
            {
                using var p = new Pen(counted ? Theme.Gold : Theme.Faint, 1.2f);
                Icons.DrawDiamond(g, p, at.X, at.Y, size * 0.8f);
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (Compact) return;
        var best = -1;
        var bestD = 12f;
        for (var i = 0; i < _marks.Count; i++)
        {
            var d = Math.Abs(_marks[i].At.X - e.X) + Math.Abs(_marks[i].At.Y - e.Y) * 0.5f;
            if (d < bestD) { bestD = d; best = i; }
        }
        if (best != _hover) { _hover = best; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        if (_match == null)
        {
            Theme.Text_(g, EmptyText, Theme.Body, Theme.Faint, ClientRectangle, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }
        var track = Track;
        _marks = new();
        Draw(g, track, _match, _options, Theme.Line, _reveal, _marks, _hover);
        if (Compact) return;

        // Eixo: marcas a cada 5 minutos.
        var duration = Math.Max(60f, Math.Max(_match.DurationSeconds, _match.Chests.Concat(_match.Extras).Select(c => c.TimeSeconds).DefaultIfEmpty(0).Max() + 5));
        for (var min = 0; min * 60 <= duration; min += 5)
        {
            var x = track.X + track.Width * (min * 60 / duration);
            Theme.VLine(g, x, track.Bottom + 2, track.Bottom + 6, Theme.Line);
            var label = new Rectangle((int)x - 30, (int)track.Bottom + 6, 60, 16);
            var align = TextFormatFlags.HorizontalCenter;
            if (min == 0) { label.X = (int)x - 2; align = TextFormatFlags.Left; }
            Theme.Text_(g, $"{min} min", Theme.Small, Theme.Faint, label, align | TextFormatFlags.Top);
        }

        var caption = Lang.T("strip.hint");
        var color = Theme.Faint;
        if (_hover >= 0 && _hover < _marks.Count)
        {
            var o = _marks[_hover].Open;
            var t = TimeSpan.FromSeconds(o.TimeSeconds);
            caption = $"{(int)t.TotalMinutes:00}:{t.Seconds:00}  ·  {Lang.Kind(o.Kind)}{(o.Destroyed ? " " + Lang.T("open.broken") : "")}";
            color = Theme.GoldBright;
        }
        Theme.Text_(g, caption, Theme.Small, color, new Rectangle(0, 0, Width, 20), TextFormatFlags.Right | TextFormatFlags.Top);
    }
}

/// <summary>Seletor de data: botão com a data e um calendário escuro que abre por baixo.</summary>
internal sealed class DateField : Control
{
    private readonly GoldButton _button;
    private DateTime _value = DateTime.Today;
    public event Action<DateTime>? Picked;
    /// <summary>Dias com partidas (marcados com um ponto no calendário).</summary>
    public HashSet<DateTime> MarkedDays { get; set; } = new();

    public DateField()
    {
        _button = new GoldButton("00/00/0000", GoldButton.Style.Ghost, Icons.Kind.Calendar) { TrailingIcon = Icons.Kind.ChevronDown, Height = 28 };
        _button.AutoSizeToText();
        _button.Click += (_, _) => Open();
        Size = _button.Size;
        Controls.Add(_button);
        BackColor = Theme.Ground;
    }

    public DateTime Value
    {
        get => _value;
        set { _value = value.Date; _button.Text = Lang.DMY(_value); _button.AutoSizeToText(); Size = _button.Size; }
    }

    private void Open()
    {
        var cal = new CalendarView(_value, MarkedDays) { Size = new Size(252, 262) };
        var host = new ToolStripControlHost(cal) { Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = false, Size = cal.Size };
        var drop = new ToolStripDropDown { Padding = new Padding(1), BackColor = Theme.Line, Renderer = new DarkMenuRenderer() };
        drop.Items.Add(host);
        cal.Picked += d =>
        {
            drop.Close();
            Value = d;
            Picked?.Invoke(d);
        };
        drop.Opacity = 0;
        drop.Show(this, new Point(0, Height + 4));
        Anim.To(cal, "fade", 0, 1, 200, v => { if (!drop.IsDisposed) drop.Opacity = v; });
        cal.Focus();
    }
}

/// <summary>Calendário de um mês, desenhado no tema. Dias futuros desativados.</summary>
internal sealed class CalendarView : Canvas
{
    private DateTime _month;
    private DateTime _selected;
    private readonly HashSet<DateTime> _marked;
    private DateTime? _hot;
    public event Action<DateTime>? Picked;

    public CalendarView(DateTime selected, HashSet<DateTime> marked)
    {
        _selected = selected.Date;
        _month = new DateTime(selected.Year, selected.Month, 1);
        _marked = marked;
        BackColor = Theme.Surface;
        TabStop = true;
    }

    private RectangleF Cell(int row, int col) => new(10 + col * 33, 70 + row * 30, 32, 28);
    private static RectangleF PrevBtn => new(8, 8, 28, 28);
    private RectangleF NextBtn => new(Width - 36, 8, 28, 28);

    private IEnumerable<(DateTime Day, RectangleF R)> Days()
    {
        // Semana a começar à segunda-feira.
        var offset = ((int)_month.DayOfWeek + 6) % 7;
        var days = DateTime.DaysInMonth(_month.Year, _month.Month);
        for (var d = 0; d < days; d++)
        {
            var idx = d + offset;
            yield return (_month.AddDays(d), Cell(idx / 7, idx % 7));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        DateTime? hot = null;
        foreach (var (day, r) in Days()) if (r.Contains(e.Location) && day <= DateTime.Today) hot = day;
        var onButton = PrevBtn.Contains(e.Location) || (NextBtn.Contains(e.Location) && _month.AddMonths(1) <= DateTime.Today);
        Cursor = hot != null || onButton ? Cursors.Hand : Cursors.Default;
        if (hot != _hot) { _hot = hot; Invalidate(); }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (PrevBtn.Contains(e.Location)) { _month = _month.AddMonths(-1); Invalidate(); return; }
        if (NextBtn.Contains(e.Location) && _month.AddMonths(1) <= DateTime.Today) { _month = _month.AddMonths(1); Invalidate(); return; }
        foreach (var (day, r) in Days())
        {
            if (r.Contains(e.Location) && day <= DateTime.Today) { Picked?.Invoke(day); return; }
        }
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var delta = e.KeyCode switch { Keys.Left => -1, Keys.Right => 1, Keys.Up => -7, Keys.Down => 7, _ => 0 };
        if (delta != 0)
        {
            var d = _selected.AddDays(delta);
            if (d <= DateTime.Today) { _selected = d; _month = new DateTime(d.Year, d.Month, 1); Invalidate(); }
        }
        else if (e.KeyCode == Keys.Enter) Picked?.Invoke(_selected);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        var title = Lang.MonthYear(_month);
        Theme.Text_(g, title, Theme.Strong, Theme.Text, new Rectangle(0, 8, Width, 28), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        Icons.Draw(g, Icons.Kind.ChevronLeft, PrevBtn, Theme.Muted);
        Icons.Draw(g, Icons.Kind.ChevronRight, NextBtn, _month.AddMonths(1) <= DateTime.Today ? Theme.Muted : Theme.Dim);

        var names = Lang.T("cal.initials").Select(c => c.ToString()).ToArray();
        for (var i = 0; i < 7; i++)
        {
            var r = Cell(0, i);
            Theme.Text_(g, names[i], Theme.Small, Theme.Faint, new Rectangle((int)r.X, 44, (int)r.Width, 20), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        foreach (var (day, r) in Days())
        {
            var future = day > DateTime.Today;
            var selected = day == _selected;
            if (selected) Theme.Fill(g, Theme.Gold, r, 6);
            else if (day == _hot) Theme.Fill(g, Theme.Hover, r, 6);
            if (day == DateTime.Today && !selected) Theme.Stroke(g, Theme.GoldDeep, RectangleF.Inflate(r, -0.5f, -0.5f), 6);
            var color = selected ? Theme.OnGold : future ? Theme.Dim : Theme.Text;
            Theme.Text_(g, day.Day.ToString(), selected ? Theme.Strong : Theme.Body, color, Rectangle.Round(r), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            if (_marked.Contains(day))
            {
                using var b = new SolidBrush(selected ? Theme.OnGold : Theme.Gold);
                g.FillEllipse(b, r.X + r.Width / 2 - 1.5f, r.Bottom - 5, 3, 3);
            }
        }
    }
}
