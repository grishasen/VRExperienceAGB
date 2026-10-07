using System;
using System.Collections.Generic;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Application
{
    public sealed class BoostingPoint
    {
        public int Iteration { get; }
        public double RawScore { get; }
        public double Probability { get; }
        internal BoostingPoint(int iteration, double raw)
        {
            Iteration = iteration; RawScore = raw;
            double exp = Math.Exp(raw >= 0 ? -raw : raw);
            Probability = raw >= 0 ? 1 / (1 + exp) : exp / (1 + exp);
        }
    }
    /// <summary>Prefix results from an already complete evaluation. Never commits route contributions.</summary>
    public static class BoostingProgression
    {
        public static IReadOnlyList<BoostingPoint> Points(EnsembleSession session)
        {
            if (session == null || session.Model.StructureOnlyPreview || session.Evaluation == null)
                return Array.Empty<BoostingPoint>();
            var points = new BoostingPoint[session.Model.Trees.Count + 1];
            double total = session.Model.BaseScore; points[0] = new BoostingPoint(0, total);
            for (int i = 0; i < session.Evaluation.Trees.Count; i++)
            { total += session.Evaluation.Trees[i].Contribution; points[i + 1] = new BoostingPoint(i + 1, total); }
            return Array.AsReadOnly(points);
        }
    }
}
