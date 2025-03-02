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
        var tcs = new TaskCompletionSource<OperationResult>();
        var process = new Process();

        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data) == false)
                {
                    EventBus<ProcessInfoLogEvent>.Publish(
                        new ProcessInfoLogEvent(e.Data, process.StartInfo.FileName));
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data) == false)
                {
                    EventBus<ProcessInfoLogEvent>.Publish(
                        new ProcessInfoLogEvent(e.Data, process.StartInfo.FileName));
                }
            };

            process.Exited += (sender, e) =>
            {
                process.WaitForExit();

                var exitCode = process.ExitCode;
                var result = exitCode == 0 
                    ? new OperationResult() 
                    : new OperationResult(new Exception($"Process failed. Code: {exitCode}\nCommand: {command} {arguments}"));
                
                process.Dispose();
                tcs.TrySetResult(result);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            process.Dispose();
            tcs.TrySetResult(new OperationResult(ex));
        }

        return tcs.Task;
    }
}
