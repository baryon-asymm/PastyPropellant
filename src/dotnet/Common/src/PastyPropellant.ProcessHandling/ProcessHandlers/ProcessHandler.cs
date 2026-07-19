namespace PastyPropellant.ProcessHandling.ProcessHandlers;

using System.Diagnostics;
using System.Threading;
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

    // After killing a hung process we give its redirected stderr reader a bounded moment to
    // drain, so the failure exception still carries whatever the process managed to emit.
    private static readonly TimeSpan KilledProcessDrainGrace = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Starts <paramref name="command"/> and completes when it exits, is cancelled, or times out.
    /// </summary>
    /// <param name="cancellationToken">
    /// Optional. When cancelled, the process tree is killed and the task completes with a failed
    /// <see cref="OperationResult"/>. Defaults to <see cref="CancellationToken.None"/>, which
    /// preserves the previous wait-forever behaviour.
    /// </param>
    /// <param name="timeout">
    /// Optional wall-clock cap on the run. <c>null</c> (the default) or
    /// <see cref="Timeout.InfiniteTimeSpan"/> means no timeout, preserving previous behaviour.
    /// </param>
    /// <remarks>
    /// The returned task never faults and is never left unresolved: cancellation and timeout are
    /// reported as a failed <see cref="OperationResult"/> carrying the buffered stderr, not as an
    /// <see cref="OperationCanceledException"/>. That keeps the five existing call sites — which
    /// branch on <c>IsSuccess</c> and never catch — behaving correctly without change.
    /// </remarks>
    public static Task<OperationResult> RunProcessAsync(
        string command,
        string arguments,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        if (timeout.HasValue &&
            timeout.Value != Timeout.InfiniteTimeSpan &&
            timeout.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout), timeout, "Timeout must be positive or Timeout.InfiniteTimeSpan.");
        }

        var hasTimeout = timeout.HasValue && timeout.Value != Timeout.InfiniteTimeSpan;

        var tcs = new TaskCompletionSource<OperationResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var process = new Process();

        var stderrLines = new Queue<string>();
        var stderrChars = 0;
        var stderrTruncated = false;
        var stderrLock = new object();

        // Exactly one of {normal exit, cancellation/timeout, start failure} may dispose the
        // process and complete the task. Everything else becomes a no-op.
        var completed = 0;

        CancellationTokenSource? linkedCts = null;
        CancellationTokenRegistration registration = default;

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                process.Dispose();
                tcs.TrySetResult(new OperationResult(new OperationCanceledException(
                    $"Process was not started because the operation was already cancelled.\n" +
                    $"Command: {command} {arguments}")));

                return tcs.Task;
            }

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
                        new ProcessInfoLogEvent(e.Data, command));
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
                        new ProcessErrorLogEvent(e.Data, command));
                }
            };

            process.Exited += (sender, e) =>
            {
                // Must not throw: this runs on a Process event thread, where an escaping
                // exception would tear down the process rather than fail the task.
                try
                {
                    // Parameterless WaitForExit also waits for the redirected stdout/stderr
                    // readers to reach EOF, so the stderr buffer is complete at this point.
                    process.WaitForExit();

                    var exitCode = process.ExitCode;
                    var result = exitCode == 0
                        ? new OperationResult()
                        : new OperationResult(new Exception(
                            $"Process failed. Code: {exitCode}\n" +
                            $"Command: {command} {arguments}{FormatStderr()}"));

                    Complete(result);
                }
                catch (Exception ex)
                {
                    // Typically an ObjectDisposedException because the cancellation path already
                    // completed and disposed; Complete() is idempotent so this is harmless.
                    Complete(new OperationResult(ex));
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (hasTimeout || cancellationToken.CanBeCanceled)
            {
                var effectiveToken = cancellationToken;

                if (hasTimeout)
                {
                    linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    linkedCts.CancelAfter(timeout!.Value);
                    effectiveToken = linkedCts.Token;
                }

                registration = effectiveToken.Register(() =>
                {
                    // Hand off to the thread pool so we never block the canceller (a timer thread
                    // for CancelAfter, or the caller's thread for an explicit Cancel()).
                    ThreadPool.UnsafeQueueUserWorkItem(
                        _ => KillAndFail(cancellationToken.IsCancellationRequested == false && hasTimeout),
                        null);
                });

                // Register() fires synchronously if the token was already cancelled, so the race
                // where the process exits before registration is covered by Complete()'s gate.
            }
        }
        catch (Exception ex)
        {
            registration.Dispose();
            linkedCts?.Dispose();
            process.Dispose();
            tcs.TrySetResult(new OperationResult(ex));

            return tcs.Task;
        }

        return tcs.Task;

        void KillAndFail(bool timedOut)
        {
            // Bail early if the process already exited normally; avoids touching a disposed Process.
            if (Volatile.Read(ref completed) != 0) return;

            try
            {
                process.Kill(entireProcessTree: true);

                // Bounded, best-effort drain so the buffered stderr is as complete as possible.
                process.WaitForExit((int)KilledProcessDrainGrace.TotalMilliseconds);
            }
            catch
            {
                // Process already exited, was never started, or the tree kill partially failed.
                // Either way the task must still complete — fall through.
            }

            var reason = timedOut
                ? $"Process timed out after {timeout!.Value} and was killed."
                : "Process was cancelled and killed.";

            Complete(new OperationResult(new OperationCanceledException(
                $"{reason}\nCommand: {command} {arguments}{FormatStderr()}")));
        }

        void Complete(OperationResult result)
        {
            if (Interlocked.Exchange(ref completed, 1) != 0) return;

            // Dispose the registration before the process so a pending cancellation callback
            // cannot resurrect a disposed Process. The callback never blocks, so this cannot
            // deadlock even when invoked from within it.
            registration.Dispose();
            linkedCts?.Dispose();
            process.Dispose();

            tcs.TrySetResult(result);
        }

        string FormatStderr()
        {
            lock (stderrLock)
            {
                if (stderrLines.Count == 0) return string.Empty;

                var prefix = stderrTruncated
                    ? $"\nStderr (last {stderrLines.Count} lines):\n"
                    : "\nStderr:\n";

                return prefix + string.Join('\n', stderrLines);
            }
        }
    }
}
