using System.Drawing.Imaging;

namespace ContadorBaus.Ui;

/// <summary>
/// Transição entre páginas: tira uma imagem da página antiga e da nova e faz um cruzamento suave
/// (a antiga desvanece e sobe um pouco, a nova aparece a subir), por cima das páginas reais.
/// </summary>
internal sealed class PageTransition : Canvas
{
    private Bitmap? _from, _to;
    private float _t;

    public PageTransition()
    {
        Dock = DockStyle.Fill;
        Visible = false;
    }

    /// <summary>Corre a transição de <paramref name="from"/> para <paramref name="to"/>; <paramref name="swap"/> troca as páginas reais.</summary>
    public void Run(Control from, Control to, Action swap)
    {
        if (!SystemInformation.UIEffectsEnabled || from.Width <= 0 || from.Height <= 0 || !from.Visible)
        {
            swap();
            return;
        }
        _from?.Dispose();
        _to?.Dispose();
        _from = Snapshot(from);
        _t = 0;
        Visible = true;
        BringToFront();
        Refresh();

        swap();
        to.PerformLayout();
        _to = Snapshot(to);

        Anim.To(this, "t", 0, 1, 300, v =>
        {
            _t = v;
            Invalidate();
            if (v >= 1)
            {
                Visible = false;
                _from?.Dispose(); _from = null;
                _to?.Dispose(); _to = null;
            }
        });
    }

    private static Bitmap Snapshot(Control c)
    {
        var bmp = new Bitmap(Math.Max(1, c.Width), Math.Max(1, c.Height));
        c.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
        return bmp;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.Ground);
        if (_from != null && _t < 1) Blend(g, _from, 1 - Math.Min(1, _t * 1.6f), -10 * _t);
        if (_to != null) Blend(g, _to, _t, 14 * (1 - _t));
    }

    private static void Blend(Graphics g, Image img, float alpha, float dy)
    {
        if (alpha <= 0.01f) return;
        using var attrs = new ImageAttributes();
        attrs.SetColorMatrix(new ColorMatrix { Matrix33 = Math.Clamp(alpha, 0, 1) });
        g.DrawImage(img, new Rectangle(0, (int)dy, img.Width, img.Height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attrs);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _from?.Dispose(); _to?.Dispose(); }
        base.Dispose(disposing);
    }
}
