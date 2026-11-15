using System.Diagnostics;
using ContadorBaus.Ui;

namespace ContadorBaus.Pages;

/// <summary>Definições: o que conta no total, a janela compacta, a pasta dos replays e o histórico.</summary>
internal sealed class SettingsPage : Page
{
    private readonly Dictionary<string, (Toggle Toggle, Panel Row)> _extras = new();
    private readonly Dictionary<string, int> _extraCounts = new();
    private readonly Toggle _overlayVisible = new();
    private readonly Toggle _overlayLocked = new();
    private readonly Toggle _withFortnite = new();
    private readonly List<GoldButton> _opacity = new();
    private readonly TextLabel _folder = new("", Theme.Body, Theme.Muted);
    private readonly TextLabel _history = new("", Theme.Body, Theme.Muted);
    private readonly GoldButton _resetFolder = new(Lang.T("btn.resetFolder"));
    private readonly GoldButton _retry = new(Lang.T("btn.retry"), GoldButton.Style.Ghost, Icons.Kind.Refresh);
    private readonly List<GoldButton> _languages = new();
    private bool _loading;

    /// <summary>Pedido para mostrar/esconder/bloquear a janela compacta (tratado pela janela principal).</summary>
    public event Action? OverlayChanged;
    /// <summary>O idioma mudou: a janela principal volta a construir-se na nova língua.</summary>
    public event Action? LanguageChanged;

    public override string Title => T("nav.settings");

    private static readonly Dictionary<string, string> ExtraHelp = new()
    {
        [ChestKinds.AmmoBox] = "help.ammo",
        [ChestKinds.Cannon] = "help.cannon",
        [ChestKinds.FoodBox] = "help.food",
        [ChestKinds.Poster] = "help.poster",
        [ChestKinds.OtherContainer] = "help.other",
    };

    public SettingsPage(AppState state) : base(state)
    {
        AutoScroll = true;
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            BackColor = Theme.Ground,
            Padding = new Padding(0, 0, 8, 0),
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Idioma: o nome da app e todos os textos mudam.
        Section(body, T("set.lang.title"), T("set.lang.text"));
        var langRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Ground, Margin = new Padding(0, 10, 0, 10) };
        foreach (var (lang, code, native) in Lang.All)
        {
            var b = new GoldButton(native, GoldButton.Style.Chip) { Tag = lang };
            b.Click += (_, _) =>
            {
                if (Lang.Current == lang) return;
                State.UpdateSettings(s => s.Language = code, recompute: false);
                LanguageChanged?.Invoke();
            };
            _languages.Add(b);
            langRow.Controls.Add(b);
        }
        Row(body, T("set.lang.row"), (string?)null, langRow);

        Section(body, T("set.count.title"), T("set.count.text"));
        Row(body, T("set.chests"), T("set.chests.help"), Locked(new Toggle { Checked = true, Enabled = false }));
        foreach (var kind in ChestKinds.ExtraKinds)
        {
            var toggle = new Toggle();
            var k = kind;
            toggle.CheckedChanged += (_, _) => { if (!_loading) State.SetExtraCounted(k, toggle.Checked); };
            var row = Row(body, Lang.Kind(kind), () => T(ExtraHelp[k]) + "  ·  " +
                (_extraCounts.GetValueOrDefault(k) is var n && n > 0 ? F("set.inPeriod", Lang.N(n)) : T("set.noneInPeriod")), toggle);
            _extras[kind] = (toggle, row);
        }

        Section(body, T("set.startup.title"), T("set.startup.text"));
        _withFortnite.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            try { FortniteLauncher.SetEnabled(_withFortnite.Checked); }
            catch (Exception ex)
            {
                MessageBox.Show(FindForm(), F("set.startup.error", ex.Message), Lang.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _loading = true; _withFortnite.Checked = FortniteLauncher.IsEnabled; _loading = false;
            }
        };
        Row(body, T("set.startup.row"), T("set.startup.help"), _withFortnite);

        Section(body, T("set.overlay.title"), T("set.overlay.text"));
        _overlayVisible.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            State.UpdateSettings(s => s.Overlay.Visible = _overlayVisible.Checked, recompute: false);
            OverlayChanged?.Invoke();
        };
        _overlayLocked.CheckedChanged += (_, _) =>
        {
            if (_loading) return;
            State.UpdateSettings(s => s.Overlay.Locked = _overlayLocked.Checked, recompute: false);
            OverlayChanged?.Invoke();
        };
        Row(body, T("set.overlay.show"), T("set.overlay.showHelp"), _overlayVisible);
        Row(body, T("set.overlay.lock"), T("set.overlay.lockHelp"), _overlayLocked);
        var opacityRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Ground, Margin = new Padding(0, 10, 0, 10) };
        foreach (var pct in new[] { 60, 80, 100 })
        {
            var b = new GoldButton($"{pct}%", GoldButton.Style.Chip) { Tag = pct };
            b.Click += (_, _) =>
            {
                State.UpdateSettings(s => s.Overlay.Opacity = pct, recompute: false);
                MarkOpacity();
                OverlayChanged?.Invoke();
            };
            _opacity.Add(b);
            opacityRow.Controls.Add(b);
        }
        Row(body, T("set.opacity"), T("set.opacity.help"), opacityRow);

        Section(body, T("set.replays.title"), T("set.replays.text"));
        var folderButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, BackColor = Theme.Ground, Margin = new Padding(0, 10, 0, 10) };
        var open = new GoldButton(T("btn.open"), GoldButton.Style.Ghost, Icons.Kind.Folder);
        open.Click += (_, _) => OpenFolder(State.Scanner.Folder);
        var choose = new GoldButton(T("btn.choose"));
        choose.Click += (_, _) => ChooseFolder();
        _resetFolder.Click += (_, _) => { State.UpdateSettings(s => s.ReplayFolder = null, recompute: false); State.Scanner.FolderChanged(); Render(); };
        folderButtons.Controls.AddRange(new Control[] { open, choose, _resetFolder });
        Row(body, T("set.folder"), (string?)null, folderButtons, _folder);
        _retry.Click += (_, _) => State.Scanner.Rescan(retryFailed: true);
        Row(body, T("set.failed"), T("set.failed.help"), _retry);

        Section(body, T("set.history.title"), T("set.history.text"));
        var openHistory = new GoldButton(T("btn.openFolder"), GoldButton.Style.Ghost, Icons.Kind.Folder);
        openHistory.Click += (_, _) => Process.Start("explorer.exe", $"/select,\"{State.Store.FilePath}\"");
        Row(body, T("set.historyFile"), (string?)null, openHistory, _history);

        Controls.Add(body);
    }

    private static Toggle Locked(Toggle t) { t.Cursor = Cursors.Default; return t; }

    private static void Section(TableLayoutPanel body, string title, string text)
    {
        var head = new Panel { Height = 70, Dock = DockStyle.Fill, BackColor = Theme.Ground, Margin = new Padding(0, 8, 0, 0) };
        head.Paint += (_, e) =>
        {
            Theme.Text_(e.Graphics, title, Theme.Heading, Theme.Gold, new Rectangle(0, 10, head.Width, 24));
            Theme.Text_(e.Graphics, text, Theme.Small, Theme.Muted, new Rectangle(0, 36, head.Width, 32), TextFormatFlags.WordBreak | TextFormatFlags.Top);
        };
        head.Resize += (_, _) => head.Invalidate();
        body.Controls.Add(head);
        body.SetColumnSpan(head, 2);
    }

    private static Panel Row(TableLayoutPanel body, string title, string? help, Control control, Control? extra = null)
        => Row(body, title, help == null ? null : () => help, control, extra);

    private static Panel Row(TableLayoutPanel body, string title, Func<string>? help, Control control, Control? extra = null)
    {
        // Grupos de botões (idioma, opacidade, pasta) ficam por baixo do título; interruptores e botões soltos à direita.
        var stacked = control is FlowLayoutPanel;
        var y = help == null ? 32 : 52;
        var height = stacked ? y + 36 + (extra != null ? 24 : 0) : help == null && extra == null ? 44 : 54;
        var left = new Panel { Dock = DockStyle.Fill, Height = height, BackColor = Theme.Ground, Margin = Padding.Empty };
        left.Paint += (_, e) =>
        {
            Theme.HLine(e.Graphics, 0, left.Width, 0.5f);
            Theme.Text_(e.Graphics, title, Theme.Body, Theme.Text, new Rectangle(0, 8, left.Width, 22));
            if (help != null) Theme.Text_(e.Graphics, help(), Theme.Small, Theme.Faint, new Rectangle(0, 29, left.Width, 20));
        };
        left.Resize += (_, _) => left.Invalidate();
        if (stacked)
        {
            control.Margin = Padding.Empty;
            control.Location = new Point(0, y);
            left.Controls.Add(control);
            if (extra != null)
            {
                extra.Location = new Point(0, y + 36);
                left.Controls.Add(extra);
            }
        }
        else if (extra != null)
        {
            extra.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            extra.Location = new Point(0, 28);
            left.Controls.Add(extra);
        }
        var holder = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Ground, Margin = Padding.Empty, Height = left.Height, Width = stacked ? 64 : Math.Max(control.Width, 60) + 4 };
        holder.Paint += (_, e) => Theme.HLine(e.Graphics, 0, holder.Width, 0.5f);
        if (!stacked)
        {
            control.Anchor = AnchorStyles.Right;
            control.Location = new Point(holder.Width - control.Width - 2, (holder.Height - control.Height) / 2);
            holder.Controls.Add(control);
            control.SizeChanged += (_, _) =>
            {
                holder.Width = control.Width + 4;
                control.Location = new Point(holder.Width - control.Width - 2, (holder.Height - control.Height) / 2);
            };
        }
        body.Controls.Add(left);
        body.Controls.Add(holder);
        return left;
    }

    private void MarkOpacity()
    {
        foreach (var b in _opacity) b.Selected = (int)b.Tag! == State.Settings.Overlay.Opacity;
    }

    protected override void Render()
    {
        _loading = true;
        foreach (var (kind, (toggle, row)) in _extras)
        {
            toggle.Checked = State.Settings.CountedExtras.Contains(kind);
            _extraCounts[kind] = State.Period.Sum(m => m.ExtraCount(kind));
            row.Invalidate();
        }
        _withFortnite.Checked = FortniteLauncher.IsEnabled;
        _overlayVisible.Checked = State.Settings.Overlay.Visible;
        _overlayLocked.Checked = State.Settings.Overlay.Locked;
        MarkOpacity();
        foreach (var b in _languages) b.Selected = (Language)b.Tag! == Lang.Current;
        _folder.Text = State.Scanner.Folder + (Directory.Exists(State.Scanner.Folder) ? "" : "  " + T("set.notExists"));
        _folder.ForeColor = Directory.Exists(State.Scanner.Folder) ? Theme.Muted : Theme.Error;
        _resetFolder.Enabled = !string.IsNullOrWhiteSpace(State.Settings.ReplayFolder);
        _retry.Enabled = State.FailedCount > 0;
        _history.Text = Plural(State.AllValid.Count, "u.saved") +
                        (State.FailedCount > 0 ? " · " + Plural(State.FailedCount, "u.failed") : "") +
                        (State.CreativeSkipped > 0 ? " · " + F("set.creativeSkipped", State.CreativeSkipped) : "");
        _loading = false;
    }

    private void OpenFolder(string folder)
    {
        if (Directory.Exists(folder)) Process.Start("explorer.exe", folder);
        else MessageBox.Show(FindForm(), F("folder.missing", folder), Lang.AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ChooseFolder()
    {
        using var dlg = new FolderBrowserDialog { Description = T("folder.pick"), SelectedPath = State.Scanner.Folder, UseDescriptionForTitle = true };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        State.UpdateSettings(s => s.ReplayFolder = dlg.SelectedPath, recompute: false);
        State.Scanner.FolderChanged();
        Render();
    }
}
