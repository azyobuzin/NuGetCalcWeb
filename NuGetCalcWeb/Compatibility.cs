using System.Collections.Generic;
using System.Runtime.Versioning;

namespace NuGetCalcWeb
{
    public class Compatibility
    {
        public FrameworkName Framework { get; set; }
        public long Score { get; set; }
    }

    public class CompatibilitiesResult
    {
        public string PackageId { get; set; }
        public string PackageTitle { get; set; }
        public string PackageVersion { get; set; }
        public IReadOnlyList<Compatibility> Compatibilities { get; set; }
    }
}