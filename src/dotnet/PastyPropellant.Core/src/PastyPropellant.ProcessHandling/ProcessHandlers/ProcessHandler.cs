namespace PastyPropellant.ProcessHandling.ProcessHandlers;

using System.Diagnostics;
using System.Threading.Tasks;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;

public static class ProcessHandler
{
    public static Task<OperationResult> RunProcessAsync(
        string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        var tcs = new TaskCompletionSource<OperationResult>();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                EventBus<ProcessInfoLogEvent>.Publish(
                    new ProcessInfoLogEvent(e.Data, processStartInfo.FileName));
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                EventBus<ProcessInfoLogEvent>.Publish(
                    new ProcessInfoLogEvent(e.Data, processStartInfo.FileName));
            }
        };

        process.Exited += (sender, e) =>
        {
            var exitCode = process.ExitCode;
            process.Dispose();
            if (exitCode == 0)
            {
                tcs.SetResult(new OperationResult());
            }
            else
            {
                tcs.SetResult(new OperationResult(
                    new Exception($"Process exited with code {exitCode}.")));
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return tcs.Task;
    }
}
