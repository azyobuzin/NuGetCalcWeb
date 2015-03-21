using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Client;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.PackagingCore;
using NuGet.Versioning;

namespace NuGetCalcWeb
{
    using Resources = Tuple<MetadataResource, DownloadResource>;

    public static class NuGetUtility
    {
        private static ConcurrentDictionary<string, Resources> resourceCache = new ConcurrentDictionary<string, Resources>();

        private static Resources GetResources(string source)
        {
            var result = resourceCache.GetOrAdd(source, key =>
            {
                var repo = RepositoryFactory.Create(key);
                return Tuple.Create(repo.GetResource<MetadataResource>(), repo.GetResource<DownloadResource>());
            });

            if (result.Item1 == null || result.Item2 == null)
                throw new NuGetUtilityException("The source is not a package repository or not working.");

            return result;
        }

        private static string SourceToDirectoryName(string source)
        {
            using (var md5 = MD5.Create())
            {
                return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(source)))
                    .TrimEnd('=').Replace('/', '-');
            }
        }

        public static async Task<PackageFolderReader> GetPackage(string source, string packageId, NuGetVersion version)
        {
            if (string.IsNullOrWhiteSpace(source))
                source = NuGetConstants.V3FeedUrl;

            var resources = GetResources(source);

            if (version == null)
            {
                version = await resources.Item1.GetLatestVersion(packageId, true, false, CancellationToken.None).ConfigureAwait(false);

                if (version == null)
                    throw new NuGetUtilityException("Couldn't find the package.");
            }

            var identity = new PackageIdentity(packageId, version);
            var pathResolver = new PackagePathResolver(
                Path.Combine("App_Data", "repositories", SourceToDirectoryName(source)));
            var directory = pathResolver.GetInstallPath(identity);

            if (!Directory.Exists(directory))
            {
                Debug.WriteLine("Downloading {0} from {1}", identity, source);

                Stream stream;
                try
                {
                    stream = await resources.Item2.GetStream(identity, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new NuGetUtilityException("Couldn't download the package.", ex);
                }
                if (stream == null) throw new NuGetUtilityException("Couldn't download the package.");

                try
                {
                    using (stream)
                    using (var buffer = new MemoryStream())
                    {
                        await stream.CopyToAsync(buffer).ConfigureAwait(false);
                        buffer.Position = 0;
                        await PackageExtractor.ExtractPackageAsync(buffer, identity, pathResolver,
                            new PackageExtractionContext() { CopySatelliteFiles = true },
                            PackageSaveModes.Nuspec, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        Directory.Delete(directory, true);
                    }
                    catch (DirectoryNotFoundException) { }
                    throw new NuGetUtilityException("Couldn't extract the package.", ex);
                }
            }

            return new PackageFolderReader(directory);
        }

        public static FrameworkSpecificGroup FindMostCompatibleReferenceGroup(PackageReaderBase package, NuGetFramework target)
        {
            var groups = package.GetReferenceItems(); //中身は List<T> なので二度舐め OK
            var nearest = new FrameworkReducer().GetNearest(target, groups.Select(x => x.TargetFramework));
            return nearest != null
                ? groups.Single(x => x.TargetFramework.Equals(nearest))
                : null;
        }

        public static PackageDependencyGroup FindMostCompatibleDependencyGroup(PackageReaderBase package, NuGetFramework target)
        {
            var groups = package.GetPackageDependencies().ToArray();
            var nearest = new FrameworkReducer().GetNearest(target, groups.Select(x => x.TargetFramework).Where(x => x != null));
            return nearest != null
                ? groups.Single(x => x.TargetFramework.Equals(nearest))
                : groups.SingleOrDefault(x => x.TargetFramework == null);
        }
    }

    public class NuGetUtilityException : Exception
    {
        public NuGetUtilityException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}