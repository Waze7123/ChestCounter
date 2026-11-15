using System.Runtime.InteropServices;
using ContadorBaus.Ui;

namespace ContadorBaus;

/// <summary>
/// Janela compacta sempre por cima: baús da última partida e total do período.
/// Bloqueada fica transparente aos cliques (os cliques vão para o jogo); desbloqueada arrasta-se com o rato.
/// </summary>
internal sealed class OverlayForm : Form
{
    private const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x8000000, WS_EX_TOPMOST = 0x8;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private readonly AppState _state;
    private bool _locked;
    private bool _hover;
    private float _shownLast, _shownTotal;
    private int _last, _total;
    private string _lastNote = "";
    private float _flash;

    /// <summary>O utilizador mexeu na janela (posição) ou pediu para a bloquear.</summary>
    public event Action? LockRequested;

    public OverlayForm(AppState state)
    {
        _state = state;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Theme.Ground;
        Size = new Size(282, 74);
        DoubleBuffered = true;
        Text = Lang.AppName;
        SetStyle(ControlStyles.ResizeRedraw, true);
        state.Changed += Refresh_;
        state.StatusChanged += Invalidate;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
            return cp;
        }
    }

    public void ApplySettings()
    {
        var o = _state.Settings.Overlay;
        var area = Screen.PrimaryScreen!.WorkingArea;
        var x = o.X ?? area.Right - Width - 24;
        var y = o.Y ?? area.Top + 24;
        // Garante que fica num ecrã visível (um monitor pode ter sido desligado).
        var r = new Rectangle(x, y, Width, Height);
        if (!Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(r))) { x = area.Right - Width - 24; y = area.Top + 24; }
        Location = new Point(x, y);
        SetLocked(o.Locked);
        _targetOpacity = Math.Clamp(o.Opacity, 40, 100) / 100.0;
        if (Visible) Anim.To(this, "op", (float)Opacity, (float)_targetOpacity, 200, v => Opacity = v);
        Refresh_();
    }

    private void SetLocked(bool locked)
    {
        _locked = locked;
        if (!IsHandleCreated) return;
        var ex = GetWindowLong(Handle, GWL_EXSTYLE);
        ex = locked ? ex | WS_EX_TRANSPARENT : ex & ~WS_EX_TRANSPARENT;
        SetWindowLong(Handle, GWL_EXSTYLE, ex);
        Cursor = locked ? Cursors.Default : Cursors.SizeAll;
        Invalidate();
    }

    private double _targetOpacity = 0.92;

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        // Aparece a desvanecer até à opacidade escolhida.
        if (Visible) Anim.To(this, "op", 0, (float)_targetOpacity, 300, v => Opacity = v);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        SetLocked(_locked);
    }

    private void Refresh_()
    {
        var last = _state.Period.FirstOrDefault() ?? _state.AllValid.FirstOrDefault();
        var newLast = last == null ? 0 : _state.Count(last);
        var newTotal = _state.Period.Sum(_state.Count);
        _lastNote = last == null ? Lang.T("overlay.none") : last.StartLocal.Date == DateTime.Today ? Lang.F("overlay.at", last.StartLocal.ToString("HH:mm")) : Lang.DM(last.StartLocal);
        if (newLast != _last)
        {
            var grew = Visible && newTotal > _total && _total > 0;
            _last = newLast;
            Anim.To(this, "l", _shownLast, newLast, 600, v => { _shownLast = v; Invalidate(); });
            // Nova partida contada: o número acende e volta ao dourado.
            if (grew) Anim.To(this, "flash", 1, 0, 1600, v => { _flash = v; Invalidate(); });
        }
        if (newTotal != _total) { _total = newTotal; Anim.To(this, "t", _shownTotal, newTotal, 700, v => { _shownTotal = v; Invalidate(); }); }
        Invalidate();
    }

    private Rectangle LockButton => new(Width - 30, 6, 24, 24);

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        Cursor = LockButton.Contains(e.Location) ? Cursors.Hand : Cursors.SizeAll;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = false; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_locked || e.Button != MouseButtons.Left) return;
        if (LockButton.Contains(e.Location))
        {
            LockRequested?.Invoke();
            return;
        }
        // Arrastar a janela pela área toda.
        ReleaseCapture();
        SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        _state.UpdateSettings(s => { s.Overlay.X = Left; s.Overlay.Y = Top; }, recompute: false);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        g.Clear(Theme.Ground);
        var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        Theme.Stroke(g, _locked ? Theme.Line : Theme.GoldDeep, r, 0);

        Theme.DrawEmblem(g, new RectangleF(12, 21, 30, 30));

        // Última partida (ou partida a decorrer).
        var pt = Lang.Culture;
        var lastText = ((int)Math.Round(_shownLast)).ToString(pt);
        Theme.Text_(g, lastText, Theme.Number(24), Theme.Mix(Theme.Gold, Color.White, _flash * 0.7f), new Rectangle(50, 8, 70, 36), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        var caption = _state.Live ? Lang.T("overlay.live") : Lang.F("overlay.last", _lastNote);
        Theme.Text_(g, caption, Theme.Small, _state.Live ? Theme.Live : Theme.Muted, new Rectangle(50, 44, 126, 20), TextFormatFlags.Left | TextFormatFlags.Top);

        Theme.VLine(g, 184, 14, Height - 14);
        var totalText = Lang.N((int)Math.Round(_shownTotal));
        Theme.Text_(g, totalText, Theme.Number(18), Theme.Text, new Rectangle(196, 8, Width - 200, 36), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        Theme.Text_(g, Lang.T("overlay.total"), Theme.Small, Theme.Muted, new Rectangle(196, 44, Width - 200, 20), TextFormatFlags.Left | TextFormatFlags.Top);

        if (!_locked && _hover)
        {
            Theme.Fill(g, Theme.Hover, LockButton, 6);
            Icons.Draw(g, Icons.Kind.Lock, RectangleF.Inflate(LockButton, -4, -4), Theme.GoldBright);
        }
    }
}
