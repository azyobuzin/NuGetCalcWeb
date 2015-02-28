using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using NuGet;
using NuGet.Client;
using NuGet.Data;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace NuGetCalcWeb
{
    public static class NuGetUtility
    {
        private static Func<FrameworkName, FrameworkName, long> getProfileCompatbility;
        public static long GetProfileCompatibility(FrameworkName projectFrameworkName, FrameworkName packageTargetFrameworkName)
        {
            if (getProfileCompatbility == null)
            {
                var arg0 = Expression.Parameter(typeof(FrameworkName), "projectFrameworkName");
                var arg1 = Expression.Parameter(typeof(FrameworkName), "packageTargetFrameworkName");
                getProfileCompatbility = Expression.Lambda<Func<FrameworkName, FrameworkName, long>>(
                    Expression.Call(
                        typeof(VersionUtility).GetMethod("GetProfileCompatibility", BindingFlags.NonPublic | BindingFlags.Static),
                        arg0,
                        arg1
                    ),
                    arg0, arg1).Compile();
            }

            return getProfileCompatbility(projectFrameworkName, packageTargetFrameworkName);
        }

        private static DataClient dataClient = new DataClient();
        private static V3RegistrationResource registrationResource =
            new V3RegistrationResource(dataClient, new Uri("https://api.nuget.org/v3/registration0/"));
        private static V3MetadataResource metadataResource = new V3MetadataResource(dataClient, registrationResource);
        private static V3DownloadResource downloadResource = new V3DownloadResource(dataClient, registrationResource);

        public static async Task<ZipPackage> DownloadPackage(string package, string version)
        {
            var nversion = string.IsNullOrWhiteSpace(version)
                ? await metadataResource.GetLatestVersion(package, true, false, CancellationToken.None).ConfigureAwait(false)
                : new NuGetVersion(version);

            var tmpFile = PackageTempFiles.Get(package, nversion);
            if (tmpFile == null)
            {
                Debug.WriteLine("Downloading {0} {1}", package, nversion);
                try
                {
                    tmpFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                    using (var pkgStream = await downloadResource.GetStream(new PackageIdentity(package, nversion), CancellationToken.None).ConfigureAwait(false))
                    using (var tmpStream = new FileStream(tmpFile, FileMode.Create, FileAccess.Write))
                    {
                        await pkgStream.CopyToAsync(tmpStream).ConfigureAwait(false);
                    }
                    PackageTempFiles.Add(package, nversion, tmpFile);
                }
                catch
                {
                    File.Delete(tmpFile);
                    throw;
                }
            }
            return new ZipPackage(tmpFile);
        }

        public static IEnumerable<Compatibility> GetCompatibilities(IPackage package, string targetFrameworkName)
        {
            var target = VersionUtility.ParseFrameworkName(targetFrameworkName);
            return package.AssemblyReferences
                .SelectMany(item => item.SupportedFrameworks ?? Enumerable.Empty<FrameworkName>())
                .Distinct()
                .Where(f => f != null & VersionUtility.IsCompatible(target, new[] { f }))
                .Select(f => new Compatibility
                {
                    Framework = f,
                    Score = GetProfileCompatibility(target, f),
                    PackageDependencies = package.GetCompatiblePackageDependencies(f).ToArray()
                })
                .OrderByDescending(c => c.Score);
        }
    }
}