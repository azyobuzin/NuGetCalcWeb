using System;
using System.IO;
using Microsoft.Owin;
using NuGetCalcWeb.ViewModels;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace NuGetCalcWeb.RazorSupport
{
    public static class RazorHelper
    {
        public static void Run(IOwinContext owinContext, TextWriter writer, string viewName, Type modelType, object model, DynamicViewBag viewBag)
        {
            using (var service = RazorEngineService.Create(new TemplateServiceConfiguration()
            {
                Activator = new AppTemplateActivator(owinContext),
                BaseTemplateType = typeof(AppTemplateBase<>),
                TemplateManager = AppTemplateManager.Default
            }))
            {
                if (service.IsTemplateCached(viewName, modelType))
                    service.Run(viewName, writer, modelType, model, viewBag);
                else
                    service.RunCompile(AppTemplateManager.ResolveView(viewName), viewName, writer, modelType, model, viewBag);
            }
        }

        public static void Run<T>(IOwinContext owinContext, TextWriter writer, string viewName, T model, DynamicViewBag viewBag = null)
        {
            Run(owinContext, writer, viewName, typeof(T), model, viewBag);
        }

        public static void Run(IOwinContext owinContext, TextWriter writer, string viewName, DynamicViewBag viewBag = null)
        {
            Run(owinContext, writer, viewName, null, null, null);
        }
    }
}
