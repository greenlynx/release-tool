using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ReleaseTool.Domain
{
    public class Release
    {
        public ProductVersion Version { get; }
        public IImmutableList<Change> Changes { get; }

        public Release(ProductVersion version, IEnumerable<Change> changes = null)
        {
            Version = version;
            Changes = changes == null ? ImmutableList.Create<Change>() : ImmutableList.Create(changes.OrderBy(x => x.Type).ToArray());
        }
    }
}
