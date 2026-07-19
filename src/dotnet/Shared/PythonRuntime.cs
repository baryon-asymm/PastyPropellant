using System.Runtime.InteropServices;
using System.Text;
using PastyPropellant.Core.Utils;
using PastyPropellant.ProcessHandling.ProcessHandlers;

namespace PastyPropellant.Interop;

/// <summary>
/// Single source of truth for shelling out to the repository's Python helpers.
///
/// <para>This file is intentionally NOT its own assembly: it is compile-linked
/// (<c>&lt;Compile Include="…/Shared/PythonRuntime.cs" Link="…"/&gt;</c>) into every project
/// that needs it, so the four <c>Tools/*</c> libraries stay mutually independent and
/// nobody gains a project reference just to launch a script. The type is
/// <see langword="internal"/> precisely so the per-assembly copies cannot collide in
/// consumers (such as the console host) that reference several of those libraries at once.</para>
///
/// <para>It replaces three previously duplicated behaviours:</para>
/// <list type="bullet">
///   <item>hardcoded 5-level <c>../../../../../src/python/…</c> relative paths, which were
///     only correct for one particular working directory (see <see cref="RepositoryRoot"/>);</item>
///   <item>a bare <c>"python3"</c> literal per wrapper (see <see cref="InterpreterPath"/>);</item>
///   <item>hand-interpolated single-string command lines, which broke on any path
///     containing a space (see <see cref="BuildArguments"/>).</item>
/// </list>
/// </summary>
internal static class PythonRuntime
{
    /// <summary>Overrides the interpreter that <see cref="InterpreterPath"/> resolves to.</summary>
    public const string InterpreterEnvironmentVariable = "PASTYPROPELLANT_PYTHON";

    /// <summary>Overrides the directory that <see cref="RepositoryRoot"/> resolves to.</summary>
    public const string RepositoryRootEnvironmentVariable = "PASTYPROPELLANT_ROOT";

    /// <summary>Directory (relative to the repository root) holding the first-party Python sources.</summary>
    public const string PythonSourceDirectory = "src/python";

    // Files that exist at the repository root and nowhere below it.
    private static readonly string[] RootMarkers = ["PastyPropellant.sln", ".git"];

    private static readonly Lazy<string> LazyRepositoryRoot = new(FindRepositoryRoot);
    private static readonly Lazy<string> LazyInterpreterPath = new(FindInterpreter);

    /// <summary>
    /// Absolute path of the repository root, discovered by walking up from the directory the
    /// assembly was loaded from (and, failing that, from the current working directory) until a
    /// directory containing <c>PastyPropellant.sln</c> or <c>.git</c> is found.
    ///
    /// <para>This is deliberately anchored on <see cref="AppContext.BaseDirectory"/> rather than on
    /// the process working directory: the campaign runs the host out of
    /// <c>artifacts/bin/PastyPropellant.ConsoleApp/&lt;Configuration&gt;/net10.0/</c>, which is five
    /// levels below the root, but a developer may launch it from anywhere. Walking up finds the
    /// root in both cases without depending on how deep the output directory happens to be.</para>
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">
    /// No ancestor of either probe directory contains a root marker.
    /// </exception>
    public static string RepositoryRoot => LazyRepositoryRoot.Value;

    /// <summary>
    /// The Python interpreter to launch. Resolution order:
    /// <list type="number">
    ///   <item>the <c>PASTYPROPELLANT_PYTHON</c> environment variable, if set to a non-blank value
    ///     (an absolute path, or a bare name to be resolved off <c>PATH</c> by the OS);</item>
    ///   <item>the first platform-appropriate candidate (<c>python3</c>, <c>python</c>; plus the
    ///     <c>py</c> launcher on Windows) that is actually present on <c>PATH</c>;</item>
    ///   <item>the platform default, so that a missing interpreter surfaces as a normal
    ///     process-start failure with a readable message rather than a silent no-op.</item>
    /// </list>
    /// </summary>
    public static string InterpreterPath => LazyInterpreterPath.Value;

    /// <summary>
    /// Resolves a path relative to the repository root. Segments are combined, so
    /// <c>ResolveRepositoryPath("externals", "src/python/Foo/src/main.py")</c> works.
    /// An absolute <paramref name="segments"/> entry is returned unchanged, which lets callers
    /// pass either a repo-relative or a fully qualified path.
    /// </summary>
    public static string ResolveRepositoryPath(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Length == 0)
            throw new ArgumentException("At least one path segment is required.", nameof(segments));

        var combined = segments.Length == 1 ? segments[0] : Path.Combine(segments);
        if (Path.IsPathRooted(combined))
            return Path.GetFullPath(combined);

        return Path.GetFullPath(Path.Combine(RepositoryRoot, combined));
    }

    /// <summary>
    /// Resolves a script path under <c>&lt;repo&gt;/src/python/</c>, e.g.
    /// <c>ScriptPath("PropellantsPlotRendering/src/main.py")</c>.
    /// </summary>
    public static string ScriptPath(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Length == 0)
            throw new ArgumentException("At least one path segment is required.", nameof(segments));

        var combined = segments.Length == 1 ? segments[0] : Path.Combine(segments);
        if (Path.IsPathRooted(combined))
            return Path.GetFullPath(combined);

        return ResolveRepositoryPath(PythonSourceDirectory, combined);
    }

    /// <summary>
    /// Runs <paramref name="scriptPath"/> under <see cref="InterpreterPath"/> with
    /// <paramref name="arguments"/> passed as discrete, individually quoted tokens.
    /// </summary>
    /// <remarks>
    /// <see cref="ProcessHandler.RunProcessAsync"/> takes a single command-line string, so the
    /// quoting is applied here by <see cref="BuildArguments"/> instead of via
    /// <c>ProcessStartInfo.ArgumentList</c>. Callers therefore pass raw, unescaped values —
    /// a path with a space in it is handled correctly, and no caller should pre-quote.
    /// </remarks>
    public static Task<OperationResult> RunScriptAsync(string scriptPath, params string[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scriptPath);
        ArgumentNullException.ThrowIfNull(arguments);

        var tokens = new string[arguments.Length + 1];
        tokens[0] = scriptPath;
        arguments.CopyTo(tokens, 1);

        return ProcessHandler.RunProcessAsync(InterpreterPath, BuildArguments(tokens));
    }

    /// <summary>
    /// Joins <paramref name="arguments"/> into one command-line string using the escaping rules the
    /// .NET process launcher applies when re-splitting <c>ProcessStartInfo.Arguments</c> (the same
    /// rules on Unix and Windows), so each element survives as exactly one argv entry regardless of
    /// spaces, quotes, or trailing backslashes.
    /// </summary>
    public static string BuildArguments(IEnumerable<string> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var builder = new StringBuilder();
        foreach (var argument in arguments)
        {
            if (builder.Length > 0)
                builder.Append(' ');

            AppendArgument(builder, argument ?? string.Empty);
        }

        return builder.ToString();
    }

    private static void AppendArgument(StringBuilder builder, string argument)
    {
        // An argument only needs quoting if it is empty or contains a delimiter.
        if (argument.Length > 0 && argument.AsSpan().IndexOfAny(' ', '\t', '"') < 0)
        {
            builder.Append(argument);
            return;
        }

        builder.Append('"');
        for (var i = 0; i < argument.Length; i++)
        {
            var backslashes = 0;
            while (i < argument.Length && argument[i] == '\\')
            {
                i++;
                backslashes++;
            }

            if (i == argument.Length)
            {
                // Backslashes immediately before the closing quote must be doubled,
                // otherwise they would escape that quote.
                builder.Append('\\', backslashes * 2);
                break;
            }

            if (argument[i] == '"')
            {
                builder.Append('\\', backslashes * 2 + 1).Append('"');
            }
            else
            {
                builder.Append('\\', backslashes).Append(argument[i]);
            }
        }

        builder.Append('"');
    }

    private static string FindRepositoryRoot()
    {
        var configured = Environment.GetEnvironmentVariable(RepositoryRootEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured) == false)
        {
            var root = Path.GetFullPath(configured.Trim());
            if (Directory.Exists(root))
                return root;

            throw new DirectoryNotFoundException(
                $"{RepositoryRootEnvironmentVariable} is set to '{configured}', which is not an existing directory.");
        }

        // AppContext.BaseDirectory first: it is stable no matter where the process was launched
        // from. The working directory is only a fallback (e.g. single-file/odd hosting layouts).
        var probes = new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() };
        foreach (var probe in probes)
        {
            var found = WalkUpToMarker(probe);
            if (found != null)
                return found;
        }

        throw new DirectoryNotFoundException(
            $"Unable to locate the PastyPropellant repository root. Searched upwards from " +
            $"'{probes[0]}' and '{probes[1]}' for a directory containing one of: " +
            $"{string.Join(", ", RootMarkers)}. Set the {RepositoryRootEnvironmentVariable} " +
            $"environment variable to override this discovery.");
    }

    private static string? WalkUpToMarker(string startDirectory)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
            return null;

        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
        while (directory != null)
        {
            foreach (var marker in RootMarkers)
            {
                var candidate = Path.Combine(directory.FullName, marker);
                if (File.Exists(candidate) || Directory.Exists(candidate))
                    return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string FindInterpreter()
    {
        var configured = Environment.GetEnvironmentVariable(InterpreterEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured) == false)
            return configured.Trim();

        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        string[] candidates = isWindows
            ? ["python.exe", "python3.exe", "py.exe"]
            : ["python3", "python"];

        foreach (var candidate in candidates)
        {
            var resolved = ResolveOnPath(candidate);
            if (resolved != null)
                return resolved;
        }

        // Nothing on PATH: hand the platform default to the process launcher so the failure is
        // reported as a normal "cannot start process" error instead of being swallowed here.
        return isWindows ? "python.exe" : "python3";
    }

    private static string? ResolveOnPath(string executable)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
            return null;

        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(directory))
                continue;

            string candidate;
            try
            {
                candidate = Path.Combine(directory.Trim(), executable);
            }
            catch (ArgumentException)
            {
                // A malformed PATH entry must not take down interpreter discovery.
                continue;
            }

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
