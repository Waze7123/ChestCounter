using System.ComponentModel;

namespace ContadorBaus.Ui;

/// <summary>Controlo desenhado à mão, com double-buffer e fundo do tema.</summary>
internal class Canvas : Control
{
    public Canvas()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint
                 | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Theme.Ground;
        ForeColor = Theme.Text;
        Font = Theme.Body;
    }
}

/// <summary>Botão do tema: dourado (ação principal), discreto (secundária) ou "chip" selecionável.</summary>
internal sealed class GoldButton : Button
{
    public enum Style { Primary, Ghost, Chip }

    private float _hover;
    private float _sel;
    private bool _selected;
    public Style Variant { get; }
    public Icons.Kind? Icon { get; set; }
    public Icons.Kind? TrailingIcon { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            if (IsHandleCreated && Visible) Anim.To(this, "sel", _sel, value ? 1 : 0, 240, v => { _sel = v; Invalidate(); });
            else { _sel = value ? 1 : 0; Invalidate(); }
        }
    }

    public GoldButton(string text, Style variant = Style.Ghost, Icons.Kind? icon = null)
    {
        Text = text;
        Variant = variant;
        Icon = icon;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        Font = variant == Style.Primary ? Theme.Strong : Theme.Body;
        Cursor = Cursors.Hand;
        Height = variant == Style.Chip ? 28 : 32;
        Margin = new Padding(0, 0, 6, 0);
        AutoSizeToText();
    }

    public void AutoSizeToText()
    {
        var w = Theme.Measure(Text, Font).Width + (Variant == Style.Chip ? 22 : 28);
        if (Icon != null) w += 22;
        if (TrailingIcon != null) w += 18;
        Width = w;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Anim.To(this, "h", _hover, 1, 120, v => { _hover = v; Invalidate(); }); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Anim.To(this, "h", _hover, 0, 180, v => { _hover = v; Invalidate(); }); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Ground);
        Theme.HighQuality(g);
        var r = new RectangleF(0.5f, 0.5f, Width - 1, Height - 1);
        Color fill, text, border;
        switch (Variant)
        {
            case Style.Primary:
                fill = Enabled ? Theme.Mix(Theme.Gold, Theme.GoldBright, _hover) : Theme.GoldDeep;
                text = Theme.OnGold;
                border = fill;
                break;
            case Style.Chip:
                fill = Theme.Mix(Theme.Mix(Theme.Ground, Theme.Hover, _hover), Theme.GoldWash, _sel);
                text = Theme.Mix(Theme.Mix(Theme.Muted, Theme.Text, _hover), Theme.GoldBright, _sel);
                border = Theme.Mix(Theme.Mix(Theme.Line, Theme.Faint, _hover * 0.6f), Theme.GoldDeep, _sel);
                break;
            default:
                fill = Theme.Mix(Theme.Surface, Theme.Hover, _hover);
                text = Enabled ? Theme.Text : Theme.Faint;
                border = Theme.Mix(Theme.Line, Theme.Faint, _hover * 0.6f);
                break;
        }
        Theme.Fill(g, fill, r, 7);
        Theme.Stroke(g, border, r, 7);
        if (Focused && ShowFocusCues)
        {
            Theme.Stroke(g, Theme.GoldBright, RectangleF.Inflate(r, -2, -2), 5, 1.5f);
        }

        var x = Variant == Style.Chip ? 11 : 14;
        if (Icon is { } icon)
        {
            Icons.Draw(g, icon, new RectangleF(x - 1, (Height - 16) / 2f, 16, 16), text);
            x += 21;
        }
        var textRect = new Rectangle(x, 0, Width - x - (TrailingIcon != null ? 24 : 6), Height);
        Theme.Text_(g, Text, Font, text, textRect);
        if (TrailingIcon is { } trailing)
        {
            Icons.Draw(g, trailing, new RectangleF(Width - 24, (Height - 14) / 2f, 14, 14), Theme.Muted);
        }
    }
}

/// <summary>Interruptor ligado/desligado com o botão a deslizar.</summary>
internal sealed class Toggle : CheckBox
{
    private float _pos;
    private float _hover;

    public Toggle()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(40, 22);
        Cursor = Cursors.Hand;
        Text = "";
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Anim.To(this, "p", _pos, Checked ? 1 : 0, 220, v => { _pos = v; Invalidate(); });
    }

    protected override void OnCreateControl()
    {
        base.OnCreateControl();
        _pos = Checked ? 1 : 0;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = 1; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = 0; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Ground);
        Theme.HighQuality(g);
        var track = new RectangleF(1, 2, Width - 2, Height - 4);
        var on = Theme.Mix(Theme.Hover, Theme.Gold, _pos);
        if (!Enabled) on = Theme.Mix(Theme.Hover, Theme.GoldDeep, _pos);
        Theme.Fill(g, on, track, track.Height / 2);
        Theme.Stroke(g, Theme.Mix(Theme.Mix(Theme.Line, Theme.Faint, _hover), on, _pos), track, track.Height / 2);
        var d = track.Height - 6;
        var x = track.X + 3 + (track.Width - d - 6) * _pos;
        using var knob = new SolidBrush(Theme.Mix(Theme.Muted, Theme.OnGold, _pos));
        g.FillEllipse(knob, x, track.Y + 3, d, d);
        if (Focused && ShowFocusCues) Theme.Stroke(g, Theme.GoldBright, RectangleF.Inflate(track, 1.5f, 1.5f), track.Height / 2 + 1.5f, 1.2f);
    }
}

/// <summary>Barra de navegação lateral: marca da app, páginas e estado no fundo.</summary>
internal sealed class NavRail : Canvas
{
    private readonly List<(string Text, Icons.Kind Icon)> _items = new();
    private readonly float[] _hover = new float[8];
    private int _hot = -1;
    private int _selected;
    private float _indicatorY = -1;

    public event Action<int>? SelectedChanged;
    public string Status { get; set; } = "";
    public Color StatusColor { get; set; } = Theme.Faint;

    public NavRail()
    {
        BackColor = Theme.Rail;
        Width = 172;
    }

    public void Add(string text, Icons.Kind icon) => _items.Add((text, icon));

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Selected
    {
        get => _selected;
        set
        {
            if (value == _selected) return;
            var from = ItemTop(_selected);
            _selected = value;
            Anim.To(this, "ind", from, ItemTop(value), 260, v => { _indicatorY = v; Invalidate(); });
            SelectedChanged?.Invoke(value);
        }
    }

    private const int ItemsTop = 78, ItemH = 38;
    private static float ItemTop(int i) => ItemsTop + i * (ItemH + 2);

    private int HitTest(Point p)
    {
        for (var i = 0; i < _items.Count; i++)
            if (p.Y >= ItemTop(i) && p.Y < ItemTop(i) + ItemH && p.X > 8 && p.X < Width - 8) return i;
        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var hot = HitTest(e.Location);
        if (hot == _hot) return;
        if (_hot >= 0) { var i = _hot; Anim.To(this, "h" + i, _hover[i], 0, 180, v => { _hover[i] = v; Invalidate(); }); }
        if (hot >= 0) { var i = hot; Anim.To(this, "h" + i, _hover[i], 1, 120, v => { _hover[i] = v; Invalidate(); }); }
        _hot = hot;
        Cursor = hot >= 0 ? Cursors.Hand : Cursors.Default;
    }

    protected override void OnMouseLeave(EventArgs e) => OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, -100, -100, 0));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        var i = HitTest(e.Location);
        if (i >= 0) Selected = i;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        Theme.HighQuality(g);
        Theme.DrawEmblem(g, new RectangleF(18, 20, 30, 30));
        Theme.Text_(g, Lang.T("app.name.line1"), Theme.Semibold(10.5f), Theme.Text, new Rectangle(54, 18, Width - 60, 20));
        Theme.Text_(g, Lang.T("app.name.line2"), Theme.Semibold(10.5f), Theme.Gold, new Rectangle(54, 36, Width - 60, 20));

        if (_indicatorY < 0) _indicatorY = ItemTop(_selected);
        var ind = new RectangleF(10, _indicatorY, Width - 20, ItemH);
        Theme.Fill(g, Theme.GoldWash, ind, 8);

        for (var i = 0; i < _items.Count; i++)
        {
            var r = new RectangleF(10, ItemTop(i), Width - 20, ItemH);
            var selected = i == _selected;
            if (!selected && _hover[i] > 0) Theme.Fill(g, Color.FromArgb((int)(255 * _hover[i]), Theme.Hover), r, 8);
            var color = selected ? Theme.GoldBright : Theme.Mix(Theme.Muted, Theme.Text, _hover[i]);
            Icons.Draw(g, _items[i].Icon, new RectangleF(r.X + 12, r.Y + (ItemH - 18) / 2f, 18, 18), color);
            Theme.Text_(g, _items[i].Text, selected ? Theme.Strong : Theme.Body, color, new Rectangle((int)r.X + 40, (int)r.Y, (int)r.Width - 44, ItemH));
        }

        // Estado no fundo da barra.
        var y = Height - 34;
        using (var b = new SolidBrush(StatusColor)) g.FillEllipse(b, 22, y + 9, 6, 6);
        Theme.Text_(g, Status, Theme.Small, Theme.Muted, new Rectangle(34, y, Width - 40, 24));
        Theme.VLine(g, Width - 1, 0, Height, Theme.Line);
    }
}

/// <summary>Lista em tabela, desenhada no tema (cabeçalho, linhas, seleção, barras escuras).</summary>
internal class Ledger : ListView
{
    private int _hot = -1;

    /// <summary>Cor de uma célula (null = cor do texto).</summary>
    public Func<ListViewItem, int, Color?>? CellColor { get; set; }
    /// <summary>Desenho personalizado de uma célula; devolve true se desenhou.</summary>
    public Func<Graphics, Rectangle, ListViewItem, int, bool>? CellPainter { get; set; }

    public Ledger(int rowHeight = 30)
    {
        View = View.Details;
        FullRowSelect = true;
        OwnerDraw = true;
        HideSelection = false;
        MultiSelect = false;
        BorderStyle = BorderStyle.None;
        BackColor = Theme.Ground;
        ForeColor = Theme.Text;
        Font = Theme.Body;
        HeaderStyle = ColumnHeaderStyle.Nonclickable;
        DoubleBuffered = true;
        SmallImageList = new ImageList { ImageSize = new Size(1, rowHeight) };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.DarkScrollbars(this);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hot = GetItemAt(e.X, e.Y)?.Index ?? -1;
        if (hot == _hot) return;
        var old = _hot;
        _hot = hot;
        if (old >= 0 && old < Items.Count) Invalidate(Items[old].Bounds);
        if (hot >= 0) Invalidate(Items[hot].Bounds);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hot >= 0 && _hot < Items.Count) Invalidate(Items[_hot].Bounds);
        _hot = -1;
    }

    /// <summary>A altura acompanha as linhas (sem zona vazia por baixo), até ao espaço disponível.</summary>
    public bool AutoHeight { get; set; }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        FitLastColumn();
    }

    protected override void OnClientSizeChanged(EventArgs e)
    {
        base.OnClientSizeChanged(e);
        FitLastColumn();
    }

    public void FitHeight()
    {
        if (!AutoHeight || Parent == null) return;
        var rowH = SmallImageList?.ImageSize.Height ?? 30;
        var wanted = Items.Count > 0 ? Items[^1].Bounds.Bottom : rowH * 2;
        var available = Parent.ClientSize.Height - Top - Parent.Padding.Bottom;
        Height = Math.Max(rowH * 2, Math.Min(wanted, available));
        FitLastColumn();
    }

    /// <summary>A última coluna ocupa a largura que sobra (não fica zona sem desenho no cabeçalho).</summary>
    private bool _fitting;

    public void FitLastColumn()
    {
        // Evita ciclos: mudar a largura pode mostrar/esconder uma barra e voltar a chamar este método.
        if (_fitting || Columns.Count == 0 || !IsHandleCreated) return;
        _fitting = true;
        try { FitLastColumnCore(); } finally { _fitting = false; }
    }

    private void FitLastColumnCore()
    {
        var used = 0;
        for (var i = 0; i < Columns.Count - 1; i++) used += Columns[i].Width;
        // ClientSize já desconta a barra vertical quando existe; OnClientSizeChanged volta a ajustar quando ela aparece.
        var w = ClientSize.Width - used;
        if (w > 30 && Columns[^1].Width != w) Columns[^1].Width = w;
    }

    protected override void OnDrawColumnHeader(DrawListViewColumnHeaderEventArgs e)
    {
        using (var bg = new SolidBrush(Theme.Ground)) e.Graphics.FillRectangle(bg, e.Bounds);
        Theme.HLine(e.Graphics, e.Bounds.Left, e.Bounds.Right, e.Bounds.Bottom - 1);
        var flags = TextFormatFlags.VerticalCenter | Align(e.Header?.TextAlign ?? HorizontalAlignment.Left);
        Theme.Text_(e.Graphics, e.Header?.Text, Theme.Small, Theme.Faint, Rectangle.Inflate(e.Bounds, -10, 0), flags);
    }

    protected override void OnDrawItem(DrawListViewItemEventArgs e) { }

    protected override void OnDrawSubItem(DrawListViewSubItemEventArgs e)
    {
        if (e.Item == null) return;
        var g = e.Graphics;
        var selected = e.Item.Selected;
        var bg = selected ? Theme.GoldWash : e.ItemIndex == _hot ? Theme.Hover : Theme.Ground;
        using (var b = new SolidBrush(bg)) g.FillRectangle(b, e.Bounds);
        Theme.HLine(g, e.Bounds.Left, e.Bounds.Right, e.Bounds.Bottom - 1, Color.FromArgb(26, 23, 18));

        var cell = Rectangle.Inflate(e.Bounds, -10, 0);
        if (CellPainter?.Invoke(g, cell, e.Item, e.ColumnIndex) == true) return;
        var color = CellColor?.Invoke(e.Item, e.ColumnIndex) ?? Theme.Text;
        if (selected && color == Theme.Text) color = Theme.GoldBright;
        var font = e.SubItem?.Font ?? Font;
        Theme.Text_(g, e.SubItem?.Text, font, color, cell, TextFormatFlags.VerticalCenter | Align(Columns[e.ColumnIndex].TextAlign));
    }

    private static TextFormatFlags Align(HorizontalAlignment a) => a switch
    {
        HorizontalAlignment.Right => TextFormatFlags.Right,
        HorizontalAlignment.Center => TextFormatFlags.HorizontalCenter,
        _ => TextFormatFlags.Left,
    };
}

/// <summary>Texto simples sobre o fundo do tema.</summary>
internal sealed class TextLabel : Label
{
    public TextLabel(string text = "", Font? font = null, Color? color = null)
    {
        Text = text;
        Font = font ?? Theme.Body;
        ForeColor = color ?? Theme.Text;
        BackColor = Color.Transparent;
        AutoSize = true;
        UseMnemonic = false;
    }
}

/// <summary>Menus de contexto escuros.</summary>
internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer() : base(new DarkColors()) { RoundedEdges = false; }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = !e.Item.Enabled ? Theme.Faint : e.Item.Selected ? Theme.GoldBright : Theme.Text;
        base.OnRenderItemText(e);
    }

    private sealed class DarkColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground => Theme.Surface;
        public override Color MenuItemSelected => Theme.GoldWash;
        public override Color MenuItemBorder => Theme.GoldWash;
        public override Color MenuBorder => Theme.Line;
        public override Color SeparatorDark => Theme.Line;
        public override Color SeparatorLight => Theme.Surface;
        public override Color ImageMarginGradientBegin => Theme.Surface;
        public override Color ImageMarginGradientMiddle => Theme.Surface;
        public override Color ImageMarginGradientEnd => Theme.Surface;
    }

    public static ContextMenuStrip Menu() => new() { Renderer = new DarkMenuRenderer(), ShowImageMargin = false, Font = Theme.Body, Padding = new Padding(4) };
}
