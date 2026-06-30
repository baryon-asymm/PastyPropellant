using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ParametricCombustionModel.Optimization.Utils;

/// <summary>
/// Single source of truth for serialising an optimisation parameter vector to text, shared by the
/// console host's <c>best_vector.txt</c> writer and the PDF "reproduction vector" block so the two
/// renderings cannot drift apart. Genes are written at round-trip ("R", invariant culture) precision:
/// a PDF-rounded vector does NOT reproduce its objective (the radiative-temperature closure amplifies
/// 4-significant-figure rounding), so exact formatting here is load-bearing for replay via --forward-eval.
/// </summary>
public static class VectorFormat
{
    /// <summary>Round-trip ("R") invariant-culture rendering of a single gene.</summary>
    public static string FormatGene(double gene) => gene.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>One gene per line, round-trip precision (robust against PDF text-extraction reflow).</summary>
    public static IEnumerable<string> FormatLines(double[] genes) => genes.Select(FormatGene);

    /// <summary>Canonical newline-joined rendering used as the checksum pre-image.</summary>
    public static string Canonical(double[] genes) => string.Join('\n', FormatLines(genes));

    /// <summary>
    /// Lowercase-hex SHA-256 over the canonical rendering. Stable across write → parse → re-render
    /// (double → "R" → double → "R" is idempotent), so a reader can recompute it from parsed values
    /// and detect a value mangled in copy/paste even when the count is preserved.
    /// </summary>
    public static string Checksum(double[] genes)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Canonical(genes)));
        return Convert.ToHexStringLower(bytes);
    }
}
