using ReleaseTool.Domain;

namespace ReleaseTool.Core
{
    public static class ProductVersionExtensions
    {
        public static string DisplayString(this ProductVersion? version) => version == null ? "(None)" : $"{version.Value.Major}.{version.Value.Minor}.{version.Value.Patch}";
    }
}
