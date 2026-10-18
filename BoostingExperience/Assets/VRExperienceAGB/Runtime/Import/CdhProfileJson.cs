using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    /// <summary>Resolve CDH execution inputs from JSON, arrays or JSONL. Recorded outputs remain evidence only.</summary>
    public static class CdhProfileJson
    {
        private sealed class InvalidCdh : Exception
        {
            public Diagnostic Diagnostic { get; }
            public InvalidCdh(string code, string message, string location)
            { Diagnostic = new Diagnostic(code, message, location); }
        }
        private static readonly JsonLoadSettings LoadSettings = new JsonLoadSettings
        { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };

        public static Outcome<ProfileSet> Read(string json, ModelDefinition model, bool packageBinding = false)
        {
            try
            {
                if (model.ExportInfo == null || model.StructureOnlyPreview)
                    return Failure("CdhRequiresAgbExport", "Open an AGB export before importing CDH records.");
                var profiles = new List<PreparedProfile>();
                int record = 0;
                foreach (var root in Documents(json))
                {
                    record++;
                    string location = "record[" + record + "]";
                    var common = new Dictionary<string, JObject>(StringComparer.Ordinal);
                    foreach (var item in RequiredArray(root, "pxCommonInputs", location))
                    {
                        var obj = Object(item, location + ".pxCommonInputs");
                        string reference = Scalar(obj["pzCommonInputReference"], location);
                        if (common.ContainsKey(reference)) throw new InvalidCdh("DuplicateCommonReference", "Common input references must be unique within a record.", location);
                        common.Add(reference, Object(Embedded(obj["pxCommonInput"], location)["values"], location + ".values"));
                    }
                    var indices = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var item in RequiredArray(root, "pxModelExecutionResults", location))
                    {
                        var outer = Object(item, location + ".pxModelExecutionResults");
                        var execution = Embedded(outer["pxModelExecutionResults"], location);
                        if ((string)execution["modelType"] != "ADM" || (!packageBinding && !Matches(execution, model.ExportInfo.Name))) continue;
                        string index = Scalar(execution["modelExecutionIndex"], location);
                        string address = location + ".execution[" + index + "]";
                        if (!indices.Add(index)) throw new InvalidCdh("DuplicateExecution", "Execution indices must be unique within a record.", address);
                        var merged = new Dictionary<string, JToken>(StringComparer.Ordinal);
                        foreach (var reference in References(execution["pzCommonInputReferences"] ?? outer["pzCommonInputReferences"], address))
                        {
                            if (!common.TryGetValue(reference, out var input)) throw new InvalidCdh("UnknownCommonReference", "An execution refers to missing common inputs.", address);
                            foreach (var property in input.Properties())
                            {
                                if (merged.TryGetValue(property.Name, out var previous) && !JToken.DeepEquals(previous, property.Value))
                                    throw new InvalidCdh("ConflictingCommonInput", "Referenced common inputs disagree for a predictor.", address + "." + property.Name);
                                merged[property.Name] = property.Value;
                            }
                        }
                        var parameters = execution["parameterPredictors"] == null ? new JObject() :
                            Object(Object(execution["parameterPredictors"], address)["values"], address + ".parameterPredictors.values");
                        // Execution-specific inputs override common inputs. Partition provides context only when absent.
                        foreach (var property in parameters.Properties()) merged[property.Name] = property.Value;
                        var partition = execution["partition"] == null ? new JObject() : Object(execution["partition"], address + ".partition");
                        foreach (var property in partition.Properties())
                        {
                            if (parameters.TryGetValue(property.Name, out var parameter) && !JToken.DeepEquals(Unwrap(parameter), Unwrap(property.Value)))
                                throw new InvalidCdh("ConflictingContext", "Execution parameters and partition context disagree.", address + "." + property.Name);
                            if (!merged.ContainsKey(property.Name)) merged.Add(property.Name, property.Value);
                        }
                        var values = new Dictionary<string, ProfileValue>(StringComparer.Ordinal);
                        foreach (var feature in model.Features)
                            if (merged.TryGetValue(feature.Id, out var token)) values.Add(feature.Id, Value(token, feature, address));
                        var outputs = execution["values"] == null ? new JObject() : Object(execution["values"], address + ".values");
                        double? recorded = null;
                        if (outputs["pyPropensity"] != null && Unwrap(outputs["pyPropensity"]).Type != JTokenType.Null)
                        {
                            recorded = Number(Unwrap(outputs["pyPropensity"]), address + ".pyPropensity");
                            if (recorded < 0 || recorded > 1) throw new InvalidCdh("InvalidRecordedPropensity", "Recorded propensity must be between zero and one.", address);
                        }
                        string Context(string key) => merged.TryGetValue(key, out var v) ? Text(Unwrap(v)) : "";
                        string action = Context("pyName"), treatment = Context("pyTreatment");
                        string name = "Record " + record + " · " + (string.IsNullOrEmpty(treatment) ? action : treatment) + " · execution " + index;
                        var source = new CdhProfileSource(record, index, Text(execution["ruleName"]), Text(execution["component"]),
                            Text(execution["modelVersion"]), Text(Unwrap(outputs["pyModelTechnique"])),
                            Text(outer["pxUpdateDateTime"]), action, treatment, recorded);
                        var profile = new PreparedProfile(1, model.Id, "cdh-" + record + "-" + index, name, values, source);
                        var validation = ModelValidator.ValidateProfile(model, profile);
                        if (validation.Count > 0) return Outcome<ProfileSet>.Failure(validation.Select(e => new Diagnostic(e.Code, e.Message, address, model.Id, e.TreeId, e.NodeId, e.FeatureId)));
                        profiles.Add(profile);
                    }
                }
                return profiles.Count == 0 ? Failure("NoMatchingCdhExecution", packageBinding ? "The package contains no ADM execution profiles." : "No ADM execution matches the loaded model name: " + model.ExportInfo.Name + ". Keep the original export filename.") :
                    Outcome<ProfileSet>.Success(new ProfileSet(1, model.Id, profiles));
            }
            catch (InvalidCdh e) { return Outcome<ProfileSet>.Failure(new[] { e.Diagnostic }); }
            catch (JsonException) { return Failure("MalformedCdhJson", "CDH JSON is malformed or contains duplicate fields."); }
            catch (Exception e) when (e is ArgumentException || e is FormatException || e is OverflowException || e is InvalidCastException)
            { return Failure("InvalidCdhValue", "A CDH field has an unsupported shape or value."); }
        }

        private static bool Matches(JObject execution, string name)
        {
            if (string.Equals(Text(execution["ruleName"]), name, StringComparison.OrdinalIgnoreCase)) return true;
            string component = Text(execution["component"]);
            string last = component.Substring(component.LastIndexOf('.') + 1);
            return string.Equals(last, name, StringComparison.OrdinalIgnoreCase) || string.Equals(last, name + "s", StringComparison.OrdinalIgnoreCase);
        }
        private static IEnumerable<JObject> Documents(string json)
        {
            using (var input = new StringReader(json ?? ""))
            using (var reader = new JsonTextReader(input) { SupportMultipleContent = true, DateParseHandling = DateParseHandling.None, MaxDepth = 128 })
                while (reader.Read())
                {
                    var token = JToken.ReadFrom(reader, LoadSettings);
                    if (token is JArray array) foreach (var record in array) yield return Object(record, "root[]");
                    else yield return Object(token, "root");
                }
        }
        private static JObject Embedded(JToken token, string location)
        {
            if (token is JObject obj) return obj;
            if (token?.Type != JTokenType.String) throw new InvalidCdh("InvalidCdhObject", "Expected a JSON object or encoded JSON object.", location);
            var documents = Documents((string)token).ToArray();
            if (documents.Length != 1) throw new InvalidCdh("InvalidCdhObject", "An embedded field must contain exactly one object.", location);
            return documents[0];
        }
        private static JObject Object(JToken token, string location) => token as JObject ?? throw new InvalidCdh("InvalidCdhObject", "Expected a CDH object.", location);
        private static JArray RequiredArray(JObject obj, string key, string location) => obj[key] as JArray ?? throw new InvalidCdh("InvalidCdhArray", "Expected array: " + key, location);
        private static string Text(JToken token) => token == null || token.Type == JTokenType.Null ? "" : token.Type == JTokenType.String ? (string)token : token.ToString(Formatting.None);
        private static JToken Unwrap(JToken token) => token is JObject obj ? obj["value"] ?? JValue.CreateNull() : token ?? JValue.CreateNull();
        private static string Scalar(JToken token, string location)
        {
            if (token == null || (token.Type != JTokenType.String && token.Type != JTokenType.Integer) || string.IsNullOrWhiteSpace(Text(token)))
                throw new InvalidCdh("InvalidCdhReference", "Expected a nonempty string or integer reference.", location);
            return Text(token);
        }
        private static IEnumerable<string> References(JToken token, string location)
        {
            var refs = token is JArray array ? array.Select(v => Scalar(v, location)).ToArray() : Scalar(token, location).Split(',').Select(v => v.Trim()).ToArray();
            if (refs.Length == 0 || refs.Any(string.IsNullOrWhiteSpace) || refs.Distinct().Count() != refs.Length)
                throw new InvalidCdh("InvalidCdhReference", "Common input references must be present and unique.", location);
            return refs;
        }
        private static ProfileValue Value(JToken token, FeatureDefinition feature, string address)
        {
            string location = address + "." + feature.Id;
            string declared = token is JObject obj ? Text(obj["type"]) : null;
            if (token is JObject typed && typed["value"] == null) throw new InvalidCdh("CdhValueRequired", "A typed input requires a value field.", location);
            var raw = Unwrap(token);
            if (raw.Type == JTokenType.Null) return ProfileValue.Missing;
            if (raw.Type == JTokenType.String && ((string)raw).Length == 0)
                throw new InvalidCdh("AmbiguousCdhEmptyValue", "A used predictor contains an empty string. Supply an explicit value or null; Missing is not inferred from empty text.", location);
            if (declared != null && declared != "NUMERIC" && declared != "SYMBOLIC")
                throw new InvalidCdh("UnsupportedCdhType", "Supported CDH input types are NUMERIC and SYMBOLIC.", location);
            if (declared == "NUMERIC" || (declared == null && (feature.Kind == FeatureKind.Number || raw.Type == JTokenType.Integer || raw.Type == JTokenType.Float)))
                return ProfileValue.FromNumber(Number(raw, location));
            if (raw.Type != JTokenType.String) throw new InvalidCdh("InvalidCdhSymbolic", "A symbolic input must be a string.", location);
            return ProfileValue.FromCategory((string)raw);
        }
        private static double Number(JToken token, string location)
        {
            if ((token.Type != JTokenType.String && token.Type != JTokenType.Float && token.Type != JTokenType.Integer) ||
                !double.TryParse(Text(token), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || !ModelValidator.IsFinite(value))
                throw new InvalidCdh("InvalidCdhNumber", "Expected a finite invariant-culture number.", location);
            return value;
        }
        private static Outcome<ProfileSet> Failure(string code, string message) => Outcome<ProfileSet>.Failure(new[] { new Diagnostic(code, message) });
    }
}
