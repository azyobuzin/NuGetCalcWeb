using System.Text;
using System.Threading.Tasks;
using Azyobuzi.OwinRazor;
using Microsoft.Owin;
using NuGetCalcWeb.ViewModels;

namespace NuGetCalcWeb
{
    public static class ResponseHelper
    {
        public static readonly Encoding DefaultEncoding = new UTF8Encoding(false);

        public static Task Error(this IOwinResponse response, int statusCode, ErrorModel errorModel)
        {
            response.StatusCode = statusCode;
            return response.View(new Views.Error(), errorModel);
        }
    }
}
