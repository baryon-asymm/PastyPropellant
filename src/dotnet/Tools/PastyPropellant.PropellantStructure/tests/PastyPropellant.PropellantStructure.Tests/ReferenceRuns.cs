using System.Text.Json;
using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The 43 archived runs of the original, as parsed into
/// <c>reference/propstruct/runs.json</c>.
/// </summary>
/// <remarks>
/// ⚠ The fraction bounds in <see cref="ReferenceRun.Fractions"/> are <em>not</em>
/// exact inputs. They are recovered from the <c>Dfr</c> array in the <c>.m</c>
/// report, printed at E9.3 — three significant digits. Three runs prove it:
/// <c>r_p35050n</c>, <c>r_p35050nn</c> and <c>rnano2</c> report a widest bound of
/// 314, 314 and 315 µm while their <c>dok_max</c>, printed at F7.2 from the same
/// number, reads 313.57, 313.57 and 314.66. So an assertion against that field may
/// only claim what three digits can carry.
/// <para>
/// ⚠ Half-lifted on 2026-08-15. Schema <c>/3</c> of runs.json carries
/// <c>input_dat</c> — the original <c>.dat</c> named by the <c>.m</c> header
/// itself, with the bounds at the precision they were written and with
/// <c>AK1..AK4</c>, which no report prints. It is exposed as
/// <see cref="ReferenceRun.InputDat"/>, and only 20 of the 43 files still describe
/// their own run: <see cref="ReferenceInputFile.AgreesWithM"/> says which, and
/// reading the file without checking that flag replays a different run on half the
/// sample. <see cref="ReferenceRun.ToConfiguration"/> applies the rule for you.
/// </para>
/// <para>
/// ⚠ That count was 21 until 2026-08-23. The archive's own <c>agrees_with_m</c>
/// compares each fraction's size bounds and never its mass share, so a file that
/// kept a later recipe on the same size grid passed it — which is what
/// <c>HPEPA10.dat</c> did to <c>hp90</c>. The flag is re-checked here against the
/// shares the report echoed; see <see cref="ReferenceInputFile.SharesMatchReport"/>.
/// </para>
/// </remarks>
public static class ReferenceRuns
{
    private static readonly Lazy<IReadOnlyList<ReferenceRun>> LazyAll = new(Load);

    public static IReadOnlyList<ReferenceRun> All => LazyAll.Value;

    public static ReferenceRun ById(string id) =>
        All.FirstOrDefault(run => run.Id == id)
        ?? throw new KeyNotFoundException($"No archived run '{id}' in runs.json.");

    /// <summary>Every run id, as xunit member data. Public because xunit requires
    /// a MemberData source to be.</summary>
    public static TheoryData<string> AllIds
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var run in All)
            {
                data.Add(run.Id);
            }

            return data;
        }
    }

    /// <summary>
    /// How many runs the archive held when it was parsed. A floor, never an equality.
    /// </summary>
    /// <remarks>
    /// The archive can gain runs — a newly recovered <c>.m</c>, a re-parse that
    /// recovers one the parser used to drop — and a test that failed on <i>any</i>
    /// change would make growing it a chore, so it would stop growing. A <b>fall</b> is
    /// the other thing entirely: every id below is quoted somewhere as evidence, so a
    /// run leaving the archive silently removes evidence from tests that still claim to
    /// rest on it.
    /// </remarks>
    internal const int ArchivedRunsAtLeast = 43;

    private static IReadOnlyList<ReferenceRun> Load()
    {
        var path = RepositoryPaths.Resolve("reference", "propstruct", "runs.json");
        if (!File.Exists(path))
        {
            // ⚠ Worth its own message rather than a bare FileNotFoundException from the
            // read below. The file name alone does not say that the whole L1/L2 ladder
            // has lost its oracle rather than one test being unable to open one file -
            // and since the archive IS tracked, its absence means a partial checkout or
            // a deletion, which is worth saying too.
            throw new FileNotFoundException(
                $"The reference archive is missing: '{path}' does not exist. It is the oracle "
                    + "for the whole L1/L2 ladder and is tracked in this repository, so it "
                    + "should be present; see reference/propstruct/README.md for how it is "
                    + "produced.",
                path);
        }

        using var document = JsonDocument.Parse(File.ReadAllBytes(path));

        var runs = new List<ReferenceRun>();
        foreach (var element in document.RootElement.GetProperty("runs").EnumerateArray())
        {
            var input = element.GetProperty("input");
            var parameters = element.GetProperty("parameters");

            var fractions = input.GetProperty("fractions").EnumerateArray()
                .Select(fraction => new ReferenceFraction(
                    fraction.GetProperty("mass_fraction").GetDouble(),
                    fraction.GetProperty("d_min_m").GetDouble(),
                    fraction.GetProperty("d_max_m").GetDouble()))
                .ToArray();

            var scalars = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var scalar in element.GetProperty("expected").GetProperty("scalars").EnumerateObject())
            {
                if (scalar.Value.ValueKind == JsonValueKind.Number)
                {
                    scalars[scalar.Name] = scalar.Value.GetDouble();
                }
            }

            var parameterValues = new Dictionary<string, double>(StringComparer.Ordinal);
            foreach (var parameter in parameters.EnumerateObject())
            {
                if (parameter.Value.ValueKind == JsonValueKind.Number)
                {
                    parameterValues[parameter.Name] = parameter.Value.GetDouble();
                }
            }

            runs.Add(new ReferenceRun(
                element.GetProperty("id").GetString()!,
                input.GetProperty("size_distribution_law").GetInt32(),
                parameters.GetProperty("p_alpha").GetDouble(),
                fractions,
                scalars)
            {
                OxidiserDensity = input.GetProperty("oxidiser_density").GetDouble(),
                BinderMetalDensity = input.GetProperty("binder_metal_density").GetDouble(),
                OxidiserMassFraction = input.GetProperty("oxidiser_mass_fraction").GetDouble(),
                MetalMassFraction = input.GetProperty("metal_mass_fraction").GetDouble(),
                BaseParticles = input.GetProperty("base_particles").GetInt64(),
                Cycles = input.GetProperty("cycles").GetInt32(),
                Generator = input.GetProperty("generator").GetInt32(),
                PocketFormingFractions = ReadFlags(input, "pocket_forming_fractions"),
                Parameters = parameterValues,
                RecoveredEta = element.GetProperty("derived").GetProperty("recovered_eta").GetDouble(),
                InputDat = ReadInputFile(element, fractions),
                Arrays = ReadArrays(element.GetProperty("expected"), "arrays"),
                ArraySteps = ReadNumbers(element.GetProperty("expected"), "array_steps"),
                ConditionCounters = ReadNumbers(element.GetProperty("expected"), "conditions_per_particle"),
                ConditionalDok = ReadConditionalDok(element.GetProperty("expected")),
            });
        }

        return runs;
    }

    /// <summary>Every named array of a block, in printed order.</summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<double>> ReadArrays(JsonElement parent, string name)
    {
        var arrays = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal);
        if (!parent.TryGetProperty(name, out var block))
        {
            return arrays;
        }

        foreach (var array in block.EnumerateObject())
        {
            arrays[array.Name] = array.Value.EnumerateArray().Select(cell => cell.GetDouble()).ToArray();
        }

        return arrays;
    }

    private static IReadOnlyDictionary<string, double> ReadNumbers(JsonElement parent, string name)
    {
        var numbers = new Dictionary<string, double>(StringComparer.Ordinal);
        if (!parent.TryGetProperty(name, out var block))
        {
            return numbers;
        }

        foreach (var number in block.EnumerateObject().Where(entry => entry.Value.ValueKind == JsonValueKind.Number))
        {
            numbers[number.Name] = number.Value.GetDouble();
        }

        return numbers;
    }

    private static ReferenceConditionalDok? ReadConditionalDok(JsonElement expected)
    {
        if (!expected.TryGetProperty("conditional_dok", out var block))
        {
            return null;
        }

        return new ReferenceConditionalDok(
            block.GetProperty("pocket_category_um").EnumerateArray().Select(cell => cell.GetDouble()).ToArray(),
            block.GetProperty("rows").EnumerateArray()
                .Select(row => (IReadOnlyList<double>)row.EnumerateArray().Select(cell => cell.GetDouble()).ToArray())
                .ToArray());
    }

    private static IReadOnlyList<bool>? ReadFlags(JsonElement input, string name)
    {
        if (!input.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        // The report prints SFR through a real format, so the flags arrive as 1.0
        // and 0.0 rather than as integers.
        return element.EnumerateArray().Select(flag => flag.GetDouble() != 0.0).ToArray();
    }

    /// <summary>
    /// The <c>.dat</c> record, with <c>agrees_with_m</c> re-checked here against the
    /// mass shares the report itself echoed.
    /// </summary>
    /// <remarks>
    /// The generator's own check compares only each fraction's <em>size bounds</em>,
    /// never its mass share, and a <c>.dat</c> that kept a later revision of the same
    /// size grid therefore passes it. One run does exactly that: <c>hp90</c>'s file
    /// carries <c>hp95</c>'s shares under <c>hp90</c>'s bounds, so the flag said the
    /// file still described the run and the fixture replayed the wrong recipe under
    /// the right name. Fourteen of the forty-three files disagree on shares; the
    /// other thirteen were already caught by the bounds, which is why re-checking
    /// here moves exactly one run and cannot quietly disqualify the rest.
    /// <para>
    /// The tolerance is the resolution of the report's own <c>E9.3</c> print — half
    /// of the last printed digit. The worst agreeing run misses by 3e-4 of its
    /// share and the disagreeing one by 2.5e-2, so the two populations are two
    /// orders of magnitude apart and the threshold is not a tuned number.
    /// </para>
    /// </remarks>
    private static ReferenceInputFile? ReadInputFile(
        JsonElement run,
        IReadOnlyList<ReferenceFraction> reported)
    {
        if (!run.TryGetProperty("input_dat", out var dat))
        {
            return null;
        }

        var bounds = dat.GetProperty("fractions").EnumerateArray()
            .Select(fraction => new ReferenceFraction(
                fraction.GetProperty("mass_fraction").GetDouble(),
                fraction.GetProperty("d_min_m").GetDouble(),
                fraction.GetProperty("d_max_m").GetDouble()))
            .ToArray();

        return new ReferenceInputFile(
            // archive_file, not source_file: the .m header spells the name in lower
            // case and the archive stores it in upper, so only this one opens on a
            // case-sensitive filesystem.
            dat.GetProperty("archive_file").GetString()!,
            dat.GetProperty("agrees_with_m").GetBoolean(),
            SharesAgree(reported, bounds),
            new GeometricCriteria(
                dat.GetProperty("ak1").GetDouble(),
                dat.GetProperty("ak2").GetDouble(),
                dat.GetProperty("ak3").GetDouble(),
                dat.GetProperty("ak4").GetDouble()),
            dat.GetProperty("cycles_requested").GetInt32(),
            bounds);
    }

    /// <summary>
    /// Whether two fraction lists carry the same mass shares to the precision the
    /// report printed them at.
    /// </summary>
    private static bool SharesAgree(
        IReadOnlyList<ReferenceFraction> reported,
        IReadOnlyList<ReferenceFraction> file)
    {
        if (reported.Count != file.Count)
        {
            return false;
        }

        for (var index = 0; index < reported.Count; index++)
        {
            var share = reported[index].MassFraction;
            if (Math.Abs(share - file[index].MassFraction) > PrintedResolution(share))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Half of the last digit of <c>E9.3</c>, which prints a three-digit mantissa in
    /// [0.1, 1) against a decimal exponent.
    /// </summary>
    private static double PrintedResolution(double value)
    {
        if (value == 0.0)
        {
            return 5e-4;
        }

        var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;
        return (0.5 * Math.Pow(10.0, exponent - 3)) + 1e-12;
    }
}

/// <summary>One archived run: what it was given and what it printed.</summary>
public sealed record ReferenceRun(
    string Id,
    int Law,
    double Alpha,
    IReadOnlyList<ReferenceFraction> Fractions,
    IReadOnlyDictionary<string, double> Scalars)
{
    public required double OxidiserDensity { get; init; }

    public required double BinderMetalDensity { get; init; }

    public required double OxidiserMassFraction { get; init; }

    public required double MetalMassFraction { get; init; }

    public required long BaseParticles { get; init; }

    /// <summary>
    /// <c>KXX</c> as the report prints it — that is, already normalised to 1. The
    /// value the input file actually held is
    /// <see cref="ReferenceInputFile.CyclesRequested"/>, and it is 0.
    /// </summary>
    public required int Cycles { get; init; }

    public required int Generator { get; init; }

    /// <summary>
    /// <c>SFR</c>, present for the two runs that set it and <see langword="null"/>
    /// for the 41 that leave every fraction forming pockets.
    /// </summary>
    public required IReadOnlyList<bool>? PocketFormingFractions { get; init; }

    /// <summary>The <c>parameters</c> block, verbatim.</summary>
    public required IReadOnlyDictionary<string, double> Parameters { get; init; }

    /// <summary>
    /// <c>eta</c>, which the report never printed and which the parser recovers by
    /// inverting a printed formula. Zero in 42 runs, 0.5 in one.
    /// </summary>
    public required double RecoveredEta { get; init; }

    /// <summary>
    /// The <c>.dat</c> this run named, when the archive still holds it — 41 of 43.
    /// Only usable as an input record where <see cref="ReferenceInputFile.AgreesWithM"/>.
    /// </summary>
    public required ReferenceInputFile? InputDat { get; init; }

    /// <summary>
    /// The fourteen printed arrays, under the original's own names.
    /// </summary>
    /// <remarks>
    /// ⚠ Not fourteen distributions: <c>Dkarmcat</c>, <c>dokkarm43</c> and
    /// <c>dokkarm10</c> are curves of length against pocket category, and
    /// <c>epsdokfr</c> is a draw-accuracy diagnostic. The six <c>*_n</c> arrays
    /// printed by <c>arrayprint2</c> need <c>KXX &gt; 1</c> and appear in no
    /// archived run.
    /// </remarks>
    public required IReadOnlyDictionary<string, IReadOnlyList<double>> Arrays { get; init; }

    /// <summary>
    /// Grid step per array, where the report states one.
    /// </summary>
    /// <remarks>
    /// ⚠ <c>fqmkm1</c> has two entries: <c>fqmkm1</c> is the step the array was
    /// built with (0.001) and <c>fqmkm1_as_printed</c> is the one the report claims
    /// (0.01). They differ by a factor of ten, and the report is the wrong one.
    /// </remarks>
    public required IReadOnlyDictionary<string, double> ArraySteps { get; init; }

    /// <summary>
    /// The nine rejection counters, named, per base particle.
    /// </summary>
    /// <remarks>
    /// ⚠ Two divisors: the first five are divided by <c>FI + N</c> and the last four
    /// by <c>FI</c>. Comparing across the two groups is meaningless.
    /// </remarks>
    public required IReadOnlyDictionary<string, double> ConditionCounters { get; init; }

    /// <summary>
    /// Particle sizes resolved by pocket-size category — 40 categories in every
    /// archived run.
    /// </summary>
    public required ReferenceConditionalDok? ConditionalDok { get; init; }

    /// <summary>
    /// Whether the exact input survives for this run: the <c>.dat</c> is present
    /// <em>and</em> still describes it.
    /// </summary>
    public bool HasExactInput => InputDat is { AgreesWithM: true };

    /// <summary>
    /// The run as a configuration of the port.
    /// </summary>
    /// <remarks>
    /// Where <see cref="HasExactInput"/> holds, the bounds and <c>AK1..AK4</c> come
    /// from the <c>.dat</c> — full precision, and the four criteria exist nowhere
    /// else. Otherwise the bounds come from the report at three significant digits
    /// and the criteria fall back to
    /// <see cref="GeometricCriteria.Historical"/>, which is what every checked
    /// <c>.dat</c> holds anyway. A test that needs a bit-exact input must gate on
    /// <see cref="HasExactInput"/> rather than assume this method delivered one.
    /// </remarks>
    public StructureRunConfiguration ToConfiguration()
    {
        var exact = HasExactInput ? InputDat : null;
        var bounds = exact?.Fractions ?? Fractions;

        var fractions = new List<OxidiserFraction>(bounds.Count);
        for (var index = 0; index < bounds.Count; index++)
        {
            var formsPockets = PocketFormingFractions is null
                || index >= PocketFormingFractions.Count
                || PocketFormingFractions[index];

            fractions.Add(new OxidiserFraction(
                bounds[index].MassFraction,
                Length.FromMeters(bounds[index].MinSizeMetres),
                Length.FromMeters(bounds[index].MaxSizeMetres),
                formsPockets));
        }

        return new StructureRunConfiguration
        {
            OxidiserDensity = Density.FromKilogramsPerCubicMeter(OxidiserDensity),
            BinderMetalDensity = Density.FromKilogramsPerCubicMeter(BinderMetalDensity),
            OxidiserMassFraction = OxidiserMassFraction,
            MetalMassFraction = MetalMassFraction,
            Fractions = fractions,
            BaseParticles = BaseParticles,
            Cycles = exact?.CyclesRequested ?? Cycles,
            CalculationVariant = (int)Parameters["calculation_variant"],
            Criteria = exact?.Criteria ?? GeometricCriteria.Historical,
            MinimumParticleSize = Length.FromMicrometers(Parameters["d_min_um"]),
            ParticleHistogramStep = Length.FromMicrometers(Parameters["di_um"]),
            PocketHistogramStep = Length.FromMicrometers(Parameters["dj_um"]),
            Coefficients = new ModelCoefficients(
                Parameters["k5_surrounding_volume"],
                Parameters["k7_pocket_in_pocket"],
                Parameters["k8_pocket_in_bridge"],
                Parameters["eps"],
                Alpha,
                Parameters["homogenised_oxidiser"],
                Parameters["pocket_bridge_ratio_min"],
                Parameters["pocket_bridge_ratio_max"]),
            Generator = (GeneratorSelection)Generator,
            Law = (SizeDistributionLaw)Law,
            AgglomeratedOxideShare = RecoveredEta,
            // A test must not write into the working directory.
            ResolvedRunPath = null,
        };
    }
}

/// <summary>A fraction of the reference input, in metres.</summary>
public sealed record ReferenceFraction(double MassFraction, double MinSizeMetres, double MaxSizeMetres);

/// <summary>
/// The <c>.dat</c> a run named, as schema <c>/3</c> parsed it.
/// </summary>
/// <param name="ArchiveFile">the name under <c>reference/propstruct/inputs/</c></param>
/// <param name="FlaggedAgreement">
/// the archive's own <c>agrees_with_m</c>, which compares each fraction's
/// <em>size bounds</em> only. False for 20 of the 41: a <c>.dat</c> was a working
/// file, kept only its last state, and using a drifted one silently replays a
/// different case
/// </param>
/// <param name="SharesMatchReport">
/// the half of the comparison the archive never made — whether the file's mass
/// shares are the ones the report echoed. One run passes the bounds and fails this
/// </param>
/// <param name="Criteria">
/// <c>AK1..AK4</c> — the four constants that decide what counts as a pocket, and
/// which the <c>.m</c> report never printed
/// </param>
/// <param name="CyclesRequested"><c>KXX</c> as written, which is 0</param>
/// <param name="Fractions">the bounds at the precision they were typed</param>
public sealed record ReferenceInputFile(
    string ArchiveFile,
    bool FlaggedAgreement,
    bool SharesMatchReport,
    GeometricCriteria Criteria,
    int CyclesRequested,
    IReadOnlyList<ReferenceFraction> Fractions)
{
    /// <summary>
    /// Whether the file still describes the run: both halves of the comparison, not
    /// just the one the archive made.
    /// </summary>
    public bool AgreesWithM => FlaggedAgreement && SharesMatchReport;
}

/// <summary>
/// The <c>conditional_dok</c> block: what surrounds a pocket, by pocket size.
/// </summary>
/// <param name="PocketCategoriesMicrometres">
/// the category upper bounds as printed, in micrometres (<c>Dkarmcat</c>)
/// </param>
/// <param name="Rows">
/// one numeric density distribution of particle sizes per category, in the same
/// order — <c>fqdokkarm(i,:)</c>
/// </param>
public sealed record ReferenceConditionalDok(
    IReadOnlyList<double> PocketCategoriesMicrometres,
    IReadOnlyList<IReadOnlyList<double>> Rows);
