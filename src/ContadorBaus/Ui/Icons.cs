using System.Drawing.Drawing2D;

namespace ContadorBaus.Ui;

/// <summary>Ícones de linha desenhados à mão: um só traço (1,6 px a 18 px) e pontas redondas.</summary>
internal static class Icons
{
    public enum Kind { Dashboard, Matches, Modes, Team, Settings, Lock, Unlock, Folder, Refresh, Calendar, ChevronDown, ChevronLeft, ChevronRight, Overlay, Close, Crown }

    private static Pen MakePen(Color c, float scale) => new(c, 1.6f * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

    /// <summary>Desenha o ícone num quadrado de 18×18 (escalado para <paramref name="r"/>).</summary>
    public static void Draw(Graphics g, Kind kind, RectangleF r, Color c)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var s = Math.Min(r.Width, r.Height) / 18f;
        g.TranslateTransform(r.X + (r.Width - 18 * s) / 2, r.Y + (r.Height - 18 * s) / 2);
        g.ScaleTransform(s, s);
        using var pen = MakePen(c, 1f);
        using var brush = new SolidBrush(c);
        switch (kind)
        {
            case Kind.Dashboard:
                using (var p = Theme.Rounded(new RectangleF(2, 2, 6, 8), 1.5f)) g.DrawPath(pen, p);
                using (var p = Theme.Rounded(new RectangleF(10, 2, 6, 5), 1.5f)) g.DrawPath(pen, p);
                using (var p = Theme.Rounded(new RectangleF(2, 12, 6, 4), 1.5f)) g.DrawPath(pen, p);
                using (var p = Theme.Rounded(new RectangleF(10, 9, 6, 7), 1.5f)) g.DrawPath(pen, p);
                break;
            case Kind.Matches:
                // Três linhas com uma marca cada: o "ritmo" de uma partida.
                for (var i = 0; i < 3; i++)
                {
                    var y = 4 + i * 5;
                    g.DrawLine(pen, 2, y, 16, y);
                    var x = new[] { 6f, 11f, 8f }[i];
                    FillDiamond(g, brush, x, y, 2.4f);
                }
                break;
            case Kind.Modes:
                DrawDiamondPath(g, pen, 9, 6, 7, 3.5f);
                g.DrawLines(pen, new PointF[] { new(2, 9.5f), new(9, 13), new(16, 9.5f) });
                g.DrawLines(pen, new PointF[] { new(2, 13), new(9, 16.5f), new(16, 13) });
                break;
            case Kind.Team:
                g.DrawEllipse(pen, 3.5f, 3, 5, 5);
                g.DrawArc(pen, 1.5f, 10, 9, 9, 180, 180);
                g.DrawEllipse(pen, 10.5f, 4.5f, 4, 4);
                g.DrawArc(pen, 9.5f, 11, 7, 7, 200, 140);
                break;
            case Kind.Settings:
                g.DrawLine(pen, 2, 5, 16, 5);
                g.DrawLine(pen, 2, 13, 16, 13);
                FillCircle(g, Theme.Ground, 6, 5, 2.4f); g.DrawEllipse(pen, 3.6f, 2.6f, 4.8f, 4.8f);
                FillCircle(g, Theme.Ground, 12, 13, 2.4f); g.DrawEllipse(pen, 9.6f, 10.6f, 4.8f, 4.8f);
                break;
            case Kind.Lock:
            case Kind.Unlock:
                using (var p = Theme.Rounded(new RectangleF(3.5f, 8, 11, 8), 1.8f)) g.DrawPath(pen, p);
                if (kind == Kind.Lock) g.DrawArc(pen, 5.5f, 2.5f, 7, 9, 180, 180);
                else g.DrawArc(pen, 5.5f, 1.5f, 7, 9, 180, 140);
                g.DrawLine(pen, 9, 11, 9, 13);
                break;
            case Kind.Folder:
                g.DrawLines(pen, new PointF[] { new(2, 14.5f), new(2, 4), new(7, 4), new(8.5f, 6), new(16, 6), new(16, 14.5f), new(2, 14.5f) });
                break;
            case Kind.Refresh:
                g.DrawArc(pen, 3, 3, 12, 12, -40, 290);
                g.DrawLines(pen, new PointF[] { new(14.6f, 2.6f), new(14.2f, 6.2f), new(10.6f, 5.8f) });
                break;
            case Kind.Calendar:
                using (var p = Theme.Rounded(new RectangleF(2.5f, 3.5f, 13, 12), 2f)) g.DrawPath(pen, p);
                g.DrawLine(pen, 2.5f, 7.5f, 15.5f, 7.5f);
                g.DrawLine(pen, 6, 2, 6, 5);
                g.DrawLine(pen, 12, 2, 12, 5);
                break;
            case Kind.ChevronDown:
                g.DrawLines(pen, new PointF[] { new(5, 7.5f), new(9, 11.5f), new(13, 7.5f) });
                break;
            case Kind.ChevronLeft:
                g.DrawLines(pen, new PointF[] { new(11, 5), new(7, 9), new(11, 13) });
                break;
            case Kind.ChevronRight:
                g.DrawLines(pen, new PointF[] { new(7, 5), new(11, 9), new(7, 13) });
                break;
            case Kind.Overlay:
                using (var p = Theme.Rounded(new RectangleF(2, 3, 14, 12), 2f)) g.DrawPath(pen, p);
                using (var p = Theme.Rounded(new RectangleF(9, 9, 5, 4), 1f)) g.FillPath(brush, p);
                break;
            case Kind.Close:
                g.DrawLine(pen, 5, 5, 13, 13);
                g.DrawLine(pen, 13, 5, 5, 13);
                break;
            case Kind.Crown:
                g.DrawLines(pen, new PointF[] { new(3, 14), new(2.5f, 6), new(6.5f, 9.5f), new(9, 4), new(11.5f, 9.5f), new(15.5f, 6), new(15, 14), new(3, 14) });
                break;
        }
        g.Restore(state);
    }

    public static void FillDiamond(Graphics g, Brush b, float cx, float cy, float r)
    {
        g.FillPolygon(b, new PointF[] { new(cx, cy - r), new(cx + r, cy), new(cx, cy + r), new(cx - r, cy) });
    }

    public static void DrawDiamond(Graphics g, Pen p, float cx, float cy, float r)
    {
        g.DrawPolygon(p, new PointF[] { new(cx, cy - r), new(cx + r, cy), new(cx, cy + r), new(cx - r, cy) });
    }

    private static void DrawDiamondPath(Graphics g, Pen p, float cx, float cy, float rx, float ry)
    {
        g.DrawPolygon(p, new PointF[] { new(cx, cy - ry), new(cx + rx, cy), new(cx, cy + ry), new(cx - rx, cy) });
    }

    private static void FillCircle(Graphics g, Color c, float cx, float cy, float r)
    {
        using var b = new SolidBrush(c);
        g.FillEllipse(b, cx - r, cy - r, r * 2, r * 2);
    }

    /// <summary>A marca da app: um baú em linhas douradas (tampa arqueada, fecho em losango).</summary>
    public static void ChestMark(Graphics g, RectangleF r, Color c)
    {
        var state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var w = r.Width;
        using var pen = new Pen(c, Math.Max(1.4f, w * 0.075f)) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var fill = new SolidBrush(Color.FromArgb(46, c));
        var lidTop = r.Y + r.Height * 0.08f;
        var seam = r.Y + r.Height * 0.44f;
        using (var lid = new GraphicsPath())
        {
            lid.AddLine(r.X, seam, r.X, lidTop + r.Height * 0.16f);
            lid.AddBezier(r.X, lidTop + r.Height * 0.16f, r.X + w * 0.1f, lidTop - r.Height * 0.04f, r.Right - w * 0.1f, lidTop - r.Height * 0.04f, r.Right, lidTop + r.Height * 0.16f);
            lid.AddLine(r.Right, lidTop + r.Height * 0.16f, r.Right, seam);
            lid.CloseFigure();
            g.FillPath(fill, lid);
            g.DrawPath(pen, lid);
        }
        var body = new RectangleF(r.X, seam, w, r.Bottom - seam);
        g.FillRectangle(fill, body);
        g.DrawRectangle(pen, body.X, body.Y, body.Width, body.Height);
        using var solid = new SolidBrush(c);
        FillDiamond(g, solid, r.X + w / 2, seam, w * 0.13f);
        g.Restore(state);
    }
}
