using ContadorBaus.Ui;

namespace ContadorBaus.Pages;

/// <summary>Modos: partidas, baús, média e vitórias em cada modo, com a parte de cada um no total.</summary>
internal sealed class ModesPage : Page
{
    private readonly Canvas _summary = new() { Dock = DockStyle.Top, Height = 64 };
    private readonly Ledger _table = new(36) { Dock = DockStyle.Top, AutoHeight = true };
    private List<ModeStats> _rows = new();
    private int _total;

    public override string Title => T("nav.modes");

    public ModesPage(AppState state) : base(state)
    {
        _table.Columns.Add(T("col.mode"), 220);
        _table.Columns.Add(T("col.matches"), 80, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.chests"), 80, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.avg"), 76, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.wins"), 76, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.share"), 200);
        _table.CellColor = (_, col) => col switch { 2 => Theme.Gold, 1 or 3 or 4 => Theme.Muted, _ => null };
        _table.CellPainter = (g, r, item, col) =>
        {
            if (col != 5 || item.Tag is not ModeStats s) return false;
            Theme.HighQuality(g);
            var share = _total == 0 ? 0 : (float)s.Chests / _total;
            var bar = new RectangleF(r.X + 4, r.Y + r.Height / 2f - 3, Math.Max(0, r.Width - 56), 6);
            Theme.Fill(g, Theme.Hover, bar, 3);
            if (share > 0) Theme.Fill(g, Theme.Gold, new RectangleF(bar.X, bar.Y, Math.Max(6, bar.Width * share), bar.Height), 3);
            Theme.Text_(g, share.ToString("0%", Pt), Theme.Small, Theme.Muted, new Rectangle((int)bar.Right + 6, r.Y, 46, r.Height), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            return true;
        };
        _summary.Paint += PaintSummary;
        Controls.Add(_table);
        Controls.Add(_summary);
        Resize += (_, _) => _table.FitHeight();
    }

    protected override void Render()
    {
        _rows = Stats.ByMode(State.Period, State.Options);
        _total = _rows.Sum(r => r.Chests);
        _table.BeginUpdate();
        _table.Items.Clear();
        foreach (var r in _rows)
        {
            var item = new ListViewItem(r.Mode) { Tag = r };
            item.SubItems.Add(Lang.N(r.Matches));
            item.SubItems.Add(Lang.N(r.Chests));
            item.SubItems.Add(r.Average.ToString("0.0", Pt));
            item.SubItems.Add(Lang.N(r.Wins));
            item.SubItems.Add("");
            _table.Items.Add(item);
        }
        _table.EndUpdate();
        _table.FitHeight();
        _summary.Invalidate();
    }

    private void PaintSummary(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        if (_rows.Count == 0)
        {
            Theme.Text_(g, T("matches.noneInPeriod"), Theme.Body, Theme.Faint, new Rectangle(0, 0, _summary.Width, 40));
            return;
        }
        var top = _rows[0];
        var bestAvg = _rows.OrderByDescending(r => r.Average).First();
        Theme.Text_(g, F("modes.top", top.Mode), Theme.Heading, Theme.Text, new Rectangle(0, 0, _summary.Width, 24));
        var line = F("modes.inPeriod", Plural(_rows.Count, "u.mode"));
        if (_rows.Count > 1) line += "  ·  " + F("modes.bestAvg", bestAvg.Mode, Lang.N1(bestAvg.Average));
        Theme.Text_(g, line, Theme.Body, Theme.Muted, new Rectangle(0, 26, _summary.Width, 22));
    }
}

/// <summary>Equipa: baús da equipa (tu + colegas) e cada colega com quem jogaste.</summary>
internal sealed class TeamPage : Page
{
    private readonly Canvas _summary = new() { Dock = DockStyle.Top, Height = 118 };
    private readonly Ledger _table = new(34) { Dock = DockStyle.Top, AutoHeight = true };
    private int _team, _mine, _mates, _matches;
    private float _shownTeam;

    public override string Title => T("nav.team");

    public TeamPage(AppState state) : base(state)
    {
        _table.Columns.Add(T("col.mate"), 200);
        _table.Columns.Add(T("col.together"), 124, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.chests"), 70, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.avg"), 76, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.winsTogether"), 124, HorizontalAlignment.Right);
        _table.Columns.Add(T("col.lastTime"), 110, HorizontalAlignment.Right);
        _table.CellColor = (_, col) => col switch { 2 => Theme.Gold, 0 => null, _ => Theme.Muted };
        _summary.Paint += PaintSummary;
        Controls.Add(_table);
        Controls.Add(_summary);
        Resize += (_, _) => _table.FitHeight();
    }

    protected override void Render()
    {
        var withMates = State.Period.Where(m => m.Teammates.Count > 0).ToList();
        _matches = withMates.Count;
        _mine = withMates.Sum(State.Count);
        _team = withMates.Sum(m => Stats.TeamCount(m, State.Options));
        _mates = _team - _mine;
        Anim.To(_summary, "n", _shownTeam, _team, 650, v => { _shownTeam = v; _summary.Invalidate(); });

        _table.BeginUpdate();
        _table.Items.Clear();
        foreach (var t in Stats.ByTeammate(State.Period, State.Options))
        {
            var item = new ListViewItem(t.Name) { Tag = t };
            item.SubItems.Add(Lang.N(t.MatchesTogether));
            item.SubItems.Add(Lang.N(t.Chests));
            item.SubItems.Add(t.Average.ToString("0.0", Pt));
            item.SubItems.Add(Lang.N(t.WinsTogether));
            item.SubItems.Add(t.LastPlayedLocal.Date == DateTime.Today ? T("today.lower") : Lang.DM(t.LastPlayedLocal));
            _table.Items.Add(item);
        }
        _table.EndUpdate();
        _table.FitHeight();
        _summary.Invalidate();
    }

    private void PaintSummary(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        var w = _summary.Width;
        if (_matches == 0)
        {
            Theme.Text_(g, T("team.none"), Theme.Body, Theme.Faint, new Rectangle(0, 0, w, 40));
            return;
        }
        var n = Lang.N((int)Math.Round(_shownTeam));
        var size = Theme.Measure(n, Theme.Number(40));
        Theme.Text_(g, n, Theme.Number(40), Theme.Gold, new Rectangle(-3, 0, size.Width + 30, 60), TextFormatFlags.Left | TextFormatFlags.Top);
        Theme.Text_(g, T("team.chests"), Theme.Ui(10.5f), Theme.Muted, new Rectangle(size.Width + 10, 28, 200, 24), TextFormatFlags.Left | TextFormatFlags.Top);
        Theme.Text_(g, F("team.split", Lang.N(_mine), Lang.N(_mates), Plural(_matches, "u.match")),
            Theme.Body, Theme.Text, new Rectangle(0, 62, w, 22));
        Theme.Text_(g, T("team.caveat"),
            Theme.Small, Theme.Faint, new Rectangle(0, 86, w, 20));
    }
}
