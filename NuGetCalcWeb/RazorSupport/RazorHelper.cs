using System;
using System.Text;
using Microsoft.Owin;
using NuGetCalcWeb.ViewModels;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace NuGetCalcWeb.RazorSupport
{
    public static class RazorHelper
    {
        public static string Run(IOwinContext owinContext, string viewName, Type modelType = null, object model = null)
        {
            using (var service = RazorEngineService.Create(new TemplateServiceConfiguration()
            {
                Activator = new AppTemplateActivator(owinContext),
                BaseTemplateType = typeof(AppTemplateBase<>),
                TemplateManager = AppTemplateManager.Default
            }))
            {
                return service.IsTemplateCached(viewName, modelType)
                    ? service.Run(viewName, modelType, model)
                    : service.RunCompile(
                        AppTemplateManager.ResolveView(viewName),
                        viewName, modelType, model);
            }
        }
    }
}
