﻿using System;
using System.IO;
using System.Text;
using Microsoft.Owin;
using Newtonsoft.Json;
using NuGetCalcWeb.RazorSupport;
using NuGetCalcWeb.ViewModels;
using RazorEngine.Configuration;
using RazorEngine.Templating;

namespace NuGetCalcWeb
{
    public static class ResponseHelper
    {
        private static UTF8Encoding encoding = new UTF8Encoding(false);

        private static void View(IOwinResponse response, string viewName, Type modelType, object model)
        {
            response.ContentType = "text/html; charset=utf-8";
            using (var service = RazorEngineService.Create(new TemplateServiceConfiguration()
            {
                Activator = new AppTemplateActivator(response.Context),
                BaseTemplateType = typeof(AppTemplateBase<>),
                TemplateManager = AppTemplateManager.Default
            }))
            using (var writer = new StreamWriter(response.Body, encoding))
            {
                if (service.IsTemplateCached(viewName, modelType))
                    service.Run(viewName, writer, modelType, model);
                else
                    service.RunCompile(
                        AppTemplateManager.ResolveView(viewName),
                        viewName, writer, modelType, model);
            }
        }

        public static void View<T>(this IOwinResponse response, string viewName, T model)
        {
            View(response, viewName, typeof(T), model);
        }

        public static void View(this IOwinResponse response, string viewName)
        {
            View(response, viewName, null, null);
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