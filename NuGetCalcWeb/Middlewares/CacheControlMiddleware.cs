using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace NuGetCalcWeb.Middlewares
{
    public class CacheControlMiddleware : OwinMiddleware
    {
        public const string RespondNotModifiedKey = "NuGetCalcWeb.Middlewares.CacheControlMiddleware#RespondNotModified";

        public CacheControlMiddleware(OwinMiddleware next, DateTime utcNow)
            : base(next)
        {
            this.lastModified = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, utcNow.Second, DateTimeKind.Utc);
            this.lastModifiedString = this.lastModified.ToString("R", CultureInfo.InvariantCulture);
            this.etag = string.Concat(BitConverter.GetBytes(utcNow.Ticks).Select(b => b.ToString("x2")));
        }

        private DateTime lastModified;
        private string lastModifiedString;
        private string etag;

        public override Task Invoke(IOwinContext context)
        {
            if (context.Request.IsGetOrHeadRequest())
            {
                var res = context.Response;
                res.Headers.Set("Last-Modified", this.lastModifiedString);
                res.ETag = string.Format("\"{0}\"", this.etag);
                context.Set(RespondNotModifiedKey, ((Func<bool>)(() =>
                {
                    var ifNoneMatch = context.Request.Headers.GetCommaSeparatedValues("If-None-Match");
                    if (ifNoneMatch != null && ifNoneMatch.Any(x => x == "*" || x == etag))
                    {
                        res.StatusCode = 304;
                        return true;
                    }

                    var ifModifiedSince = context.Request.Headers.Get("If-Modified-Since");
                    DateTime ifModifiedSinceDt;
                    if (ifModifiedSince != null
                        && DateTime.TryParseExact(ifModifiedSince, "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out ifModifiedSinceDt)
                        && ifModifiedSinceDt >= lastModified)
                    {
                        res.StatusCode = 304;
                        return true;
                    }

                    return false;
                })));
            }
            return this.Next.Invoke(context);
        }
    }

    public static class CacheControlExtensions
    {
        public static bool RespondNotModified(this IOwinContext context)
        {
            var func = context.Get<Func<bool>>(CacheControlMiddleware.RespondNotModifiedKey);
            if (func == null)
            {
                var req = context.Request;
                Trace.TraceWarning("{0} {1}?{2}: RespondNotModified is not in the environment", req.Method, req.Path, req.QueryString);
                return false;
            }
            return func();
        }
    }
}