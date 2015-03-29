using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                ? pathBase.ToUriComponent() + path.Substring(1)
                : path;
        }
    }

    public static class TemplateExtensions
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return true;
            using (var e = source.GetEnumerator())
                return !e.MoveNext();
        }

        public static string Times(this string s, int count)
        {
            if (count == 0) return "";
            if (count < 0) throw new ArgumentOutOfRangeException();

            var sb = new StringBuilder(s.Length * count);
            for (var i = 0; i < count; i++)
                sb.Append(s);
            return sb.ToString();
        }

        public static string HumanizeBytes(this long length)
        {
            var b = ByteSize.ByteSize.FromBytes(length);
            return string.Format("{0:0.##} {1}", b.LargestWholeNumberValue, b.LargestWholeNumberSymbol);
        }
    }
}