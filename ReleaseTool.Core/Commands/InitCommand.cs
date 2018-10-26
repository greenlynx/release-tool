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
    public class InitCommand : CommandBase
    {
        public InitCommand(Action<string> log) : base(log) { }

        public ReleaseHistory Execute(Settings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ProductName))
            {
                throw new ErrorException("No product name was specified! You must use the --ProductName=\"...\" command line switch to specify one");
            }

            Log("Initialising directory for automated releases...");

            var releaseHistory = ReadReleaseHistory(settings);

            WriteReleaseHistory(settings, releaseHistory);

            CreateBlankLatestChangesFile(settings, false);

            if (!string.IsNullOrWhiteSpace(settings.VersionFileName))
            {
                WriteVersionFile(settings, releaseHistory);
            }

            return releaseHistory;
        }
    }
}