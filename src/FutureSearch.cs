using System.Diagnostics;

namespace WanderburgDamageHUD;

internal sealed record FutureOption<T>(string Id,T State,double Value);
internal sealed record FutureResult(string Id,double Mean,double EndMean,double WinShare,double PositiveShare,double P10,int Samples);

// Independent planning draws never inspect the held-out future offer stream.
// This is a sampled policy search, not an exhaustive or clairvoyant optimum.
internal sealed class FutureSearch<T>
{
    private readonly FutureOption<T>[] roots;
    private readonly Func<T,Random,FutureOption<T>[]> draw;
    private readonly List<double>[] values,ends;
    private readonly double[] wins;
    internal int Depth { get; }
    internal int Completed { get; private set; }
    internal double ComputeMs { get; private set; }
    internal double MaxSliceMs { get; private set; }
    internal bool Done { get; private set; }
    internal FutureResult[] Results => roots.Select((r,i)=>new FutureResult(r.Id,
        values[i].DefaultIfEmpty(0).Average(),ends[i].DefaultIfEmpty(0).Average(),Completed>0?wins[i]/Completed:0,
        Completed>0?(double)values[i].Count(v=>v>0)/Completed:0,
        Completed>0?values[i].OrderBy(x=>x).ElementAt((int)((Completed-1)*.1)):0,Completed)).ToArray();

    internal FutureSearch(FutureOption<T>[] roots,Func<T,Random,FutureOption<T>[]> draw,int depth)
    {
        if(roots.Length<2 || roots.Any(r=>!double.IsFinite(r.Value)) || roots.Select(r=>r.Id).Distinct().Count()!=roots.Length)
            throw new ArgumentException("Future comparison needs distinct, finite starting choices");
        this.roots=roots;this.draw=draw;Depth=Math.Clamp(depth,1,8);
        values=roots.Select(_=>new List<double>()).ToArray();ends=roots.Select(_=>new List<double>()).ToArray();
        wins=new double[roots.Length];
    }

    internal IEnumerable<int> Run(int trials=64,double budgetMs=2000,int seed=7042026)
    {
        for(int trial=0;trial<trials;trial++)
        {
            var totals=new double[roots.Length];var final=new double[roots.Length];
            for(int root=0;root<roots.Length;root++)
            {
                var current=roots[root];double sum=current.Value;
                // Same uniforms across current picks, with state-dependent pools.
                var outcomes=new Random(unchecked(seed+trial*7919));
                for(int step=0;step<Depth;step++)
                {
                    var timer=Stopwatch.StartNew();
                    var menu=draw(current.State,outcomes).Where(x=>double.IsFinite(x.Value)).ToArray();
                    RecordSlice(timer);yield return step;timer.Restart();
                    if(menu.Length>0)
                    {
                        var chosen=menu[0];double best=double.NegativeInfinity;
                        foreach(var option in menu)
                        {
                            double value=option.Value;
                            if(step+1<Depth)
                            {
                                // Receding horizon: one additional decision, four fresh chance samples.
                                double next=0;
                                var planning=new Random(unchecked(seed^0x56437^(trial*104729+step*31)));
                                for(int j=0;j<4;j++)
                                {
                                    var preview=draw(option.State,planning).Where(x=>double.IsFinite(x.Value)).ToArray();
                                    next+=preview.Length>0?preview.Max(x=>x.Value):option.Value;
                                    RecordSlice(timer);yield return step;timer.Restart();
                                }
                                value+=next/4;
                            }
                            if(value>best+1e-9) {best=value;chosen=option;}
                        }
                        current=chosen;
                    }
                    sum+=current.Value;
                    RecordSlice(timer);
                    yield return step;
                }
                totals[root]=sum/(Depth+1);final[root]=current.Value;
            }
            double top=totals.Max();int ties=totals.Count(x=>Math.Abs(x-top)<1e-7);
            for(int i=0;i<roots.Length;i++)
            {
                values[i].Add(totals[i]);ends[i].Add(final[i]);
                if(Math.Abs(totals[i]-top)<1e-7) wins[i]+=1d/ties;
            }
            Completed++;
            // Never compare roots after unequal numbers of completed trials.
            if(Completed>=16 && ComputeMs>=budgetMs) break;
        }
        Done=true;
    }

    private void RecordSlice(Stopwatch timer)
    {
        timer.Stop();ComputeMs+=timer.Elapsed.TotalMilliseconds;
        MaxSliceMs=Math.Max(MaxSliceMs,timer.Elapsed.TotalMilliseconds);
    }

    internal static int WeightedIndex(IReadOnlyList<float> weights,Random random)
    {
        double total=weights.Where(x=>float.IsFinite(x) && x>0).Sum(x=>(double)x);
        if(total<=0) return -1;
        double value=random.NextDouble()*total;
        for(int i=0;i<weights.Count;i++)
        {if(!float.IsFinite(weights[i]) || weights[i]<=0) continue;value-=weights[i];if(value<0) return i;}
        return Enumerable.Range(0,weights.Count).Last(i=>float.IsFinite(weights[i]) && weights[i]>0);
    }
}
