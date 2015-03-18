using System.IO;
using RazorEngine.Templating;

namespace NuGetCalcWeb.RazorSupport
{
    public sealed class AppTemplateManager : DelegateTemplateManager
    {
        public static string ResolveView(string key)
        {
            return File.ReadAllText(Path.Combine("Views", key + ".cshtml"));
        }

        private AppTemplateManager() : base(ResolveView) { }

        private static AppTemplateManager _default;
        public static AppTemplateManager Default
        {
            get
            {
                if (_default == null)
                    _default = new AppTemplateManager();
                return _default;
            }
        }
    }
}