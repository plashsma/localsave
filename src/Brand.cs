using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

internal static class Brand {
    internal static Color Navy = Color.FromArgb(20, 37, 53);
    internal static Color Teal = Color.FromArgb(38, 102, 214);
    internal static Color Ink = Color.FromArgb(26, 43, 55);
    internal static Color Muted = Color.FromArgb(86, 105, 117);
    internal static Color Paper = Color.FromArgb(245, 248, 249);
    internal static void Background(Graphics g, Rectangle bounds, Point offset) {
        var state=g.Save();g.TranslateTransform(-offset.X,-offset.Y);
        using(LinearGradientBrush fill=new LinearGradientBrush(new Rectangle(-210,0,980,730),Color.FromArgb(224,239,250),Color.FromArgb(235,222,250),42f)) g.FillRectangle(fill,new Rectangle(offset,bounds.Size));
        Glow(g,new RectangleF(240,-200,790,660),Color.FromArgb(210,115,179,248));
        Glow(g,new RectangleF(-360,110,710,810),Color.FromArgb(195,88,219,207));
        Glow(g,new RectangleF(150,430,780,480),Color.FromArgb(170,191,132,229));
        Glow(g,new RectangleF(50,-270,550,670),Color.FromArgb(150,255,255,255));
        using(GraphicsPath ribbon=new GraphicsPath()) {
            ribbon.AddBezier(-210,610,110,90,420,880,860,190);
            using(Pen haze=new Pen(Color.FromArgb(24,255,255,255),110)) g.DrawPath(haze,ribbon);
            using(Pen light=new Pen(Color.FromArgb(36,255,255,255),36)) g.DrawPath(light,ribbon);
        }
        g.Restore(state);
    }
    static void Glow(Graphics g,RectangleF rect,Color center) {
        using(GraphicsPath p=new GraphicsPath()) {p.AddEllipse(rect);using(PathGradientBrush b=new PathGradientBrush(p)) {b.CenterColor=center;b.SurroundColors=new Color[]{Color.FromArgb(0,center)};g.FillPath(b,p);}}
    }
    internal static GraphicsPath Round(RectangleF r, float radius) {
        GraphicsPath p = new GraphicsPath(); float d = radius * 2;
        p.AddArc(r.X,r.Y,d,d,180,90); p.AddArc(r.Right-d,r.Y,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.X,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
    internal static Bitmap Logo(int size) {
        Bitmap bitmap = new Bitmap(size,size);
        using (Graphics g = Graphics.FromImage(bitmap)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(size/128f,size/128f);
            using (GraphicsPath p = Round(new RectangleF(2,2,124,124),28)) {
                using (Brush b = new LinearGradientBrush(new Rectangle(0,0,128,128),Color.FromArgb(69,139,232),Color.FromArgb(104,100,202),65f)) g.FillPath(b,p);
                using(Pen rim=new Pen(Color.FromArgb(150,255,255,255),2)) g.DrawPath(rim,p);
            }
            using (Pen line = new Pen(Color.White,6)) {
                line.StartCap = line.EndCap = LineCap.Round; line.LineJoin = LineJoin.Round;
                g.DrawLines(line,new PointF[] {new PointF(74,27),new PointF(39,27),new PointF(39,98),new PointF(89,98),new PointF(89,44),new PointF(74,27),new PointF(74,45),new PointF(88,45)});
            }
            using (Pen line = new Pen(Color.FromArgb(78,226,187),8)) {
                line.StartCap = line.EndCap = LineCap.Round; line.LineJoin = LineJoin.Round;
                g.DrawLines(line,new PointF[] {new PointF(50,70),new PointF(61,81),new PointF(81,60)});
            }
        }
        return bitmap;
    }
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    internal static Icon Icon() {
        using (Bitmap b = Logo(64)) {
            IntPtr h = b.GetHicon();
            try { using (Icon i = System.Drawing.Icon.FromHandle(h)) return (Icon)i.Clone(); }
            finally { DestroyIcon(h); }
        }
    }
}
