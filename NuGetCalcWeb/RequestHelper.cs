using System;
using Microsoft.Owin;

namespace NuGetCalcWeb
{
    public static class RequestHelper
    {
        public static bool IsHeadRequest(this IOwinRequest request)
        {
            return request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
        }
    }
}
