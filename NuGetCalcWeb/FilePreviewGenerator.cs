using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Hnx8.ReadJEnc;

namespace NuGetCalcWeb
{
    public static class FilePreviewGenerator
    {
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

        private static Task GenerateFromAssemblyFile(FileInfo input, StreamWriter writer)
        {
            //TODO

            return GenerateFromFile(input, writer);
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
                await writer.WriteAsync(@"<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/8.4/styles/vs.min.css""><pre><code>").ConfigureAwait(false);
                await writer.WriteAsync(await Highlight(text).ConfigureAwait(false)).ConfigureAwait(false);
                await writer.WriteLineAsync("</code></pre>").ConfigureAwait(false);
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

        private static Task<string> Highlight(string code)
        {
            var p = Process.Start(new ProcessStartInfo("node")
            {
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            });
            using (var stdin = p.StandardInput)
            {
                stdin.Write("process.stdout.write(require('highlight.js').highlightAuto(");
                stdin.Write(HttpUtility.JavaScriptStringEncode(code, true));
                stdin.Write(").value)");
            }
            return p.StandardOutput.ReadToEndAsync();
        }
    }
}