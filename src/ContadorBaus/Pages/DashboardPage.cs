using ContadorBaus.Ui;

namespace ContadorBaus.Pages;

/// <summary>Painel: o total do período, o registo do dia e médias, o ritmo da última partida e os baús por dia.</summary>
internal sealed class DashboardPage : Page
{
    private readonly HeroLedger _hero = new() { Dock = DockStyle.Top, Height = 124 };
    private readonly Canvas _lastHeader = new() { Dock = DockStyle.Top, Height = 34 };
    private readonly LootStrip _strip = new() { Dock = DockStyle.Top, Height = 84, EmptyText = Lang.T("dash.empty") };
    private readonly DayBars _bars = new() { Dock = DockStyle.Fill };
    private MatchRecord? _last;

    public override string Title => T("nav.dashboard");

    public DashboardPage(AppState state) : base(state)
    {
        _lastHeader.Paint += PaintLastHeader;
        var gap1 = new Panel { Dock = DockStyle.Top, Height = 18 };
        var gap2 = new Panel { Dock = DockStyle.Top, Height = 22 };
        Controls.Add(_bars);
        Controls.Add(gap2);
        Controls.Add(_strip);
        Controls.Add(_lastHeader);
        Controls.Add(gap1);
        Controls.Add(_hero);
    }

    protected override void Render()
    {
        var period = State.Period;
        var total = period.Sum(State.Count);
        var today = period.Where(m => m.StartLocal.Date == DateTime.Today).ToList();
        var best = period.OrderByDescending(State.Count).ThenBy(m => m.StartUtc).FirstOrDefault();
        var start = State.StartDate;

        _hero.SetValues(
            total,
            start == null
                ? (State.AllValid.Count == 0 ? T("dash.noneYet") : F("dash.sinceFirst", Lang.DMY(State.EarliestDay)))
                : start.Value == DateTime.Today ? T("dash.today") : F("dash.since", Lang.DMY(start.Value)),
            new[]
            {
                (Lang.N(today.Sum(State.Count)), today.Count == 0 ? T("dash.todayNone") : F("dash.todayIn", Plural(today.Count, "u.match"))),
                (Lang.N(period.Count), F("dash.matches", Plural(period.Count(m => m.IsWin), "u.win"))),
                (period.Count == 0 ? "—" : ((double)total / period.Count).ToString("0.0", Pt), T("dash.perMatch")),
                (best == null ? "—" : State.Count(best).ToString(Pt), best == null ? T("dash.best") : F("dash.bestOn", Lang.DM(best.StartLocal))),
            });

        _last = period.FirstOrDefault();
        _strip.SetMatch(_last, State.Options);
        _lastHeader.Invalidate();

        var from = (start ?? State.EarliestDay).Date;
        if ((DateTime.Today - from).TotalDays > 29) from = DateTime.Today.AddDays(-29);
        _bars.SetData(period.Count == 0 ? new() : Stats.ByDay(period, State.Options, from, DateTime.Today));
    }

    private void PaintLastHeader(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.Text_(g, T("dash.last"), Theme.Heading, Theme.Text, new Rectangle(0, 0, 220, 26));
        if (_last == null) return;
        var meta = $"{Lang.ShortDay(_last.StartLocal)} {Lang.DM(_last.StartLocal)}, {_last.StartLocal:HH:mm}  ·  {Modes.Friendly(_last.Playlist)}  ·  {Placement(_last)}  ·  {Plural(State.Count(_last), "u.chest")}";
        Theme.Text_(g, meta, Theme.Body, Theme.Muted, new Rectangle(220, 0, _lastHeader.Width - 220, 26), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>O número do período (grande, dourado, conta até ao valor) e o registo de quatro valores ao lado.</summary>
internal sealed class HeroLedger : Canvas
{
    private float _shown;
    private int _total;
    private string _caption = "";
    private (string Value, string Label)[] _items = Array.Empty<(string, string)>();

    public void SetValues(int total, string caption, (string, string)[] items)
    {
        _caption = caption;
        _items = items;
        if (total != _total)
        {
            _total = total;
            Anim.To(this, "n", _shown, total, 650, v => { _shown = v; Invalidate(); });
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        var number = Lang.N((int)Math.Round(_shown));
        var numberFont = Theme.Number(50);
        var size = Theme.Measure(number, numberFont);
        Theme.Text_(g, number, numberFont, Theme.Gold, new Rectangle(-4, 0, size.Width + 40, 78), TextFormatFlags.Left | TextFormatFlags.Top);
        Theme.Text_(g, _caption, Theme.Ui(10.5f), Theme.Muted, new Rectangle(0, 82, Width / 2, 24), TextFormatFlags.Left | TextFormatFlags.Top);

        // Registo à direita: quatro colunas separadas por linhas finas, valor por cima e legenda por baixo.
        var left = Math.Max(size.Width + 48, (int)(Width * 0.42));
        var colW = (Width - left) / Math.Max(1, _items.Length);
        for (var i = 0; i < _items.Length; i++)
        {
            var x = left + i * colW;
            Theme.VLine(g, x, 14, 100);
            Theme.Text_(g, _items[i].Value, Theme.Number(22), Theme.Text, new Rectangle(x + 16, 18, colW - 20, 40), TextFormatFlags.Left | TextFormatFlags.Top);
            Theme.Text_(g, _items[i].Label, Theme.Small, Theme.Muted, new Rectangle(x + 16, 62, colW - 20, 36), TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak);
        }
    }
}
