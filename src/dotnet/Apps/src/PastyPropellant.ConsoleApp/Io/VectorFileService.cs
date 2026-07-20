using System.Globalization;
using ParametricCombustionModel.Optimization.Utils;

namespace PastyPropellant.ConsoleApp.Io;

/// <summary>
/// Reads and writes the plain-text parameter-vector file (<c>best_vector.txt</c>) that lets an
/// optimisation run be replayed exactly through <c>--forward-eval</c>.
///
/// <para>The PDF report is far too lossy to recover a vector from, so the converged genes are also
/// persisted at round-trip ("R") precision, one value per line. The file carries two self-describing
/// header comments — <c>&#35; count = N</c> and <c>&#35; checksum = &lt;hex&gt;</c> — which
/// <see cref="Read"/> verifies. That guard is the whole point of the format: these files get pasted
/// between terminals and issue trackers, and a truncated or mangled copy would otherwise replay
/// silently as a different point in parameter space.</para>
///
/// <para>The gene lines themselves are produced by <see cref="VectorFormat"/>, the same formatter the
/// PDF report uses, so the block in the report and the file on disk are textually identical.</para>
/// </summary>
public static class VectorFileService
{
    /// <summary>
    /// Reads a list of doubles (invariant culture) from a file: whitespace/comma/semicolon separated,
    /// with optional '#' line comments so the vector file can be self-documenting.
    /// </summary>
    /// <exception cref="FileNotFoundException">No file exists at <paramref name="path"/>.</exception>
    /// <exception cref="InvalidDataException">
    /// The file declares a <c>count</c> or <c>checksum</c> that does not match its contents.
    /// </exception>
    public static double[] Read(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Vector file not found: {path}");

        var values = new List<double>();
        string? declaredChecksum = null;
        int? declaredCount = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var commentStart = rawLine.IndexOf('#');
            if (commentStart >= 0)
            {
                // Self-describing guards emitted by the report / Write: "# count = N", "# checksum = <hex>".
                var directive = rawLine[(commentStart + 1)..].Trim();
                var eq = directive.IndexOf('=');
                if (eq > 0)
                {
                    var key = directive[..eq].Trim();
                    var val = directive[(eq + 1)..].Trim();
                    if (key.Equals("checksum", StringComparison.OrdinalIgnoreCase))
                        declaredChecksum = val;
                    else if (key.Equals("count", StringComparison.OrdinalIgnoreCase)
                             && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                        declaredCount = n;
                }
            }

            var line = commentStart >= 0 ? rawLine[..commentStart] : rawLine;

            foreach (var token in line.Split([' ', '\t', ',', ';'], StringSplitOptions.RemoveEmptyEntries))
                values.Add(double.Parse(token, CultureInfo.InvariantCulture));
        }

        var genes = values.ToArray();

        if (declaredCount is { } expectedCount && expectedCount != genes.Length)
            throw new InvalidDataException(
                $"Vector file '{path}' declares # count = {expectedCount} but contains {genes.Length} values (copy/paste truncated?).");

        if (declaredChecksum is { } expectedChecksum)
        {
            var actualChecksum = VectorFormat.Checksum(genes);
            if (!string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    $"Vector file '{path}' checksum mismatch (declared {expectedChecksum}, computed {actualChecksum}) — a value was likely mangled in transit.");
        }

        return genes;
    }

    /// <summary>
    /// Persists a vector at round-trip ("R") precision, one value per line, so an optimization run can
    /// be replayed exactly via --forward-eval (a PDF report is too lossy to recover the vector from).
    /// </summary>
    public static void Write(string path, double[] genes)
    {
        // Self-documenting guards (ignored by Read's parser, verified by it) so a truncated or
        // mangled copy is caught on read; gene lines use the shared VectorFormat so they match the PDF block.
        var lines = new List<string>
        {
            $"# count = {genes.Length}",
            $"# checksum = {VectorFormat.Checksum(genes)}"
        };
        lines.AddRange(VectorFormat.FormatLines(genes));
        File.WriteAllLines(path, lines);
    }
}
