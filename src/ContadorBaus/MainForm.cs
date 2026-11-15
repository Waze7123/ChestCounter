using ContadorBaus.Pages;
using ContadorBaus.Ui;

namespace ContadorBaus;

/// <summary>Janela principal: barra lateral, barra do período e as páginas.</summary>
internal sealed class MainForm : Form
{
    private readonly AppState _state;
    private readonly NavRail _rail = new() { Dock = DockStyle.Left };
    private readonly Panel _top = new() { Dock = DockStyle.Top, Height = 66, BackColor = Theme.Ground };
    private readonly Panel _host = new() { Dock = DockStyle.Fill, BackColor = Theme.Ground };
    private readonly PageTransition _transition = new();
    private readonly TextLabel _title = new("", Theme.Title, Theme.Text);
    private readonly DateField _date = new();
    private readonly GoldButton _all = new(Lang.T("period.all"), GoldButton.Style.Chip);
    private readonly GoldButton _today = new(Lang.T("period.today"), GoldButton.Style.Chip);
    private readonly GoldButton _week = new(Lang.T("period.week"), GoldButton.Style.Chip);
    private readonly GoldButton _month = new(Lang.T("period.month"), GoldButton.Style.Chip);
    private readonly GoldButton _scan = new(Lang.T("btn.scan"), GoldButton.Style.Primary, Icons.Kind.Refresh);
    private readonly List<Page> _pages = new();
    private readonly NotifyIcon _tray = new();
    private readonly SettingsPage _settings;
    private OverlayForm? _overlay;

    private readonly bool _startInTray;

    /// <summary>Fechada para voltar a abrir noutra língua (o Program cria uma janela nova).</summary>
    public bool RestartRequested { get; private set; }
    /// <summary>Página aberta (para a nova janela abrir na mesma página).</summary>
    public int CurrentPage => _rail.Selected;
    private bool _started;

    public MainForm(bool startInTray = false, int startPage = 0)
    {
        _startInTray = startInTray;
        Text = Lang.AppName;
        Icon = Theme.AppIcon();
        BackColor = Theme.Ground;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1040, 660);
        MinimumSize = new Size(900, 580);

        _state = new AppState(this);
        _settings = new SettingsPage(_state);
        _settings.OverlayChanged += SyncOverlay;
        _settings.LanguageChanged += () => { RestartRequested = true; Close(); };
        _pages.AddRange(new Page[] { new DashboardPage(_state), new MatchesPage(_state), new ModesPage(_state), new TeamPage(_state), _settings });

        _rail.Add(Lang.T("nav.dashboard"), Icons.Kind.Dashboard);
        _rail.Add(Lang.T("nav.matches"), Icons.Kind.Matches);
        _rail.Add(Lang.T("nav.modes"), Icons.Kind.Modes);
        _rail.Add(Lang.T("nav.team"), Icons.Kind.Team);
        _rail.Add(Lang.T("nav.settings"), Icons.Kind.Settings);
        _rail.SelectedChanged += ShowPage;

        BuildTopBar();
        foreach (var p in _pages)
        {
            p.Dock = DockStyle.Fill;
            p.Visible = false;
            _host.Controls.Add(p);
        }
        _host.Controls.Add(_transition);
        Controls.Add(_host);
        Controls.Add(_top);
        Controls.Add(_rail);

        _state.Changed += SyncPeriodControls;
        _state.StatusChanged += SyncStatus;
        BuildTray();
        ShowPage(Math.Clamp(startPage, 0, _pages.Count - 1));
    }

    private void BuildTopBar()
    {
        _title.Location = new Point(28, 18);
        _top.Controls.Add(_title);

        var right = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Theme.Ground,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        var since = new TextLabel(Lang.T("period.since"), Theme.Body, Theme.Muted) { Margin = new Padding(0, 6, 8, 0) };
        _date.Margin = new Padding(0, 0, 12, 0);
        _date.Picked += d => _state.SetStartDate(d);
        _all.Click += (_, _) => _state.SetStartDate(null);
        _today.Click += (_, _) => _state.SetStartDate(DateTime.Today);
        _week.Click += (_, _) => _state.SetStartDate(DateTime.Today.AddDays(-6));
        _month.Click += (_, _) => _state.SetStartDate(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
        _scan.Margin = new Padding(12, -2, 0, 0);
        _scan.Click += (_, _) => _state.Scanner.Rescan();
        right.Controls.AddRange(new Control[] { since, _date, _all, _today, _week, _month, _scan });
        _top.Controls.Add(right);
        void Arrange()
        {
            // Títulos compridos (ex.: "Tableau de bord"): esconde a palavra "Desde" antes de se sobrepor.
            since.Visible = true;
            right.PerformLayout();
            right.Location = new Point(_top.Width - right.Width - 28, 20);
            if (_title.Left + _title.PreferredWidth + 16 > right.Left)
            {
                since.Visible = false;
                right.PerformLayout();
                right.Location = new Point(_top.Width - right.Width - 28, 20);
            }
        }
        _top.Resize += (_, _) => Arrange();
        _title.TextChanged += (_, _) => Arrange();
        _title.SizeChanged += (_, _) => Arrange();
        _top.Paint += (_, e) => Theme.HLine(e.Graphics, 28, _top.Width - 28, _top.Height - 1);
    }

    private void BuildTray()
    {
        _tray.Icon = Icon;
        _tray.Text = Lang.AppName;
        var menu = DarkMenuRenderer.Menu();
        menu.Items.Add(Lang.T("tray.open"), null, (_, _) => RestoreFromTray());
        var toggleOverlay = new ToolStripMenuItem(Lang.T("tray.showOverlay"));
        toggleOverlay.Click += (_, _) => { _state.UpdateSettings(s => s.Overlay.Visible = !s.Overlay.Visible, recompute: false); SyncOverlay(); _settings.RenderData(); };
        var lockOverlay = new ToolStripMenuItem(Lang.T("tray.lockOverlay"));
        lockOverlay.Click += (_, _) => { _state.UpdateSettings(s => s.Overlay.Locked = !s.Overlay.Locked, recompute: false); SyncOverlay(); _settings.RenderData(); };
        menu.Items.Add(toggleOverlay);
        menu.Items.Add(lockOverlay);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Lang.T("tray.exit"), null, (_, _) => Close());
        menu.Opening += (_, _) =>
        {
            toggleOverlay.Checked = _state.Settings.Overlay.Visible;
            lockOverlay.Checked = _state.Settings.Overlay.Locked;
            lockOverlay.Enabled = _state.Settings.Overlay.Visible;
        };
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => RestoreFromTray();
        _tray.Visible = true;
    }

    private void RestoreFromTray()
    {
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    private void ShowPage(int index)
    {
        var current = _pages.FirstOrDefault(p => p.Visible);
        void Swap()
        {
            for (var i = 0; i < _pages.Count; i++) _pages[i].Visible = i == index;
            _title.Text = _pages[index].Title;
        }
        if (current != null && current != _pages[index] && IsHandleCreated && Visible) _transition.Run(current, _pages[index], Swap);
        else Swap();
        if (_rail.Selected != index) _rail.Selected = index;
    }

    private void SyncPeriodControls()
    {
        var start = _state.StartDate;
        _date.MarkedDays = _state.AllValid.Select(m => m.StartLocal.Date).ToHashSet();
        _date.Value = start ?? _state.EarliestDay;
        _all.Selected = start == null;
        _today.Selected = start == DateTime.Today;
        _week.Selected = start == DateTime.Today.AddDays(-6);
        _month.Selected = start == new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var tip = $"{Lang.AppName} · {Lang.Plural(_state.Period.Sum(_state.Count), "u.chest")}";
        _tray.Text = tip.Length > 63 ? tip[..63] : tip;
    }

    private void SyncStatus()
    {
        if (_state.Progress is { } p)
        {
            _rail.Status = Lang.F("status.scanning", p.Done, p.Total);
            _rail.StatusColor = Theme.Gold;
        }
        else if (!Directory.Exists(_state.Scanner.Folder))
        {
            _rail.Status = Lang.T("status.noFolder");
            _rail.StatusColor = Theme.Error;
        }
        else if (_state.Live)
        {
            _rail.Status = Lang.T("status.live");
            _rail.StatusColor = Theme.Live;
        }
        else
        {
            _rail.Status = Lang.T("status.idle");
            _rail.StatusColor = Theme.Gold;
        }
        _scan.Enabled = _state.Progress == null;
        _rail.Invalidate();
    }

    private void SyncOverlay()
    {
        var o = _state.Settings.Overlay;
        if (!o.Visible)
        {
            _overlay?.Hide();
            return;
        }
        if (_overlay == null || _overlay.IsDisposed)
        {
            _overlay = new OverlayForm(_state);
            _overlay.LockRequested += () => { _state.UpdateSettings(s => s.Overlay.Locked = true, recompute: false); SyncOverlay(); _settings.RenderData(); };
        }
        _overlay.ApplySettings();
        if (!_overlay.Visible) _overlay.Show();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.DarkTitleBar(this);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        // A janela aparece com um desvanecer curto depois da abertura.
        if (SystemInformation.UIEffectsEnabled)
        {
            Opacity = 0;
            Anim.To(this, "win", 0, 1, 320, v => Opacity = v);
        }
        StartOnce();
    }

    private void StartOnce()
    {
        if (_started) return;
        _started = true;
        _state.Start();
        SyncStatus();
        SyncOverlay();
    }

    /// <summary>Aberta pelo Fortnite: a janela não aparece (fica na bandeja) mas a contagem e a janela compacta arrancam.</summary>
    protected override void SetVisibleCore(bool value)
    {
        if (value && _startInTray && !_started)
        {
            if (!IsHandleCreated) CreateHandle();
            base.SetVisibleCore(false);
            StartOnce();
            _tray.ShowBalloonTip(3000, Lang.AppName, Lang.T("tray.balloon"), ToolTipIcon.None);
            return;
        }
        base.SetVisibleCore(value);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // Minimizar esconde na bandeja: a contagem continua.
        if (WindowState == FormWindowState.Minimized && _tray.Visible) Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        _tray.Visible = false;
        _overlay?.Close();
        _state.Dispose();
    }

    // --- Diagnóstico: "--captura pasta" guarda uma imagem de cada página quando a análise acaba e fecha.
    public void CaptureWhenIdle(string folder)
    {
        void Capture(bool _)
        {
            BeginInvoke(async () =>
            {
                _state.Scanner.ScanFinished -= Capture;
                await Task.Delay(1200);
                Directory.CreateDirectory(folder);
                for (var i = 0; i < _pages.Count; i++)
                {
                    ShowPage(i);
                    await Task.Delay(900);
                    using var bmp = Snapshot();
                    bmp.Save(Path.Combine(folder, $"{i + 1}-{_pages[i].Title}.png"), System.Drawing.Imaging.ImageFormat.Png);
                    if (_pages[i].AutoScroll)
                    {
                        // Página com deslocamento: captura também o fundo.
                        _pages[i].AutoScrollPosition = new Point(0, 5000);
                        await Task.Delay(400);
                        using var bottom = Snapshot();
                        bottom.Save(Path.Combine(folder, $"{i + 1}b-{_pages[i].Title}.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
                if (_overlay is { Visible: true })
                {
                    using var ob = new Bitmap(_overlay.Width, _overlay.Height);
                    _overlay.DrawToBitmap(ob, new Rectangle(0, 0, ob.Width, ob.Height));
                    ob.Save(Path.Combine(folder, "overlay.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                Close();
            });
        }
        _state.Scanner.ScanFinished += Capture;
    }

    /// <summary>
    /// Imagem da janela como aparece no Windows: o DrawToBitmap desenha a moldura clássica (clara), por isso
    /// usa-se só a área da app e desenha-se por cima a barra de título escura que a app pede ao Windows.
    /// </summary>
    private Bitmap Snapshot()
    {
        using var full = new Bitmap(Width, Height);
        DrawToBitmap(full, new Rectangle(0, 0, Width, Height));
        var origin = PointToScreen(Point.Empty);
        var client = new Rectangle(origin.X - Left, origin.Y - Top, ClientSize.Width, ClientSize.Height);

        const int bar = 32;
        var shot = new Bitmap(client.Width + 2, client.Height + bar + 1);
        using var g = Graphics.FromImage(shot);
        Theme.HighQuality(g);
        g.Clear(Theme.Rail);
        g.DrawImage(full, new Rectangle(1, bar, client.Width, client.Height), client, GraphicsUnit.Pixel);
        using (var ico = new Icon(Icon, 16, 16)) g.DrawIcon(ico, 10, (bar - 16) / 2);
        Theme.Text_(g, Text, Theme.Body, Theme.Muted, new Rectangle(34, 0, 400, bar));
        // Botões de minimizar, maximizar e fechar, discretos como no tema escuro do Windows.
        using var pen = new Pen(Theme.Muted, 1);
        var x = shot.Width - 46 * 3;
        g.DrawLine(pen, x + 18, bar / 2, x + 28, bar / 2);
        g.DrawRectangle(pen, x + 46 + 18, bar / 2 - 5, 10, 10);
        g.DrawLine(pen, x + 92 + 18, bar / 2 - 5, x + 92 + 28, bar / 2 + 5);
        g.DrawLine(pen, x + 92 + 28, bar / 2 - 5, x + 92 + 18, bar / 2 + 5);
        using var border = new Pen(Theme.Line);
        g.DrawRectangle(border, 0, 0, shot.Width - 1, shot.Height - 1);
        return shot;
    }
}
