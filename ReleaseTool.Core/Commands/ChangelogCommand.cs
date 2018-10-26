using System;

namespace ReleaseTool
{
    public class ChangelogCommand : CommandBase
    {
        public ChangelogCommand(Action<string> log) : base(log) { }

        public string Execute(Settings settings)
        {
            var releaseHistory = ReadReleaseHistory(settings);
            var changelog = GenerateChangelog(settings, releaseHistory.WithOnlyLatestRelease());
            Log(changelog);
            return changelog;
        }
    }
}