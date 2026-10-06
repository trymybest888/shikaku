using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public static class UniquePuzzle {
    [ThreadStatic] static Rectangle ambiguous;
    sealed class Candidate {public int Clue;public Rectangle Rect;public int[] Cells;}
    public static int Count(Puzzle puzzle,int milliseconds) {
        ambiguous=Rectangle.Empty;var watch=Stopwatch.StartNew();int n=puzzle.Size;
        int[,] prefix=new int[n+1,n+1];foreach(var clue in puzzle.Clues)prefix[clue.Cell.X+1,clue.Cell.Y+1]++;
        for(int y=1;y<=n;y++)for(int x=1;x<=n;x++)prefix[x,y]+=prefix[x-1,y]+prefix[x,y-1]-prefix[x-1,y-1];
        var choices=new List<Candidate>[puzzle.Clues.Count];var perCell=Enumerable.Range(0,n*n).Select(i=>new List<Candidate>()).ToArray();
        for(int id=0;id<choices.Length;id++){
            if(watch.ElapsedMilliseconds>milliseconds)return -1;
            choices[id]=new List<Candidate>();Clue c=puzzle.Clues[id];
            for(int w=1;w<=n;w++){if(c.Value%w!=0)continue;int h=c.Value/w;if(h>n)continue;
                for(int y=Math.Max(0,c.Cell.Y-h+1);y<=Math.Min(c.Cell.Y,n-h);y++)for(int x=Math.Max(0,c.Cell.X-w+1);x<=Math.Min(c.Cell.X,n-w);x++){
                    if(prefix[x+w,y+h]-prefix[x,y+h]-prefix[x+w,y]+prefix[x,y]!=1)continue;
                    int[] cells=new int[w*h];int k=0;for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)cells[k++]=yy*n+xx;
                    var candidate=new Candidate{Clue=id,Rect=new Rectangle(x,y,w,h),Cells=cells};choices[id].Add(candidate);foreach(int cell in cells)perCell[cell].Add(candidate);
                }
            }
            if(choices[id].Count==0)return 0;
        }
        bool[] occupied=new bool[n*n],used=new bool[choices.Length];var chosen=new List<Candidate>();int found=0;bool timedOut=false;
        Func<Candidate,bool> available=c=>{if(used[c.Clue])return false;foreach(int cell in c.Cells)if(occupied[cell])return false;return true;};
        Action<int> search=null;search=depth=>{
            if(found>=2||timedOut)return;if(watch.ElapsedMilliseconds>milliseconds){timedOut=true;return;}
            if(depth==choices.Length){if(occupied.All(v=>v)){found++;foreach(var c in chosen)if(!puzzle.Solution.Contains(c.Rect)){ambiguous=puzzle.Solution.FirstOrDefault(r=>r.Contains(puzzle.Clues[c.Clue].Cell));break;}}return;}
            List<Candidate> best=null;int bestCount=int.MaxValue;
            for(int cell=0;cell<occupied.Length;cell++)if(!occupied[cell]){int count=0;foreach(var c in perCell[cell])if(available(c)){count++;if(count>=bestCount)break;}if(count==0)return;if(count<bestCount){best=perCell[cell];bestCount=count;if(count==1)break;}}
            foreach(var c in best)if(available(c)){
                used[c.Clue]=true;foreach(int cell in c.Cells)occupied[cell]=true;chosen.Add(c);search(depth+1);chosen.RemoveAt(chosen.Count-1);used[c.Clue]=false;foreach(int cell in c.Cells)occupied[cell]=false;
                if(found>=2||timedOut)return;
            }
        };search(0);return timedOut?-1:found;
    }
    public static bool Ensure(Puzzle puzzle) {
        var total=Stopwatch.StartNew();var random=new Random();var seen=new HashSet<string>();
        Func<string> signature=()=>string.Join(";",puzzle.Clues.Select(c=>c.Cell.X+","+c.Cell.Y));
        for(int pass=0;pass<puzzle.Size*puzzle.Size*2;pass++){
            if(total.ElapsedMilliseconds>2500)return false;
            if(puzzle.Clues.Any(c=>c.Value<2))return false;
            seen.Add(signature());int count=Count(puzzle,160);if(count==1)return true;
            Rectangle region=ambiguous.Width>0?ambiguous:puzzle.Solution.OrderByDescending(r=>r.Width*r.Height).First();
            Clue clue=puzzle.Clues.First(c=>region.Contains(c.Cell));Point original=clue.Cell;
            // Resolve ambiguity by moving the existing clue, without introducing 1s.
            var locations=new List<Point>();for(int y=region.Top;y<region.Bottom;y++)for(int x=region.Left;x<region.Right;x++)if(x!=original.X||y!=original.Y)locations.Add(new Point(x,y));
            bool moved=false;foreach(Point position in locations.OrderBy(p=>random.Next())){clue.Cell=position;if(!seen.Contains(signature())){moved=true;break;}}
            if(moved&&pass%5!=4)continue;clue.Cell=original;
            // Split only into two regions of area >= 2. Even an unsplittable
            // ambiguous domino can be constrained by splitting another region.
            var eligible=puzzle.Solution.Where(r=>r.Width*r.Height>=4).OrderBy(r=>r==region?0:1).ThenByDescending(r=>r.Width*r.Height).ToList();
            bool divided=false;
            foreach(Rectangle r in eligible){
                var splits=new List<Rectangle[]>();
                for(int cut=1;cut<r.Width;cut++)if(cut*r.Height>=2&&(r.Width-cut)*r.Height>=2)splits.Add(new[]{new Rectangle(r.X,r.Y,cut,r.Height),new Rectangle(r.X+cut,r.Y,r.Width-cut,r.Height)});
                for(int cut=1;cut<r.Height;cut++)if(cut*r.Width>=2&&(r.Height-cut)*r.Width>=2)splits.Add(new[]{new Rectangle(r.X,r.Y,r.Width,cut),new Rectangle(r.X,r.Y+cut,r.Width,r.Height-cut)});
                if(splits.Count==0)continue;var pair=splits[random.Next(splits.Count)];puzzle.Solution.Remove(r);puzzle.Clues.RemoveAll(c=>r.Contains(c.Cell));
                foreach(Rectangle part in pair){puzzle.Solution.Add(part);puzzle.Clues.Add(new Clue{Cell=new Point(part.X+random.Next(part.Width),part.Y+random.Next(part.Height)),Value=part.Width*part.Height});}divided=true;break;
            }
            if(!divided){if(moved){clue.Cell=locations.First(p=>{clue.Cell=p;return !seen.Contains(signature());});}else return false;}
        }
        return false;
    }
}
