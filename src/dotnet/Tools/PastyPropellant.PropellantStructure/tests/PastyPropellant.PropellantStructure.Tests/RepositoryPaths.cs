namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// Locates files by walking up from the test assembly to the repository root.
/// </summary>
/// <remarks>
/// Not a convenience. Hardcoded <c>../../../../../</c> paths in this repository
/// were invalidated by the centralised <c>artifacts/</c> output layout, and the
/// tests that used them failed before launching anything — while appearing to
/// pass in about 110 ms. Anchoring on a root marker cannot fail that way.
/// </remarks>
internal static class RepositoryPaths
{
    private static readonly string[] RootMarkers = ["PastyPropellant.sln", ".git"];

    private static readonly Lazy<string> LazyRoot = new(FindRoot);

    internal static string Root => LazyRoot.Value;

    internal static string Resolve(params string[] segments) =>
        Path.GetFullPath(Path.Combine(Root, Path.Combine(segments)));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (RootMarkers.Any(marker => Path.Exists(Path.Combine(directory.FullName, marker))))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"No repository root above '{AppContext.BaseDirectory}': " +
            $"looked for {string.Join(" or ", RootMarkers)}.");
    }
}
