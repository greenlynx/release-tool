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
using System.Reflection;
using ReleaseTool.Core;

namespace ReleaseTool
{
    public class PrepareCommand : CommandBase
    {
        public PrepareCommand(Action<string> log) : base(log) { }

        public ReleaseHistory Execute(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ProductName))
            {
                throw new ErrorException("No product name was specified! You must use the --ProductName=\"...\" command line switch to specify one");
            }

            var releaseHistory = ReadReleaseHistory(settings);
            var latestChanges = ReadLatestChanges(settings);
            var versionIncrementType = CalculateVersionIncrementType(latestChanges);
            var newVersion = latestChanges.Any() ? CalculateNewVersion(releaseHistory.CurrentVersion, versionIncrementType, settings) : releaseHistory.CurrentVersion;

            Log($"There are {releaseHistory.Releases.Count()} existing releases, and the latest version is {releaseHistory.CurrentVersion.DisplayString()}");

            if (latestChanges.Any())
            {
                Log("Found new changes:");
                foreach (var change in latestChanges)
                {
                    Log($" - {change.TypeDescription} - {change.Description}");
                }
                Log(string.Empty);

                Log($"New version will be {newVersion}");

                releaseHistory = releaseHistory.WithNewRelease(new Release(newVersion.Value, latestChanges));
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
                PatchAllDotNetProjectFiles(settings, releaseHistory.CurrentVersion.Value);
                PatchAllAssemblyInfoFiles(settings, releaseHistory.CurrentVersion.Value);
            }

            return releaseHistory;
        }
    }
}
