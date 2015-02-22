using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using NuGet;

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

        public static async Task<DownloadedNuGetPackage> DownloadPackage(string package, string version)
        {
            var tmpFile = Path.GetTempFileName();
            try
            {
                using (var wc = new WebClient())
                {
                    var uri = "https://www.nuget.org/api/v2/package/" + (version != null
                        ? string.Format("{0}/{1}", package, version)
                        : package
                    );
                    await wc.DownloadFileTaskAsync(uri, tmpFile).ConfigureAwait(false);
                }
            }
            catch
            {
                File.Delete(tmpFile);
                throw;
            }
            return new DownloadedNuGetPackage(tmpFile);
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
                    Score = GetProfileCompatibility(target, f)
                })
                .OrderByDescending(c => c.Score);
        }
    }
}