using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

public static class Regression {
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    [STAThread] public static void Main(string[] args){
        string directory=Path.Combine(Path.GetFullPath(args[0]),"test-data-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try{
            foreach(int size in new[]{5,10,20,30,40})for(int trial=0;trial<5;trial++){
                Puzzle p=new Puzzle();p.New(size);Check(p.ValidateGenerated(),"partition");Check(p.Clues.All(c=>c.Value>=2),"no clue equals one");Check(UniquePuzzle.Count(p,5000)==1,"unique solution");
                foreach(var r in p.Solution.Take(p.Solution.Count-1))p.Placed.Add(new RegionBox(r,p.Placed.Count));
                Check(p.CompleteLastRegion()&&p.Won,"auto complete last region");
            }
            Puzzle ambiguous=new Puzzle{Size=2};ambiguous.Clues.Add(new Clue{Cell=new Point(0,0),Value=2});ambiguous.Clues.Add(new Clue{Cell=new Point(1,1),Value=2});Check(UniquePuzzle.Count(ambiguous,1000)==2,"ambiguous fixture");ambiguous.Clues[0].Value=3;Check(UniquePuzzle.Count(ambiguous,1000)==0,"impossible fixture");
            Console.WriteLine("PASS: 25 unique puzzles, all sizes, automatic finish, ambiguous/impossible fixtures.");
            string progress=Path.Combine(directory,"progress.xml");var first=new PlayerProgress(progress);var stale=new PlayerProgress(progress);Guid id=Guid.NewGuid();Check(first.Award(id,0),"first award");Check(stale.Award(Guid.NewGuid(),4),"stale award");var reload=new PlayerProgress(progress);Check(reload.TotalWins==2&&reload.TotalExp==1625,"stale instance merge");Check(!reload.Award(id,0),"duplicate after restart");
            using(var locked=new FileStream(progress+".lock",FileMode.Open,FileAccess.ReadWrite,FileShare.None)){bool failed=false;try{reload.Award(Guid.NewGuid(),1);}catch(IOException){failed=true;}Check(failed&&reload.TotalWins==2,"locked save failure");}Check(reload.Award(Guid.NewGuid(),1)&&reload.TotalExp==1725,"retry after unlock");Console.WriteLine("PASS: EXP persistence, stale-instance merge, duplicate prevention, failure/retry.");
            Puzzle original=new Puzzle();original.New(10);original.Place(original.Solution[0],3);original.Place(original.Solution[1],4);Guid game=Guid.NewGuid();var saved=new SavedGame{Id=game.ToString(),Difficulty=1,ElapsedSeconds=123,Scale=1.5f,Sound=false,Solution=original.Solution.Select(r=>new SavedRectangle(r,0)).ToList(),Clues=original.Clues.Select(c=>new SavedClue{X=c.Cell.X,Y=c.Cell.Y,Value=c.Value}).ToList(),Placed=original.Placed.Select(r=>new SavedRectangle(r.Bounds,r.Color)).ToList(),History=original.History.Select(state=>state.Select(r=>new SavedRectangle(r.Bounds,r.Color)).ToList()).ToList()};saved.Pending.Add(new PendingReward{Id=game.ToString(),Difficulty=1});
            var store=new SessionStore(Path.Combine(directory,"session.xml"));store.Save(saved);var loaded=store.Load();Puzzle restored=new Puzzle();SessionStore.Restore(loaded,restored);Check(loaded.Id==saved.Id&&loaded.ElapsedSeconds==123&&loaded.Scale==1.5f&&!loaded.Sound&&loaded.Pending.Count==1,"session fields");Check(restored.Placed.Count==2&&restored.History.Count==2&&restored.ValidateGenerated(),"session restore");Check(restored.History.Pop().Count==1,"undo restored");store.Save(saved);File.WriteAllText(Path.Combine(directory,"session.xml"),"broken");Check(store.Load()!=null&&store.Warning!=null,"backup restore");Console.WriteLine("PASS: session persistence, same game id, pending reward, timer/settings, undo and backup recovery.");
            string queueDir=Path.Combine(directory,"queue");Directory.CreateDirectory(queueDir);
            using(var queueWindow=new GameWindow(queueDir)){
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                var current=(Puzzle)typeof(GameWindow).GetField("puzzle",flags).GetValue(queueWindow);
                foreach(var r in current.Solution)current.Placed.Add(new RegionBox(r,current.Placed.Count));
                using(var locked=new FileStream(Path.Combine(queueDir,"progress.xml.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)){
                    typeof(GameWindow).GetMethod("RefreshState",flags).Invoke(queueWindow,null);
                    var snapshot=new SessionStore(Path.Combine(queueDir,"session.xml")).Load();Check(snapshot.Pending.Count==1,"failed reward persisted");
                    typeof(GameWindow).GetMethod("NewGame",flags).Invoke(queueWindow,new object[]{1});
                    Check(new SessionStore(Path.Combine(queueDir,"session.xml")).Load().Pending.Count==1,"pending retained on restart");
                }
                typeof(GameWindow).GetMethod("RetryRewards",flags).Invoke(queueWindow,null);
                var awardedProgress=new PlayerProgress(Path.Combine(queueDir,"progress.xml"));Check(awardedProgress.TotalWins==1&&awardedProgress.TotalExp==25,"pending reward retried");
                typeof(GameWindow).GetMethod("RetryRewards",flags).Invoke(queueWindow,null);Check(new PlayerProgress(Path.Combine(queueDir,"progress.xml")).TotalWins==1,"retry idempotent");
            }
            foreach(string file in Directory.GetFiles(queueDir))File.Delete(file);Directory.Delete(queueDir);
            Console.WriteLine("PASS: failed win reward queued on disk, retained across new games, retried once.");
            using(var window=new GameWindow(directory)){
                using(var achievement=window.CreateAchievementWindow()){
                    achievement.ShowInTaskbar=false;achievement.Opacity=0;achievement.ClientSize=new Size(480,360);achievement.Show();Application.DoEvents();Check(achievement.Controls.OfType<Panel>().Any(p=>p.AutoScroll),"achievement scroll");Check(achievement.Controls.OfType<Panel>().Any(p=>p.Dock==DockStyle.Bottom&&p.Visible),"pinned return button");using(var image=new Bitmap(achievement.Width,achievement.Height)){achievement.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(Path.Combine(args[0],"achievement-small.png"));}achievement.Close();
                }
                window.ShowInTaskbar=false;window.Opacity=0;window.Show();Application.DoEvents();
                var uiFlags=BindingFlags.Instance|BindingFlags.NonPublic;var uiPuzzle=(Puzzle)typeof(GameWindow).GetField("puzzle",uiFlags).GetValue(window);uiPuzzle.Placed.Clear();
                var uiWatch=System.Diagnostics.Stopwatch.StartNew();typeof(GameWindow).GetMethod("RequestNewGame",uiFlags).Invoke(window,new object[]{4});
                Check(uiWatch.ElapsedMilliseconds<1000,"Master generation must return control immediately");
                int pumps=0;while((bool)typeof(GameWindow).GetField("generating",uiFlags).GetValue(window)&&uiWatch.ElapsedMilliseconds<10000){Application.DoEvents();System.Threading.Thread.Sleep(10);pumps++;}
                Check(!(bool)typeof(GameWindow).GetField("generating",uiFlags).GetValue(window)&&pumps>1&&uiPuzzle.Size==40,"responsive Master generation");
                Check(uiPuzzle.ValidateGenerated()&&UniquePuzzle.Count(uiPuzzle,5000)==1,"Master has one solution");Check(uiPuzzle.Solution.Any(r=>r.Height>1)&&uiPuzzle.Solution.Any(r=>r.Width>1&&r.Height>1),"Master has mixed rectangle shapes");Check(uiPuzzle.Clues.All(c=>c.Value>=2),"Master has no ones");
                Console.WriteLine("PASS: Master generation returns immediately, UI pumps while loading, varied Master verified.");
                var toggle=typeof(GameWindow).GetMethod("ToggleFull",BindingFlags.Instance|BindingFlags.NonPublic);toggle.Invoke(window,null);Check(window.FormBorderStyle==FormBorderStyle.None,"fullscreen");toggle.Invoke(window,null);Check(window.FormBorderStyle==FormBorderStyle.Sizable,"exit fullscreen");window.Close();
            }
            Console.WriteLine("PASS: native UI, small-screen achievements, fixed return button, fullscreen restore.");
        }finally{foreach(string nested in Directory.GetDirectories(directory)){foreach(string file in Directory.GetFiles(nested))File.Delete(file);Directory.Delete(nested);}foreach(string file in Directory.GetFiles(directory))File.Delete(file);Directory.Delete(directory);}
    }
}
