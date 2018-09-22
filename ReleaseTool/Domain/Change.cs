using Newtonsoft.Json;

namespace ReleaseTool.Domain
{
    public class Change
    {
        public ChangeType Type { get; }
        public string Description { get; }

        public Change(ChangeType type, string description)
        {
            Type = type;
            Description = description;
        }

        [JsonIgnore]
        public string TypeDescription => Type.GetDescription();
    }
}
