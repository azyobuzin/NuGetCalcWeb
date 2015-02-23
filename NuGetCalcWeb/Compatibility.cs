using System.Collections.Generic;
using System.Runtime.Versioning;
using NuGet;

namespace NuGetCalcWeb
{
    public class Compatibility
    {
        public FrameworkName Framework { get; set; }
        public long Score { get; set; }
        public IReadOnlyList<PackageDependency> PackageDependencies { get; set; }
    }

    public class CompatibilitiesResult
    {
        public string PackageId { get; set; }
        public SemanticVersion PackageVersion { get; set; }
        public IReadOnlyList<Compatibility> Compatibilities { get; set; }
    }
}