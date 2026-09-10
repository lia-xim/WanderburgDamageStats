using WanderburgDamageHUD;

internal static class FutureChecks
{
    private sealed record State(int Branch,int Step=0,int Bet=-1);

    internal static void Run()
    {
        int checks=0;
        void Check(bool ok,string label) {checks++;if(!ok) throw new Exception(label);}
        void Near(double x,double y,string label)=>Check(Math.Abs(x-y)<1e-8,label+$" ({x} vs {y})");
        FutureResult[] Finish<T>(FutureSearch<T> search,int n=64,double budget=double.PositiveInfinity)
        {foreach(var _ in search.Run(n,budget)) {} return search.Results;}
        var roots=new[]{new FutureOption<State>("immediate",new(0),.4),new FutureOption<State>("setup",new(1),.05)};
        FutureOption<State>[] Unlock(State s,Random r)
        {
            // A setup unlocks the stronger successor, not a free extra choice.
            double value=s.Branch==0?.4+.05*(s.Step+1):1.2+.1*s.Step;
            return new[]{new FutureOption<State>("successor",s with{Step=s.Step+1},value)};
        }
        var synergy=new FutureSearch<State>(roots,Unlock,3);
        var result=Finish(synergy);
        Check(result[1].Mean>result[0].Mean,"future synergy can beat the strongest immediate pick");
        Near(result[1].WinShare,1,"setup wins every deterministic path");
        Near(result[1].EndMean,1.4,"chained state receives exactly three future picks");
        Check(synergy.Completed==64 && synergy.Done,"complete balanced trials");
        Check(result.All(x=>x.Samples==64),"each current card gets the same sample budget");
        var replay=Finish(new FutureSearch<State>(roots,Unlock,3));
        Check(result.SequenceEqual(replay),"seed replay is deterministic");

        var flat=new[]{new FutureOption<int>("a",0,.2),new FutureOption<int>("b",1,.2)};
        var ties=Finish(new FutureSearch<int>(flat,(_,_)=>Array.Empty<FutureOption<int>>(),8));
        Near(ties[0].WinShare,.5,"ties share wins instead of preferring first card");
        Near(ties[1].Mean,.2,"exhausted pool holds build constant");
        var budgeted=new FutureSearch<State>(roots,Unlock,8);
        var bounded=Finish(budgeted,64,0);
        Check(budgeted.Completed==16 && bounded.All(x=>x.Samples==16),"time budget stops only after balanced minimum trials");
        Check(new FutureSearch<State>(roots,Unlock,100).Depth==8,"depth ceiling");
        Check(new FutureSearch<State>(roots,Unlock,0).Depth==1,"depth floor");
        var invalid=Finish(new FutureSearch<int>(flat,(_,_)=>new[]{new FutureOption<int>("bad",0,double.NaN)},2));
        Near(invalid[0].EndMean,.2,"invalid future value cannot poison results");
        var losses=Finish(new FutureSearch<int>(new[]{new FutureOption<int>("a",0,-.1),new FutureOption<int>("b",1,-.2)},(_,_)=>Array.Empty<FutureOption<int>>(),3));
        Near(losses[0].Mean,-.1,"negative paths remain negative");
        Near(losses[0].PositiveShare,0,"losses do not become positive outcomes");
        var progress=Finish(new FutureSearch<int>(new[]{new FutureOption<int>("now",0,.8),new FutureOption<int>("late",1,0)},
            (s,_)=>new[]{new FutureOption<int>("next",s,s==0?.8:1)},1));
        Check(progress[0].Mean>progress[1].Mean && progress[0].EndMean<progress[1].EndMean,"route objective accounts for intermediate strength, not only the final build");

        FutureOption<State>[] HiddenCoin(State s,Random r)
        {
            if(s.Step==0) return new[]{new FutureOption<State>("left",s with{Step=1,Bet=0},0),new FutureOption<State>("right",s with{Step=1,Bet=1},0)};
            return new[]{new FutureOption<State>("outcome",s with{Step=2},r.Next(2)==s.Bet?1:0)};
        }
        var fair=Finish(new FutureSearch<State>(new[]{new FutureOption<State>("a",new(0),0),new FutureOption<State>("b",new(0),0)},HiddenCoin,2),1024);
        Check(fair[0].EndMean>.45 && fair[0].EndMean<.55,"policy cannot foresee the held-out random outcome");
        Near(fair[0].WinShare,.5,"identical options receive identical outcome streams");
        var rng=new Random(12);int heavy=0;
        for(int i=0;i<20000;i++) if(FutureSearch<int>.WeightedIndex(new[]{1f,3f,0f,float.NaN,-1f},rng)==1) heavy++;
        Check(heavy>14500 && heavy<15500,"weighted offers follow the configured weights");
        Check(FutureSearch<int>.WeightedIndex(new[]{0f,float.NaN,-1f},rng)==-1,"invalid or empty pool has no draw");

        // Formula regression: later flat damage is multiplied by the chosen Fury value.
        // Keep the same number of picks and the same observed behavior on both branches.
        var blank=new AttackProfile(AttackModel.Projectile,0,0,15,4,0,0,0,1,1,0,"ram",Array.Empty<string>());
        var ram=new RamProfile(10,4,0,1.6f,4,15,1,1,_=>1);
        var profile=new ModelProfile(blank,blank,ram,Array.Empty<string>());
        var evidence=new CombatEvidence(0,0,100,100,20,4,0);
        double RamGain(float flatDamage,float fury)=>PredictionModel.Compare(profile,profile with{Ram=ram with{Flat=flatDamage,FuryMultiplier=fury}},evidence,Enumerable.Range(0,4).Select(_=>new RamSample(1,true,5)).ToArray()).Gain!.Value;
        double common=RamGain(30,1.6f),rare=RamGain(0,1.9f);
        Check(common>rare,"Ram rare is not forced to win by its color");
        double rareThenFlat=RamGain(30,1.9f);
        Check(rareThenFlat>common+rare,"later flat and Fury changes compound through the actual model");
        foreach(int depth in new[]{3,5,8})
        {
            var bench=new FutureSearch<State>(roots,(s,r)=>Enumerable.Range(0,3).Select(i=>new FutureOption<State>(i.ToString(),s with{Step=s.Step+1},(s.Step+1)*.1+r.NextDouble()*.2)).ToArray(),depth);
            Finish(bench);
            Console.WriteLine($"Planner fixture depth {depth}: {bench.Completed} paths/card, {bench.ComputeMs:0.0} ms CPU; no native game calls in this benchmark.");
        }
        Console.WriteLine($"PASS: {checks} future-planner checks.");
    }
}
