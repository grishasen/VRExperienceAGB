using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using VRExperienceAGB.Domain;

namespace VRExperienceAGB.Import
{
    public sealed partial class ModelFileLibrary
    {
        public const long MaximumExpandedBytes = 64L * 1024 * 1024;
        public const int MaximumPackageEntries = 256;

        public string PackageFor(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            string prefix = Path.GetFullPath(PackagesPath) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(file);
            if (!full.StartsWith(prefix, StringComparison.Ordinal)) return null;
            var parts = full.Substring(prefix.Length).Split(Path.DirectorySeparatorChar);
            if (parts.Length < 3 || parts[0].StartsWith(".", StringComparison.Ordinal) ||
                (parts[1] != "Models" && parts[1] != "Profiles")) return null;
            return Path.Combine(PackagesPath, parts[0]);
        }

        public string Label(string file)
        {
            string package = PackageFor(file);
            return package == null ? DisplayName(file) : DisplayName(package) + " / " + DisplayName(file);
        }

        public void CheckProfileScope(string profilePath, string modelPath)
        {
            if (!string.Equals(PackageFor(profilePath), PackageFor(modelPath), StringComparison.Ordinal))
                throw new InvalidDataException("Profiles must belong to the same ZIP package as the model. Import a new ZIP to change its profiles.");
        }

        /// <summary>Validate a complete archive in staging, then publish its isolated folder atomically.</summary>
        public string ImportPackage(string zipPath, string name)
        {
            string staging = Path.Combine(PackagesPath, ".pending-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(staging);
            try
            {
                string input = Path.Combine(staging, "Inputs");
                Directory.CreateDirectory(input);
                var files = new List<string>();
                string hash;
                using (var stream = File.OpenRead(zipPath))
                {
                    if (stream.Length > MaximumBytes) throw new InvalidDataException("ZIP exceeds the 32 MiB import limit.");
                    using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    stream.Position = 0;
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        if (archive.Entries.Count > MaximumPackageEntries)
                            throw new InvalidDataException("ZIP contains more than 256 entries.");
                        long expanded = 0;
                        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var entry in archive.Entries)
                        {
                            string relative = SafeEntryPath(entry.FullName);
                            if (!paths.Add(relative.TrimEnd('/'))) throw new InvalidDataException("ZIP contains duplicate paths.");
                            int kind = (entry.ExternalAttributes >> 16) & 0xF000;
                            if (kind != 0 && kind != 0x8000 && kind != 0x4000)
                                throw new InvalidDataException("ZIP links and special files are not supported.");
                            if (entry.Length > MaximumBytes || entry.Length > MaximumExpandedBytes - expanded)
                                throw new InvalidDataException("ZIP exceeds the 32 MiB per-file or 64 MiB expanded limit.");
                            expanded += entry.Length;
                            if (relative.EndsWith("/", StringComparison.Ordinal)) continue;
                            // Ignore Finder metadata, never treat it as model or profile data.
                            if (relative.Split('/').Contains("__MACOSX") || Path.GetFileName(relative) == ".DS_Store") continue;
                            string extension = Path.GetExtension(relative);
                            if (!extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase))
                                throw new InvalidDataException("ZIP may contain only model JSON and profile JSON / JSONL files.");
                            string target = Path.Combine(input, relative);
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                            using (var source = entry.Open())
                            using (var output = new FileStream(target, FileMode.CreateNew))
                            {
                                byte[] buffer = new byte[16384];
                                long written = 0;
                                int read;
                                while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    written += read;
                                    if (written > entry.Length || written > MaximumBytes)
                                        throw new InvalidDataException("ZIP entry exceeds its declared size.");
                                    output.Write(buffer, 0, read);
                                }
                                if (written != entry.Length) throw new InvalidDataException("ZIP entry is incomplete.");
                            }
                            files.Add(target);
                        }
                    }
                }

                ImportedModel imported = null;
                string modelFile = null;
                foreach (string file in files)
                {
                    // Models must remain discoverable in the saved-model JSON listing.
                    if (!Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase)) continue;
                    ImportedModel candidate;
                    try { candidate = ParseModel(Read(file), Path.GetFileName(file)); }
                    catch (InvalidDataException) { continue; }
                    if (imported != null) throw new InvalidDataException("ZIP must contain exactly one model.");
                    imported = candidate;
                    modelFile = file;
                }
                if (imported == null || files.Count < 2)
                    throw new InvalidDataException("ZIP requires exactly one valid model and at least one profile file.");
                foreach (string file in files.Where(p => p != modelFile))
                {
                    try
                    {
                        var profiles = ParseProfiles(Read(file), imported.Model, packageBinding: true);
                        foreach (var profile in profiles.Profiles)
                        {
                            var result = ModelEvaluator.Evaluate(imported.Model, profile);
                            if (!result.IsSuccess) throw new InvalidDataException(Errors(result.Diagnostics));
                        }
                    }
                    catch (InvalidDataException error) { throw new InvalidDataException(Path.GetFileName(file) + ": " + error.Message, error); }
                }

                string modelName = Path.GetFileName(modelFile);
                Directory.CreateDirectory(Path.Combine(staging, "Models"));
                File.Move(modelFile, Path.Combine(staging, "Models", modelName));
                Directory.Move(input, Path.Combine(staging, "Profiles"));
                string stem = new string(Path.GetFileNameWithoutExtension(name ?? "package").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').Take(60).ToArray());
                if (stem.Length == 0) stem = "package";
                string destination = Path.Combine(PackagesPath, stem + "-" + hash.Substring(0, 16));
                if (!Directory.Exists(destination)) Directory.Move(staging, destination);
                return Path.Combine(destination, "Models", modelName);
            }
            catch (Exception error) when (error is NotSupportedException || error is ArgumentException)
            {
                throw new InvalidDataException("Unsupported or invalid ZIP archive.", error);
            }
            finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        }

        private static string SafeEntryPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path.Length > 240 || path.StartsWith("/", StringComparison.Ordinal) || path.Contains("\\") || path.Contains(":"))
                throw new InvalidDataException("ZIP contains an unsafe path.");
            var parts = path.TrimEnd('/').Split('/');
            if (parts.Any(p => p.Length == 0 || p == "." || p == ".." || p.Any(char.IsControl)))
                throw new InvalidDataException("ZIP contains an unsafe path.");
            return path;
        }
    }
}
