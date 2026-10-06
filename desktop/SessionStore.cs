using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;

public class SavedRectangle { public int X,Y,W,H,Color; public SavedRectangle(){} public SavedRectangle(Rectangle r,int color){X=r.X;Y=r.Y;W=r.Width;H=r.Height;Color=color;} public Rectangle Bounds(){return new Rectangle(X,Y,W,H);} }
public class SavedClue {public int X,Y,Value;}
public class PendingReward {public string Id;public int Difficulty;}
public class SavedGame {
    public string Id;public int Difficulty;public long ElapsedSeconds;public bool Completed,RewardGranted,Sound=true;public float Scale=1;public string Daily;public int Hints;
    public List<SavedRectangle> Solution=new List<SavedRectangle>(),Placed=new List<SavedRectangle>();
    public List<SavedClue> Clues=new List<SavedClue>();public List<List<SavedRectangle>> History=new List<List<SavedRectangle>>();
    public List<PendingReward> Pending=new List<PendingReward>();
}
public sealed class SessionStore {
    readonly string path;public string Warning;
    public SessionStore(string file){path=file;}
    public SavedGame Load(){if(!File.Exists(path))return null;try{return Read(path);}catch{try{var result=Read(path+".bak");Warning="กู้คืนเกมจากไฟล์สำรองแล้ว";return result;}catch{Warning="อ่านเกมค้างไม่ได้ เก็บไฟล์เดิมไว้แล้ว";return null;}}}
    SavedGame Read(string file){using(var reader=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null})){
        var saved=(SavedGame)new XmlSerializer(typeof(SavedGame)).Deserialize(reader);Guid.Parse(saved.Id);
        if(saved.Difficulty<0||saved.Difficulty>4||saved.ElapsedSeconds<0||saved.ElapsedSeconds>315360000||saved.Hints<0)throw new InvalidDataException();
        if(saved.Daily!=null)DateTime.ParseExact(saved.Daily,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
        var puzzle=new Puzzle();Restore(saved,puzzle);if(!puzzle.ValidateGenerated())throw new InvalidDataException();
        var all=new List<List<SavedRectangle>>(saved.History){saved.Placed};foreach(var state in all){bool[] used=new bool[puzzle.Size*puzzle.Size];foreach(var r in state){if(r.W<=0||r.H<=0||r.X<0||r.Y<0||r.X+r.W>puzzle.Size||r.Y+r.H>puzzle.Size||r.Color<0)throw new InvalidDataException();for(int y=r.Y;y<r.Y+r.H;y++)for(int x=r.X;x<r.X+r.W;x++){int i=y*puzzle.Size+x;if(used[i])throw new InvalidDataException();used[i]=true;}}}
        foreach(var reward in saved.Pending){Guid.Parse(reward.Id);if(reward.Difficulty<0||reward.Difficulty>4)throw new InvalidDataException();}
        return saved;
    }}
    public static void Restore(SavedGame saved,Puzzle puzzle){puzzle.Size=new[]{5,10,20,30,40}[saved.Difficulty];puzzle.Solution=saved.Solution.Select(r=>r.Bounds()).ToList();puzzle.Clues=saved.Clues.Select(c=>new Clue{Cell=new Point(c.X,c.Y),Value=c.Value}).ToList();puzzle.Placed=saved.Placed.Select(r=>new RegionBox(r.Bounds(),r.Color)).ToList();puzzle.History.Clear();foreach(var state in saved.History.AsEnumerable().Reverse())puzzle.History.Push(state.Select(r=>new RegionBox(r.Bounds(),r.Color)).ToList());}
    public void Save(SavedGame saved){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(SavedGame)).Serialize(stream,saved);stream.Flush(true);}if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
