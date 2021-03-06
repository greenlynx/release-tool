﻿using System.Collections.Generic;
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

        public ProductVersion? Execute(Settings settings)
        {
            var releaseHistory = ReadReleaseHistory(settings);
            var latestChanges = ReadLatestChanges(settings);
            var versionIncrementType = CalculateVersionIncrementType(latestChanges);
            var newVersion = CalculateNewVersion(releaseHistory.CurrentVersion, versionIncrementType, settings);

            Log(newVersion.DisplayString());

            return newVersion;
        }
    }
}