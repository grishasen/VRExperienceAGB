namespace VRExperienceAGB.Domain
{
    /// <summary>Identity of the immutable exported tree snapshot, not a served prediction contract.</summary>
    public sealed class AgbExportInfo
    {
        public string Name { get; }
        public string Version { get; }
        public string Sha256 { get; }
        public string UpdateTime { get; }
        public AgbExportInfo(string name, string version, string sha256, string updateTime)
        { Name = name; Version = version; Sha256 = sha256; UpdateTime = updateTime; }
    }

    /// <summary>Recorded CDH evidence. It never participates in evaluation or changes a calculated score.</summary>
    public sealed class CdhProfileSource
    {
        public int Record { get; }
        public string ExecutionIndex { get; }
        public string RuleName { get; }
        public string Component { get; }
        public string ModelVersion { get; }
        public string Technique { get; }
        public string UpdateTime { get; }
        public string Action { get; }
        public string Treatment { get; }
        public double? RecordedPropensity { get; }
        public CdhProfileSource(int record, string index, string rule, string component, string version,
            string technique, string updateTime, string action, string treatment, double? propensity)
        {
            Record = record; ExecutionIndex = index; RuleName = rule; Component = component;
            ModelVersion = version; Technique = technique; UpdateTime = updateTime;
            Action = action; Treatment = treatment; RecordedPropensity = propensity;
        }
    }
}
