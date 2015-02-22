using LightNode.Formatter;
using LightNode.Server;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(NuGetCalcWeb.Startup))]

namespace NuGetCalcWeb
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseLightNode(new LightNodeOptions(AcceptVerbs.Get, new JsonNetContentFormatter())
            {
                ErrorHandlingPolicy = ErrorHandlingPolicy.ReturnInternalServerErrorIncludeErrorDetails,
                OperationMissingHandlingPolicy = OperationMissingHandlingPolicy.ReturnErrorStatusCodeIncludeErrorDetails
            });
        }
    }
}