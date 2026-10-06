using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

// Level names and thresholds can be configured later without changing saved EXP.
public sealed class LevelDefinition {
    public string Name; public long RequiredExp;
}
public sealed class PlayerProgress {
    public static readonly int[] ExpRewards={25,100,400,900,1600};
    public const int ExpPerLevel=1000;
    public static readonly List<LevelDefinition> Levels=new List<LevelDefinition>{
        new LevelDefinition{Name="มือใหม่",RequiredExp=0},
        new LevelDefinition{Name="ผู้ชำนาญ",RequiredExp=5000},
        new LevelDefinition{Name="มืออาชีพ",RequiredExp=10000},
        new LevelDefinition{Name="เทพเจ้า",RequiredExp=20000}
    };
    public long LevelNumber {get{return TotalExp/ExpPerLevel+1;}}
    public long ExpInLevel {get{return TotalExp%ExpPerLevel;}}
    public long ExpToNextLevel {get{return ExpPerLevel-ExpInLevel;}}
    public long[] Wins=new long[5];
    readonly HashSet<string> awarded=new HashSet<string>();
    readonly string path;
    public string Warning {get;private set;}
    public long TotalWins {get{return Wins.Sum();}}
    public long TotalExp {get{return Wins.Select((count,i)=>checked(count*ExpRewards[i])).Sum();}}
    public LevelDefinition CurrentLevel {get{return Levels.Where(l=>l.RequiredExp<=TotalExp).OrderByDescending(l=>l.RequiredExp).FirstOrDefault();}}
    public PlayerProgress(string file) {path=file;Load();}
    public PlayerProgress():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Shikaku","progress.xml")) {}
    void Read(string file) {
        XmlDocument doc=new XmlDocument();doc.XmlResolver=null;doc.Load(file);
        if(doc.DocumentElement==null||doc.DocumentElement.Name!="shikakuProgress")throw new InvalidDataException("Invalid progress file");
        if(doc.SelectNodes("/shikakuProgress/wins/win").Count!=5)throw new InvalidDataException("Missing difficulty counts");
        bool[] seen=new bool[5];long[] counts=new long[5];HashSet<string> ids=new HashSet<string>();
        foreach(XmlNode win in doc.SelectNodes("/shikakuProgress/wins/win")) {
            int difficulty=int.Parse(win.Attributes["difficulty"].Value);
            long count=long.Parse(win.Attributes["count"].Value);
            if(difficulty<0||difficulty>=5||count<0)throw new InvalidDataException("Invalid win count");
            if(seen[difficulty])throw new InvalidDataException("Duplicate difficulty");seen[difficulty]=true;counts[difficulty]=count;
        }
        foreach(XmlNode game in doc.SelectNodes("/shikakuProgress/awarded/game")) {string id=game.Attributes["id"].Value;Guid.Parse(id);ids.Add(id);}
        // Validate arithmetic before accepting saved data.
        long total=counts.Select((count,i)=>checked(count*ExpRewards[i])).Sum();
        Wins=counts;awarded.Clear();foreach(string id in ids)awarded.Add(id);
    }
    void Load() {
        Warning=null;
        if(!File.Exists(path))return;
        try{Read(path);}catch(Exception){
            try{Read(path+".bak");Warning="กู้คืนสถิติจากไฟล์สำรองแล้ว";}
            catch(Exception){Warning="อ่านสถิติเดิมไม่ได้ กรุณาเก็บไฟล์ progress.xml ไว้ก่อน";}
        }
    }
    public bool Award(Guid game,int difficulty) {
        if(difficulty<0||difficulty>=5)throw new ArgumentOutOfRangeException("difficulty");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
        using(var writeLock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
            // Reload under the lock so two stale instances cannot overwrite each other's wins.
            if(File.Exists(path))Load();
            string id=game.ToString();if(awarded.Contains(id))return false;
            if(Warning!=null&&Warning.StartsWith("อ่านสถิติ"))throw new IOException(Warning);
            Wins[difficulty]=checked(Wins[difficulty]+1);awarded.Add(id);
            try{Save();return true;}catch{Wins[difficulty]--;awarded.Remove(id);throw;}
        }
    }
    void Save() {
        string directory=Path.GetDirectoryName(Path.GetFullPath(path));Directory.CreateDirectory(directory);
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try {
            using(XmlWriter writer=XmlWriter.Create(temporary,new XmlWriterSettings{Indent=true})) {
                writer.WriteStartElement("shikakuProgress");writer.WriteAttributeString("version","1");
                writer.WriteStartElement("wins");for(int i=0;i<5;i++){writer.WriteStartElement("win");writer.WriteAttributeString("difficulty",i.ToString());writer.WriteAttributeString("count",Wins[i].ToString());writer.WriteEndElement();}writer.WriteEndElement();
                writer.WriteStartElement("awarded");foreach(string id in awarded){writer.WriteStartElement("game");writer.WriteAttributeString("id",id);writer.WriteEndElement();}writer.WriteEndElement();writer.WriteEndElement();
            }
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);
        }finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
