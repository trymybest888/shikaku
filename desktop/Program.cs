using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Media;
using System.IO;
using System.Windows.Forms;

internal static class Program {
    [STAThread] static void Main() {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        bool created;
        using(var instance=new System.Threading.Mutex(true,"Local\\Shikaku-"+Environment.UserName,out created)){
            if(!created){MessageBox.Show("Shikaku เปิดอยู่แล้ว กรุณากลับไปยังหน้าต่างเดิม", "Shikaku");return;}
            try{Application.Run(new GameWindow());}finally{instance.ReleaseMutex();}
        }
    }
}
public class RegionBox {
    public Rectangle Bounds; public int Color;
    public RegionBox(Rectangle bounds, int color) { Bounds=bounds; Color=color; }
    public RegionBox Copy() { return new RegionBox(Bounds,Color); }
}
public class Clue { public Point Cell; public int Value; }
public class Puzzle {
    public int Size; public List<Clue> Clues=new List<Clue>();
    public List<RegionBox> Placed=new List<RegionBox>();
    public List<Rectangle> Solution=new List<Rectangle>();
    public Stack<List<RegionBox>> History=new Stack<List<RegionBox>>();
    Random random=new Random();
    public void New(int size) {Create(size,null);}
    // Daily puzzles run without time limits so every machine generates the same board from the same seed.
    public void NewDaily(int size,int seed) {Create(size,new Random(seed));}
    void Create(int size,Random seeded) {
        if(size<2||size>40)throw new ArgumentOutOfRangeException("size");
        if(seeded!=null)random=seeded;
        for(int retry=0;retry<8;retry++) { Generate(size); if(ValidateGenerated()){if(UniquePuzzle.Ensure(this,random,seeded==null)&&ValidateGenerated()&&Clues.All(c=>c.Value>=2))return;} }
        throw new InvalidOperationException("ไม่สามารถสร้างโจทย์ที่ตรวจสอบคำตอบแล้วได้ กรุณาเริ่มเกมใหม่");
    }
    private void Generate(int size) {
        Size=size; Placed.Clear(); History.Clear(); Clues.Clear();
        Solution=new List<Rectangle>{new Rectangle(0,0,size,size)};
        int target=(int)Math.Ceiling(size*size/(size==5?4.0:6.0));
        for(int attempt=0;Solution.Count<target && attempt<size*size*10;attempt++) {
            int i=random.Next(Solution.Count); Rectangle r=Solution[i]; bool horizontal=random.Next(2)==0;
            int length=horizontal?r.Width:r.Height, other=horizontal?r.Height:r.Width;
            List<int> cuts=new List<int>();
            for(int k=1;k<length;k++) if(k*other>=2 && (length-k)*other>=2) cuts.Add(k);
            if(cuts.Count==0) continue; int cut=cuts[random.Next(cuts.Count)];
            Solution.RemoveAt(i);
            if(horizontal) {Solution.Add(new Rectangle(r.X,r.Y,cut,r.Height));Solution.Add(new Rectangle(r.X+cut,r.Y,r.Width-cut,r.Height));}
            else {Solution.Add(new Rectangle(r.X,r.Y,r.Width,cut));Solution.Add(new Rectangle(r.X,r.Y+cut,r.Width,r.Height-cut));}
        }
        foreach(Rectangle r in Solution) Clues.Add(new Clue{Cell=new Point(r.X+random.Next(r.Width),r.Y+random.Next(r.Height)),Value=r.Width*r.Height});
    }
    public bool ValidateGenerated() {
        if(Size<2||Solution.Count==0||Clues.Count!=Solution.Count)return false;
        if(Clues.Any(c=>c.Value<=0||c.Cell.X<0||c.Cell.Y<0||c.Cell.X>=Size||c.Cell.Y>=Size))return false;
        if(Clues.Select(c=>c.Cell).Distinct().Count()!=Clues.Count)return false;
        if(Clues.Sum(c=>(long)c.Value)!=(long)Size*Size)return false;
        bool[] occupied=new bool[Size*Size];
        foreach(Rectangle r in Solution) {
            if(r.Width<=0||r.Height<=0||r.X<0||r.Y<0||r.Right>Size||r.Bottom>Size||!Valid(r))return false;
            for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++) {
                int index=y*Size+x;if(occupied[index])return false;occupied[index]=true;
            }
        }
        return occupied.All(cell=>cell);
    }
    public void Save() { History.Push(Placed.Select(r=>r.Copy()).ToList()); }
    public void Place(Rectangle r,int color) {Save();Placed.RemoveAll(p=>p.Bounds.IntersectsWith(r));Placed.Add(new RegionBox(r,color));}
    public bool Remove(Point p) {int i=Placed.FindIndex(r=>r.Bounds.Contains(p));if(i<0)return false;Save();Placed.RemoveAt(i);return true;}
    public bool Valid(Rectangle r) {var nums=Clues.Where(c=>r.Contains(c.Cell)).ToList();return nums.Count==1&&nums[0].Value==r.Width*r.Height;}
    public bool CompleteLastRegion() {
        if(Placed.Count==0||Placed.Any(p=>!Valid(p.Bounds)))return false;
        if(Clues.Count(c=>!Placed.Any(p=>p.Bounds.Contains(c.Cell)))!=1)return false;
        List<Point> remaining=new List<Point>();
        for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)if(!Placed.Any(p=>p.Bounds.Contains(x,y)))remaining.Add(new Point(x,y));
        if(remaining.Count==0)return false;
        int left=remaining.Min(p=>p.X),top=remaining.Min(p=>p.Y),right=remaining.Max(p=>p.X),bottom=remaining.Max(p=>p.Y);
        Rectangle last=new Rectangle(left,top,right-left+1,bottom-top+1);
        if(last.Width*last.Height!=remaining.Count||!Valid(last)||Placed.Any(p=>p.Bounds.IntersectsWith(last)))return false;
        // Keep the automatic fill in the same undo step as the player's last placement.
        Placed.Add(new RegionBox(last,Placed.Max(p=>p.Color)+1));return true;
    }
    public bool Hint(int color) {
        var missing=Solution.Where(s=>!Placed.Any(p=>p.Bounds==s)).ToList();if(missing.Count==0)return false;
        // Prefer a region that also replaces one of the player's wrong rectangles.
        Rectangle pick=missing.OrderByDescending(s=>Placed.Any(p=>!Valid(p.Bounds)&&p.Bounds.IntersectsWith(s))).ThenBy(s=>s.Width*s.Height).First();
        Place(pick,color);return true;
    }
    public int Covered { get {return Placed.Sum(r=>r.Bounds.Width*r.Bounds.Height);} }
    public bool Won { get {return Covered==Size*Size&&Placed.All(r=>Valid(r.Bounds));} }
}
public class BoardControl : Control {
    public Puzzle Puzzle; public bool ShowErrors; public int NextColor;
    Point first,last; bool dragging; int dragColor;
    public event Action<bool> Changed;
    public BoardControl(Puzzle puzzle) {Puzzle=puzzle;DoubleBuffered=true;ResizeRedraw=true;Cursor=Cursors.Cross;SetStyle(ControlStyles.Selectable,true);}
    float Cell {get{return Width/(float)Puzzle.Size;}}
    Point CellAt(Point p) {return new Point(Math.Max(0,Math.Min(Puzzle.Size-1,(int)(p.X/Cell))),Math.Max(0,Math.Min(Puzzle.Size-1,(int)(p.Y/Cell))));}
    Rectangle Selection {get{return new Rectangle(Math.Min(first.X,last.X),Math.Min(first.Y,last.Y),Math.Abs(first.X-last.X)+1,Math.Abs(first.Y-last.Y)+1);}}
    public void CancelDrag(){dragging=false;Capture=false;Invalidate();}
    protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();first=last=CellAt(e.Location);dragColor=NextColor++;dragging=true;Capture=true;Invalidate();}
    protected override void OnMouseMove(MouseEventArgs e) {base.OnMouseMove(e);if(!dragging)return;last=CellAt(e.Location);Invalidate();}
    protected override void OnMouseUp(MouseEventArgs e) {base.OnMouseUp(e);if(e.Button!=MouseButtons.Left||!dragging)return;last=CellAt(e.Location);Rectangle r=Selection;dragging=false;Capture=false;ShowErrors=false;bool placed=true;if(r.Width==1&&r.Height==1&&Puzzle.Remove(last))placed=false;else Puzzle.Place(r,dragColor);Invalidate();if(Changed!=null)Changed(placed);}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture&&dragging){dragging=false;Invalidate();}}
    RectangleF Pixels(Rectangle r){return new RectangleF(r.X*Cell,r.Y*Cell,r.Width*Cell,r.Height*Cell);}
    void PaintRegion(Graphics g,Rectangle r,int color,bool preview) {RectangleF p=Pixels(r);using(var brush=new SolidBrush(preview?Color.FromArgb(185,Theme.Region(color,false)):Theme.Region(color,false)))g.FillRectangle(brush,p);using(var pen=new Pen(Theme.Region(color,true),2))g.DrawRectangle(pen,p.X+1,p.Y+1,Math.Max(0,p.Width-2),Math.Max(0,p.Height-2));}
    public static Font FitClueFont(Graphics graphics,string text,float cell,StringFormat format) {
        float size=Math.Max(1,Math.Min(26,cell*.70f));
        Font font=new Font(Theme.NumFamily,size,FontStyle.Regular,GraphicsUnit.Pixel);
        SizeF measured=graphics.MeasureString(text,font,PointF.Empty,format);
        float factor=Math.Min(1,Math.Min(cell*.84f/measured.Width,cell*.84f/measured.Height));
        if(factor<1){font.Dispose();font=new Font(Theme.NumFamily,Math.Max(.5f,size*factor),FontStyle.Regular,GraphicsUnit.Pixel);}
        return font;
    }
    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);if(Puzzle.Size==0)return;Graphics g=e.Graphics;g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        foreach(var r in Puzzle.Placed)PaintRegion(g,r.Bounds,r.Color,false);
        if(dragging)PaintRegion(g,Selection,dragColor,true);
        using(var thin=new Pen(Theme.GridThin,.8f))using(var group=new Pen(Theme.GridGroup,1.2f))for(int i=0;i<=Puzzle.Size;i++){Pen pen=Puzzle.Size>=10&&i%5==0?group:thin;g.DrawLine(pen,i*Cell,0,i*Cell,Height);g.DrawLine(pen,0,i*Cell,Width,i*Cell);}
        using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}) {
            using(var textFormat=(StringFormat)StringFormat.GenericTypographic.Clone())using(var ink=new SolidBrush(Theme.Ink)) {
                textFormat.Alignment=StringAlignment.Center;textFormat.LineAlignment=StringAlignment.Center;
                textFormat.FormatFlags|=StringFormatFlags.NoWrap;
                foreach(var c in Puzzle.Clues)using(var font=FitClueFont(g,c.Value.ToString(),Cell,textFormat))
                    g.DrawString(c.Value.ToString(),font,ink,new PointF((c.Cell.X+.5f)*Cell,(c.Cell.Y+.5f)*Cell),textFormat);
            }
            if(dragging){RectangleF p=Pixels(Selection);float size=Math.Max(18,Math.Min(34,Cell*.45f));using(var font=new Font(Theme.NumFamily,size,FontStyle.Regular,GraphicsUnit.Pixel))using(var path=new GraphicsPath()){path.AddString((Selection.Width*Selection.Height).ToString(),font.FontFamily,(int)FontStyle.Regular,size,new PointF(p.X+p.Width/2,p.Y+p.Height/2-size*.65f),new StringFormat{Alignment=StringAlignment.Center});using(var outline=new Pen(Color.White,3){LineJoin=LineJoin.Round})g.DrawPath(outline,path);using(var ink=new SolidBrush(Theme.Accent))g.FillPath(ink,path);}}
        }
        if(ShowErrors)using(var pen=new Pen(Theme.Seal,3){DashStyle=DashStyle.Dash})foreach(var r in Puzzle.Placed.Where(p=>!Puzzle.Valid(p.Bounds))){RectangleF p=Pixels(r.Bounds);g.DrawRectangle(pen,p.X+2,p.Y+2,Math.Max(0,p.Width-4),Math.Max(0,p.Height-4));}
        using(var border=new Pen(Theme.Ink,2))g.DrawRectangle(border,1,1,Width-2,Height-2);
    }
}
public class GameWindow : Form {
    readonly PlayerProgress playerProgress; readonly SessionStore session; readonly List<PendingReward> pending=new List<PendingReward>(); Guid gameId; bool rewardGranted; string rewardMessage=""; string daily; int hints;
    readonly Puzzle puzzle=new Puzzle(); readonly string[] names={"Easy","Medium","Hard","Expert","Master"}; readonly int[] sizes={5,10,20,30,40};
    readonly Panel side=new Panel(),content=new Panel(),viewport=new Panel(),top=new Panel(),bottom=new Panel();
    readonly Label title=new Label(),stats=new Label(),clock=new Label(),status=new Label(),scaleLabel=new Label(),profileLevel=new Label(),profileExp=new Label(); Label rules;
    readonly Meter progress=new Meter(),profileMeter=new Meter(); readonly FlowLayoutPanel views=new FlowLayoutPanel(),actions=new FlowLayoutPanel();
    readonly List<PillButton> levelButtons=new List<PillButton>(); Button undo,minus,plus,sound,exitFull,retrySave; PillButton dailyButton;
    readonly BoardControl board; readonly Timer timer=new Timer(); readonly SoundPlayer player; readonly MemoryStream audio;
    int level; float scale=1,previousScale=1; DateTime started; bool completed,soundOn=true,full,layingOut,initialized,generating,replaceLegacyMaster;long finishedSeconds;DateTime lastSave;
    FormBorderStyle oldBorder; FormWindowState oldState; Rectangle oldBounds;
    static readonly Color Ink=Theme.Ink,Muted=Theme.Muted;
    public GameWindow():this(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Shikaku")){}
    public GameWindow(string storage) {
        playerProgress=new PlayerProgress(Path.Combine(storage,"progress.xml"));session=new SessionStore(Path.Combine(storage,"session.xml"));AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
        Text="Shikaku";ClientSize=new Size(1200,850);MinimumSize=new Size(900,600);StartPosition=FormStartPosition.CenterScreen;BackColor=Theme.Paper;Font=Theme.Ui(10);KeyPreview=true;
        board=new BoardControl(puzzle);audio=CreateSound();player=new SoundPlayer(audio);player.Load();
        // The left spine holds the brand, the level picker and the player's progress, like the spine of a puzzle book.
        side.Dock=DockStyle.Left;side.Width=236;side.BackColor=Theme.Spine;Controls.Add(side);
        Label mark=new Label{Text=Theme.SealFamily!=null?"四角":"▦",Font=new Font(Theme.SealFamily??Theme.UiFamily,20,FontStyle.Bold),ForeColor=Theme.SpineText,AutoSize=true,Location=new Point(14,16)};
        Label brand=new Label{Text="Shikaku",Font=Theme.Ui(13,FontStyle.Bold),ForeColor=Theme.SpineText,AutoSize=true,Location=new Point(86,24)};
        Label tagline=new Label{Text="ปริศนาแบ่งสี่เหลี่ยม",ForeColor=Theme.SpineMuted,AutoSize=true,Location=new Point(18,68)};
        Label pick=new Label{Text="เลือกระดับ",ForeColor=Theme.SpineMuted,AutoSize=true,Location=new Point(20,116)};side.Controls.AddRange(new Control[]{mark,brand,tagline,pick});
        for(int i=0;i<5;i++){int choice=i;PillButton b=MakeButton(names[i],()=>RequestNewGame(choice),ButtonKind.Spine);b.Detail=sizes[i]+" × "+sizes[i];b.AutoSize=false;b.SetBounds(12,142+i*42,212,38);side.Controls.Add(b);levelButtons.Add(b);}
        dailyButton=MakeButton("โจทย์ประจำวัน",()=>RequestDaily(),ButtonKind.Spine);dailyButton.Detail="★";dailyButton.AutoSize=false;dailyButton.SetBounds(12,142+5*42+10,212,38);side.Controls.Add(dailyButton);
        rules=new Label{Text="ลากเพื่อวาดสี่เหลี่ยม\nแต่ละรูปต้องมีตัวเลขหนึ่งตัว\nและมีจำนวนช่องเท่ากับตัวเลข\nคลิกรูปเดิมเพื่อลบ\nคำใบ้ช่วยเติมให้ 1 รูป\n(กระดานนั้นไม่ได้ EXP)",ForeColor=Theme.SpineMuted,Location=new Point(20,418),Size=new Size(210,132)};side.Controls.Add(rules);
        Panel profile=new Panel{Dock=DockStyle.Bottom,Height=132,BackColor=Theme.SpineDeep};side.Controls.Add(profile);
        profileLevel.SetBounds(16,16,204,26);profileLevel.Font=Theme.Ui(11,FontStyle.Bold);profileLevel.ForeColor=Theme.SpineText;
        profileMeter.SetBounds(16,46,204,6);profileMeter.Track=Theme.SpineLine;profileMeter.Fill=Theme.SpineFill;
        profileExp.SetBounds(16,56,204,22);profileExp.ForeColor=Theme.SpineMuted;
        PillButton achievements=MakeButton("ดูสถิติและยศ",()=>ShowAchievements(),ButtonKind.Spine);achievements.Outlined=true;achievements.AutoSize=false;achievements.SetBounds(12,86,212,34);
        profile.Controls.AddRange(new Control[]{profileLevel,profileMeter,profileExp,achievements});
        side.Resize+=(s,e)=>rules.Visible=rules.Bottom+8<=side.ClientSize.Height-profile.Height;
        Shown+=(s,e)=>top.PerformLayout();
        content.Dock=DockStyle.Fill;content.Padding=new Padding(24,18,24,12);content.BackColor=Theme.Paper;Controls.Add(content);content.BringToFront();
        top.Dock=DockStyle.Top;top.Height=96;content.Controls.Add(top);
        title.SetBounds(0,0,420,40);title.Font=Theme.Ui(19,FontStyle.Bold);title.ForeColor=Ink;stats.SetBounds(0,46,420,24);stats.ForeColor=Muted;
        clock.Font=Theme.Num(26);clock.ForeColor=Ink;clock.TextAlign=ContentAlignment.MiddleRight;clock.SetBounds(0,0,180,44);
        views.FlowDirection=FlowDirection.RightToLeft;views.WrapContents=false;views.SetBounds(0,52,460,40);top.Controls.AddRange(new Control[]{title,stats,clock,views});
        top.Layout+=(s,e)=>{int width=top.ClientSize.Width;clock.Left=width-clock.Width;views.Left=width-views.Width;title.Width=Math.Max(120,clock.Left-16);stats.Width=Math.Max(120,views.Left-16);};
        minus=MakeButton("−",()=>SetScale(scale-.25f));plus=MakeButton("+",()=>SetScale(scale+.25f));scaleLabel.TextAlign=ContentAlignment.MiddleCenter;scaleLabel.Size=new Size(58,34);scaleLabel.ForeColor=Muted;scaleLabel.Margin=new Padding(0,0,8,0);
        views.Controls.AddRange(new Control[]{MakeButton("เต็มจอ",()=>ToggleFull()),MakeButton("พอดีจอ",()=>SetScale(1)),plus,scaleLabel,minus});
        bottom.Dock=DockStyle.Bottom;bottom.Height=104;content.Controls.Add(bottom);progress.Dock=DockStyle.Top;progress.Height=4;bottom.Controls.Add(progress);
        actions.SetBounds(0,18,850,44);actions.WrapContents=false;actions.Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right;bottom.Controls.Add(actions);
        Button fresh=MakeButton("เกมใหม่",()=>RequestNewGame(level),ButtonKind.Primary);
        undo=MakeButton("ย้อนกลับ",()=>{if(puzzle.History.Count>0){puzzle.Placed=puzzle.History.Pop();completed=false;board.ShowErrors=false;RefreshState();board.Invalidate();status.Text="ย้อนกลับแล้ว";}});
        sound=MakeButton("เสียง: เปิด",()=>{soundOn=!soundOn;sound.Text="เสียง: "+(soundOn?"เปิด":"ปิด");Persist();});
        retrySave=MakeButton("ลองบันทึกใหม่",()=>{Persist();RetryRewards();});
        actions.Controls.AddRange(new Control[]{fresh,undo,MakeButton("ล้างกระดาน",()=>{if(puzzle.Placed.Count>0&&MessageBox.Show(this,"ล้างรูปที่วางทั้งหมดหรือไม่? ย้อนกลับได้", "ล้างกระดาน",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){puzzle.Save();puzzle.Placed.Clear();completed=false;board.ShowErrors=false;board.Invalidate();RefreshState();}}),MakeButton("ตรวจคำตอบ",CheckAnswer),MakeButton("คำใบ้",UseHint),sound,retrySave});
        status.SetBounds(0,70,850,28);status.Anchor=AnchorStyles.Left|AnchorStyles.Right|AnchorStyles.Top;status.ForeColor=Muted;bottom.Controls.Add(status);
        viewport.Dock=DockStyle.Fill;viewport.BackColor=Theme.Paper;viewport.AutoScroll=true;content.Controls.Add(viewport);viewport.BringToFront();viewport.Controls.Add(board);
        exitFull=MakeButton("ออกจากเต็มจอ (Esc)",()=>ToggleFull());exitFull.Visible=false;viewport.Controls.Add(exitFull);
        // A flat offset shadow lifts the board off the desk like a sheet of paper.
        viewport.Paint+=(s,e)=>{using(var shade=new SolidBrush(Theme.Shadow))e.Graphics.FillRectangle(shade,board.Left+5,board.Top+5,board.Width,board.Height);};
        viewport.Resize+=(s,e)=>LayoutBoard();board.Changed+=placed=>{if(placed&&soundOn){try{player.Play();}catch{}}status.Text=placed?"วางสี่เหลี่ยมแล้ว · กดตรวจคำตอบเมื่อต้องการ":"ลบสี่เหลี่ยมแล้ว";RefreshState();};
        timer.Interval=250;timer.Tick+=(s,e)=>{if(!completed)clock.Text=Elapsed();if(initialized&&(DateTime.Now-lastSave).TotalSeconds>=10)Persist();};timer.Start();KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape&&full){ToggleFull();e.Handled=true;}};
        LoadGame();Shown+=(s,e)=>{Rectangle work=Screen.FromControl(this).WorkingArea;Size=new Size(Math.Min(Width,work.Width),Math.Min(Height,work.Height));LayoutBoard();RetryRewards();if(replaceLegacyMaster)RequestNewGame(level);};UpdateProfile();FormClosing+=(s,e)=>{if(!Persist()&&MessageBox.Show(this,"บันทึกเกมไม่สำเร็จ ต้องการปิดและทิ้งการเปลี่ยนแปลงหรือไม่?", "บันทึกเกม",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)e.Cancel=true;};
    }
    PillButton MakeButton(string text,Action action,ButtonKind kind=ButtonKind.Ghost){PillButton b=new PillButton{Text=text,Kind=kind,AutoSize=true,Height=34,MinimumSize=new Size(40,34),Padding=new Padding(10,2,10,2),Margin=new Padding(0,0,8,0)};b.Click+=(s,e)=>action();return b;}
    void UpdateProfile(){profileLevel.Text="Level "+playerProgress.LevelNumber+"  "+playerProgress.CurrentLevel.Name;profileMeter.Maximum=PlayerProgress.ExpPerLevel;profileMeter.Value=(int)playerProgress.ExpInLevel;profileExp.Text=playerProgress.ExpInLevel.ToString("N0")+" / "+PlayerProgress.ExpPerLevel.ToString("N0")+" EXP";}
    string Elapsed(){TimeSpan t=TimeSpan.FromSeconds(completed?finishedSeconds:Math.Max(0,(DateTime.Now-started).TotalSeconds));return ((int)t.TotalMinutes).ToString("00")+":"+t.Seconds.ToString("00");}
    void NewGame(int chosen){var generated=new Puzzle();generated.New(sizes[chosen]);ApplyNewPuzzle(chosen,generated);}
    void ApplyNewPuzzle(int chosen,Puzzle generated,string date=null){initialized=false;daily=date;hints=0;gameId=date==null?Guid.NewGuid():DailyId(date);rewardGranted=false;rewardMessage="";level=chosen;puzzle.Size=generated.Size;puzzle.Solution=generated.Solution;puzzle.Clues=generated.Clues;puzzle.Placed.Clear();puzzle.History.Clear();board.NextColor=0;board.ShowErrors=false;board.CancelDrag();scale=1;completed=false;finishedSeconds=0;started=DateTime.Now;clock.Text="00:00";title.Text=TitleText();HighlightLevel();status.Text=playerProgress.Warning??"ลากบนกระดานเพื่อสร้างสี่เหลี่ยมรูปแรก";initialized=true;RefreshState();LayoutBoard();board.Invalidate();Persist();}
    void RefreshState(){
        if(puzzle.CompleteLastRegion()){board.NextColor=Math.Max(board.NextColor,puzzle.Placed.Max(p=>p.Color)+1);board.Invalidate();status.Text="เติมสี่เหลี่ยมสุดท้ายให้อัตโนมัติแล้ว";}
        stats.Text=puzzle.Size+" × "+puzzle.Size+" ช่อง     วางแล้ว "+puzzle.Placed.Count+" จาก "+puzzle.Clues.Count+" รูป     "+(puzzle.Covered*100/(puzzle.Size*puzzle.Size))+"%";
        progress.Value=puzzle.Covered*100/(puzzle.Size*puzzle.Size);undo.Enabled=puzzle.History.Count>0;
        if(!puzzle.Won){completed=false;Persist();return;}if(completed){Persist();return;}
        finishedSeconds=(long)(DateTime.Now-started).TotalSeconds;completed=true;clock.Text=Elapsed();
        if(hints>0)rewardMessage="ใช้คำใบ้ "+hints+" ครั้ง · กระดานนี้ไม่ได้รับ EXP";
        else if(!rewardGranted&&!pending.Any(p=>p.Id==gameId.ToString()))pending.Add(new PendingReward{Id=gameId.ToString(),Difficulty=level});
        if(Persist())RetryRewards();else status.Text=rewardMessage;
        if(IsHandleCreated)BeginInvoke((Action)(()=>ShowWin()));
    }
    void RequestNewGame(int chosen){StartGeneration(chosen,null);}
    void RequestDaily(){string today=DateTime.Today.ToString("yyyy-MM-dd");if(daily==today){status.Text="กำลังเล่นโจทย์ประจำวัน "+today+" อยู่แล้ว";return;}StartGeneration(1,today);}
    static int DailySeed(string date){return int.Parse(date.Replace("-",""));}
    // One fixed id per day, so the daily EXP can only be awarded once.
    static Guid DailyId(string date){using(var md5=System.Security.Cryptography.MD5.Create())return new Guid(md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes("shikaku-daily-"+date)));}
    string TitleText(){return daily!=null?"โจทย์ประจำวัน · "+daily:names[level];}
    void HighlightLevel(){for(int i=0;i<5;i++)levelButtons[i].Selected=daily==null&&i==level;dailyButton.Selected=daily!=null;}
    void UseHint(){
        if(generating||puzzle.Won)return;
        if(hints==0&&!rewardGranted&&MessageBox.Show(this,"ใช้คำใบ้แล้วกระดานนี้จะไม่ได้รับ EXP ต้องการใช้คำใบ้หรือไม่?", "คำใบ้",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        board.CancelDrag();if(!puzzle.Hint(board.NextColor++))return;hints++;board.ShowErrors=false;board.Invalidate();
        status.Text="คำใบ้: เติมสี่เหลี่ยมที่ถูกต้องให้ 1 รูป · ใช้คำใบ้แล้ว "+hints+" ครั้ง";RefreshState();
    }
    async void StartGeneration(int chosen,string date){
        if(generating)return;if(pending.Count>0&&!Persist())return;
        if(puzzle.Placed.Count>0&&!puzzle.Won&&MessageBox.Show(this,"เริ่มกระดานใหม่หรือไม่? กระดานปัจจุบันจะถูกแทนที่", "เกมใหม่",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
        generating=true;content.Enabled=false;side.Enabled=false;UseWaitCursor=true;status.Text="กำลังสร้าง "+(date!=null?"โจทย์ประจำวัน":names[chosen])+" · หน้าต่างยังตอบสนอง กรุณารอสักครู่";
        try{var generated=await System.Threading.Tasks.Task.Run(()=>{var result=new Puzzle();if(date==null)result.New(sizes[chosen]);else result.NewDaily(sizes[chosen],DailySeed(date));return result;});if(!IsDisposed&&!Disposing)ApplyNewPuzzle(chosen,generated,date);}
        catch(Exception error){if(!IsDisposed)status.Text="สร้างโจทย์ไม่สำเร็จ: "+error.Message;}
        finally{if(!IsDisposed){generating=false;content.Enabled=true;side.Enabled=true;UseWaitCursor=false;}}
    }
    SavedGame Snapshot(){return new SavedGame{Id=gameId.ToString(),Difficulty=level,ElapsedSeconds=completed?finishedSeconds:(long)(DateTime.Now-started).TotalSeconds,Completed=completed,RewardGranted=rewardGranted,Daily=daily,Hints=hints,Sound=soundOn,Scale=scale,Solution=puzzle.Solution.Select(r=>new SavedRectangle(r,0)).ToList(),Clues=puzzle.Clues.Select(c=>new SavedClue{X=c.Cell.X,Y=c.Cell.Y,Value=c.Value}).ToList(),Placed=puzzle.Placed.Select(r=>new SavedRectangle(r.Bounds,r.Color)).ToList(),History=puzzle.History.Select(state=>state.Select(r=>new SavedRectangle(r.Bounds,r.Color)).ToList()).ToList(),Pending=pending.Select(p=>new PendingReward{Id=p.Id,Difficulty=p.Difficulty}).ToList()};}
    bool Persist(){if(!initialized)return true;try{session.Save(Snapshot());lastSave=DateTime.Now;retrySave.Visible=pending.Count>0;return true;}catch(Exception error){status.Text="บันทึกเกมไม่สำเร็จ: "+error.Message;rewardMessage="รางวัลรอบันทึก · กดลองบันทึกใหม่";retrySave.Visible=true;return false;}}
    void RetryRewards(){if(pending.Count==0)return;if(!Persist())return;foreach(var reward in pending.ToList()){try{bool fresh=playerProgress.Award(Guid.Parse(reward.Id),reward.Difficulty);pending.Remove(reward);if(reward.Id==gameId.ToString())rewardGranted=true;rewardMessage=fresh?"+"+PlayerProgress.ExpRewards[reward.Difficulty]+" EXP · EXP รวม "+playerProgress.TotalExp.ToString("N0"):"กระดานนี้รับ EXP ไปแล้ว";}catch(Exception error){rewardMessage="รางวัลรอบันทึก: "+error.Message;status.Text=rewardMessage;break;}}Persist();retrySave.Visible=pending.Count>0;UpdateProfile();}
    void LoadGame(){SavedGame saved=session.Load();if(saved==null){NewGame(0);if(session.Warning!=null)status.Text=session.Warning;return;}SessionStore.Restore(saved,puzzle);replaceLegacyMaster=puzzle.Clues.Any(c=>c.Value==1)||saved.Difficulty==4&&saved.Placed.Count==0&&puzzle.Solution.All(r=>r.Height==1)&&puzzle.Clues.GroupBy(c=>c.Cell.Y).Select(row=>string.Join(",",row.OrderBy(c=>c.Cell.X).Select(c=>c.Cell.X+":"+c.Value))).Distinct().Count()==1;gameId=Guid.Parse(saved.Id);level=saved.Difficulty;completed=saved.Completed&&puzzle.Won;finishedSeconds=saved.ElapsedSeconds;started=DateTime.Now-TimeSpan.FromSeconds(saved.ElapsedSeconds);rewardGranted=saved.RewardGranted;daily=saved.Daily;hints=saved.Hints;pending.AddRange(saved.Pending);scale=Math.Max(1,Math.Min(4,saved.Scale));soundOn=saved.Sound;sound.Text="เสียง: "+(soundOn?"เปิด":"ปิด");board.NextColor=puzzle.Placed.Count==0?0:puzzle.Placed.Max(r=>r.Color)+1;title.Text=TitleText();HighlightLevel();initialized=true;RefreshState();clock.Text=Elapsed();status.Text=session.Warning??"เล่นต่อจากกระดานที่บันทึกไว้";LayoutBoard();}
    void CheckAnswer(){board.ShowErrors=true;board.Invalidate();RefreshState();int wrong=puzzle.Placed.Count(r=>!puzzle.Valid(r.Bounds));status.Text=wrong>0?"มี "+wrong+" รูปที่ยังไม่ถูกต้อง (กรอบแดง)":"สี่เหลี่ยมที่วางถูกต้องแล้ว · เหลือ "+(puzzle.Size*puzzle.Size-puzzle.Covered)+" ช่อง";}
    void SetScale(float value){scale=Math.Max(1,Math.Min(4,value));board.CancelDrag();LayoutBoard();Persist();}
    void LayoutBoard(){if(layingOut||viewport.ClientSize.Width<30||viewport.ClientSize.Height<30)return;layingOut=true;try{viewport.AutoScrollPosition=Point.Empty;int size=(int)(Math.Min(viewport.ClientSize.Width-28,viewport.ClientSize.Height-28)*scale);size=Math.Max(40,size);board.Size=new Size(size,size);board.Location=new Point(Math.Max(12,(viewport.ClientSize.Width-size)/2),Math.Max(12,(viewport.ClientSize.Height-size)/2));viewport.AutoScrollMinSize=new Size(size+24,size+24);viewport.AutoScrollPosition=new Point(Math.Max(0,(size+24-viewport.ClientSize.Width)/2),Math.Max(0,(size+24-viewport.ClientSize.Height)/2));scaleLabel.Text=(scale*100).ToString("0")+"%";minus.Enabled=scale>1;plus.Enabled=scale<4;exitFull.Location=new Point(Math.Max(4,viewport.ClientSize.Width-exitFull.Width-12),12);exitFull.BringToFront();board.Invalidate();viewport.Invalidate();}finally{layingOut=false;}}
    void ToggleFull(){board.CancelDrag();if(!full){previousScale=scale;oldBorder=FormBorderStyle;oldState=WindowState;oldBounds=Bounds;full=true;side.Visible=top.Visible=bottom.Visible=false;content.Padding=Padding.Empty;WindowState=FormWindowState.Normal;FormBorderStyle=FormBorderStyle.None;Bounds=Screen.FromControl(this).Bounds;scale=1;exitFull.Visible=true;}else{full=false;exitFull.Visible=false;FormBorderStyle=oldBorder;Bounds=oldBounds;WindowState=oldState;side.Visible=top.Visible=bottom.Visible=true;content.Padding=new Padding(24,18,24,12);scale=previousScale;}LayoutBoard();}
    void ShowWin(){if(!completed||!puzzle.Won||IsDisposed)return;using(var dialog=new Form{Text="สำเร็จ",ClientSize=new Size(440,350),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,ShowInTaskbar=false,BackColor=Color.White,Font=Font}){
        Panel seal=new Panel{Dock=DockStyle.Top,Height=120};seal.Paint+=(s,e)=>Theme.DrawSeal(e.Graphics,new PointF(seal.ClientSize.Width/2f,66),44);
        Label caption=new Label{Text="ทำสำเร็จแล้ว",Font=Theme.Ui(17,FontStyle.Bold),ForeColor=Ink,TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Top,Height=46};
        Label detail=new Label{Text=TitleText()+"  "+puzzle.Size+" × "+puzzle.Size+"\nเวลา "+Elapsed()+"\n"+rewardMessage,ForeColor=Muted,TextAlign=ContentAlignment.TopCenter,Dock=DockStyle.Top,Height=76};
        dialog.Controls.Add(detail);dialog.Controls.Add(caption);dialog.Controls.Add(seal);
        PillButton again=MakeButton("เล่นกระดานใหม่",()=>{dialog.DialogResult=DialogResult.Retry;dialog.Close();},ButtonKind.Primary);again.AutoSize=false;again.SetBounds(110,250,220,38);dialog.Controls.Add(again);
        PillButton close=MakeButton("ดูกระดานที่เล่นจบ",()=>dialog.Close());close.AutoSize=false;close.SetBounds(110,296,220,36);dialog.Controls.Add(close);
        dialog.AcceptButton=again;dialog.CancelButton=close;if(dialog.ShowDialog(this)==DialogResult.Retry)RequestNewGame(level);}}
    void ShowAchievements(){using(Form window=CreateAchievementWindow())window.ShowDialog(this);UpdateProfile();}
    public Form CreateAchievementWindow(){
        Form window=new Form{Text="สถิติและยศ",ClientSize=new Size(650,Math.Min(734,Screen.FromControl(this).WorkingArea.Height-100)),MinimumSize=new Size(480,360),StartPosition=FormStartPosition.CenterParent,BackColor=Theme.Paper,Font=Font,MaximizeBox=false,AutoScaleMode=AutoScaleMode.Dpi};
        Panel achievementBody=new Panel{Dock=DockStyle.Fill,AutoScroll=true};window.Controls.Add(achievementBody);
        Label heading=new Label{Text="สถิติและยศ",Font=Theme.Ui(20,FontStyle.Bold),ForeColor=Ink,Location=new Point(24,20),Size=new Size(550,42)};achievementBody.Controls.Add(heading);
        Label subtitle=new Label{Text="ทุกกระดานที่ชนะ คือความก้าวหน้าของคุณ",ForeColor=Muted,Location=new Point(26,68),Size=new Size(550,26)};achievementBody.Controls.Add(subtitle);
        Panel summary=new Panel{BackColor=Color.White,Location=new Point(24,106),Size=new Size(578,112)};achievementBody.Controls.Add(summary);
        Label totals=new Label{Text="Level "+playerProgress.LevelNumber+" · "+playerProgress.CurrentLevel.Name+"\nชนะ "+playerProgress.TotalWins.ToString("N0")+" กระดาน · "+playerProgress.TotalExp.ToString("N0")+" EXP",Font=Theme.Ui(14,FontStyle.Bold),ForeColor=Ink,Location=new Point(132,26),AutoSize=true};summary.Controls.Add(totals);string rankId=playerProgress.LevelNumber>=21?"divine":playerProgress.LevelNumber>=11?"professional":playerProgress.LevelNumber>=6?"skilled":"novice";AddRankPicture(summary,rankId,new Rectangle(6,6,100,100));
        TableLayoutPanel table=new TableLayoutPanel{Location=new Point(24,234),Size=new Size(578,228),ColumnCount=3,RowCount=6,BackColor=Color.White,CellBorderStyle=TableLayoutPanelCellBorderStyle.Single};table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,30));
        for(int row=0;row<6;row++)table.RowStyles.Add(new RowStyle(SizeType.Percent,100f/6));
        string[] headers={"ระดับ / ขนาด","ชนะแล้ว","EXP ต่อกระดาน"};for(int col=0;col<3;col++)table.Controls.Add(new Label{Text=headers[col],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Ink,BackColor=Theme.TableHead},col,0);
        for(int row=0;row<5;row++){string[] values={names[row]+"  "+sizes[row]+" × "+sizes[row],playerProgress.Wins[row].ToString("N0")+" กระดาน","+"+PlayerProgress.ExpRewards[row].ToString("N0")+" EXP"};for(int col=0;col<3;col++)table.Controls.Add(new Label{Text=values[col],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Ink},col,row+1);}achievementBody.Controls.Add(table);
        string levelText="Level "+playerProgress.LevelNumber+" · "+playerProgress.CurrentLevel.Name;
        achievementBody.Controls.Add(new Label{Text=levelText+"\n"+playerProgress.ExpInLevel.ToString("N0")+" / 1,000 EXP · อีก "+playerProgress.ExpToNextLevel.ToString("N0")+" EXP ถึง Level ถัดไป",ForeColor=Muted,Location=new Point(26,478),Size=new Size(570,44)});
        Meter levelProgress=new Meter{Location=new Point(26,526),Size=new Size(576,6),Maximum=1000,Value=(int)playerProgress.ExpInLevel};achievementBody.Controls.Add(levelProgress);
        string[] rankIds={"novice","skilled","professional","divine"};string[] rankTitles={"มือใหม่","ผู้ชำนาญ","มืออาชีพ","เทพเจ้า"};string[] bands={"Level 1–5","Level 6–10","Level 11–20","Level 21+"};
        var rankCards=new List<Panel>();for(int i=0;i<4;i++){Panel card=new Panel{Location=new Point(24+i*145,546),Size=new Size(140,124),BackColor=Color.White};AddRankPicture(card,rankIds[i],new Rectangle(29,0,82,82));card.Controls.Add(new Label{Text=rankTitles[i]+"\n"+bands[i],Location=new Point(0,84),Size=new Size(140,40),TextAlign=ContentAlignment.MiddleCenter,ForeColor=Ink});achievementBody.Controls.Add(card);rankCards.Add(card);}
        Button back=MakeButton("กลับไปเล่น",()=>window.Close(),ButtonKind.Primary);Panel footer=new Panel{Dock=DockStyle.Bottom,Height=54,Padding=new Padding(12)};back.Dock=DockStyle.Right;back.Width=174;footer.Controls.Add(back);window.Controls.Add(footer);achievementBody.BringToFront();achievementBody.AutoScrollMinSize=new Size(620,680);bool sizing=false;Action resizeAchievements=()=>{if(sizing)return;sizing=true;try{
            int available=Math.Max(300,window.ClientSize.Width-48-SystemInformation.VerticalScrollBarWidth);
            heading.Width=subtitle.Width=available;summary.Width=table.Width=available;
            totals.AutoSize=false;totals.Size=new Size(Math.Max(120,available-144),82);totals.Font=Theme.Ui(available<480?11:14,FontStyle.Bold);
            foreach(Control item in achievementBody.Controls){if(item is Label&&item.Top==478)item.Width=available;}
            levelProgress.Width=available;int columns=available<550?2:4,cardWidth=(available-8*(columns-1))/columns;
            for(int i=0;i<rankCards.Count;i++){Panel card=rankCards[i];card.SetBounds(24+(i%columns)*(cardWidth+8),546+(i/columns)*132,cardWidth,124);foreach(Control child in card.Controls){if(child is PictureBox)child.Left=(cardWidth-child.Width)/2;else if(child is Label)child.Width=cardWidth;}}
            achievementBody.AutoScrollMinSize=new Size(0,546+((4+columns-1)/columns)*132+12);
        }finally{sizing=false;}};window.Resize+=(sender,args)=>resizeAchievements();resizeAchievements();window.AcceptButton=back;window.CancelButton=back;return window;
    }
    static void AddRankPicture(Control parent,string id,Rectangle bounds){
        using(Stream stream=typeof(GameWindow).Assembly.GetManifestResourceStream("Shikaku.Rank."+id))using(Image original=Image.FromStream(stream)){
            PictureBox picture=new PictureBox{Image=new Bitmap(original),Bounds=bounds,SizeMode=PictureBoxSizeMode.Zoom};
            picture.Disposed+=(sender,args)=>picture.Image.Dispose();parent.Controls.Add(picture);
        }
    }
    static MemoryStream CreateSound(){int rate=22050,count=(int)(rate*.20);MemoryStream stream=new MemoryStream();using(BinaryWriter writer=new BinaryWriter(stream,System.Text.Encoding.ASCII,true)){writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);for(int i=0;i<count;i++){double t=i/(double)rate;double value=Math.Sin(2*Math.PI*(t<.06?660:880)*t)*Math.Exp(-t*23)*.13;writer.Write((short)(value*32767));}}stream.Position=0;return stream;}
    protected override void Dispose(bool disposing){if(disposing){timer.Dispose();player.Dispose();audio.Dispose();}base.Dispose(disposing);}
}
