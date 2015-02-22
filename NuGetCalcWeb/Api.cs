using System.Linq;
using System.Threading.Tasks;
using LightNode.Server;

namespace NuGetCalcWeb
{
    public class Api : LightNodeContract
    {
        public string Ping()
        {
            return "pong";
        }

        public async Task<CompatibilitiesResult> Compatibilities(string package, string targetFramework, string packageVersion = null)
        {
            using (var packageRef = await NuGetUtility.DownloadPackage(package, packageVersion))
            {
                return new CompatibilitiesResult()
                {
                    PackageId = packageRef.Package.Id,
                    PackageTitle = packageRef.Package.Title,
                    PackageVersion = packageRef.Package.Version.ToString(),
                    Compatibilities = NuGetUtility.GetCompatibilities(packageRef.Package, targetFramework).ToArray()
                };
            }
        }
    }
}