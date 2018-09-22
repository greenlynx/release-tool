namespace ReleaseTool
{
    public class Settings
    {
        public string ReleaseHistoryFileName { get; set; } = "RELEASE-HISTORY.json";
        public string VersionFileName { get; set; } = "VERSION";
        public string LatestChangesFileName { get; set; } = "LATEST-CHANGES.txt";
        public string MarkdownChangeLogFileName { get; set; } = "CHANGELOG.md";
        public string HtmlChangeLogFileName { get; set; } = null;

        public string ProductName { get; set; } = null;

        public bool PatchAssemblyVersions { get; set; } = true;
        public bool DoNotPrompt { get; set; } = false;
    }
}
