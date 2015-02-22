using System;
using System.IO;
using NuGet;

namespace NuGetCalcWeb
{
    public sealed class DownloadedNuGetPackage : IDisposable
    {
        public DownloadedNuGetPackage(string tmpFile)
        {
            this.TempFile = tmpFile;
            this.Package = new ZipPackage(tmpFile);
        }

        public string TempFile { get; private set; }
        public ZipPackage Package { get; private set; }

        private void DisposeImpl()
        {
            try
            {
                File.Delete(this.TempFile);
            }
            catch { }
        }

        public void Dispose()
        {
            this.DisposeImpl();
            GC.SuppressFinalize(this);
        }

        ~DownloadedNuGetPackage()
        {
            this.DisposeImpl();
        }
    }
}