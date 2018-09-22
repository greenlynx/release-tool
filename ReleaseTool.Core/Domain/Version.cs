using System;

namespace ReleaseTool.Domain
{
    public class ProductVersion : IComparable<ProductVersion>
    {
        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }

        private readonly Version _systemVersion;

        public ProductVersion(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            _systemVersion = new Version(Major, Minor, Patch);
        }

        public static ProductVersion Default => new ProductVersion(1, 0, 0);
        public static ProductVersion FromSystemVersion(Version version) => new ProductVersion(version.Major, version.Minor, version.Build);

        public ProductVersion IncrementMajor() => new ProductVersion(Major + 1, 0, 0);
        public ProductVersion IncrementMinor() => new ProductVersion(Major, Minor + 1, 0);
        public ProductVersion IncrementPatch() => new ProductVersion(Major, Minor, Patch + 1);

        public int CompareTo(ProductVersion other) => _systemVersion.CompareTo(other?._systemVersion);

        public override string ToString() => $"{Major}.{Minor}.{Patch}";
    }
}
