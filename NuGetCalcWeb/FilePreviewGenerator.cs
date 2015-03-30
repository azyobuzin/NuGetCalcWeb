using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Hnx8.ReadJEnc;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory.CSharp;
using Microsoft.Owin;
using Mono.Cecil;
using NuGetCalcWeb.RazorSupport;
using NuGetCalcWeb.ViewModels.FilePreview;
using RazorEngine.Templating;

namespace NuGetCalcWeb
{
    public sealed class FilePreviewGenerator
    {
        private const string HighlightCss = @"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/8.4/styles/vs.min.css"">";

        public static string GetHtmlFilePath(string input)
        {
            var splitedFileName = input.Split(Path.DirectorySeparatorChar);
            return Path.Combine("App_Data", "html", Path.Combine(
                splitedFileName.Skip(Array.LastIndexOf(splitedFileName, "packages") + 1).ToArray()));
        }

        private static ConcurrentDictionary<string, Task> tasks = new ConcurrentDictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

        public FilePreviewGenerator(FileInfo input)
        {
            this.input = input;
            this.htmlFile = new FileInfo(GetHtmlFilePath(input.FullName));
        }

        private readonly FileInfo input;
        private FileInfo htmlFile;
        private IOwinContext owinContext;
        private HeaderModel header;

        public bool NeedsGenerate
        {
            get
            {
                return tasks.ContainsKey(this.input.FullName) || !this.htmlFile.Exists;
            }
        }

        public async Task GenerateHtml(IOwinContext owinContext, HeaderModel header)
        {
            this.owinContext = owinContext;
            this.header = header;
            var fullName = this.input.FullName;

            try
            {
                await tasks.GetOrAdd(fullName, _ => Task.Run(async () =>
                {
                    if (this.htmlFile.Exists) return;

                    Debug.WriteLine("Generating " + this.input.Name);
                    Directory.CreateDirectory(this.htmlFile.DirectoryName);
                    try
                    {
                        var ext = this.input.Extension;
                        await (ext == ".dll" || ext == ".exe"
                            ? this.GenerateFromAssemblyFile()
                            : this.GenerateFromFile()
                        ).ConfigureAwait(false);
                    }
                    catch
                    {
                        try
                        {
                            this.htmlFile.Delete();
                        }
                        catch { }
                        throw;
                    }
                })).ConfigureAwait(false);
            }
            finally
            {
                Task tmp;
                tasks.TryRemove(fullName, out tmp);
            }
        }

        private async Task GenerateFromAssemblyFile()
        {
            ModuleDefinition module = null;
            try
            {
                module = ModuleDefinition.ReadModule(this.input.FullName, new ReaderParameters()
                {
                    ReadingMode = ReadingMode.Immediate,
                    AssemblyResolver = new MyAssemblyResolver()
                });
            }
            catch (BadImageFormatException) { }

            if (module == null)
            {
                await this.GenerateFromFile().ConfigureAwait(false);
                return;
            }

            var model = new AssemblyModel()
            {
                AssemblyName = module.Assembly.Name.Name,
                AssemblyDescription = await HighlightCs(GenerateAssemblyDescription(module)).ConfigureAwait(false)
            };

            var types = new List<TypeDefinition>();
            model.Namespaces = module.Types
                .Where(t => t.IsPublic)
                .GroupBy(t => t.Namespace)
                .OrderBy(g => g.Key)
                .Select(g => new NamespaceModel()
                {
                    Name = g.Key,
                    Types = g.OrderBy(t => t.Name).Select(t => CreateTypeModel(t, types)).ToArray()
                })
                .ToArray();

            var tasks = new Task<TypeDescription>[types.Count];
            using (var genSemaphore = new SemaphoreSlim(4))
            {
                for (var i = 0; i < types.Count; i++)
                {
                    var type = types[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        await genSemaphore.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            Debug.WriteLine(type.FullName);
                            return new TypeDescription()
                            {
                                FullName = type.FullName,
                                Code = await HighlightCs(GenerateTypeDescription(module, type)).ConfigureAwait(false)
                            };
                        }
                        finally
                        {
                            genSemaphore.Release();
                        }
                    });
                }

                model.TypeDescriptions = await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            this.RunTemplate("Assembly", model);
        }

        private static TypeModel CreateTypeModel(TypeDefinition type, List<TypeDefinition> types)
        {
            types.Add(type);
            return new TypeModel()
            {
                Name = type.Name,
                FullName = type.FullName,
                HtmlClass = type.IsEnum ? "enum"
                    : type.IsValueType ? "struct"
                    : type.IsInterface ? "interface"
                    : type.IsDelegate() ? "delegate"
                    : "class",
                NestedTypes = type.NestedTypes
                    .Where(t => t.IsPublic || t.IsNestedPublic)
                    .OrderBy(t => t.Name)
                    .Select(t => CreateTypeModel(t, types))
                    .ToArray()
            };
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

            if (!(type.IsEnum || type.IsInterface || type.IsDelegate()))
            {
                var typeNode = astBuilder.SyntaxTree.GetTypes(false).First();

                // Remove nested types
                foreach (var node in typeNode.Children)
                    if (node is TypeDeclaration)
                        node.Remove();

                // Remove non-public members
                foreach (var member in typeNode.Members)
                    if (!(member.HasModifier(Modifiers.Public) || member.HasModifier(Modifiers.Protected)))
                        member.Remove();
            }

            astBuilder.RunTransformations();
            AddXmlDocTransform.Run(astBuilder.SyntaxTree);
            var output = new PlainTextOutput();
            astBuilder.GenerateCode(output);
            return output.ToString();
        }

        private void RunTemplate<T>(string viewName, T model) where T : FilePreviewModel
        {
            model.Header = this.header;
            var viewBag = new DynamicViewBag();
            viewBag.SetValue("Title", this.header.Breadcrumbs[this.header.Breadcrumbs.Length - 1]); //TODO: アップロードでおかしくなりそう
            viewBag.SetValue("NoIndex", true);
            using (var writer = new StreamWriter(this.htmlFile.FullName, false, ResponseHelper.DefaultEncoding))
                RazorHelper.Run(this.owinContext, writer, "FilePreview/" + viewName, model, viewBag);
        }

        private async Task GenerateFromFile()
        {
            CharCode charCode;
            string text;
            using (var r = new FileReader(this.input))
            {
                charCode = r.Read(this.input);
                text = r.Text;
            }

            if (charCode == FileType.READERROR)
                throw new Exception("READERROR: " + this.input.FullName);
            if (charCode == FileType.HUGEFILE)
                throw new Exception("HUGEFILE: " + this.input.FullName); // unreachable

            if (text != null)
            {
                this.RunTemplate("TextFile", new ContentModel(
                    this.input.Extension.ToLowerInvariant() == ".txt"
                        ? HttpUtility.HtmlEncode(text)
                        : await HighlightAuto(text).ConfigureAwait(false)
                ));
            }
            else if (charCode == FileType.EMPTYFILE)
            {
                this.RunTemplate("Alert", new ContentModel("This is an empty file."));
            }
            else if (charCode is FileType.Image)
            {
                this.RunTemplate("ImageFile", new ContentModel(new UriBuilder(this.owinContext.Request.Uri)
                {
                    Query = "dl=true"
                }.Uri.AbsoluteUri));
            }
            else
            {
                this.RunTemplate("Alert", new ContentModel("This is a binary file."));
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