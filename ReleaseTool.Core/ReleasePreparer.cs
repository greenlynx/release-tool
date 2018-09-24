using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mustache;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ReleaseTool.Domain;
using System;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ReleaseTool
{
    public class ReleasePreparer
    {
        private readonly Action<string> Log;

        public ReleasePreparer(Action<string> log)
        {
            Log = log;
        }

        public ReleaseHistory PrepareRelease(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ProductName))
            {
                throw new ErrorException("No product name was specified! You must use the --ProductName=\"...\" command line switch to specify one");
            }

            Log("Configuration:");
            Log($" ReleaseHistoryFileName = {settings.ReleaseHistoryFileName}");
            Log($" VersionFileName = {settings.VersionFileName}");
            Log($" LatestChangesFileName = {settings.LatestChangesFileName}");
            Log($" ChangeLogFileName = {settings.MarkdownChangeLogFileName}");
            Log($" HtmlChangeLogFileName = {settings.HtmlChangeLogFileName}");
            Log($" ProductName = {settings.ProductName}");
            Log($" PatchAssemblyVersions = {settings.PatchAssemblyVersions}");
            Log($" DoNotPrompt = {settings.DoNotPrompt}");
            Log(string.Empty);

            var releaseHistory = ReadReleaseHistory(settings);

            Log($"There are {releaseHistory.Releases.Count()} existing versions, and the latest one is {releaseHistory.CurrentVersion}");

            var latestChanges = ReadLatestChanges(settings);
            if (latestChanges.Any())
            {
                Log("Found new changes:");
                foreach (var change in latestChanges)
                {
                    Log($" - {change.TypeDescription} - {change.Description}");
                }
                Log(string.Empty);

                var versionIncrementType = CalculateVersionIncrementType(latestChanges);

                var newVersion = releaseHistory.Releases.Any() ? CalculateNewVersion(releaseHistory.CurrentVersion, versionIncrementType) : ProductVersion.Default;

                Log($"New version will be {newVersion}");

                releaseHistory = releaseHistory.WithNewRelease(new Release(newVersion, latestChanges));
            }
            else
            {
                Log($"No changes recorded since last release - the version will remain at {releaseHistory.CurrentVersion}. The output files will be regenerated but no new release can be made without recording at least one change.");
            }

            if (!settings.DoNotPrompt)
            {
                Console.WriteLine("Press ENTER to go ahead with the release, or CTRL-C to cancel...");
                Console.ReadLine();
            }

            WriteReleaseHistory(settings, releaseHistory);

            CreateBlankLatestChangesFile(settings);

            if (!string.IsNullOrWhiteSpace(settings.VersionFileName))
            {
                WriteVersionFile(settings, releaseHistory);
            }

            if (!string.IsNullOrWhiteSpace(settings.MarkdownChangeLogFileName))
            {
                WriteChangelog(settings, releaseHistory);
            }

            if (!string.IsNullOrWhiteSpace(settings.HtmlChangeLogFileName))
            {
                ConvertChangelogToHtml(settings, releaseHistory);
            }

            if (settings.PatchAssemblyVersions)
            {
                PatchAllDotNetProjectFiles(settings, releaseHistory.CurrentVersion);
                PatchAllAssemblyInfoFiles(settings, releaseHistory.CurrentVersion);
            }

            return releaseHistory;
        }

        private ReleaseHistory ReadReleaseHistory(Settings settings)
        {
            if (!File.Exists(settings.ReleaseHistoryFileName)) return ReleaseHistory.Default(settings.ProductName);

            var releaseHistoryJson = File.ReadAllText(settings.ReleaseHistoryFileName);
            var releaseHistory = JsonConvert.DeserializeObject<ReleaseHistory>(releaseHistoryJson) ?? ReleaseHistory.Default(settings.ProductName);
            return releaseHistory.WithProductName(settings.ProductName);
        }

        private void CreateBlankLatestChangesFile(Settings settings)
        {
            Log($"Writing default skeleton file to {settings.LatestChangesFileName}");

            const string defaultFileContents = @"# Every time you make a change, you should add a line to this file, along with a prefix to tag what sort of change it was (FIX/FEATURE/BREAKING).
# Each time a release is created, the changes will be moved from this file into the changelog. The type of changes included in a release determine what happens to its version number:
# - If any BREAKING changes are included, a new major version will be released
# - If no BREAKING changes are included, but there are one or more FEATUREs, a new minor version will be released
# - If only FIXes have been made, only the patch version of the release will be incremented
# - If no changes are listed here, a release cannot be made
#
# Do not edit any other part of this file - it will be generated next release and your changes will be lost!
#
# Examples:
#
# FIX: Fixed widget rendering [JIRA-123]
# FEATURE: Added gadget info screen [JIRA-456]
# BREAKING: Change API schema [JIRA-789]

";

            File.WriteAllText(settings.LatestChangesFileName, defaultFileContents, Encoding.UTF8);
        }

        private IEnumerable<Change> ReadLatestChanges(Settings settings)
        {
            if (!File.Exists(settings.LatestChangesFileName)) return Enumerable.Empty<Change>();

            var changes = new List<Change>();

            var lines = File.ReadAllLines(settings.LatestChangesFileName, Encoding.UTF8);

            var errors = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                void AddError(string error)
                {
                    errors.Add($"Line {i + 1}: {error}");
                }

                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                var parts = line.Split(':');
                var changeTypeString = parts[0];
                var descriptionString = (parts.Length > 1 ? string.Join(":", parts.Skip(1).ToArray()).Trim() : null);

                if (string.IsNullOrWhiteSpace(descriptionString))
                {
                    AddError("Change description cannot be blank");
                    continue;
                }

                switch (changeTypeString)
                {
                    case "FIX":
                        changes.Add(new Change(ChangeType.Fix, descriptionString));
                        break;
                    case "FEATURE":
                        changes.Add(new Change(ChangeType.NewFeature, descriptionString));
                        break;
                    case "BREAKING":
                        changes.Add(new Change(ChangeType.BreakingChange, descriptionString));
                        break;
                    default:
                        AddError($"Unknown change type '{changeTypeString}'");
                        continue;
                }
            }

            if (errors.Any())
            {
                throw new ErrorException($@"{settings.LatestChangesFileName} could not be parsed. The following errors were found:

{string.Join(Environment.NewLine, errors)}");
            }

            return changes;
        }

        private VersionIncrementType CalculateVersionIncrementType(IEnumerable<Change> changes)
        {
            if (changes.Any(x => x.Type == ChangeType.BreakingChange)) return VersionIncrementType.Major;
            if (changes.Any(x => x.Type == ChangeType.NewFeature)) return VersionIncrementType.Minor;
            if (changes.Any(x => x.Type == ChangeType.Fix)) return VersionIncrementType.Patch;
            return VersionIncrementType.None;
        }

        private ProductVersion CalculateNewVersion(ProductVersion previousVersion, VersionIncrementType incrementType)
        {
            switch (incrementType)
            {
                case VersionIncrementType.Patch: return previousVersion.IncrementPatch();
                case VersionIncrementType.Minor: return previousVersion.IncrementMinor();
                case VersionIncrementType.Major: return previousVersion.IncrementMajor();
                default: throw new ArgumentException($"Unknown value for IncrementType: {incrementType}", nameof(incrementType));
            }
        }

        private void WriteVersionFile(Settings settings, ReleaseHistory releaseHistory)
        {
            File.WriteAllText(settings.VersionFileName, releaseHistory.CurrentVersion.ToString());
        }

        private void WriteReleaseHistory(Settings settings, ReleaseHistory releaseHistory)
        {
            Log($"Writing JSON release history to {settings.ReleaseHistoryFileName}");

            var releaseHistoryJson = JsonConvert.SerializeObject(releaseHistory, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            });
            File.WriteAllText(settings.ReleaseHistoryFileName, releaseHistoryJson);
        }

        private void WriteChangelog(Settings settings, ReleaseHistory releaseHistory)
        {
            Log($"Writing Markdown changelog to {settings.MarkdownChangeLogFileName}");

            var format = $@"# {{{{ProductName}}}} {{{{CurrentVersion}}}}{{{{#newline}}}}
{{{{#newline}}}}
{{{{#each Releases}}}}{{{{#newline}}}}
## {{{{Version}}}}{{{{#newline}}}}
{{{{#each Changes}}}}{{{{#newline}}}}
* **{{{{TypeDescription}}}}**: {{{{Description}}}}
{{{{/each}}}}{{{{#newline}}}}
{{{{/each}}}}";

            var compiler = new FormatCompiler();
            var generator = compiler.Compile(format);
            var changeLog = generator.Render(releaseHistory);

            File.WriteAllText(settings.MarkdownChangeLogFileName, changeLog);
        }

        private void ConvertChangelogToHtml(Settings settings, ReleaseHistory releaseHistory)
        {
            Log($"Writing HTML changelog to {settings.HtmlChangeLogFileName}");

            string changelogBody;
            using (var reader = new StreamReader(settings.MarkdownChangeLogFileName))
            using (var writer = new StringWriter())
            {
                CommonMark.CommonMarkConverter.Convert(reader, writer);
                changelogBody = writer.ToString();
            }

            var html = $@"<html>
<head>
<title>{settings.ProductName} {releaseHistory.CurrentVersion}</title>
<link rel='stylesheet' href='http://fonts.googleapis.com/css?family=Roboto:300,300italic,700,700italic' />
<link rel='stylesheet' href='http://cdn.rawgit.com/necolas/normalize.css/master/normalize.css' />
<link rel='stylesheet' href='http://cdn.rawgit.com/milligram/milligram/master/dist/milligram.min.css' />
</head>
<body>
<!-- DO NOT EDIT THIS FILE! It is automatically generated and your changes will be lost when the next release is made -->
<div class='container'>
{changelogBody}
</div>
</body>
</html>";

            File.WriteAllText(settings.HtmlChangeLogFileName, html);
        }

        private void PatchAllDotNetProjectFiles(Settings settings, ProductVersion version)
        {
            Log("Patching assembly versions in .csproj files...");

            var projectFiles = Directory.EnumerateFiles(".", "*.csproj", SearchOption.AllDirectories);
            foreach (var projectFile in projectFiles)
            {
                Log($" Patching {projectFile} to version {version}");

                var xdoc = XDocument.Load(projectFile);
                var propertyGroupElement = xdoc.Element("Project").Elements("PropertyGroup").First(x => !x.HasAttributes);
                if (propertyGroupElement == null)
                {
                    propertyGroupElement = new XElement("PropertyGroup");
                    xdoc.Add(propertyGroupElement);
                }

                void PatchOrAddElement(string name, string value)
                {
                    var element = propertyGroupElement.Element(name);
                    if (element == null)
                    {
                        element = new XElement(name);
                        propertyGroupElement.Add(element);
                    }

                    element.Value = value;
                }

                PatchOrAddElement("Version", version.ToString());
                PatchOrAddElement("AssemblyVersion", $"{version.RoundToMajor}");
                PatchOrAddElement("FileVersion", $"{version}.0");
                PatchOrAddElement("InformationalVersion", version.ToString());

                xdoc.Save(projectFile);
            }
        }

        private void PatchAllAssemblyInfoFiles(Settings settings, ProductVersion version)
        {
            Log("Patching assembly versions in AssemblyInfo.cs files...");

            var files = Directory.EnumerateFiles(".", "AssemblyInfo.cs", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                Log($" Patching {file} to version {version}");

                var lines = File.ReadAllLines(file);
                var newLines = new List<string>();
                foreach (var line in lines)
                {
                    if (line.TrimStart().StartsWith("//")) continue;

                    var newLine = Regex.Replace(line, @"\[assembly\s*:\s*AssemblyVersion\s*\(\s*""[\d\.]+""\s*\)\s*\]", $"[assembly: AssemblyVersion(\"{version.RoundToMajor}.0\")]");
                    newLine = Regex.Replace(newLine, @"\[assembly\s*:\s*AssemblyFileVersion\s*\(\s*""[\d\.]+""\s*\)\s*\]", $"[assembly: AssemblyFileVersion(\"{version}.0\")]");
                    newLine = Regex.Replace(newLine, @"\[assembly\s*:\s*AssemblyInformationalVersion\s*\(\s*""[\d\.]+""\s*\)\s*\]", $"[assembly: AssemblyInformationalVersion(\"{version}\")]");

                    newLines.Add(newLine);
                }

                File.WriteAllLines(file, newLines);
            }
        }
    }
}
