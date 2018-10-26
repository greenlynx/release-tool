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
    public class NextVersionCommand : CommandBase
    {
        public NextVersionCommand(Action<string> log) : base(log) { }

        public ReleaseHistory Execute(Settings settings)
        {
            var releaseHistory = ReadReleaseHistory(settings);
            var latestChanges = ReadLatestChanges(settings);
            var versionIncrementType = CalculateVersionIncrementType(latestChanges);
            var newVersion = latestChanges.Any() ? CalculateNewVersion(releaseHistory.CurrentVersion, versionIncrementType, settings) : releaseHistory.CurrentVersion;

            Log(newVersion.DisplayString());

            return releaseHistory;
        }
    }
}