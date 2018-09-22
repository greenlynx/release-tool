using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace ReleaseTool.Domain
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChangeType
    {
        [Description("Breaking change")]
        BreakingChange,

        [Description("New feature")]
        NewFeature,

        [Description("Bug fix")]
        BugFix
    }
}
