using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Hnx8.ReadJEnc;

namespace NuGetCalcWeb
{
    public static class FilePreviewGenerator
    {
        public static Task<string> GenerateHtml(FileInfo input)
        {
            var ext = input.Extension;
            return ext == ".dll" || ext == ".exe"
                ? GenerateFromAssemblyFile(input)
                : GenerateFromFile(input);
        }

        private static Task<string> GenerateFromAssemblyFile(FileInfo input)
        {
            //TODO

            return GenerateFromFile(input);
        }

        private static async Task<string> GenerateFromFile(FileInfo input)
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
                return string.Format(@"<pre><code>{0}</code></pre>",
                    await Highlight(text).ConfigureAwait(false));
            }

            if (charCode == FileType.EMPTYFILE)
            {
                return @"<p class=""alert alert-warning"">This is an empty file.</p>";
            }

            if (charCode is FileType.Image)
            {
                var sb = new StringBuilder(@"<div style=""text-align:center""><img style=""max-width:100%"" src=""data:");
                sb.Append(
                    charCode == FileType.BMP ? "image/x-ms-bmp"
                    : charCode == FileType.GIF ? "image/gif"
                    : charCode == FileType.IMGICON ? "image/vnd.microsoft.icon"
                    : charCode == FileType.JPEG ? "image/jpeg"
                    : charCode == FileType.PNG ? "image/png"
                    : charCode == FileType.TIFF ? "image/tiff"
                    : "application/octet-stream");
                sb.Append(";base64,");

                using (var stream = input.OpenRead())
                {
                    const int bufSize = 15 * 1024; // multiples of 3
                    var buf = new byte[bufSize];
                    int count;
                    while (true)
                    {
                        count = stream.Read(buf, 0, bufSize);
                        if (count == 0) break;
                        sb.Append(Convert.ToBase64String(buf, 0, count));
                    }
                }

                sb.Append(@""" /></div>");
                return sb.ToString();
            }

            return @"<p class=""alert alert-warning"">This is a binary file.</p>";
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