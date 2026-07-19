namespace PastyPropellant.ProcessHandling.ProcessHandlers;

using System.Diagnostics;
using System.Threading.Tasks;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;

public static class ProcessHandler
{
    // Stderr is buffered so a failing subprocess stays diagnosable even when no
    // ProcessErrorLogEvent subscriber is attached. Bounded (tail kept) to avoid
    // unbounded growth on a process that spews to stderr indefinitely.
    private const int MaxStderrLines = 50;
    private const int MaxStderrChars = 8192;

    public static Task<OperationResult> RunProcessAsync(
        string command, string arguments)
    {
        var tcs = new TaskCompletionSource<OperationResult>();
        var process = new Process();

        var stderrLines = new Queue<string>();
        var stderrChars = 0;
        var stderrTruncated = false;
        var stderrLock = new object();

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
                    lock (stderrLock)
                    {
                        stderrLines.Enqueue(e.Data);
                        stderrChars += e.Data.Length;

                        while (stderrLines.Count > MaxStderrLines || stderrChars > MaxStderrChars)
                        {
                            stderrChars -= stderrLines.Dequeue().Length;
                            stderrTruncated = true;
                        }
                    }

                    EventBus<ProcessErrorLogEvent>.Publish(
                        new ProcessErrorLogEvent(e.Data, process.StartInfo.FileName));
                }
            };

            process.Exited += (sender, e) =>
            {
                // Parameterless WaitForExit also waits for the redirected stdout/stderr
                // readers to reach EOF, so the stderr buffer is complete at this point.
                process.WaitForExit();

                var exitCode = process.ExitCode;
                var result = exitCode == 0
                    ? new OperationResult()
                    : new OperationResult(new Exception(
                        $"Process failed. Code: {exitCode}\nCommand: {command} {arguments}{FormatStderr()}"));

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

        string FormatStderr()
        {
            lock (stderrLock)
            {
                if (stderrLines.Count == 0) return string.Empty;

                var prefix = stderrTruncated ? $"\nStderr (last {stderrLines.Count} lines):\n" : "\nStderr:\n";

                return prefix + string.Join('\n', stderrLines);
            }
        }
    }
}
