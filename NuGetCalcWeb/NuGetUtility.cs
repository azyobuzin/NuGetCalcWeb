using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        private static string Base64(this byte[] inArray)
        {
            return Convert.ToBase64String(inArray).TrimEnd('=').Replace('/', '-');
        }

        private static string SourceToDirectoryName(string source)
        {
            using (var md5 = MD5.Create())
                return md5.ComputeHash(Encoding.UTF8.GetBytes(source)).Base64();
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
                    var tempFile = Path.GetTempFileName();
                    try
                    {
                        using (var buffer = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite))
                        {
                            await stream.CopyToAsync(buffer).ConfigureAwait(false);
                            buffer.Position = 0;
                            await PackageExtractor.ExtractPackageAsync(buffer, identity, pathResolver,
                                new PackageExtractionContext() { CopySatelliteFiles = true },
                                PackageSaveModes.Nuspec, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        stream.Close();
                        try
                        {
                            File.Delete(tempFile);
                        }
                        catch { }
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

        public static async Task<string> ExtractUploadedFile(MultipartFileData file)
        {
            var fileInfo = new FileInfo(file.LocalFileName);
            if (fileInfo.Length > 20 * 1024 * 1024)
                throw new NuGetUtilityException("Too large file. The package file must be smaller than 20 MiB");

            string result;
            using (var md5 = MD5.Create())
            using (var stream = fileInfo.OpenRead())
            {
                var hashBytes = md5.ComputeHash(stream);
                var lengthBytes = BitConverter.GetBytes((int)fileInfo.Length);
                var bytes = new byte[hashBytes.Length + lengthBytes.Length];
                Array.Copy(hashBytes, bytes, hashBytes.Length);
                Array.Copy(lengthBytes, 0, bytes, hashBytes.Length, lengthBytes.Length);
                result = bytes.Base64();
            }

            var directory = Path.Combine("App_Data", "upload", result);
            if (!Directory.Exists(directory))
            {
                var pathResolver = new PackagePathResolver(directory);
                PackageIdentity identity;

                try
                {
                    using (var stream = fileInfo.OpenRead())
                    {
                        using (var package = new PackageReader(stream, true))
                            identity = package.GetIdentity();

                        stream.Position = 0;

                        await PackageExtractor.ExtractPackageAsync(stream, identity, pathResolver,
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

                fileInfo.MoveTo(Path.Combine(directory, pathResolver.GetPackageFileName(identity)));
            }

            return result;
        }

        public static PackageFolderReader GetUploadedPackage(string hash)
        {
            var dir = new DirectoryInfo(Path.Combine("App_Data", "upload", hash))
                .EnumerateDirectories()
                .SingleOrDefault(x => !x.Attributes.HasFlag(FileAttributes.Hidden) && !x.Name.StartsWith("."));
            if (dir == null)
                throw new NuGetUtilityException("The package has not been uploaded.");
            return new PackageFolderReader(dir);
        }
    }

    public class NuGetUtilityException : Exception
    {
        public NuGetUtilityException(string message, Exception innerException = null)
            : base(message, innerException) { }
    }
}