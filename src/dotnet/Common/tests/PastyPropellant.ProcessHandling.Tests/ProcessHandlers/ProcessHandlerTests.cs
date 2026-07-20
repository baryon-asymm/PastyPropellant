namespace PastyPropellant.ProcessHandling.Tests.ProcessHandlers;

using System.Diagnostics;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using PastyPropellant.ProcessHandling.ProcessHandlers;

/// <summary>
/// Behaviour contract of <see cref="ProcessHandler.RunProcessAsync"/> after the bounded-stderr
/// (BUG-2) and cancellation/timeout (COR-5) changes.
///
/// Every test drives a real child process, so each one is kept to well under a second of wall
/// clock and asserts an upper bound on how long the returned task may take.
/// </summary>
public sealed class ProcessHandlerTests
{
    private static readonly TimeSpan CompletionBudget = TimeSpan.FromSeconds(20);

    // The stderr buffer's own limits, mirrored here so the tests state the contract explicitly.
    private const int MaxStderrLines = 50;
    private const int MaxStderrChars = 8192;

    [Fact]
    public async Task SuccessfulProcess_ReportsSuccess()
    {
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("exit 0\n");

        var result = await RunAsync(script);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task StdoutLines_ArePublishedAsProcessInfoLogEvents()
    {
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("echo hello-from-child\nexit 0\n");
        using var scope = new EventBusScope<ProcessInfoLogEvent>();

        var messages = new List<string>();
        scope.Subscribe(e =>
        {
            lock (messages) messages.Add(e.Message);
        });

        var result = await RunAsync(script);

        Assert.True(result.IsSuccess);

        lock (messages) Assert.Contains("hello-from-child", messages);
    }

    [Fact]
    public async Task NonZeroExit_SurfacesExitCodeAndStderrInTheException()
    {
        // BUG-2: stderr used to be published only, so a failing subprocess was undiagnosable when
        // nothing was subscribed. It must now travel inside the failure exception itself.
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript(
            """
            echo "first failure detail" >&2
            echo "second failure detail" >&2
            exit 3
            """);

        var result = await RunAsync(script);

        Assert.False(result.IsSuccess);

        var message = result.Exception!.Message;

        Assert.Contains("Code: 3", message);
        Assert.Contains("Stderr:", message);
        Assert.Contains("first failure detail", message);
        Assert.Contains("second failure detail", message);
    }

    [Fact]
    public async Task NonZeroExit_WithNoStderr_OmitsTheStderrSection()
    {
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("exit 7\n");

        var result = await RunAsync(script);

        Assert.False(result.IsSuccess);

        var message = result.Exception!.Message;

        Assert.Contains("Code: 7", message);
        Assert.DoesNotContain("Stderr", message);
    }

    [Fact]
    public async Task StderrBuffer_IsBoundedByLineCount_AndKeepsTheTail()
    {
        if (Shell.IsAvailable == false) return;

        const int emitted = 200;

        using var script = new TempScript(
            $"""
            i=1
            while [ $i -le {emitted} ]; do
              echo "ERRLINE-$i" >&2
              i=$((i+1))
            done
            exit 9
            """);

        var result = await RunAsync(script);

        Assert.False(result.IsSuccess);

        var lines = ExtractStderrLines(result.Exception!.Message);

        // Short lines, so the line cap (not the char cap) is what bounds this.
        Assert.Equal(MaxStderrLines, lines.Count);

        // Truncation is announced...
        Assert.Contains($"Stderr (last {MaxStderrLines} lines):", result.Exception.Message);

        // ...and the TAIL is what survives — the newest lines, not the oldest.
        var expected = Enumerable
            .Range(emitted - MaxStderrLines + 1, MaxStderrLines)
            .Select(i => $"ERRLINE-{i}")
            .ToList();

        Assert.Equal(expected, lines);
    }

    [Fact]
    public async Task StderrBuffer_IsBoundedByCharacterCount()
    {
        if (Shell.IsAvailable == false) return;

        // 5 lines well under the 50-line cap but far over the 8192-char cap, so only the char
        // bound can be what trims them.
        const int chunks = 400;   // 400 * 10 == 4000 padding characters per line
        const int emitted = 5;

        using var script = new TempScript(
            $"""
            chunk="XXXXXXXXXX"
            pad=""
            n=0
            while [ $n -lt {chunks} ]; do
              pad="$pad$chunk"
              n=$((n+1))
            done
            i=1
            while [ $i -le {emitted} ]; do
              echo "L$i$pad" >&2
              i=$((i+1))
            done
            exit 9
            """);

        var result = await RunAsync(script);

        Assert.False(result.IsSuccess);

        var lines = ExtractStderrLines(result.Exception!.Message);

        Assert.True(lines.Count < emitted, $"expected trimming, kept {lines.Count} of {emitted} lines");
        Assert.True(
            lines.Sum(l => l.Length) <= MaxStderrChars,
            $"buffer held {lines.Sum(l => l.Length)} chars, cap is {MaxStderrChars}");

        // Tail again: the last emitted line must be the last one kept.
        Assert.StartsWith($"L{emitted}", lines[^1]);
    }

    [Fact]
    public async Task Timeout_KillsTheProcess_AndCompletesTheTask()
    {
        // COR-5: before the fix a hung child left the returned task unresolved forever.
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("sleep 120\n");

        var stopwatch = Stopwatch.StartNew();

        var result = await RunAsync(script, timeout: TimeSpan.FromMilliseconds(300));

        stopwatch.Stop();

        Assert.False(result.IsSuccess);
        Assert.IsType<OperationCanceledException>(result.Exception);
        Assert.Contains("timed out", result.Exception!.Message);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(15),
            $"timeout path took {stopwatch.Elapsed}, which means it waited for the child instead of killing it");
    }

    [Fact]
    public async Task Timeout_StillReportsTheStderrTheChildManagedToEmit()
    {
        // The post-kill drain grace exists so a timed-out run stays diagnosable.
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript(
            """
            echo "detail before the hang" >&2
            sleep 120
            """);

        var result = await RunAsync(script, timeout: TimeSpan.FromMilliseconds(500));

        Assert.False(result.IsSuccess);
        Assert.Contains("detail before the hang", result.Exception!.Message);
    }

    [Fact]
    public async Task Timeout_KillsTheWholeProcessTree()
    {
        // Kill(entireProcessTree: true): the shell's own background child must die too, otherwise
        // a timed-out Python pipeline would leave orphans behind.
        if (OperatingSystem.IsLinux() == false) return;

        var pidFile = Path.Combine(Path.GetTempPath(), $"pastypropellant-test-{Guid.NewGuid():N}.pid");

        using var script = new TempScript(
            $"""
            sleep 120 &
            echo $! > "{pidFile}"
            wait
            """);

        try
        {
            var result = await RunAsync(script, timeout: TimeSpan.FromMilliseconds(500));

            Assert.False(result.IsSuccess);

            var childPid = ReadPid(pidFile);

            Assert.True(childPid > 0, "the script never reported its background child's pid");
            Assert.True(
                WaitUntilGone(childPid, TimeSpan.FromSeconds(10)),
                $"grandchild process {childPid} survived the tree kill");
        }
        finally
        {
            var leftover = ReadPid(pidFile);

            if (leftover > 0) TryKill(leftover);

            try
            {
                File.Delete(pidFile);
            }
            catch (IOException)
            {
                // Best effort.
            }
        }
    }

    [Fact]
    public async Task Cancellation_MidRun_TerminatesAndCompletesTheTask()
    {
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("sleep 120\n");
        using var cts = new CancellationTokenSource();

        var task = ProcessHandler.RunProcessAsync(Shell.Command, script.Path, cts.Token);

        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var result = await WithBudget(task);

        Assert.False(result.IsSuccess);
        Assert.IsType<OperationCanceledException>(result.Exception);
        Assert.Contains("cancelled", result.Exception!.Message);
    }

    [Fact]
    public async Task AlreadyCancelledToken_FailsWithoutStartingTheProcess()
    {
        if (Shell.IsAvailable == false) return;

        var marker = Path.Combine(Path.GetTempPath(), $"pastypropellant-test-{Guid.NewGuid():N}.marker");

        using var script = new TempScript($"touch \"{marker}\"\n");
        using var cts = new CancellationTokenSource();

        cts.Cancel();

        var result = await WithBudget(
            ProcessHandler.RunProcessAsync(Shell.Command, script.Path, cts.Token));

        Assert.False(result.IsSuccess);
        Assert.IsType<OperationCanceledException>(result.Exception);
        Assert.Contains("not started", result.Exception!.Message);
        Assert.False(File.Exists(marker), "the process was started despite the pre-cancelled token");
    }

    [Fact]
    public async Task InfiniteTimeout_BehavesLikeNoTimeout()
    {
        if (Shell.IsAvailable == false) return;

        using var script = new TempScript("exit 0\n");

        var result = await RunAsync(script, timeout: Timeout.InfiniteTimeSpan);

        Assert.True(result.IsSuccess);

        // Same sentinel spelled the other way round, which must not be mistaken for a negative
        // timeout and rejected.
        Assert.Equal(Timeout.InfiniteTimeSpan, TimeSpan.FromMilliseconds(-1));

        var viaNegativeOne = await RunAsync(script, timeout: TimeSpan.FromMilliseconds(-1));

        Assert.True(viaNegativeOne.IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    // NB: -1 ms is deliberately absent — TimeSpan.FromMilliseconds(-1) *is*
    // Timeout.InfiniteTimeSpan, so it is a legal "no timeout" value, not a rejected one.
    [InlineData(-5)]
    [InlineData(-1000)]
    public void NonPositiveTimeout_IsRejectedUpFront(int milliseconds)
    {
        // Thrown synchronously, before the process is started — so this is deliberately not an
        // async assertion: the returned Task must never be created at all.
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = ProcessHandler.RunProcessAsync(
                Shell.Command, "-c true", CancellationToken.None, TimeSpan.FromMilliseconds(milliseconds));
        });
    }

    [Fact]
    public async Task UnstartableCommand_FailsTheResultRatherThanThrowingOrHanging()
    {
        var result = await WithBudget(ProcessHandler.RunProcessAsync(
            $"/definitely/not/a/real/binary-{Guid.NewGuid():N}", string.Empty));

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Exception);
    }

    private static Task<OperationResult> RunAsync(TempScript script, TimeSpan? timeout = null) =>
        WithBudget(ProcessHandler.RunProcessAsync(
            Shell.Command, script.Path, CancellationToken.None, timeout));

    /// <summary>
    /// Fails fast instead of hanging the whole test run if the handler ever regresses to leaving
    /// its task unresolved — the exact failure mode COR-5 fixed.
    /// </summary>
    private static async Task<OperationResult> WithBudget(Task<OperationResult> task)
    {
        var completed = await Task.WhenAny(task, Task.Delay(CompletionBudget));

        Assert.True(
            ReferenceEquals(completed, task),
            $"RunProcessAsync did not complete within {CompletionBudget}");

        return await task;
    }

    /// <summary>Pulls the stderr block out of the failure message, in emission order.</summary>
    private static List<string> ExtractStderrLines(string message)
    {
        var lines = message.Split('\n');
        var header = Array.FindIndex(lines, l => l.StartsWith("Stderr", StringComparison.Ordinal));

        return header < 0
            ? []
            : lines.Skip(header + 1).ToList();
    }

    private static int ReadPid(string pidFile)
    {
        try
        {
            return File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile).Trim(), out var pid)
                ? pid
                : 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static bool WaitUntilGone(int pid, TimeSpan budget)
    {
        var deadline = DateTime.UtcNow + budget;

        while (DateTime.UtcNow < deadline)
        {
            // A killed-and-reaped process loses its /proc entry; a zombie keeps one but reports
            // State: Z, which still means it is not running.
            if (IsRunning(pid) == false) return true;

            Thread.Sleep(50);
        }

        return IsRunning(pid) == false;
    }

    private static bool IsRunning(int pid)
    {
        var status = $"/proc/{pid}/stat";

        if (File.Exists(status) == false) return false;

        try
        {
            var fields = File.ReadAllText(status);
            var close = fields.LastIndexOf(')');

            // "pid (comm) state …" — Z is a reaped-pending zombie, X/x an exiting corpse.
            return close >= 0 &&
                   fields.Length > close + 2 &&
                   fields[close + 2] is not ('Z' or 'X' or 'x');
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryKill(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);

            process.Kill(entireProcessTree: true);
        }
        catch (Exception)
        {
            // The point of the cleanup is to not leave orphans; failing to find one is fine.
        }
    }
}
