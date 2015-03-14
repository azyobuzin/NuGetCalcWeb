using System.IO;
using System.Text;
using Microsoft.Owin;
using Newtonsoft.Json;
using RazorEngine;
using RazorEngine.Templating;

namespace NuGetCalcWeb
{
    public static class ResponseHelper
    {
        private static UTF8Encoding encoding = new UTF8Encoding(false);

        public static void View<T>(this IOwinResponse response, string viewName, T model)
        {
            response.ContentType = "text/html; charset=utf-8";
            using (var writer = new StreamWriter(response.Body, encoding))
            {
                var service = Engine.Razor;
                if (service.IsTemplateCached(viewName, typeof(T)))
                {
                    service.Run(viewName, writer, typeof(T), model);
                }
                else
                {
                    var source = File.ReadAllText(Path.Combine("Views", viewName + ".cshtml"));
                    service.RunCompile(source, viewName, writer, typeof(T), model);
                }
            }
        }

        public static void Json(this IOwinResponse response, object value)
        {
            var bytes = encoding.GetBytes(JsonConvert.SerializeObject(value));
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength = bytes.LongLength;
            response.Write(bytes);
        }
    }
}