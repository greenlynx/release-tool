using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ReleaseTool.Domain
{
    public class ReleaseHistory
    {
        public string ProductName { get; }
        public IImmutableList<Release> Releases { get; }
        public ProductVersion? CurrentVersion => Releases.FirstOrDefault()?.Version;

        public ReleaseHistory(string productName, IEnumerable<Release> releases = null)
        {
            ProductName = productName;
            Releases = releases == null ? ImmutableList.Create<Release>() : ImmutableList.Create(releases.OrderByDescending(x => x.Version).ToArray());
        }

        public static ReleaseHistory Default(string productName) => new ReleaseHistory(productName);
        public ReleaseHistory WithProductName(string productName) => new ReleaseHistory(productName, Releases);
        public ReleaseHistory WithNewRelease(Release release) => new ReleaseHistory(ProductName, Releases.Add(release));
        public ReleaseHistory WithOnlyLatestRelease() => new ReleaseHistory(ProductName, Releases.Take(1));
    }
}
