using ReleaseTool.Domain;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ReleaseTool.Core
{
    public class CommandHandler
    {
        private Action<string> Log { get; }

        public CommandHandler(Action<string> log)
        {
            Log = log;
        }

        public ReleaseHistory Handle(string command, Settings settings)
        {
            command = command?.ToLowerInvariant()?.Trim();
            if (settings.Verbose || command == "version")
            {
                var toolVersion = ProductVersion.FromSystemVersion(Assembly.GetCallingAssembly().GetName().Version);
                Log($"Release Tool {toolVersion}");
            }

            if (settings.Verbose)
            {
                Log("Configuration:");
                Log($" ReleaseHistoryFileName = {settings.ReleaseHistoryFileName}");
                Log($" VersionFileName = {settings.VersionFileName}");
                Log($" LatestChangesFileName = {settings.LatestChangesFileName}");
                Log($" ChangeLogFileName = {settings.MarkdownChangeLogFileName}");
                Log($" HtmlChangeLogFileName = {settings.HtmlChangeLogFileName}");
                Log($" ProductName = {settings.ProductName}");
                Log($" PatchAssemblyVersions = {settings.PatchAssemblyVersions}");
                Log($" DoNotPrompt = {settings.DoNotPrompt}");
                Log($" Verbose = {settings.Verbose}");
                Log($" Verbose = {settings.Verbose}");
                Log(string.Empty);
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                throw new ErrorException("No command was specified!");
            }

            switch (command)
            {
                case "version": return null;
                case "init": return new InitCommand(Log).Execute(settings);
                case "prepare": return new PrepareCommand(Log).Execute(settings);
                case "thisversion": return new ThisVersionCommand(Log).Execute(settings);
                case "nextversion": return new NextVersionCommand(Log).Execute(settings);
                default: throw new ErrorException($"Unknown command '{command}'");
            }
        }
    }
}
