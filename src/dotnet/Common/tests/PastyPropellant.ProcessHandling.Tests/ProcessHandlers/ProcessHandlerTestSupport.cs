namespace PastyPropellant.ProcessHandling.Tests.ProcessHandlers;

using System.Runtime.InteropServices;
using PastyPropellant.Core.Utils;

/// <summary>
/// A throwaway POSIX shell script on disk, run as <c>/bin/sh &lt;path&gt;</c>.
///
/// Passing the script as a file rather than via <c>sh -c "…"</c> is deliberate:
/// <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/> is a single string that .NET
/// re-splits using Windows quoting rules even on Unix, so an inline script would have to be
/// escaped twice. A file keeps the shell source readable and quoting-free.
/// </summary>
public sealed class TempScript : IDisposable
{
    public TempScript(string body)
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"pastypropellant-test-{Guid.NewGuid():N}.sh");

        File.WriteAllText(Path, body);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            // A leftover temp file must never fail a test.
        }
    }
}

public static class Shell
{
    public const string Command = "/bin/sh";

    /// <summary>
    /// These tests drive a real child process through a POSIX shell. On a non-Unix host the
    /// scripts are meaningless, so the tests self-skip rather than fail spuriously.
    /// </summary>
    public static bool IsAvailable =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}

/// <summary>
/// Tracks handlers registered on the static, process-wide <see cref="EventBus{TEvent}"/> and
/// removes them on <see cref="Dispose"/>, so a failing assertion cannot leak a subscription into
/// whichever test runs next. <c>ProcessInfoLogEvent</c>/<c>ProcessErrorLogEvent</c> are real
/// product types shared by every test in this assembly, so this cleanup is load-bearing.
/// </summary>
public sealed class EventBusScope<TEvent> : IDisposable
    where TEvent : struct
{
    private readonly List<Action<TEvent>> _handlers = [];

    public void Subscribe(Action<TEvent> handler)
    {
        EventBus<TEvent>.Subscribe(handler);

        lock (_handlers) _handlers.Add(handler);
    }

    public void Dispose()
    {
        lock (_handlers)
        {
            foreach (var handler in _handlers) EventBus<TEvent>.Unsubscribe(handler);

            _handlers.Clear();
        }
    }
}
