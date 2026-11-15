using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Gera o ícone da app: um baú dourado (desenho original, vetorial) em PNG 512 px e um .ico com vários tamanhos.
// Uso: dotnet run --project tools/IconGen -- <pasta de saída>
var outDir = args.Length > 0 ? args[0] : Path.Combine("src", "ContadorBaus", "Assets");
Directory.CreateDirectory(outDir);

using var master = DrawChest(512);
master.Save(Path.Combine(outDir, "icone.png"), ImageFormat.Png);

var sizes = new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };
var pngs = sizes.Select(s =>
{
    using var bmp = s <= 32 ? DrawChest(s * 4, small: true) : null;
    using var scaled = Scale(bmp ?? master, s);
    using var ms = new MemoryStream();
    scaled.Save(ms, ImageFormat.Png);
    return ms.ToArray();
}).ToList();

using (var ico = File.Create(Path.Combine(outDir, "app.ico")))
using (var w = new BinaryWriter(ico))
{
    w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)sizes.Length);
    var offset = 6 + 16 * sizes.Length;
    for (var i = 0; i < sizes.Length; i++)
    {
        var d = (byte)(sizes[i] >= 256 ? 0 : sizes[i]);
        w.Write(d); w.Write(d); w.Write((byte)0); w.Write((byte)0);
        w.Write((ushort)1); w.Write((ushort)32); w.Write(pngs[i].Length); w.Write(offset);
        offset += pngs[i].Length;
    }
    foreach (var p in pngs) w.Write(p);
}
Console.WriteLine($"Ícone gerado em {Path.GetFullPath(outDir)}");

static Bitmap Scale(Bitmap src, int size)
{
    var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.DrawImage(src, 0, 0, size, size);
    return bmp;
}

static GraphicsPath Rounded(RectangleF r, float rad)
{
    var p = new GraphicsPath();
    var d = rad * 2;
    p.AddArc(r.X, r.Y, d, d, 180, 90);
    p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
    p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
    p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
    p.CloseFigure();
    return p;
}

/// <summary>
/// Baú dourado de frente: tampa arqueada, corpo de madeira escura com faixas de ouro, fecho com fechadura,
/// rebites, brilho por trás e reflexos. Em tamanhos pequenos (<paramref name="small"/>) tira o detalhe fino.
/// </summary>
static Bitmap DrawChest(int s, bool small = false)
{
    var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.Clear(Color.Transparent);
    float U(float v) => v * s / 512f;

    var goldLight = Color.FromArgb(255, 236, 160);
    var gold = Color.FromArgb(242, 186, 70);
    var goldMid = Color.FromArgb(214, 145, 38);
    var goldDark = Color.FromArgb(150, 92, 18);
    var outline = Color.FromArgb(58, 30, 6);
    var woodDark = Color.FromArgb(84, 44, 22);
    var wood = Color.FromArgb(122, 66, 32);

    // Brilho dourado por trás do baú.
    if (!small)
    {
        using var glowPath = new GraphicsPath();
        glowPath.AddEllipse(U(20), U(40), U(472), U(440));
        using var glow = new PathGradientBrush(glowPath)
        {
            CenterColor = Color.FromArgb(150, 255, 200, 90),
            SurroundColors = new[] { Color.FromArgb(0, 255, 200, 90) },
        };
        g.FillPath(glow, glowPath);
    }

    var body = new RectangleF(U(70), U(236), U(372), U(196));
    var lidTop = U(96);
    var seam = U(246);

    // Sombra no chão.
    using (var shadow = new SolidBrush(Color.FromArgb(small ? 0 : 70, 0, 0, 0)))
        g.FillEllipse(shadow, U(80), U(414), U(352), U(46));

    // --- Corpo: madeira com tábuas.
    using (var bodyPath = Rounded(body, U(18)))
    {
        using var woodBrush = new LinearGradientBrush(body, wood, woodDark, 90f);
        g.FillPath(woodBrush, bodyPath);
        if (!small)
        {
            using var plank = new Pen(Color.FromArgb(90, 30, 14, 4), U(4));
            for (var y = body.Y + U(62); y < body.Bottom - U(20); y += U(62))
                g.DrawLine(plank, body.X + U(10), y, body.Right - U(10), y);
        }
        using var ol = new Pen(outline, U(small ? 14 : 10)) { LineJoin = LineJoin.Round };
        g.DrawPath(ol, bodyPath);
    }

    // --- Tampa: arco dourado com madeira ao centro.
    using var lid = new GraphicsPath();
    lid.AddLine(U(62), seam, U(62), lidTop + U(70));
    lid.AddBezier(U(62), lidTop + U(70), U(62), lidTop - U(18), U(450), lidTop - U(18), U(450), lidTop + U(70));
    lid.AddLine(U(450), lidTop + U(70), U(450), seam);
    lid.CloseFigure();
    // Tampa toda em ouro (é um baú dourado), com um painel central mais escuro e frisos.
    using (var lidGold = new LinearGradientBrush(new RectangleF(U(62), lidTop - U(20), U(388), seam - lidTop + U(20)), goldLight, goldMid, 90f))
        g.FillPath(lidGold, lid);
    if (!small)
    {
        var st = g.Save();
        g.SetClip(lid);
        var panel = new RectangleF(U(146), lidTop + U(20), U(220), seam - lidTop);
        using var panelBrush = new LinearGradientBrush(panel, gold, goldDark, 90f);
        g.FillRectangle(panelBrush, panel);
        using var groove = new Pen(Color.FromArgb(110, goldDark), U(5));
        for (var x = panel.X + U(55); x < panel.Right - U(20); x += U(55)) g.DrawLine(groove, x, panel.Y, x, panel.Bottom);
        g.Restore(st);
    }

    // Faixas de ouro (verticais na tampa e no corpo, horizontal na junção e na base).
    void GoldBand(RectangleF r, float radius)
    {
        using var p = Rounded(r, radius);
        using var b = new LinearGradientBrush(r, goldLight, goldMid, 90f)
        {
            InterpolationColors = new ColorBlend
            {
                Colors = new[] { goldLight, gold, goldMid, goldDark },
                Positions = new[] { 0f, 0.35f, 0.75f, 1f },
            },
        };
        g.FillPath(b, p);
        using var ol = new Pen(outline, U(small ? 10 : 7)) { LineJoin = LineJoin.Round };
        g.DrawPath(ol, p);
    }

    var state = g.Save();
    g.SetClip(lid);
    GoldBand(new RectangleF(U(62), lidTop - U(30), U(84), seam - lidTop + U(40)), U(6));
    GoldBand(new RectangleF(U(366), lidTop - U(30), U(84), seam - lidTop + U(40)), U(6));
    // Rebordo dourado do arco.
    using (var rim = new Pen(gold, U(26)))
    {
        using var arc = new GraphicsPath();
        arc.AddBezier(U(62), lidTop + U(70), U(62), lidTop - U(18), U(450), lidTop - U(18), U(450), lidTop + U(70));
        g.DrawPath(rim, arc);
    }
    g.Restore(state);
    using (var lidOl = new Pen(outline, U(small ? 14 : 10)) { LineJoin = LineJoin.Round })
        g.DrawPath(lidOl, lid);

    GoldBand(new RectangleF(U(62), U(236), U(84), U(196)), U(10));
    GoldBand(new RectangleF(U(366), U(236), U(84), U(196)), U(10));
    GoldBand(new RectangleF(U(48), seam - U(18), U(416), U(42)), U(12));
    if (!small) GoldBand(new RectangleF(U(56), U(404), U(400), U(34)), U(10));

    // --- Fecho: placa de ouro com fechadura.
    var plate = new RectangleF(U(206), U(206), U(100), U(124));
    using (var platePath = new GraphicsPath())
    {
        platePath.AddLine(plate.X, plate.Y + U(14), plate.X + U(14), plate.Y);
        platePath.AddLine(plate.Right - U(14), plate.Y, plate.Right, plate.Y + U(14));
        platePath.AddLine(plate.Right, plate.Bottom - U(36), plate.X + plate.Width / 2, plate.Bottom);
        platePath.AddLine(plate.X + plate.Width / 2, plate.Bottom, plate.X, plate.Bottom - U(36));
        platePath.CloseFigure();
        using var pb = new LinearGradientBrush(plate, goldLight, goldDark, 90f);
        g.FillPath(pb, platePath);
        using var ol = new Pen(outline, U(small ? 12 : 8)) { LineJoin = LineJoin.Round };
        g.DrawPath(ol, platePath);
    }
    using (var keyhole = new SolidBrush(outline))
    {
        g.FillEllipse(keyhole, U(240), U(240), U(32), U(32));
        g.FillPolygon(keyhole, new[] { new PointF(U(246), U(262)), new PointF(U(266), U(262)), new PointF(U(272), U(298)), new PointF(U(240), U(298)) });
    }

    if (!small)
    {
        // Rebites nas faixas.
        using var rivet = new SolidBrush(goldLight);
        using var rivetOl = new Pen(goldDark, U(3));
        foreach (var x in new[] { U(104), U(408) })
            foreach (var y in new[] { U(290), U(370) })
            {
                g.FillEllipse(rivet, x - U(9), y - U(9), U(18), U(18));
                g.DrawEllipse(rivetOl, x - U(9), y - U(9), U(18), U(18));
            }

        // Reflexos: um traço claro na tampa e brilhos em estrela.
        using (var shine = new Pen(Color.FromArgb(170, 255, 250, 220), U(10)) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            using var arc = new GraphicsPath();
            arc.AddBezier(U(160), lidTop + U(28), U(210), lidTop + U(8), U(280), lidTop + U(6), U(330), lidTop + U(18));
            g.DrawPath(shine, arc);
        }
        void Sparkle(float cx, float cy, float r)
        {
            using var b = new SolidBrush(Color.FromArgb(240, 255, 255, 235));
            g.FillPolygon(b, new[]
            {
                new PointF(cx, cy - r), new PointF(cx + r * 0.22f, cy - r * 0.22f), new PointF(cx + r, cy),
                new PointF(cx + r * 0.22f, cy + r * 0.22f), new PointF(cx, cy + r), new PointF(cx - r * 0.22f, cy + r * 0.22f),
                new PointF(cx - r, cy), new PointF(cx - r * 0.22f, cy - r * 0.22f),
            });
        }
        Sparkle(U(118), U(150), U(26));
        Sparkle(U(400), U(214), U(18));
        Sparkle(U(330), U(380), U(14));
    }
    return bmp;
}
