using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;

// Indigo ink on graph paper, like a printed puzzle book. Vermilion is kept for the win seal and mistakes.
public static class Theme {
    public static readonly Color Paper=Hex(0xF3F5F8),Ink=Hex(0x1E3756),Accent=Hex(0x3D6898),Muted=Hex(0x687891),Line=Hex(0xDDE3EB),GhostHover=Hex(0xEAF0F7),TableHead=Hex(0xE7ECF3);
    public static readonly Color Spine=Hex(0x1B3150),SpineDeep=Hex(0x152842),SpineHover=Hex(0x24406A),SpineSelected=Hex(0x2F5486),SpineText=Hex(0xEEF2F8),SpineMuted=Hex(0x9DB0C9),SpineLine=Hex(0x34507A),SpineFill=Hex(0x9CC0E8);
    public static readonly Color Seal=Hex(0xC0392B),GridThin=Hex(0xDCE2EA),GridGroup=Hex(0xB4C0CF),Shadow=Color.FromArgb(26,0x1E,0x37,0x56);
    // Muted traditional tints (mizu, wakatake, sakura, yamabuki, fuji, sora, kaki, uguisu, usubeni, kikyo).
    static readonly int[,] regions={{0xD3E6E4,0x4F8A84},{0xDCEBD2,0x5C8A4A},{0xF5DCE1,0xB0627A},{0xF7E8BE,0xA8801F},{0xE3DCF0,0x76619F},{0xD6E3F3,0x4A72A6},{0xF5DED0,0xAE6A40},{0xE6E8CC,0x7C8140},{0xEFD6E7,0x985086},{0xDCDDF3,0x5A5C9E}};
    static readonly string[] installed=new InstalledFontCollection().Families.Select(f=>f.Name).ToArray();
    static string Pick(params string[] names){return names.FirstOrDefault(n=>installed.Contains(n));}
    public static readonly string UiFamily=Pick("Leelawadee UI","Segoe UI")??"Tahoma";
    public static readonly string NumFamily=Pick("Bahnschrift SemiBold","Segoe UI Semibold")??UiFamily;
    public static readonly string SealFamily=Pick("Yu Gothic","MS Gothic");
    static Color Hex(int rgb){return Color.FromArgb((rgb>>16)&255,(rgb>>8)&255,rgb&255);}
    public static Font Ui(float points,FontStyle style=FontStyle.Regular){return new Font(UiFamily,points,style);}
    public static Font Num(float points){return new Font(NumFamily,points,FontStyle.Regular);}
    // A stride of 3 keeps consecutive rectangles in different hues.
    public static Color Region(int id,bool border){int i=(int)(((long)Math.Abs((long)id)*3)%10);return Hex(regions[i,border?1:0]);}
    public static GraphicsPath Rounded(RectangleF r,float radius){
        var path=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));if(d<=0){path.AddRectangle(r);return path;}
        path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    public static void DrawSeal(Graphics g,PointF center,float radius){
        g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;var state=g.Save();g.TranslateTransform(center.X,center.Y);g.RotateTransform(-8);
        using(var pen=new Pen(Seal,radius*.09f))g.DrawEllipse(pen,-radius,-radius,radius*2,radius*2);
        using(var pen=new Pen(Seal,radius*.03f))g.DrawEllipse(pen,-radius*.84f,-radius*.84f,radius*1.68f,radius*1.68f);
        if(SealFamily!=null)using(var font=new Font(SealFamily,radius*1.05f,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(Seal))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString("済",font,ink,0,radius*.06f,format);
        else using(var pen=new Pen(Seal,radius*.16f){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round})g.DrawLines(pen,new[]{new PointF(-radius*.42f,0),new PointF(-radius*.1f,radius*.32f),new PointF(radius*.45f,-radius*.3f)});
        g.Restore(state);
    }
}
public enum ButtonKind {Ghost,Primary,Spine}
public class PillButton : Button {
    ButtonKind kind; bool selected,hover,down; public string Detail; public bool Outlined;
    public ButtonKind Kind {get{return kind;}set{kind=value;Invalidate();}}
    public bool Selected {get{return selected;}set{selected=value;Invalidate();}}
    public PillButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=down=false;Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){down=true;Invalidate();}}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);down=false;Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;g.Clear(Parent!=null?Parent.BackColor:BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill,text,border=Color.Empty;
        if(kind==ButtonKind.Primary){fill=!Enabled?Theme.Line:down?Theme.Ink:hover?Theme.Accent:Theme.Ink;text=Color.White;}
        else if(kind==ButtonKind.Spine){fill=selected?Theme.SpineSelected:hover?Theme.SpineHover:Parent!=null?Parent.BackColor:Theme.Spine;text=selected||hover?Theme.SpineText:Theme.SpineMuted;if(Outlined)border=Theme.SpineLine;if(selected||hover||Outlined)text=Theme.SpineText;}
        else{fill=down?Theme.Line:hover&&Enabled?Theme.GhostHover:Color.White;text=Enabled?Theme.Ink:Theme.GridGroup;border=Theme.Line;}
        RectangleF box=new RectangleF(.5f,.5f,Width-1.5f,Height-1.5f);float radius=kind==ButtonKind.Spine?6:8;
        using(var path=Theme.Rounded(box,radius)){using(var brush=new SolidBrush(fill))g.FillPath(brush,path);if(border!=Color.Empty)using(var pen=new Pen(border))g.DrawPath(pen,path);}
        if(kind==ButtonKind.Spine&&selected)using(var bar=new SolidBrush(Theme.SpineFill))g.FillRectangle(bar,0,8,3,Height-16);
        if(Focused&&ShowFocusCues)using(var ring=new Pen(kind==ButtonKind.Spine?Theme.SpineFill:Theme.Accent,2))using(var path=Theme.Rounded(new RectangleF(2,2,Width-5,Height-5),radius-1))g.DrawPath(ring,path);
        if(kind==ButtonKind.Spine){
            Rectangle area=new Rectangle(14,0,Width-28,Height);
            TextRenderer.DrawText(g,Text,Font,area,text,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
            if(!string.IsNullOrEmpty(Detail))TextRenderer.DrawText(g,Detail,Font,area,selected||hover?Theme.SpineFill:Theme.SpineMuted,TextFormatFlags.Right|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
        }else TextRenderer.DrawText(g,Text,Font,ClientRectangle,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine);
    }
}
// Logo, wordmark and tagline measured and drawn with the same Graphics, so they line up at any DPI.
public class BrandMark : Control {
    public string Logo,Word,Tagline; public Font LogoFont,WordFont;
    public BrandMark(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);TabStop=false;}
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;g.Clear(Parent!=null?Parent.BackColor:Theme.Spine);g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
        using(var format=(StringFormat)StringFormat.GenericTypographic.Clone())using(var bright=new SolidBrush(Theme.SpineText))using(var dim=new SolidBrush(Theme.SpineMuted)){
            format.FormatFlags|=StringFormatFlags.NoWrap;
            SizeF logo=g.MeasureString(Logo,LogoFont,PointF.Empty,format),word=g.MeasureString(Word,WordFont,PointF.Empty,format);
            float wordX=logo.Width+WordFont.GetHeight(g)*.45f;
            // Shrink the wordmark rather than let it run past the spine.
            float fit=Math.Min(1,(Width-wordX)/Math.Max(1,word.Width));
            using(var wordFont=fit<1?new Font(WordFont.FontFamily,WordFont.Size*fit,WordFont.Style,WordFont.Unit):null){
                Font used=wordFont??WordFont;word=g.MeasureString(Word,used,PointF.Empty,format);
                g.DrawString(Logo,LogoFont,bright,0,0,format);g.DrawString(Word,used,bright,wordX,(logo.Height-word.Height)/2,format);
            }
            g.DrawString(Tagline,Font,dim,new RectangleF(1,logo.Height+Font.GetHeight(g)*.35f,Width,Height),format);
        }
    }
}
public class Meter : Control {
    int value,maximum=100; public Color Track=Theme.Line,Fill=Theme.Accent;
    public int Value {get{return value;}set{this.value=Math.Max(0,Math.Min(maximum,value));Invalidate();}}
    public int Maximum {get{return maximum;}set{maximum=Math.Max(1,value);this.value=Math.Min(this.value,maximum);Invalidate();}}
    public Meter(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw|ControlStyles.SupportsTransparentBackColor,true);TabStop=false;}
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;g.Clear(Parent!=null?Parent.BackColor:BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;float radius=Height/2f;
        using(var path=Theme.Rounded(new RectangleF(0,0,Width,Height),radius))using(var brush=new SolidBrush(Track))g.FillPath(brush,path);
        float width=Width*(float)value/maximum;if(width>=1)using(var path=Theme.Rounded(new RectangleF(0,0,Math.Max(width,Height),Height),radius))using(var brush=new SolidBrush(Fill))g.FillPath(brush,path);
    }
}
