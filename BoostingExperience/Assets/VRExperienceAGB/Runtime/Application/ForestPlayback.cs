using System;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public enum PlaybackPhase { Decision, Moving, TreeResult, Complete }

    /// <summary>Complete ordered traversal. Presentation time never changes the evaluated score.</summary>
    public sealed class ForestPlayback
    {
        public EvaluationResult A { get; }
        public EvaluationResult B { get; }
        public int TreeIndex { get; private set; }
        public int StepIndex { get; private set; }
        public PlaybackPhase Phase { get; private set; }
        public bool Paused { get; set; }
        public double Speed { get; set; } = 1;
        public double DecisionSeconds { get; set; } = 1.8;
        public double MoveSeconds { get; set; } = .8;
        public double ResultSeconds { get; set; } = 3;
        private double elapsed;
        public double Movement => Phase == PlaybackPhase.Moving ? Math.Min(1, elapsed / Math.Max(.05, MoveSeconds)) : 0;
        public bool Complete => Phase == PlaybackPhase.Complete;
        public int CompletedTrees => Complete ? A.Trees.Count : TreeIndex + (Phase == PlaybackPhase.TreeResult ? 1 : 0);
        public ForestPlayback(EvaluationResult a, EvaluationResult b = null)
        {
            A = a ?? throw new ArgumentNullException(nameof(a)); B = b;
            if (b != null && (a.ModelId != b.ModelId || a.Trees.Count != b.Trees.Count)) throw new ArgumentException("Profiles must use the same complete model.");
            ResetPhase();
        }
        public int RouteStep(bool b) => Math.Min(StepIndex, (b ? B : A).Trees[TreeIndex].Decisions.Count);
        public void Advance(double seconds)
        {
            if (Paused || Complete || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            elapsed += seconds * Math.Max(.1, Math.Min(20, Speed));
            // Deliberately one transition per tick: resume and stalls never skip explanations.
            double duration = Phase == PlaybackPhase.Decision ? DecisionSeconds : Phase == PlaybackPhase.Moving ? MoveSeconds : ResultSeconds;
            if (elapsed < Math.Max(.05, duration)) return;
            Next();
        }
        public void Next()
        {
            if (Complete) return;
            elapsed = 0;
            if (Phase == PlaybackPhase.Decision) Phase = PlaybackPhase.Moving;
            else if (Phase == PlaybackPhase.Moving) { StepIndex++; ResetPhase(); }
            else if (TreeIndex + 1 == A.Trees.Count) Phase = PlaybackPhase.Complete;
            else { TreeIndex++; StepIndex = 0; ResetPhase(); }
        }
        /// <summary>Skip presentation only; A and B already contain the complete evaluated ensemble.</summary>
        public void FinishAll()
        {
            TreeIndex=A.Trees.Count-1;
            StepIndex=Math.Max(A.Trees[TreeIndex].Decisions.Count,B?.Trees[TreeIndex].Decisions.Count??0);
            elapsed=0;Paused=false;Phase=PlaybackPhase.Complete;
        }
        public void PreviousTree() { TreeIndex = Math.Max(0, TreeIndex - 1); StepIndex = 0; elapsed = 0; ResetPhase(); }
        public void Restart() { TreeIndex = StepIndex = 0; elapsed = 0; Paused = false; ResetPhase(); }
        private void ResetPhase()
        {
            int steps = Math.Max(A.Trees[TreeIndex].Decisions.Count, B?.Trees[TreeIndex].Decisions.Count ?? 0);
            Phase = StepIndex >= steps ? PlaybackPhase.TreeResult : PlaybackPhase.Decision;
        }
        public double PresentedScore(bool b = false)
        {
            var result = b ? B : A;
            return CompletedTrees == 0 ? result.BaseScore : result.Trees[CompletedTrees - 1].RunningRawScore;
        }
    }
}
