using System;
using NUnit.Framework;
using VRExperienceAGB.Application;

namespace VRExperienceAGB.Tests
{
    public class ForestPlaybackTests
    {
        private static ForestPlayback Run(bool pair=true)
        {
            var model=Fixtures.Model();var profiles=Fixtures.Profiles(model);
            var a=Fixtures.Require(EnsembleSession.Create(model,profiles.Profiles[0]));
            var b=Fixtures.Require(EnsembleSession.Create(model,profiles.Profiles[1]));
            return new ForestPlayback(a.Evaluation,pair?b.Evaluation:null);
        }
        [Test] public void PauseInvalidTimeAndLongStallCannotSkipDecisions()
        {
            var run=Run();run.Paused=true;run.Advance(1000);
            Assert.That(run.Phase,Is.EqualTo(PlaybackPhase.Decision));
            run.Paused=false;run.Advance(double.NaN);run.Advance(double.PositiveInfinity);run.Advance(-1);
            Assert.That(run.Phase,Is.EqualTo(PlaybackPhase.Decision));
            run.Advance(1000);Assert.That(run.Phase,Is.EqualTo(PlaybackPhase.Moving));
            Assert.That(run.StepIndex,Is.Zero);Assert.That(run.TreeIndex,Is.Zero);
        }
        [Test] public void BothRoutesFinishBeforeNextTreeAndReplayNeverChangesEvaluation()
        {
            var run=Run();var a=run.A;var b=run.B;int results=0,guard=0;
            while(!run.Complete && guard++<1000){
                if(run.Phase==PlaybackPhase.TreeResult){
                    results++;Assert.That(run.RouteStep(false),Is.EqualTo(a.Trees[run.TreeIndex].Decisions.Count));
                    Assert.That(run.RouteStep(true),Is.EqualTo(b.Trees[run.TreeIndex].Decisions.Count));
                    Assert.That(run.PresentedScore(),Is.EqualTo(a.Trees[run.TreeIndex].RunningRawScore));
                }
                run.Next();
            }
            Assert.That(run.Complete,Is.True);Assert.That(results,Is.EqualTo(a.Trees.Count));
            Assert.That(run.PresentedScore(),Is.EqualTo(a.RawScore));
            run.PreviousTree();Assert.That(run.Complete,Is.False);
            run.Restart();Assert.That(run.TreeIndex,Is.Zero);Assert.That(run.PresentedScore(),Is.EqualTo(a.BaseScore));
            Assert.That(run.A,Is.SameAs(a));Assert.That(run.B,Is.SameAs(b));
        }
    }
}
