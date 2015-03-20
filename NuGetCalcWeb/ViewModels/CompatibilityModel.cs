using System;
using System.Collections.Generic;
using NuGet.PackagingCore;

namespace NuGetCalcWeb.ViewModels
{
    public class CompatibilityModel
    {
        public PackageSelectorModel PackageSelector { get; set; }
        public IEnumerable<string> ReferenceAssemblies { get; set; }
        public IEnumerable<PackageDependency> Dependencies { get; set; }
        public string Error { get; set; }
        public Exception Exception { get; set; }
    }
}