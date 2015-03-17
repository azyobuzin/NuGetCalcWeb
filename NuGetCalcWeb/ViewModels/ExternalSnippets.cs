using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using RazorEngine.Text;

namespace NuGetCalcWeb.ViewModels
{
    public class ExternalSnippets
    {
        public IEncodedString Analytics { get; private set; }
        public IEncodedString AdTop { get; private set; }

        public ExternalSnippets(string analytics, string adTop)
        {
            this.Analytics = new RawString(analytics);
            this.AdTop = new RawString(adTop);
        }

        private static string GetContentFromEnvVar(string variable)
        {
            var env = Environment.GetEnvironmentVariable(variable);
            if (env != null)
            {
                if (Regex.IsMatch(env, "^https?://"))
                {
                    try
                    {
                        using (var wc = new WebClient())
                            return wc.DownloadString(env);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning(ex.ToString());
                    }
                }
                if (File.Exists(env))
                    return File.ReadAllText(env);
            }
            return "";
        }

        private static ExternalSnippets _default;
        public static ExternalSnippets Default
        {
            get
            {
                if (_default == null)
                {
                    var analytics = GetContentFromEnvVar("NUGETCALC_ANALYTICS");
                    var ad = GetContentFromEnvVar("NUGETCALC_AD");
                    _default = new ExternalSnippets(analytics, ad);
                }
                return _default;
            }
        }
    }
}