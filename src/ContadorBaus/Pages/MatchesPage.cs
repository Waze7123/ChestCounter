using ContadorBaus.Ui;

namespace ContadorBaus.Pages;

/// <summary>Partidas: lista com o ritmo de cada uma e o detalhe da partida escolhida (baús, equipa, aberturas).</summary>
internal sealed class MatchesPage : Page
{
    private readonly Ledger _list = new(32) { Dock = DockStyle.Fill };
    private readonly Canvas _detailHead = new() { Dock = DockStyle.Top, Height = 150 };
    private readonly LootStrip _strip = new() { Dock = DockStyle.Top, Height = 80 };
    private readonly Canvas _teamBox = new() { Dock = DockStyle.Top, Height = 0 };
    private readonly Ledger _opens = new(26) { Dock = DockStyle.Fill };
    private MatchRecord? _selected;
    private float _shownCount;

    public override string Title => T("nav.matches");

    public MatchesPage(AppState state) : base(state)
    {
        _list.Columns.Add(T("col.when"), 124);
        _list.Columns.Add(T("col.mode"), 96);
        _list.Columns.Add(T("col.place"), 60, HorizontalAlignment.Right);
        _list.Columns.Add(T("col.chests"), 60, HorizontalAlignment.Right);
        _list.Columns.Add(T("col.rhythm"), 140);
        _list.CellColor = (item, col) => item.Tag is MatchRecord m ? col switch
        {
            0 or 1 => col == 1 ? Theme.Muted : null,
            2 when m.IsWin => Theme.Gold,
            2 => Theme.Muted,
            3 => Theme.Gold,
            _ => null,
        } : null;
        _list.CellPainter = (g, r, item, col) =>
        {
            if (col != 4 || item.Tag is not MatchRecord m) return false;
            Theme.HighQuality(g);
            LootStrip.Draw(g, new RectangleF(r.X + 4, r.Y + 7, r.Width - 8, r.Height - 14), m, State.Options, Theme.Line);
            return true;
        };
        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_list.SelectedItems.Count > 0) ShowDetail(_list.SelectedItems[0].Tag as MatchRecord);
        };

        _opens.Columns.Add(T("col.moment"), 80);
        _opens.Columns.Add(T("col.opening"), 160);
        _opens.CellColor = (item, col) => col == 0 ? Theme.Muted : item.Tag is ChestOpen o ? (ChestKinds.IsChest(o.Kind) ? Theme.KindColor(o.Kind) : State.Options.Counts(o.Kind) ? Theme.Gold : Theme.Faint) : null;

        _detailHead.Paint += PaintDetailHead;
        _teamBox.Paint += PaintTeam;

        var detail = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24, 0, 0, 0), BackColor = Theme.Ground };
        detail.Controls.Add(_opens);
        detail.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        detail.Controls.Add(_teamBox);
        detail.Controls.Add(_strip);
        detail.Controls.Add(_detailHead);

        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Ground };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _list.Margin = Padding.Empty;
        detail.Margin = Padding.Empty;
        split.Controls.Add(_list, 0, 0);
        split.Controls.Add(detail, 1, 0);
        Controls.Add(split);
    }

    protected override void Render()
    {
        var keep = _selected?.ReplayFile;
        var wasTop = _list.SelectedIndices.Count == 0 || _list.SelectedIndices[0] == 0;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var m in State.Period)
        {
            var item = new ListViewItem($"{Lang.ShortDay(m.StartLocal)} {Lang.DM(m.StartLocal)}  {m.StartLocal:HH:mm}") { Tag = m };
            item.SubItems.Add(ShortMode(m.Playlist));
            item.SubItems.Add(Placement(m));
            item.SubItems.Add(State.Count(m).ToString(Pt));
            item.SubItems.Add("");
            _list.Items.Add(item);
        }
        _list.EndUpdate();
        _list.FitLastColumn();

        // Mantém a partida escolhida; se era a mais recente, passa para a nova mais recente.
        var target = (!wasTop && keep != null ? _list.Items.Cast<ListViewItem>().FirstOrDefault(i => ((MatchRecord)i.Tag!).ReplayFile == keep) : null)
                     ?? (_list.Items.Count > 0 ? _list.Items[0] : null);
        if (target != null)
        {
            target.Selected = true;
            target.EnsureVisible();
            ShowDetail((MatchRecord)target.Tag!);
        }
        else ShowDetail(null);
    }

    private void ShowDetail(MatchRecord? m)
    {
        var target = m == null ? 0 : State.Count(m);
        if (!ReferenceEquals(m, _selected)) Anim.To(_detailHead, "n", m == null ? 0 : Math.Min(_shownCount, target) * 0.4f, target, 520, v => { _shownCount = v; _detailHead.Invalidate(); });
        _selected = m;
        _strip.SetMatch(m, State.Options);
        _teamBox.Height = m == null || m.Teammates.Count == 0 ? 0 : 40 + m.Teammates.Count * 24;
        _detailHead.Invalidate();
        _teamBox.Invalidate();

        _opens.BeginUpdate();
        _opens.Items.Clear();
        if (m != null)
        {
            foreach (var o in m.Chests.Concat(m.Extras).OrderBy(o => o.TimeSeconds))
            {
                var label = Lang.Kind(o.Kind) + (o.Destroyed ? " " + T("open.broken") : "") + (!ChestKinds.IsChest(o.Kind) && !State.Options.Counts(o.Kind) ? " · " + T("open.notCounted") : "");
                var item = new ListViewItem(Clock(o.TimeSeconds)) { Tag = o };
                item.SubItems.Add(label);
                _opens.Items.Add(item);
            }
        }
        _opens.EndUpdate();
        _opens.FitLastColumn();
    }

    private void PaintDetailHead(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        var w = _detailHead.Width;
        if (_selected is not { } m)
        {
            Theme.Text_(g, State.AllValid.Count == 0 ? T("matches.noneEver") : T("matches.noneInPeriod"),
                Theme.Body, Theme.Faint, new Rectangle(0, 0, w, 60), TextFormatFlags.WordBreak);
            return;
        }
        var title = $"{Lang.LongDate(m.StartLocal)}, {m.StartLocal:HH:mm}";
        Theme.Text_(g, title, Theme.Heading, Theme.Text, new Rectangle(0, 0, w, 24));
        var meta = new List<string> { ShortMode(m.Playlist) };
        if (m.Placement is int p) meta.Add(p == 1 ? T("place.royale") : m.TotalPlayers is > 0 ? F("place.of", p, m.TotalPlayers) : $"#{p}");
        if (m.Eliminations is int el) meta.Add(Plural(el, "u.kill"));
        if (m.DurationSeconds > 0) meta.Add($"{m.DurationSeconds / 60} min");
        Theme.Text_(g, string.Join("  ·  ", meta), Theme.Body, Theme.Muted, new Rectangle(0, 26, w, 22));

        var count = State.Count(m);
        var n = ((int)Math.Round(_shownCount)).ToString(Pt);
        var size = Theme.Measure(n, Theme.Number(36));
        Theme.Text_(g, n, Theme.Number(36), Theme.Gold, new Rectangle(-2, 54, size.Width + 10, 54), TextFormatFlags.Left | TextFormatFlags.Top);
        Theme.Text_(g, T(count == 1 ? "u.opened.1" : "u.opened.n"), Theme.Ui(10.5f), Theme.Muted, new Rectangle(size.Width + 6, 76, 200, 24), TextFormatFlags.Left | TextFormatFlags.Top);

        // Legenda por tipo, com a mesma marca do ritmo.
        var x = 0f;
        var groups = Stats.CountedOpens(m, State.Options).GroupBy(o => o.Kind).OrderByDescending(gr => gr.Count());
        foreach (var gr in groups)
        {
            var text = $"{Lang.Kind(gr.Key)} {gr.Count()}";
            var tw = Theme.Measure(text, Theme.Small).Width;
            if (x + tw + 22 > w) break;
            using var b = new SolidBrush(ChestKinds.IsChest(gr.Key) ? Theme.KindColor(gr.Key) : Theme.Gold);
            if (ChestKinds.IsChest(gr.Key)) Icons.FillDiamond(g, b, x + 5, 122, 4.5f);
            else using (var pen = new Pen(Theme.Gold, 1.2f)) Icons.DrawDiamond(g, pen, x + 5, 122, 4f);
            Theme.Text_(g, text, Theme.Small, Theme.Muted, new Rectangle((int)x + 14, 112, tw + 6, 20));
            x += tw + 30;
        }
    }

    /// <summary>Modo sem o prefixo "Battle Royale" (o mais comum), para caber nas colunas.</summary>
    private static string ShortMode(string? playlist) => Modes.Friendly(playlist).Replace("Battle Royale · ", "");

    private void PaintTeam(object? sender, PaintEventArgs e)
    {
        if (_selected is not { } m || m.Teammates.Count == 0) return;
        var g = e.Graphics;
        var w = _teamBox.Width;
        Theme.HLine(g, 0, w, 6);
        var team = Stats.TeamCount(m, State.Options);
        Theme.Text_(g, T("matches.team"), Theme.Strong, Theme.Text, new Rectangle(0, 12, w, 22));
        Theme.Text_(g, Plural(team, "u.chest"), Theme.Number(12), Theme.Gold, new Rectangle(0, 12, w, 22), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        var y = 38;
        var you = T("you");
        var rows = new List<(string, int)> { (you, State.Count(m)) };
        rows.AddRange(m.Teammates.Select(t => (t.Name, Stats.Count(t, State.Options))));
        foreach (var (name, n) in rows.Take(5))
        {
            Theme.Text_(g, name, Theme.Body, name == you ? Theme.Text : Theme.Muted, new Rectangle(0, y, w - 60, 22));
            Theme.Text_(g, n.ToString(Pt), Theme.Number(10.5f, false), Theme.Text, new Rectangle(w - 60, y, 60, 22), TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
            y += 24;
        }
    }
}
