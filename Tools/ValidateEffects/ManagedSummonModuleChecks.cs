using System;
using System.Collections.Generic;

public static partial class Runner
{
    private static void RunSummonContactAndScheduleChecks()
    {
        Check("contact entry fires once until exit and reentry", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter); var target = new object();
            gate.BeginStep(0f); True(gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.1f); True(!gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.1f); gate.EndStep(1f); // 한 번의 조회에서 범위 밖입니다.
            gate.BeginStep(0.1f); True(gate.TryActivate(target,1,1f,1,() => true)); True(gate.ActivationCount == 2);
        });
        Check("contact repeat tracks cooldown per target and deduplicates the same step", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.WhileTouching); var target = new object();
            gate.BeginStep(0f); True(gate.TryActivate(target,1,1f,1,() => true));
            True(!gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.4f); True(!gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.6f); True(gate.TryActivate(target,1,1f,1,() => true));
        });
        Check("buffed repeat interval affects the next contact attempt", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.WhileTouching); var target = new object();
            gate.BeginStep(0f); True(gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.5f); True(gate.TryActivate(target,1,0.5f,1,() => true));
        });
        Check("contact cooldown is retained when a target briefly leaves", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.WhileTouching); var target = new object();
            gate.BeginStep(0f); True(gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.1f); gate.EndStep(1f);
            gate.BeginStep(0.1f); True(!gate.TryActivate(target,1,1f,1,() => true));
        });
        Check("global contact activation budget is shared across different targets", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true);
            gate.BeginStep(0f); True(gate.TryActivate(new object(),1,1f,2,() => true));
            True(gate.TryActivate(new object(),1,1f,2,() => true)); True(!gate.TryActivate(new object(),1,1f,2,() => true));
            True(gate.ActivationCount == 2);
        });
        Check("per-target limit persists across leaving and reentering", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true,SummonContactLimitScope.PerTarget);
            var a = new object(); var b = new object(); gate.BeginStep(0f);
            True(gate.TryActivate(a,1,1f,1,() => true)); True(gate.TryActivate(b,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.1f); gate.EndStep(1f); gate.BeginStep(0.1f);
            True(!gate.TryActivate(a,1,1f,1,() => true));
        });
        Check("target pooling resets the per-target budget for the new life", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true,SummonContactLimitScope.PerTarget);
            var target = new object(); gate.BeginStep(0f); True(gate.TryActivate(target,1,1f,1,() => true)); gate.EndStep(1f);
            gate.BeginStep(0.1f); True(gate.TryActivate(target,2,1f,1,() => true));
        });
        Check("failed contact effects do not spend the successful activation budget", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.WhileTouching,true);
            var target = new object(); gate.BeginStep(0f); True(!gate.TryActivate(target,1,0.1f,1,() => false)); gate.EndStep(0.1f);
            gate.BeginStep(0.1f); True(gate.TryActivate(target,1,0.1f,1,() => true)); True(gate.ActivationCount == 1);
        });
        Check("buffed activation maximum allows additional activations", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true);
            gate.BeginStep(0f); True(gate.TryActivate(new object(),1,1f,1,() => true));
            True(!gate.TryActivate(new object(),1,1f,1,() => true)); True(gate.TryActivate(new object(),1,1f,2,() => true));
        });
        Check("contact callback reentry cannot bypass the total budget", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true); gate.BeginStep(0f);
            True(gate.TryActivate(new object(),1,1f,1,() => {
                True(!gate.TryActivate(new object(),1,1f,1,() => true)); return true;
            })); True(gate.ActivationCount == 1);
        });
        Check("contact exceptions release reservations without consuming a count", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true); gate.BeginStep(0f);
            bool threw = false;
            try { gate.TryActivate(new object(),1,1f,1,() => { throw new InvalidOperationException(); }); }
            catch (InvalidOperationException) { threw = true; }
            True(threw); True(gate.TryActivate(new object(),1,1f,1,() => true));
        });
        Check("clearing during a contact callback does not corrupt the new runtime", () => {
            var gate = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true); gate.BeginStep(0f);
            gate.TryActivate(new object(),1,1f,1,() => { gate.Clear(); return true; });
            True(gate.ActivationCount == 0); True(gate.TryActivate(new object(),1,1f,1,() => true));
        });
        Check("two contact runtimes have independent activation budgets", () => {
            var a = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true);
            var b = new SummonContactActivationGate(SummonContactTriggerMode.OnEnter,true); var target = new object();
            a.BeginStep(0f); b.BeginStep(0f); True(a.TryActivate(target,1,1f,1,() => true));
            True(b.TryActivate(target,1,1f,1,() => true));
        });
        Check("periodic spawn waits for its interval and cycles through different effects", () => {
            var schedule = new SummonPeriodicSpawnSchedule(); var calls = new List<int>();
            Func<int,bool> action = i => { calls.Add(i); return true; };
            True(!schedule.Advance(0.5f,1f,false,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,action,() => true));
            True(schedule.Advance(0.5f,1f,false,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,action,() => true));
            schedule.Advance(1f,1f,false,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,action,() => true);
            schedule.Advance(1f,1f,false,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,action,() => true);
            True(calls.Count == 3 && calls[0] == 0 && calls[1] == 1 && calls[2] == 0);
        });
        Check("periodic limit counts successful cycles rather than child spawn count", () => {
            var schedule = new SummonPeriodicSpawnSchedule(); int calls = 0;
            Func<int,bool> action = i => { calls++; return true; };
            True(schedule.Advance(1f,1f,true,1,3,SummonPeriodicSpawnOrder.All,n => 0,action,() => true));
            True(calls == 3 && schedule.ActivationCount == 1);
            True(!schedule.Advance(1f,1f,true,1,3,SummonPeriodicSpawnOrder.All,n => 0,action,() => true));
            True(calls == 3);
        });
        Check("failed periodic selection advances its slot without spending the count", () => {
            var schedule = new SummonPeriodicSpawnSchedule();
            True(!schedule.Advance(1f,1f,true,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,i => i == 1,() => true));
            True(schedule.ActivationCount == 0);
            True(schedule.Advance(1f,1f,true,1,2,SummonPeriodicSpawnOrder.InOrder,n => 0,i => i == 1,() => true));
        });
        Check("periodic interval and limit respond to changed runtime stats", () => {
            var schedule = new SummonPeriodicSpawnSchedule();
            True(!schedule.Advance(0.5f,1f,true,1,1,SummonPeriodicSpawnOrder.InOrder,n => 0,i => true,() => true));
            True(schedule.Advance(0f,0.5f,true,1,1,SummonPeriodicSpawnOrder.InOrder,n => 0,i => true,() => true));
            True(schedule.Advance(0.5f,0.5f,true,2,1,SummonPeriodicSpawnOrder.InOrder,n => 0,i => true,() => true));
        });
        Check("random periodic selection uses the chosen valid index", () => {
            var schedule = new SummonPeriodicSpawnSchedule(); int selected = -1;
            True(schedule.Advance(1f,1f,false,1,3,SummonPeriodicSpawnOrder.Random,n => 2,i => { selected = i; return true; },() => true));
            True(selected == 2);
        });
        Check("periodic cancellation stops the rest of an all-effects cycle", () => {
            var schedule = new SummonPeriodicSpawnSchedule(); bool alive = true; int calls = 0;
            schedule.Advance(1f,1f,false,1,3,SummonPeriodicSpawnOrder.All,n => 0,i => { calls++; alive = false; return true; },() => alive);
            True(calls == 1);
            True(!schedule.Advance(1f,1f,false,1,3,SummonPeriodicSpawnOrder.All,n => 0,i => true,() => alive));
        });
        Check("long frame produces at most one periodic spawn cycle", () => {
            var schedule = new SummonPeriodicSpawnSchedule(); int calls = 0;
            schedule.Advance(100f,1f,false,1,1,SummonPeriodicSpawnOrder.InOrder,n => 0,i => { calls++; return true; },() => true);
            True(calls == 1);
        });
    }
}
