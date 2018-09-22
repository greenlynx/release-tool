using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.Diagnostics;
using ReleaseTool.Domain;

namespace ReleaseTool.Cake
{
    public static class CakeExtensions
    {
        [CakeMethodAlias]
        public static ReleaseHistory PrepareRelease(this ICakeContext context, PrepareReleaseSettings settings = null)
        {
            var builtSettings = BuildSettings(settings);

            return new ReleasePreparer(message => context.Log.Write(Verbosity.Normal, LogLevel.Information, message)).PrepareRelease(builtSettings);
        }

        private static Settings BuildSettings(PrepareReleaseSettings settings)
        {
            var builtSettings = new Settings();

            if (settings == null) return builtSettings;

            if (settings.ReleaseHistoryFileName != null) builtSettings.ReleaseHistoryFileName = settings.ReleaseHistoryFileName;
            if (settings.VersionFileName != null) builtSettings.VersionFileName = settings.VersionFileName;
            if (settings.LatestChangesFileName != null) builtSettings.LatestChangesFileName = settings.LatestChangesFileName;
            if (settings.MarkdownChangeLogFileName != null) builtSettings.MarkdownChangeLogFileName = settings.MarkdownChangeLogFileName;
            if (settings.HtmlChangeLogFileName != null) builtSettings.HtmlChangeLogFileName = settings.HtmlChangeLogFileName;
            if (settings.ProductName != null) builtSettings.ProductName = settings.ProductName;
            if (settings.PatchAssemblyVersions.HasValue) builtSettings.PatchAssemblyVersions = settings.PatchAssemblyVersions.Value;

            builtSettings.DoNotPrompt = settings.DoNotPrompt;

            return builtSettings;
        }
    }
}
