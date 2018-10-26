using System;

namespace ReleaseTool
{
    public class FullChangelogCommand : CommandBase
    {
        public FullChangelogCommand(Action<string> log) : base(log) { }

        public string Execute(Settings settings)
        {
            var releaseHistory = ReadReleaseHistory(settings);
            var changelog = GenerateChangelog(settings, releaseHistory);
            Log(changelog);
            return changelog;
        }
    }
}