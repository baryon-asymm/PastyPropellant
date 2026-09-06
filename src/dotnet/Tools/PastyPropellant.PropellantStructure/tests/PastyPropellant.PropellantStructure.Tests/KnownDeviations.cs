using System.Diagnostics.CodeAnalysis;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>Which of the four L2 readings a deviation belongs to.</summary>
internal enum L2Group
{
    Draws,
    Conditions,
    Scalars,
    Arrays,
}

/// <summary>What the recorded number measures.</summary>
internal enum DeviationKind
{
    /// <summary>Signed <c>port - archive</c> on an integer counter. Matched exactly.</summary>
    CounterDifference,

    /// <summary>The error the test itself computes. Matched up to <see cref="KnownDeviations.Slack"/>.</summary>
    RelativeError,

    /// <summary>The port's cell count where the archive's differs. Matched exactly.</summary>
    CellCount,
}

/// <summary>One recorded deviation of the port from the archive.</summary>
/// <param name="Quantity">counter, scalar or array the deviation is on</param>
/// <param name="Kind">what <paramref name="Recorded"/> measures</param>
/// <param name="Recorded">the number measured on 2026-09-06</param>
/// <param name="AbsoluteCeiling">
/// The absolute difference measured alongside <paramref name="Recorded"/>, where one is
/// meaningful. ⚠ It exists because a recorded <em>relative</em> error is a weak statement
/// wherever the archive's value is near zero: an empty histogram bin gives a relative
/// error of exactly 1 whatever the content is, so a bare relative entry there would
/// accept any content at all. The absolute number is what actually bounds those cells -
/// <c>fqmkm1</c> on the P777 reports is 3.04e-7 of a normalised distribution, one pocket
/// out of hundreds of millions - and both bounds must hold.
/// </param>
internal sealed record KnownDeviation(
    string Quantity,
    DeviationKind Kind,
    double Recorded,
    double? AbsoluteCeiling = null);

/// <summary>
/// The deviations of the port from the archive that are known, measured and explained -
/// every one of them, listed run by run and quantity by quantity.
/// </summary>
/// <remarks>
/// <para>
/// This is <b>not</b> a suppression list. Extending the replays from seven runs to
/// twenty added six recipes on which the port and the 1990s Compaq binary that wrote the
/// archive do not agree to the last digit, and the question that had to be answered
/// before those runs could be acceptance criteria was whether the port is wrong. It was
/// answered on 2026-09-06 by triangulation: a fresh <c>gfortran</c> build of the
/// unmodified original was run on all twenty recipes and compared against the same
/// archive. Over a hundred counter comparisons the port is closer to the archive than
/// modern Fortran on 38, level on 59, and further on 3 - and six of the twenty runs
/// <c>gfortran</c> reproduces bit for bit, which is what says the build is trustworthy.
/// So the residue below is compiler arithmetic of the machine the archive was written
/// on, not a defect of the port.
/// </para>
/// <para>
/// ⚠ Nothing here is a tolerance. Each entry pins the deviation that was <b>measured</b>
/// on that date; a deviation larger than the recorded one fails, a deviation on a
/// quantity not listed fails, and a listed deviation that <b>stops</b> reproducing fails
/// too - because that means the port changed, and the change has to be re-triangulated
/// rather than absorbed. That last rule is what keeps this from decaying into a way of
/// making red go away.
/// </para>
/// <para>The six recipes, and what triangulation said about each:</para>
/// <list type="table">
    /// <item><term><c>hp1050</c></term><description>Один счётчик, три пробы из 24.7 млн. Свежая сборка gfortran расходится с
    /// архивом на 5 — порт ближе.</description></item>
    /// <item><term><c>hp95</c></term><description>Один счётчик, 15 проб из 27.1 млн; gfortran — 21.</description></item>
    /// <item><term><c>r_p35050nn</c></term><description>Миллион базовых частиц. Счётчики расходятся на 11-12 из сотен миллионов;
    /// gfortran — на 78.</description></item>
    /// <item><term><c>p777out</c></term><description>Рецептура P777, четыре архивных отчёта одного прогона (p777out, p777out1,
    /// p777out2, res_01) — поэтому четыре одинаковых блока. eta = 0.5, единственная
    /// такая в архиве. nkarm 102 против 2236 у gfortran, nfy 108 против 8200.</description></item>
    /// <item><term><c>p777out1</c></term><description>То же, что p777out (тот же прогон, другой архивный отчёт).
    /// Отличается одной лишней записью: dagg43_cor2.</description></item>
    /// <item><term><c>p777out2</c></term><description>То же, что p777out.</description></item>
    /// <item><term><c>res_01</c></term><description>То же, что p777out.</description></item>
    /// <item><term><c>rcspx01</c></term><description>Порт и свежая сборка gfortran дают здесь ОДИНАКОВЫЕ числа и одинаково
    /// расходятся с архивом ⇒ аномален архивный отчёт, а не порт. Узкий пятифракционный
    /// разрез.</description></item>
    /// <item><term><c>rnano2</c></term><description>То же, что rcspx01: порт и gfortran совпадают между собой и оба расходятся
    /// с архивом.</description></item>
/// </list>
/// <para>
/// ⚠ The magnitudes are worth keeping in proportion. The worst deviation on a
/// <em>published</em> quantity is <c>zkarm</c> on <c>rcspx01</c>, 6.7e-4 - 0.067 %.
/// <c>Dkarm43</c> and <c>Dok43</c> are exact on all six. The pocket mass fraction is
/// quoted downstream to two decimals, and the measurement uncertainty it is compared
/// against is 2.9-16 %. The large-looking entries below are all on the epsilon
/// diagnostics, which are differences of nearly equal singles and have the arithmetic's
/// own cancellation under them.
/// </para>
/// <para>
/// ⚠ On an array the bound is on its <b>worst</b> cell, because that is the one cell the
/// L2 array test reports. A cell with a smaller relative error but a larger absolute one
/// is therefore not bounded here. Widening that would mean changing what the array test
/// reports, not what this list records.
/// </para>
/// </remarks>
internal static class KnownDeviations
{
    /// <summary>
    /// How much a measured deviation may exceed the recorded one and still be the same
    /// deviation. Five percent: the port is deterministic, so this covers a last-bit
    /// difference of the JIT and nothing more. A regression moves these numbers by
    /// orders of magnitude, not by percent.
    /// </summary>
    public const double Slack = 1.05;

    private static readonly Dictionary<(string Run, L2Group Group), KnownDeviation[]> Table = new()
    {
        [("hp1050", L2Group.Draws)] =
        [
            new("nkarm", DeviationKind.CounterDifference, -3),
        ],
        [("hp1050", L2Group.Scalars)] =
        [
            new("nkarm", DeviationKind.RelativeError, 3.0),
        ],

        [("hp95", L2Group.Draws)] =
        [
            new("nkarm", DeviationKind.CounterDifference, -15),
        ],
        [("hp95", L2Group.Scalars)] =
        [
            new("nkarm", DeviationKind.RelativeError, 15.0),
        ],

        [("r_p35050nn", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 11),
            new("nfw", DeviationKind.CounterDifference, -11),
            new("nkarm", DeviationKind.CounterDifference, -12),
        ],
        [("r_p35050nn", L2Group.Scalars)] =
        [
            new("epsx5", DeviationKind.RelativeError, 0.0798),
            new("epsx6", DeviationKind.RelativeError, 0.00835),
            new("nfq", DeviationKind.RelativeError, 11.0),
            new("nfw", DeviationKind.RelativeError, 11.0),
            new("nkarm", DeviationKind.RelativeError, 12.0),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000285),
        ],
        [("r_p35050nn", L2Group.Arrays)] =
        [
            new("fmkarm_cor2", DeviationKind.RelativeError, 0.045, 1.484738928964364e-06),
            new("fqmkm2", DeviationKind.RelativeError, 0.0223, 7.516683981055402e-08),
        ],

        [("p777out", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 5),
            new("nfw", DeviationKind.CounterDifference, 105),
            new("nfx", DeviationKind.CounterDifference, 3),
            new("nfy", DeviationKind.CounterDifference, 108),
            new("nkarm", DeviationKind.CounterDifference, 102),
        ],
        [("p777out", L2Group.Conditions)] =
        [
            new("fewer_than_two_bridges", DeviationKind.RelativeError, 1.51e-05),
            new("pocket_bridge_ratio_above_max", DeviationKind.RelativeError, 2.09e-05),
        ],
        [("p777out", L2Group.Scalars)] =
        [
            new("bridges_per_particle", DeviationKind.RelativeError, 1.87e-06),
            new("dkarm43_cor2", DeviationKind.RelativeError, 0.00898),
            new("dkarm43_sd_cor2", DeviationKind.RelativeError, 0.0148),
            new("dok43_analytic", DeviationKind.RelativeError, 0.005),
            new("epsx1", DeviationKind.RelativeError, 0.00132),
            new("epsx2", DeviationKind.RelativeError, 0.00132),
            new("epsx5", DeviationKind.RelativeError, 0.317),
            new("epsx6", DeviationKind.RelativeError, 0.0441),
            new("gap_coefficient", DeviationKind.RelativeError, 1.2e-06),
            new("jammed_fraction", DeviationKind.RelativeError, 4.16e-06),
            new("nfq", DeviationKind.RelativeError, 5.0),
            new("nfw", DeviationKind.RelativeError, 105.0),
            new("nfx", DeviationKind.RelativeError, 3.0),
            new("nfy", DeviationKind.RelativeError, 108.0),
            new("nkarm", DeviationKind.RelativeError, 102.0),
            new("pocket_bridge_ratio", DeviationKind.RelativeError, 3.74e-06),
            new("zkarm", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor1", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000143),
        ],
        [("p777out", L2Group.Arrays)] =
        [
            new("fmdok", DeviationKind.RelativeError, 1.0, 4.608633119573824e-08),
            new("fmkarm_cor2", DeviationKind.RelativeError, 0.0384, 3.8783724399982034e-07),
            new("fqmkm1", DeviationKind.CellCount, 272),
            new("fqmkm1", DeviationKind.RelativeError, 1.0, 3.04e-07),
            new("fqmkm2", DeviationKind.RelativeError, 0.0343, 8.544461994664731e-07),
        ],

        [("p777out1", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 5),
            new("nfw", DeviationKind.CounterDifference, 105),
            new("nfx", DeviationKind.CounterDifference, 3),
            new("nfy", DeviationKind.CounterDifference, 108),
            new("nkarm", DeviationKind.CounterDifference, 102),
        ],
        [("p777out1", L2Group.Conditions)] =
        [
            new("fewer_than_two_bridges", DeviationKind.RelativeError, 1.51e-05),
            new("pocket_bridge_ratio_above_max", DeviationKind.RelativeError, 2.09e-05),
        ],
        [("p777out1", L2Group.Scalars)] =
        [
            new("bridges_per_particle", DeviationKind.RelativeError, 1.87e-06),
            new("dagg43_cor2", DeviationKind.RelativeError, 0.00629),
            new("dkarm43_cor2", DeviationKind.RelativeError, 0.00898),
            new("dkarm43_sd_cor2", DeviationKind.RelativeError, 0.0148),
            new("dok43_analytic", DeviationKind.RelativeError, 0.005),
            new("epsx1", DeviationKind.RelativeError, 0.00132),
            new("epsx2", DeviationKind.RelativeError, 0.00132),
            new("epsx5", DeviationKind.RelativeError, 0.317),
            new("epsx6", DeviationKind.RelativeError, 0.0441),
            new("gap_coefficient", DeviationKind.RelativeError, 1.2e-06),
            new("jammed_fraction", DeviationKind.RelativeError, 4.16e-06),
            new("nfq", DeviationKind.RelativeError, 5.0),
            new("nfw", DeviationKind.RelativeError, 105.0),
            new("nfx", DeviationKind.RelativeError, 3.0),
            new("nfy", DeviationKind.RelativeError, 108.0),
            new("nkarm", DeviationKind.RelativeError, 102.0),
            new("pocket_bridge_ratio", DeviationKind.RelativeError, 3.74e-06),
            new("zkarm", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor1", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000143),
        ],
        [("p777out1", L2Group.Arrays)] =
        [
            new("fmdok", DeviationKind.RelativeError, 1.0, 4.608633119573824e-08),
            new("fmkarm_cor2", DeviationKind.RelativeError, 0.0384, 3.8783724399982034e-07),
            new("fqmkm1", DeviationKind.CellCount, 272),
            new("fqmkm1", DeviationKind.RelativeError, 1.0, 3.04e-07),
            new("fqmkm2", DeviationKind.RelativeError, 0.0343, 8.544461994664731e-07),
        ],

        [("p777out2", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 5),
            new("nfw", DeviationKind.CounterDifference, 105),
            new("nfx", DeviationKind.CounterDifference, 3),
            new("nfy", DeviationKind.CounterDifference, 108),
            new("nkarm", DeviationKind.CounterDifference, 102),
        ],
        [("p777out2", L2Group.Conditions)] =
        [
            new("fewer_than_two_bridges", DeviationKind.RelativeError, 1.51e-05),
            new("pocket_bridge_ratio_above_max", DeviationKind.RelativeError, 2.09e-05),
        ],
        [("p777out2", L2Group.Scalars)] =
        [
            new("bridges_per_particle", DeviationKind.RelativeError, 1.87e-06),
            new("dkarm43_cor2", DeviationKind.RelativeError, 0.00898),
            new("dkarm43_sd_cor2", DeviationKind.RelativeError, 0.0148),
            new("dok43_analytic", DeviationKind.RelativeError, 0.005),
            new("epsx1", DeviationKind.RelativeError, 0.00132),
            new("epsx2", DeviationKind.RelativeError, 0.00132),
            new("epsx5", DeviationKind.RelativeError, 0.317),
            new("epsx6", DeviationKind.RelativeError, 0.0441),
            new("gap_coefficient", DeviationKind.RelativeError, 1.2e-06),
            new("jammed_fraction", DeviationKind.RelativeError, 4.16e-06),
            new("nfq", DeviationKind.RelativeError, 5.0),
            new("nfw", DeviationKind.RelativeError, 105.0),
            new("nfx", DeviationKind.RelativeError, 3.0),
            new("nfy", DeviationKind.RelativeError, 108.0),
            new("nkarm", DeviationKind.RelativeError, 102.0),
            new("pocket_bridge_ratio", DeviationKind.RelativeError, 3.74e-06),
            new("zkarm", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor1", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000143),
        ],
        [("p777out2", L2Group.Arrays)] =
        [
            new("fmdok", DeviationKind.RelativeError, 1.0, 4.608633119573824e-08),
            new("fmkarm_cor2", DeviationKind.RelativeError, 0.0384, 3.8783724399982034e-07),
            new("fqmkm1", DeviationKind.CellCount, 272),
            new("fqmkm1", DeviationKind.RelativeError, 1.0, 3.04e-07),
            new("fqmkm2", DeviationKind.RelativeError, 0.0343, 8.544461994664731e-07),
        ],

        [("res_01", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 5),
            new("nfw", DeviationKind.CounterDifference, 105),
            new("nfx", DeviationKind.CounterDifference, 3),
            new("nfy", DeviationKind.CounterDifference, 108),
            new("nkarm", DeviationKind.CounterDifference, 102),
        ],
        [("res_01", L2Group.Conditions)] =
        [
            new("fewer_than_two_bridges", DeviationKind.RelativeError, 1.51e-05),
            new("pocket_bridge_ratio_above_max", DeviationKind.RelativeError, 2.09e-05),
        ],
        [("res_01", L2Group.Scalars)] =
        [
            new("bridges_per_particle", DeviationKind.RelativeError, 1.87e-06),
            new("dkarm43_cor2", DeviationKind.RelativeError, 0.00898),
            new("dkarm43_sd_cor2", DeviationKind.RelativeError, 0.0148),
            new("dok43_analytic", DeviationKind.RelativeError, 0.005),
            new("epsx1", DeviationKind.RelativeError, 0.00132),
            new("epsx2", DeviationKind.RelativeError, 0.00132),
            new("epsx5", DeviationKind.RelativeError, 0.317),
            new("epsx6", DeviationKind.RelativeError, 0.0441),
            new("gap_coefficient", DeviationKind.RelativeError, 1.2e-06),
            new("jammed_fraction", DeviationKind.RelativeError, 4.16e-06),
            new("nfq", DeviationKind.RelativeError, 5.0),
            new("nfw", DeviationKind.RelativeError, 105.0),
            new("nfx", DeviationKind.RelativeError, 3.0),
            new("nfy", DeviationKind.RelativeError, 108.0),
            new("nkarm", DeviationKind.RelativeError, 102.0),
            new("pocket_bridge_ratio", DeviationKind.RelativeError, 3.74e-06),
            new("zkarm", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor1", DeviationKind.RelativeError, 1.51e-05),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000143),
        ],
        [("res_01", L2Group.Arrays)] =
        [
            new("fmdok", DeviationKind.RelativeError, 1.0, 4.608633119573824e-08),
            new("fmkarm_cor2", DeviationKind.RelativeError, 0.0384, 3.8783724399982034e-07),
            new("fqmkm1", DeviationKind.CellCount, 272),
            new("fqmkm1", DeviationKind.RelativeError, 1.0, 3.04e-07),
            new("fqmkm2", DeviationKind.RelativeError, 0.0343, 8.544461994664731e-07),
        ],

        [("rcspx01", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 1385),
            new("nfw", DeviationKind.CounterDifference, 2789),
            new("nfx", DeviationKind.CounterDifference, 146),
            new("nfy", DeviationKind.CounterDifference, 2789),
            new("nkarm", DeviationKind.CounterDifference, 1445),
        ],
        [("rcspx01", L2Group.Conditions)] =
        [
            new("fewer_than_two_bridges", DeviationKind.RelativeError, 0.00383),
            new("no_pockets", DeviationKind.RelativeError, 6.46e-05),
            new("pocket_bridge_ratio_below_min", DeviationKind.RelativeError, 0.000105),
        ],
        [("rcspx01", L2Group.Scalars)] =
        [
            new("bridges_per_particle", DeviationKind.RelativeError, 4.15e-06),
            new("dagg43_v1", DeviationKind.RelativeError, 0.00533),
            new("eps_dok_base", DeviationKind.RelativeError, 0.000933),
            new("epsx1", DeviationKind.RelativeError, 0.0276),
            new("epsx2", DeviationKind.RelativeError, 0.0262),
            new("epsx3", DeviationKind.RelativeError, 0.00514),
            new("epsx4", DeviationKind.RelativeError, 0.00542),
            new("epsx5", DeviationKind.RelativeError, 0.162),
            new("epsx6", DeviationKind.RelativeError, 0.151),
            new("jammed_fraction", DeviationKind.RelativeError, 3.97e-05),
            new("nfq", DeviationKind.RelativeError, 1380.0),
            new("nfw", DeviationKind.RelativeError, 2790.0),
            new("nfx", DeviationKind.RelativeError, 146.0),
            new("nfy", DeviationKind.RelativeError, 2790.0),
            new("nkarm", DeviationKind.RelativeError, 1440.0),
            new("pocket_bridge_ratio", DeviationKind.RelativeError, 1.1e-05),
            new("zkarm", DeviationKind.RelativeError, 0.000671),
            new("zkarm_cor1", DeviationKind.RelativeError, 0.000671),
            new("zkarm_cor2", DeviationKind.RelativeError, 0.000671),
        ],
        [("rcspx01", L2Group.Arrays)] =
        [
            new("coef", DeviationKind.RelativeError, 0.0118, 3.6741391901741866e-08),
            new("epsdokfr", DeviationKind.RelativeError, 0.0447, 1.6507029294502015e-06),
            new("fqmkm2", DeviationKind.RelativeError, 0.285, 2.7192405205068634e-07),
        ],

        [("rnano2", L2Group.Draws)] =
        [
            new("nfq", DeviationKind.CounterDifference, 59),
            new("nfw", DeviationKind.CounterDifference, 114),
            new("nfx", DeviationKind.CounterDifference, 6),
            new("nfy", DeviationKind.CounterDifference, 114),
            new("nkarm", DeviationKind.CounterDifference, 71),
        ],
        [("rnano2", L2Group.Conditions)] =
        [
            new("no_pockets", DeviationKind.RelativeError, 4.31e-05),
            new("pocket_bridge_ratio_below_min", DeviationKind.RelativeError, 4.62e-06),
        ],
        [("rnano2", L2Group.Scalars)] =
        [
            new("eps_dok_base", DeviationKind.RelativeError, 0.0106),
            new("epsx1", DeviationKind.RelativeError, 0.00398),
            new("epsx2", DeviationKind.RelativeError, 0.00664),
            new("epsx3", DeviationKind.RelativeError, 0.000245),
            new("epsx4", DeviationKind.RelativeError, 0.000246),
            new("epsx5", DeviationKind.RelativeError, 0.0183),
            new("epsx6", DeviationKind.RelativeError, 0.00759),
            new("jammed_fraction", DeviationKind.RelativeError, 6.11e-06),
            new("nfq", DeviationKind.RelativeError, 59.0),
            new("nfw", DeviationKind.RelativeError, 114.0),
            new("nfx", DeviationKind.RelativeError, 6.0),
            new("nfy", DeviationKind.RelativeError, 114.0),
            new("nkarm", DeviationKind.RelativeError, 71.0),
            new("zkarm", DeviationKind.RelativeError, 1.17e-05),
            new("zkarm_cor1", DeviationKind.RelativeError, 1.17e-05),
            new("zkarm_cor2", DeviationKind.RelativeError, 1.07e-05),
        ],
        [("rnano2", L2Group.Arrays)] =
        [
            new("fqmkm2", DeviationKind.RelativeError, 0.00519, 8.307866263203395e-08),
        ],
    };

    /// <summary>The deviations recorded for one run in one L2 group; empty if none.</summary>
    public static IReadOnlyList<KnownDeviation> For(string run, L2Group group) =>
        Table.TryGetValue((run, group), out var entries) ? entries : [];

    /// <summary>Every run this list mentions, for the test that checks it has no dead rows.</summary>
    public static IEnumerable<string> Runs => Table.Keys.Select(key => key.Run).Distinct(StringComparer.Ordinal);
}

/// <summary>
/// Tracks, for one run and one L2 group, which recorded deviations were actually met.
/// </summary>
/// <remarks>
/// The ledger exists so the two halves of the rule cannot drift apart: a test that only
/// asked "is this mismatch excused?" would pass while a recorded deviation quietly
/// stopped happening, and that silence is exactly the signal that the port moved.
/// </remarks>
internal sealed class DeviationLedger(string run, L2Group group)
{
    private readonly IReadOnlyList<KnownDeviation> _recorded = KnownDeviations.For(run, group);
    private readonly HashSet<(string Quantity, DeviationKind Kind)> _met = [];

    /// <summary>
    /// Whether <paramref name="observed"/> on <paramref name="quantity"/> is the recorded
    /// deviation. Consulting an entry marks it met, whatever the answer - a recorded
    /// deviation that is now <em>smaller</em> is still the port having changed.
    /// </summary>
    /// <param name="absolute">
    /// The absolute difference behind <paramref name="observed"/>, where the caller has
    /// one. An entry carrying an <see cref="KnownDeviation.AbsoluteCeiling"/> is not met
    /// without it.
    /// </param>
    public bool Excuses(
        string quantity,
        DeviationKind kind,
        double observed,
        [NotNullWhen(true)] out string? note,
        double? absolute = null)
    {
        note = null;
        var entry = _recorded.FirstOrDefault(row => row.Kind == kind && row.Quantity == quantity);
        if (entry is null)
        {
            return false;
        }

        _met.Add((quantity, kind));

        var same = kind == DeviationKind.RelativeError
            ? observed <= entry.Recorded * KnownDeviations.Slack
            : observed.Equals(entry.Recorded);
        if (!same)
        {
            return false;
        }

        if (entry.AbsoluteCeiling is { } ceiling
            && (absolute is null || absolute > ceiling * KnownDeviations.Slack))
        {
            return false;
        }

        note = $"{quantity}: known deviation {entry.Recorded:R} ({kind}), measured {observed:R}"
            + (entry.AbsoluteCeiling is { } bound ? $", absolute {absolute:R} within {bound:R}" : string.Empty)
            + ".";
        return true;
    }

    /// <summary>
    /// Recorded deviations that did not happen this time - a reason to fail, not to relax.
    /// </summary>
    public IEnumerable<string> Unmet() =>
        _recorded
            .Where(row => !_met.Contains((row.Quantity, row.Kind)))
            .OrderBy(row => row.Quantity, StringComparer.Ordinal)
            .Select(row =>
                $"{row.Quantity} ({row.Kind}): the deviation recorded for {run} ({group}) no longer "
                + "reproduces - the port changed. Re-triangulate against the original and update "
                + "KnownDeviations.");
}
