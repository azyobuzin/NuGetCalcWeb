using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NuGetCalcWeb
{
    public static class NodeRunner
    {
        public static async Task<string> Run(string code)
        {
            using (var p = Process.Start(new ProcessStartInfo("node")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }))
            {
                p.EnableRaisingEvents = true;
                var processWaitTask = new TaskCompletionSource<bool>();
                p.Exited += (sender, e) => processWaitTask.SetResult(p.ExitCode == 0);

                var stdout = p.StandardOutput.ReadToEndAsync();
                var stderr = p.StandardError.ReadToEndAsync();

                using (var stdin = p.StandardInput)
                    await stdin.WriteAsync(code).ConfigureAwait(false);

                if (await processWaitTask.Task.ConfigureAwait(false))
                    return await stdout.ConfigureAwait(false);
                else
                    throw new NodeException(p.ExitCode, await stdout.ConfigureAwait(false), await stderr.ConfigureAwait(false));
            }
        }
    }

    public class NodeException : Exception
    {
        public NodeException(int exitCode, string stdout, string stderr)
            : base(string.Format("Node exited with {0}: {1}", exitCode, stderr))
        {
            this.ExitCode = exitCode;
            this.StandardOutput = stdout;
            this.StandardError = stderr;
        }

        public int ExitCode { get; private set; }
        public string StandardOutput { get; private set; }
        public string StandardError { get; private set; }
    }
}