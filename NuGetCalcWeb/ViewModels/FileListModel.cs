using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using NuGet.PackagingCore;

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