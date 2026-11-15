using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace ContadorBaus.Ui;

/// <summary>Paleta, tipos de letra e utilitários de desenho (preto quente e dourado).</summary>
internal static class Theme
{
    // Fundo e superfícies (pretos quentes)
    public static readonly Color Ground = Color.FromArgb(12, 11, 9);
    public static readonly Color Rail = Color.FromArgb(17, 15, 12);
    public static readonly Color Surface = Color.FromArgb(21, 19, 15);
    public static readonly Color Hover = Color.FromArgb(29, 26, 21);
    public static readonly Color Line = Color.FromArgb(42, 37, 28);

    // Ouro
    public static readonly Color Gold = Color.FromArgb(227, 176, 75);
    public static readonly Color GoldBright = Color.FromArgb(246, 210, 126);
    public static readonly Color GoldDeep = Color.FromArgb(90, 68, 25);
    public static readonly Color GoldWash = Color.FromArgb(38, 31, 17);
    public static readonly Color OnGold = Color.FromArgb(24, 18, 6);

    // Texto (tingido a partir do ouro, nunca cinzento puro)
    public static readonly Color Text = Color.FromArgb(241, 233, 216);
    public static readonly Color Muted = Color.FromArgb(166, 155, 133);
    public static readonly Color Faint = Color.FromArgb(138, 128, 108);
    /// <summary>Só para traços e estados desativados (contraste baixo de propósito).</summary>
    public static readonly Color Dim = Color.FromArgb(96, 88, 74);

    // Estados (só onde têm significado)
    public static readonly Color Live = Color.FromArgb(120, 214, 140);
    public static readonly Color Error = Color.FromArgb(232, 112, 92);

    private static readonly string UiFamily = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Text") ? "Segoe UI Variable Text" : "Segoe UI";
    private static readonly string UiDisplayFamily = FontFamily.Families.Any(f => f.Name == "Segoe UI Variable Display") ? "Segoe UI Variable Display" : "Segoe UI";
    private static readonly string NumberFamily = FontFamily.Families.Any(f => f.Name == "Bahnschrift") ? "Bahnschrift" : "Segoe UI";

    private static readonly Dictionary<(string, float, FontStyle), Font> Cache = new();

    private static Font Get(string family, float size, FontStyle style = FontStyle.Regular)
    {
        var key = (family, size, style);
        if (!Cache.TryGetValue(key, out var f)) Cache[key] = f = new Font(family, size, style, GraphicsUnit.Point);
        return f;
    }

    public static Font Body => Get(UiFamily, 9.5f);
    public static Font Small => Get(UiFamily, 8.5f);
    public static Font Strong => Get("Segoe UI Semibold", 9.5f);
    public static Font Title => Get(UiDisplayFamily, 15f, FontStyle.Bold);
    public static Font Heading => Get("Segoe UI Semibold", 11f);
    public static Font Ui(float size, FontStyle style = FontStyle.Regular) => Get(UiFamily, size, style);
    public static Font Semibold(float size) => Get("Segoe UI Semibold", size);

    /// <summary>Números: Bahnschrift (DIN condensada, algarismos tabulares).</summary>
    public static Font Number(float size, bool bold = true) => Get(bold ? NumberFamily + " SemiBold" : NumberFamily, size);

    /// <summary>Cor de cada tipo de abertura (marcas do ritmo e da legenda).</summary>
    public static Color KindColor(string kind) => kind switch
    {
        ChestKinds.Rare => Color.FromArgb(214, 160, 255),
        ChestKinds.Buried => Color.FromArgb(205, 150, 98),
        ChestKinds.Extraction => Color.FromArgb(126, 182, 255),
        ChestKinds.PosterChest => Color.FromArgb(150, 214, 160),
        _ when ChestKinds.IsChest(kind) => Gold,
        _ => Muted,
    };

    public static void HighQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    public static GraphicsPath Rounded(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        if (radius <= 0.5f) { p.AddRectangle(r); return p; }
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    public static void Fill(Graphics g, Color c, RectangleF r, float radius)
    {
        using var b = new SolidBrush(c);
        using var p = Rounded(r, radius);
        g.FillPath(b, p);
    }

    public static void Stroke(Graphics g, Color c, RectangleF r, float radius, float width = 1f)
    {
        using var pen = new Pen(c, width);
        using var p = Rounded(r, radius);
        g.DrawPath(pen, p);
    }

    public static void HLine(Graphics g, float x1, float x2, float y, Color? c = null)
    {
        using var pen = new Pen(c ?? Line);
        g.DrawLine(pen, x1, y, x2, y);
    }

    public static void VLine(Graphics g, float x, float y1, float y2, Color? c = null)
    {
        using var pen = new Pen(c ?? Line);
        g.DrawLine(pen, x, y1, x, y2);
    }

    /// <summary>Texto com TextRenderer (ClearType, mesmo aspeto que os controlos nativos).</summary>
    public static void Text_(Graphics g, string? text, Font font, Color color, Rectangle r, TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter)
        => TextRenderer.DrawText(g, text, font, r, color, flags | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);

    public static Size Measure(string text, Font font) => TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

    public static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }

    /// <summary>Ícone da app (janela, barra de tarefas, bandeja), lido do .ico embutido.</summary>
    public static Icon AppIcon()
    {
        using var s = typeof(Theme).Assembly.GetManifestResourceStream("ChestCounter.app.ico");
        return s != null ? new Icon(s) : SystemIcons.Application;
    }

    private static Bitmap? _emblem;

    /// <summary>O emblema da app (bússola dourada) para desenhar como logótipo.</summary>
    public static Bitmap? Emblem
    {
        get
        {
            if (_emblem != null) return _emblem;
            using var s = typeof(Theme).Assembly.GetManifestResourceStream("ChestCounter.icone.png");
            return _emblem = s == null ? null : new Bitmap(s);
        }
    }

    /// <summary>Desenha o emblema num quadrado, com transparência opcional (0–1).</summary>
    public static void DrawEmblem(Graphics g, RectangleF r, float alpha = 1)
    {
        var img = Emblem;
        if (img == null || alpha <= 0.01f) return;
        var size = Math.Min(r.Width, r.Height);
        var dest = new RectangleF(r.X + (r.Width - size) / 2, r.Y + (r.Height - size) / 2, size, size);
        var mode = g.InterpolationMode;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        if (alpha >= 0.99f) g.DrawImage(img, dest);
        else
        {
            using var attrs = new System.Drawing.Imaging.ImageAttributes();
            attrs.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha });
            g.DrawImage(img, Rectangle.Round(dest), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attrs);
        }
        g.InterpolationMode = mode;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? appName, string? idList);

    /// <summary>Barra de título escura com a cor do fundo (Windows 10 20H1+ / 11).</summary>
    public static void DarkTitleBar(Form form)
    {
        try
        {
            var on = 1;
            if (DwmSetWindowAttribute(form.Handle, 20, ref on, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, 19, ref on, sizeof(int));
            var caption = ColorTranslator.ToWin32(Rail);
            DwmSetWindowAttribute(form.Handle, 35, ref caption, sizeof(int));
            var border = ColorTranslator.ToWin32(Line);
            DwmSetWindowAttribute(form.Handle, 34, ref border, sizeof(int));
            var textColor = ColorTranslator.ToWin32(Muted);
            DwmSetWindowAttribute(form.Handle, 36, ref textColor, sizeof(int));
        }
        catch { }
    }

    /// <summary>Barras de deslocamento escuras num controlo nativo.</summary>
    public static void DarkScrollbars(Control c)
    {
        try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { }
    }
}

/// <summary>Animações por timer: interpolação com ease-out exponencial a partir de um estado já visível.</summary>
internal static class Anim
{
    private sealed class Run(Control owner, float from, float to, int ms, Action<float> step)
    {
        public readonly Control Owner = owner;
        public readonly long Start = Environment.TickCount64;
        public readonly float From = from, To = to;
        public readonly int Ms = Math.Max(1, ms);
        public readonly Action<float> Step = step;
    }

    private static readonly System.Windows.Forms.Timer Timer = new() { Interval = 15 };
    private static readonly Dictionary<(Control, string), Run> Runs = new();

    static Anim()
    {
        Timer.Tick += (_, _) =>
        {
            var now = Environment.TickCount64;
            foreach (var (key, run) in Runs.ToList())
            {
                if (run.Owner.IsDisposed) { Runs.Remove(key); continue; }
                var t = Math.Clamp((now - run.Start) / (float)run.Ms, 0, 1);
                run.Step(run.From + (run.To - run.From) * EaseOutExpo(t));
                if (t >= 1) Runs.Remove(key);
            }
            if (Runs.Count == 0) Timer.Stop();
        };
    }

    public static float EaseOutExpo(float t) => t >= 1 ? 1 : 1 - MathF.Pow(2, -10 * t);

    /// <summary>Anima um valor; um novo pedido com a mesma chave substitui o anterior.</summary>
    public static void To(Control owner, string key, float from, float to, int ms, Action<float> step)
    {
        if (SystemInformation.UIEffectsEnabled == false || Math.Abs(from - to) < 0.0001f)
        {
            step(to);
            Runs.Remove((owner, key));
            return;
        }
        Runs[(owner, key)] = new Run(owner, from, to, ms, step);
        if (!Timer.Enabled) Timer.Start();
    }
}
