using ContadorBaus.Ui;

namespace ContadorBaus;

/// <summary>
/// Ecrã de abertura: fundo preto, logótipo à esquerda e "ChestCounter".
/// Na primeira abertura a marca desvanece e aparece a escolha do idioma; nas seguintes a janela desvanece e abre a app.
/// </summary>
internal sealed class SplashForm : Form
{
    private readonly bool _askLanguage;
    private float _logo, _word, _rule, _brandAlpha = 1, _picker, _hint;
    private readonly float[] _hover = new float[5];
    private int _hot = -1;
    private int _focus;
    private bool _closing;

    /// <summary>Idioma escolhido na primeira abertura (nulo se não foi pedido ou se a janela foi fechada).</summary>
    public Language? Chosen { get; private set; }

    public SplashForm(bool askLanguage)
    {
        _askLanguage = askLanguage;
        _focus = Math.Max(0, Lang.All.ToList().FindIndex(x => x.Lang == Lang.FromSystem()));
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Text = Lang.BaseName;
        Icon = Theme.AppIcon();
        BackColor = Color.Black;
        Size = new Size(620, 360);
        Opacity = 0;
        DoubleBuffered = true;
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
        // Entrada: a janela aparece, o logótipo desliza da esquerda, depois a palavra e o traço dourado.
        Anim.To(this, "win", 0, 1, 260, v => Opacity = v);
        Anim.To(this, "logo", 0, 1, 700, v => { _logo = v; Invalidate(); });
        After(160, () => Anim.To(this, "word", 0, 1, 760, v => { _word = v; Invalidate(); }));
        After(420, () => Anim.To(this, "rule", 0, 1, 820, v => { _rule = v; Invalidate(); }));
        After(1500, _askLanguage ? ShowPicker : FadeOut);
    }

    private void After(int ms, Action a)
    {
        var t = new System.Windows.Forms.Timer { Interval = ms };
        t.Tick += (_, _) => { t.Stop(); t.Dispose(); if (!IsDisposed) a(); };
        t.Start();
    }

    private void ShowPicker()
    {
        // A marca desvanece e sobe; a escolha do idioma aparece por baixo.
        Anim.To(this, "brand", 1, 0, 420, v => { _brandAlpha = v; Invalidate(); });
        After(220, () => Anim.To(this, "picker", 0, 1, 620, v => { _picker = v; Invalidate(); }));
        After(900, () => Anim.To(this, "hint", 0, 1, 500, v => { _hint = v; Invalidate(); }));
    }

    private void FadeOut()
    {
        if (_closing) return;
        _closing = true;
        Anim.To(this, "win", (float)Opacity, 0, 380, v =>
        {
            Opacity = v;
            if (v <= 0.001f) Close();
        });
    }

    private void Choose(int index)
    {
        if (_closing || index < 0) return;
        Chosen = Lang.All[index].Lang;
        _focus = index;
        Invalidate();
        After(180, FadeOut);
    }

    /// <summary>Diagnóstico: guarda imagens da abertura em momentos fixos (ms desde o início).</summary>
    public void CaptureAt(string folder, params int[] times)
    {
        Directory.CreateDirectory(folder);
        foreach (var ms in times)
        {
            var t = ms;
            Shown += (_, _) => After(t, () =>
            {
                using var bmp = new Bitmap(Width, Height);
                DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                bmp.Save(Path.Combine(folder, $"abertura-{t:0000}.png"));
            });
        }
        Shown += (_, _) => After(times.Max() + 200, Close);
    }

    // --- Desenho -------------------------------------------------------------------------------

    private RectangleF Option(int i)
    {
        const float w = 104, h = 92, gap = 10;
        var total = 5 * w + 4 * gap;
        var x = (Width - total) / 2 + i * (w + gap);
        var y = 150 + (1 - _picker) * 18;
        return new RectangleF(x, y, w, h);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        g.Clear(Color.Black);

        // Marca: logótipo à esquerda e "ChestCounter".
        if (_brandAlpha > 0)
        {
            var lift = (1 - _brandAlpha) * -24;
            var logoSize = 84f;
            var wordFont = Theme.Number(34);
            var chest = "Chest";
            var counter = "Counter";
            float chestW, counterW;
            using (var fmt = StringFormat.GenericTypographic)
            {
                chestW = g.MeasureString(chest, wordFont, PointF.Empty, fmt).Width;
                counterW = g.MeasureString(counter, wordFont, PointF.Empty, fmt).Width;
            }
            var total = logoSize + 22 + chestW + counterW;
            var x0 = (Width - total) / 2;
            var cy = Height / 2f - 10 + lift;

            var logoX = x0 - (1 - _logo) * 40;
            Theme.DrawEmblem(g, new RectangleF(logoX, cy - logoSize / 2, logoSize, logoSize), _logo * _brandAlpha);

            var wx = x0 + logoSize + 22 + (1 - _word) * 16;
            var wy = (int)(cy - 30);
            var a = (int)(255 * _word * _brandAlpha);
            TextRendererAlpha(g, chest, wordFont, Color.FromArgb(a, Theme.Text), new PointF(wx, wy));
            TextRendererAlpha(g, counter, wordFont, Color.FromArgb(a, Theme.Gold), new PointF(wx + chestW, wy));

            // Traço dourado que cresce por baixo da palavra.
            var ruleW = (chestW + counterW) * _rule;
            using var pen = new Pen(Color.FromArgb((int)(200 * _rule * _brandAlpha), Theme.GoldDeep), 2);
            g.DrawLine(pen, x0 + logoSize + 22, cy + 34, x0 + logoSize + 22 + ruleW, cy + 34);
        }

        if (_picker > 0)
        {
            var a = (int)(255 * _picker);
            var title = "Escolhe o idioma · Choose your language";
            var titleFont = Theme.Semibold(13);
            var tw = Theme.Measure(title, titleFont).Width;
            var ty = 86 + (1 - _picker) * 14;
            TextRendererAlpha(g, title, titleFont, Color.FromArgb(a, Theme.Text), new PointF((Width - tw) / 2f, ty));

            for (var i = 0; i < Lang.All.Count; i++)
            {
                var r = Option(i);
                var selected = Chosen == Lang.All[i].Lang;
                var focus = i == _focus && !selected;
                var fill = selected ? Theme.Gold : Theme.Mix(Theme.Surface, Theme.Hover, _hover[i]);
                var border = selected ? Theme.Gold : focus ? Theme.GoldDeep : Theme.Mix(Theme.Line, Theme.GoldDeep, _hover[i]);
                Theme.Fill(g, Color.FromArgb(a, fill), r, 10);
                Theme.Stroke(g, Color.FromArgb(a, border), r, 10, focus ? 1.5f : 1f);

                var code = Lang.All[i].Code.ToUpperInvariant();
                var codeFont = Theme.Number(22);
                var cw = Theme.Measure(code, codeFont).Width;
                var textColor = selected ? Theme.OnGold : Theme.Mix(Theme.Gold, Theme.GoldBright, _hover[i]);
                TextRendererAlpha(g, code, codeFont, Color.FromArgb(a, textColor), new PointF(r.X + (r.Width - cw) / 2, r.Y + 16 - _hover[i] * 2));
                var name = Lang.All[i].NativeName;
                var nw = Theme.Measure(name, Theme.Small).Width;
                TextRendererAlpha(g, name, Theme.Small, Color.FromArgb(a, selected ? Theme.OnGold : Theme.Muted), new PointF(r.X + (r.Width - nw) / 2, r.Y + 58));
            }

            if (_hint > 0)
            {
                var hint = "Podes mudar depois nas Definições · You can change it later in Settings";
                var hw = Theme.Measure(hint, Theme.Small).Width;
                TextRendererAlpha(g, hint, Theme.Small, Color.FromArgb((int)(255 * _hint), Theme.Faint), new PointF((Width - hw) / 2f, 276));
            }
        }
    }

    /// <summary>Texto com transparência (o TextRenderer do GDI não suporta alfa; aqui usa-se GDI+ com ClearType desligado).</summary>
    private static void TextRendererAlpha(Graphics g, string text, Font font, Color color, PointF at)
    {
        var hint = g.TextRenderingHint;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var b = new SolidBrush(color);
        using var fmt = StringFormat.GenericTypographic;
        g.DrawString(text, font, b, at, fmt);
        g.TextRenderingHint = hint;
    }

    // --- Rato e teclado ----------------------------------------------------------------------------

    private int HitTest(Point p)
    {
        if (_picker < 0.6f || _closing) return -1;
        for (var i = 0; i < Lang.All.Count; i++) if (Option(i).Contains(p)) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var hot = HitTest(e.Location);
        if (hot == _hot) return;
        if (_hot >= 0) { var i = _hot; Anim.To(this, "h" + i, _hover[i], 0, 200, v => { _hover[i] = v; Invalidate(); }); }
        if (hot >= 0) { var i = hot; Anim.To(this, "h" + i, _hover[i], 1, 140, v => { _hover[i] = v; Invalidate(); }); }
        _hot = hot;
        Cursor = hot >= 0 ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        var i = HitTest(e.Location);
        if (i >= 0) Choose(i);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_picker < 0.6f) return;
        if (e.KeyCode is Keys.Left or Keys.Up) { _focus = (_focus + 4) % 5; Invalidate(); }
        else if (e.KeyCode is Keys.Right or Keys.Down or Keys.Tab) { _focus = (_focus + 1) % 5; Invalidate(); }
        else if (e.KeyCode is Keys.Enter or Keys.Space) Choose(_focus);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Tab || base.IsInputKey(keyData);
}
