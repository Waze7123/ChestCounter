using System.Globalization;
using ContadorBaus.Ui;

namespace ContadorBaus.Pages;

/// <summary>Página da janela principal. Volta a desenhar-se quando o estado muda e está visível.</summary>
internal abstract class Page : UserControl
{
    protected static CultureInfo Pt => Lang.Culture;
    protected readonly AppState State;
    private bool _stale = true;

    public abstract string Title { get; }

    protected Page(AppState state)
    {
        State = state;
        BackColor = Theme.Ground;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        DoubleBuffered = true;
        Padding = new Padding(28, 8, 28, 24);
        State.Changed += () =>
        {
            if (Visible) RenderData();
            else _stale = true;
        };
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible && _stale) RenderData();
    }

    public void RenderData()
    {
        _stale = false;
        Render();
    }

    protected abstract void Render();

    protected static string Plural(int n, string unit) => Lang.Plural(n, unit);
    protected static string T(string key) => Lang.T(key);
    protected static string F(string key, params object[] args) => Lang.F(key, args);

    protected static string Placement(MatchRecord m) => m.Placement switch
    {
        1 => T("place.win"),
        int p => $"#{p}",
        _ => "—",
    };

    protected static string Clock(float seconds)
    {
        var t = TimeSpan.FromSeconds(seconds);
        return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
    }
}
