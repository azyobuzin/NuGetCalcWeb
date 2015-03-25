using Mono.Cecil;

namespace NuGetCalcWeb
{
    public class MyAssemblyResolver : DefaultAssemblyResolver
    {
        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            try
            {
                return base.Resolve(name);
            }
            catch (AssemblyResolutionException)
            {
                return null;
            }
        }
    }
}
