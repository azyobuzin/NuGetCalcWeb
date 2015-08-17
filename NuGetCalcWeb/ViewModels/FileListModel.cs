using System.Collections.Generic;
using System.IO;
using NuGet.Packaging.Core;

namespace NuGetCalcWeb.ViewModels
{
    public class FileListModel
    {
        public PackageIdentity Identity { get; set; }
        public string[] Breadcrumbs { get; set; }
        public IEnumerable<DirectoryInfo> Directories { get; set; }
        public IEnumerable<FileInfo> Files { get; set; }
    }
}