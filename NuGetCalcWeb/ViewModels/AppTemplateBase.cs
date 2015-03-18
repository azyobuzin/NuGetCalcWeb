using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Owin;
using NuGetCalcWeb.RazorSupport;
using RazorEngine.Templating;

namespace NuGetCalcWeb.ViewModels
{
    public abstract class AppTemplateBase<T> : HtmlTemplateBase<T>, IAppTemplate
    {
        public IOwinContext OwinContext { get; set; }

        public override string ResolveUrl(string path)
        {
            var pathBase = this.OwinContext.Request.PathBase;
            return path.StartsWith("~/")
                ? (pathBase.HasValue
                    ? pathBase.ToUriComponent() + path.Substring(2)
                    : path.Substring(1))
                : path;
        }
    }
}