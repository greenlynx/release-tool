namespace ReleaseTool.Cake
{
    public class PrepareReleaseSettings
    {
        public string ReleaseHistoryFileName { get; set; }
        public string VersionFileName { get; set; }
        public string LatestChangesFileName { get; set; }
        public string MarkdownChangeLogFileName { get; set; }
        public string HtmlChangeLogFileName { get; set; }

        public string ProductName { get; set; }

        public bool? PatchAssemblyVersions { get; set; }
        public bool DoNotPrompt { get; set; } = true;
    }
}