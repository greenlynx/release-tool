using Newtonsoft.Json;
using System;

namespace ReleaseTool.Domain
{
    public struct ProductVersion : IComparable<ProductVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        private readonly Version _systemVersion;

        public ProductVersion(int major = 0, int minor = 0, int patch = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            _systemVersion = new Version(Major, Minor, Patch);
        }

        public static ProductVersion FromSystemVersion(Version version) => new ProductVersion(version.Major, version.Minor, version.Build);

        public ProductVersion IncrementMajor() => new ProductVersion(Major + 1, 0, 0);
        public ProductVersion IncrementMinor() => new ProductVersion(Major, Minor + 1, 0);
        public ProductVersion IncrementPatch() => new ProductVersion(Major, Minor, Patch + 1);

        [JsonIgnore]
        public ProductVersion RoundToMajor => new ProductVersion(Major, 0, 0);

        public int CompareTo(ProductVersion other) => _systemVersion.CompareTo(other._systemVersion);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
