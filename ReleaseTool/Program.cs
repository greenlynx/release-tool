using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mustache;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;
using ReleaseTool.Domain;
using System;
using System.Xml.Linq;

namespace ReleaseTool
{
    public class Program
    {
        private static void Log(string message = null)
        {
            Console.WriteLine(message);
        }

        private static void LogError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Log($"ERROR! {message}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void Main(string[] args)
        {
            try
            {
                var toolVersion = ProductVersion.FromSystemVersion(Assembly.GetExecutingAssembly().GetName().Version);
                Log($"Release tool version {toolVersion}");
                Log();

                Console.CancelKeyPress += Console_CancelKeyPress;

                var config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build();

                var serviceCollection = new ServiceCollection();
                serviceCollection.AddOptions();
                serviceCollection.Configure<Settings>(config);

                var serviceProvider = serviceCollection.BuildServiceProvider();

                var settings = serviceProvider.GetRequiredService<IOptions<Settings>>().Value;

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
                Log();

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
                    Log();

                    var versionIncrementType = CalculateVersionIncrementType(latestChanges);

                    var newVersion = releaseHistory.Releases.Any() ? CalculateNewVersion(releaseHistory.CurrentVersion, versionIncrementType) : ProductVersion.Default;

                    Log($"New version will be {newVersion}");

                    releaseHistory = releaseHistory.WithNewRelease(new Release(newVersion, latestChanges));
                }
                else
                {
                    Log($"No changes recorded since last release - the version will remain at {releaseHistory.CurrentVersion}. The output files will be regenerated but no new release can be made without recording at least one change.");
                }
                
                Console.WriteLine("Press ENTER to go ahead with the release, or CTRL-C to cancel...");
                Console.ReadLine();

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
                }
            }
            catch (ErrorException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
            }

#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
#endif
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Environment.Exit(-1);
        }

        private static ReleaseHistory ReadReleaseHistory(Settings settings)
        {
            if (!File.Exists(settings.ReleaseHistoryFileName)) return ReleaseHistory.Default(settings.ProductName);

            var releaseHistoryJson = File.ReadAllText(settings.ReleaseHistoryFileName);
            var releaseHistory = JsonConvert.DeserializeObject<ReleaseHistory>(releaseHistoryJson) ?? ReleaseHistory.Default(settings.ProductName);
            return releaseHistory.WithProductName(settings.ProductName);
        }

        private static void CreateBlankLatestChangesFile(Settings settings)
        {
            Log($"Writing default skeleton file to {settings.LatestChangesFileName}");

            const string defaultFileContents = @"# Every time you make a change, you should add a line to this file, along with a prefix to tag what sort of change it was (FIX/FEATURE/BREAKING).
# Each time a release is created, the changes will be moved from this file into the changelog. The type of changes included in a release determine what happens to its version number:
# - If any BREAKING changes are included, a new major version will be released
# - If no BREAKING changes are included, but there are one or more FEATUREs, a new minor version will be released
# - Otherwise only the patch version of the release will be incremented
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

        private static IEnumerable<Change> ReadLatestChanges(Settings settings)
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
                var descriptionString = (parts.Length > 1 ? string.Join(':', parts.Skip(1).ToArray()).Trim() : null);

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

        private static VersionIncrementType CalculateVersionIncrementType(IEnumerable<Change> changes)
        {
            if (changes.Any(x => x.Type == ChangeType.BreakingChange)) return VersionIncrementType.Major;
            if (changes.Any(x => x.Type == ChangeType.NewFeature)) return VersionIncrementType.Minor;
            if (changes.Any(x => x.Type == ChangeType.Fix)) return VersionIncrementType.Patch;
            return VersionIncrementType.None;
        }

        private static ProductVersion CalculateNewVersion(ProductVersion previousVersion, VersionIncrementType incrementType)
        {
            switch (incrementType)
            {
                case VersionIncrementType.Patch: return previousVersion.IncrementPatch();
                case VersionIncrementType.Minor: return previousVersion.IncrementMinor();
                case VersionIncrementType.Major: return previousVersion.IncrementMajor();
                default: throw new ArgumentException($"Unknown value for IncrementType: {incrementType}", nameof(incrementType));
            }
        }

        private static void WriteVersionFile(Settings settings, ReleaseHistory releaseHistory)
        {
            File.WriteAllText(settings.VersionFileName, releaseHistory.CurrentVersion.ToString());
        }

        private static void WriteReleaseHistory(Settings settings, ReleaseHistory releaseHistory)
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

        private static void WriteChangelog(Settings settings, ReleaseHistory releaseHistory)
        {
            Log($"Writing Markdown changelog to {settings.MarkdownChangeLogFileName}");

            var format = $@"# {settings.ProductName} {{{{CurrentVersion}}}}{{{{#newline}}}}
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

        private static void ConvertChangelogToHtml(Settings settings, ReleaseHistory releaseHistory)
        {
            Log($"Writing HTML changelog to {settings.HtmlChangeLogFileName}");

            string changelogBody;
            using (var reader = new StreamReader(settings.MarkdownChangeLogFileName))
            using (var writer = new StringWriter())
            {
                CommonMark.CommonMarkConverter.Convert(reader, writer);
                changelogBody = writer.ToString();
            }

            var html = $@"<html><head>
<title>{settings.ProductName} {releaseHistory.CurrentVersion}</title>
<link rel='stylesheet' href='http://fonts.googleapis.com/css?family=Roboto:300,300italic,700,700italic' />
<link rel='stylesheet' href='http://cdn.rawgit.com/necolas/normalize.css/master/normalize.css' />
<link rel='stylesheet' href='http://cdn.rawgit.com/milligram/milligram/master/dist/milligram.min.css' />
</head>
<body>
<div class='container'>
{changelogBody}
</div>
</body>
</html>";

            File.WriteAllText(settings.HtmlChangeLogFileName, html);
        }

        private static void PatchAllDotNetProjectFiles(Settings settings, ProductVersion version)
        {
            Log("Patching assembly versions in .csproj files...");

            var projectFiles = Directory.EnumerateFiles(".", "*.csproj", SearchOption.AllDirectories);
            foreach (var projectFile in projectFiles)
            {
                Log($" Patching {projectFile} to version {version}");

                var xdoc = XDocument.Load(projectFile);
                var versionElements = xdoc.Descendants("Version");
                foreach (var versionElement in versionElements)
                {
                    versionElement.SetValue(version);
                }
                xdoc.Save(projectFile);
            }            
        }
    }
}
