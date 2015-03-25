using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Hnx8.ReadJEnc;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.NRefactory.Documentation;
using Mono.Cecil;

namespace NuGetCalcWeb
{
    public static class FilePreviewGenerator
    {
        private const string HighlightCss = @"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/8.4/styles/vs.min.css"">";

        private static ConcurrentDictionary<string, Task<string>> tasks = new ConcurrentDictionary<string, Task<string>>(StringComparer.OrdinalIgnoreCase);

        public static async Task<string> GenerateHtml(FileInfo input)
        {
            var fullName = input.FullName;
            var splitedFileName = fullName.Split(Path.DirectorySeparatorChar);
            var htmlFile = new FileInfo(Path.Combine("App_Data", "html", Path.Combine(
                splitedFileName.Skip(Array.LastIndexOf(splitedFileName, "App_Data") + 1).ToArray())));
            if (htmlFile.Exists)
            {
                using (var sr = new StreamReader(htmlFile.FullName))
                    return await sr.ReadToEndAsync().ConfigureAwait(false);
            }

            var result = await tasks.GetOrAdd(fullName, _ => Task.Run(async () =>
            {
                Debug.WriteLine("Generating " + input.Name);
                Directory.CreateDirectory(htmlFile.DirectoryName);
                using (var stream = new FileStream(htmlFile.FullName, FileMode.Create, FileAccess.ReadWrite))
                {
                    using (var sw = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                    {
                        var ext = input.Extension;
                        await (ext == ".dll" || ext == ".exe"
                            ? GenerateFromAssemblyFile(input, sw)
                            : GenerateFromFile(input, sw)
                        ).ConfigureAwait(false);
                    }

                    stream.Position = 0;
                    using (var sr = new StreamReader(stream))
                        return await sr.ReadToEndAsync().ConfigureAwait(false);
                }
            })).ConfigureAwait(false);

            Task<string> tmp;
            tasks.TryRemove(fullName, out tmp);
            return result;
        }

        private static async Task GenerateFromAssemblyFile(FileInfo input, StreamWriter writer)
        {
            ModuleDefinition module = null;
            try
            {
                module = ModuleDefinition.ReadModule(input.FullName, new ReaderParameters()
                {
                    AssemblyResolver = new MyAssemblyResolver()
                });
            }
            catch (BadImageFormatException) { }

            if (module == null)
                await GenerateFromFile(input, writer).ConfigureAwait(false);

            XmlDocumentationProvider xmlDoc = null;
            var xmlFile = Path.ChangeExtension(module.FullyQualifiedName, ".xml");
            if (File.Exists(xmlFile))
            {
                try
                {
                    xmlDoc = new XmlDocumentationProvider(xmlFile);
                }
                catch (Exception ex)
                {
                    Debug.Fail(ex.ToString());
                }
            }

            await writer.WriteLineAsync(string.Format(HighlightCss +
                @"<div class=""row""><div id=""type-list"" class=""col-sm-4""><a id=""link-asm"" href=""#asm"">{0}</a><ul>",
                module.Assembly.Name.Name
            )).ConfigureAwait(false);

            var types = new List<TypeDefinition>();
            foreach (var g in module.Types.Where(t => t.IsPublic).GroupBy(t => t.Namespace).OrderBy(g => g.Key))
            {
                await writer.WriteLineAsync(string.Format(@"<li class=""namespace""><a class=""link-namespace"" href=""#"">{0}</a><ul>", g.Key)).ConfigureAwait(false);
                foreach (var t in g.OrderBy(t => t.Name))
                    await WriteClass(t, writer, types).ConfigureAwait(false);
                await writer.WriteLineAsync("</ul></li>").ConfigureAwait(false);
            }

            await writer.WriteLineAsync(string.Format(
                @"</ul></div><div id=""typedesc-container"" class=""col-sm-8""><pre id=""asm"" class=""typedesc active"">{0}</pre>",
                await HighlightCs(GenerateAssemblyDescription(module)).ConfigureAwait(false)
            )).ConfigureAwait(false);

            var tasks = new Task[types.Count];

            using (var genSemaphore = new SemaphoreSlim(4))
            using (var writeSemaphore = new SemaphoreSlim(1))
            {
                for (var i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        await genSemaphore.WaitAsync().ConfigureAwait(false);
                        string html;
                        try
                        {
                            Debug.WriteLine(type.FullName);
                            html = string.Format(
                                @"<pre class=""typedesc"" id=""t{0}"">{1}</pre>",
                                type.MetadataToken.RID.ToString("x"),
                                await HighlightCs(GenerateTypeDescription(module, type)).ConfigureAwait(false)
                            );
                        }
                        finally
                        {
                            genSemaphore.Release();
                        }

                        await writeSemaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            await writer.WriteLineAsync(html).ConfigureAwait(false);
                        }
                        finally
                        {
                            writeSemaphore.Release();
                        }
                    });
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            await writer.WriteLineAsync(@"</div></div><script src=""/content/asmbrowser.js""></script>").ConfigureAwait(false);
        }

        private static async Task WriteClass(TypeDefinition type, StreamWriter writer, List<TypeDefinition> types)
        {
            types.Add(type);
            var kind = type.IsEnum ? "enum"
                : type.IsValueType ? "struct"
                : type.IsInterface ? "interface"
                : type.IsDelegate() ? "delegate"
                : "class";
            await writer.WriteLineAsync(string.Format(
                @"<li class=""type""><a class=""link-type {2}"" href=""#t{0}"" data-rid=""{0}"">{1}</a>",
                type.MetadataToken.RID.ToString("x"),
                type.Name,
                kind
            )).ConfigureAwait(false);

            var nested = type.NestedTypes.Where(t => t.IsPublic || t.IsNestedPublic).OrderBy(t => t.Name).ToArray();
            if (nested.Length > 0)
            {
                await writer.WriteLineAsync("<ul>").ConfigureAwait(false);
                foreach (var t in nested)
                {
                    await WriteClass(t, writer, types).ConfigureAwait(false);
                }
                await writer.WriteLineAsync("</ul>").ConfigureAwait(false);
            }

            await writer.WriteLineAsync("</li>").ConfigureAwait(false);
        }

        private static string GenerateAssemblyDescription(ModuleDefinition module)
        {
            var context = new DecompilerContext(module);
            var astBuilder = new AstBuilder(context) { DecompileMethodBodies = false };
            astBuilder.AddAssembly(module, true);
            var output = new PlainTextOutput();
            astBuilder.GenerateCode(output);
            return output.ToString();
        }

        private static string GenerateTypeDescription(ModuleDefinition module, TypeDefinition type)
        {
            var context = new DecompilerContext(module);
            var astBuilder = new AstBuilder(context) { DecompileMethodBodies = false };
            astBuilder.AddType(type);
            astBuilder.RunTransformations();
            //TODO: Add XML Documentation
            var output = new PlainTextOutput();
            astBuilder.GenerateCode(output);
            return output.ToString();
        }

        private static async Task GenerateFromFile(FileInfo input, StreamWriter writer)
        {
            CharCode charCode;
            string text;
            using (var r = new FileReader(input))
            {
                charCode = r.Read(input);
                text = r.Text;
            }

            if (charCode == FileType.READERROR)
                throw new Exception("READERROR: " + input.FullName);
            if (charCode == FileType.HUGEFILE)
                throw new NotImplementedException(); // unreachable

            if (text != null)
            {
                await writer.WriteAsync(HighlightCss + "<pre>").ConfigureAwait(false);
                await writer.WriteAsync(await HighlightAuto(text).ConfigureAwait(false)).ConfigureAwait(false);
                await writer.WriteLineAsync("</pre>").ConfigureAwait(false);
            }
            else if (charCode == FileType.EMPTYFILE)
            {
                await writer.WriteLineAsync(@"<p class=""alert alert-warning"">This is an empty file.</p>").ConfigureAwait(false);
            }
            else if (charCode is FileType.Image)
            {
                await writer.WriteAsync(string.Format(
                    @"<div style=""text-align:center""><img style=""max-width:100%"" src=""data:{0};base64,",
                    (charCode == FileType.BMP ? "image/x-ms-bmp"
                        : charCode == FileType.GIF ? "image/gif"
                        : charCode == FileType.IMGICON ? "image/vnd.microsoft.icon"
                        : charCode == FileType.JPEG ? "image/jpeg"
                        : charCode == FileType.PNG ? "image/png"
                        : charCode == FileType.TIFF ? "image/tiff"
                        : "application/octet-stream")
                )).ConfigureAwait(false);

                using (var stream = input.OpenRead())
                {
                    const int bufSize = 15 * 1024; // multiples of 3
                    var buf = new byte[bufSize];
                    int count;
                    while (true)
                    {
                        count = await stream.ReadAsync(buf, 0, bufSize).ConfigureAwait(false);
                        if (count == 0) break;
                        await writer.WriteAsync(Convert.ToBase64String(buf, 0, count)).ConfigureAwait(false);
                    }
                }

                await writer.WriteLineAsync(@""" /></div>").ConfigureAwait(false);
            }
            else
            {
                await writer.WriteLineAsync(@"<p class=""alert alert-warning"">This is a binary file.</p>").ConfigureAwait(false);
            }
        }

        private static Task<string> HighlightAuto(string code)
        {
            return NodeRunner.Run(string.Format(
                "process.stdout.write(require('highlight.js').highlightAuto({0}).value)",
                HttpUtility.JavaScriptStringEncode(code, true)
            ));
        }

        private static Task<string> HighlightCs(string code)
        {
            return NodeRunner.Run(string.Format(
                "process.stdout.write(require('highlight.js').highlight('cs', {0}, true).value)",
                HttpUtility.JavaScriptStringEncode(code, true)
            ));
        }
    }
}