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

        public static bool IsGetOrHeadRequest(this IOwinRequest request)
        {
            var method = request.Method;
            return method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                || method.Equals("HEAD", StringComparison.OrdinalIgnoreCase);
        }
    }
}
