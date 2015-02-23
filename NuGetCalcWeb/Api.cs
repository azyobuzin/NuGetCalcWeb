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

        public async Task<CompatibilitiesResult> Compatibilities(string packageId, string targetFramework, string packageVersion = null)
        {
            var package = await NuGetUtility.DownloadPackage(packageId, packageVersion);
            return new CompatibilitiesResult()
            {
                PackageId = package.Id,
                PackageVersion = package.Version,
                Compatibilities = NuGetUtility.GetCompatibilities(package, targetFramework).ToArray()
            };
        }
    }
}