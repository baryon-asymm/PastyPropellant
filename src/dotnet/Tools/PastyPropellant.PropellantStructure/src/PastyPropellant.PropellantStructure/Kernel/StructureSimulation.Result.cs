using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Results;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// Packs a finished run into the outward contract. Lines 1182-1515 of the original,
/// which are almost entirely print statements.
/// </summary>
/// <remarks>
/// <para>
/// The rule for this file is that a quantity is written down <b>once</b>, under the
/// name the report gives it, taken from the line of the original that prints it. Where
/// the report computes something in the print statement itself — the agglomerate
/// diameters are the pocket diameters times <c>mp</c>, line 1328-1331 — the product is
/// computed here in the same order and precision, because that product is what the
/// original published and what any archived number was read from.
/// </para>
/// <para>
/// Nothing here decides anything. Every branch of the model has already run; this
/// turns accumulators into the report's vocabulary and attaches to each value what the
/// archive is able to say about it.
/// </para>
/// </remarks>
internal sealed partial class StructureSimulation
{
    /// <summary>
    /// Runs the whole model and packs what it produced.
    /// </summary>
    /// <remarks>
    /// ⚠ The run record is written <b>first</b>, before a single number is drawn. That
    /// ordering is the guarantee in <c>API.md</c> and it is the whole point of the
    /// record: a run killed after six hours must still leave behind what it was asked
    /// to do. Writing it at the end — where the first draft of this method put it —
    /// produces a record only for runs that did not need one.
    /// </remarks>
    internal StructureResult Run(
        IProgress<StructureProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var snapshot = ResolvedRunRecord.Write(_configuration, _configuration.ResolvedRunPath);
        Simulate(progress, cancellationToken);
        return Pack(snapshot);
    }

    /// <summary>Turns the finished accumulators into the report's own names.</summary>
    private StructureResult Pack(ResolvedRunRecordSnapshot snapshot)
    {
        var summary = _summary
            ?? throw new InvalidOperationException(
                "The run produced no summary. Pack runs after Simulate, never instead of it.");

        var status = VerificationPolicy.ForPrintedQuantity(snapshot);

        var printed = new Dictionary<string, Verified<double>>(StringComparer.Ordinal);
        var lengths = new Dictionary<string, Verified<Length>>(StringComparer.Ordinal);

        void Scalar(string name, double value) => printed[name] = new Verified<double>(value, status);
        void Metres(string name, double value) =>
            lengths[name] = new Verified<Length>(Length.FromMeters(value), status);

        // lines 1237-1238: the six generator accuracies. The report's epsx(5) and
        // epsx(6) are EPS6 and EPS7 - there is no printed EPS5.
        Scalar("epsx1", summary.Eps[1]);
        Scalar("epsx2", summary.Eps[2]);
        Scalar("epsx3", summary.Eps[3]);
        Scalar("epsx4", summary.Eps[4]);
        Scalar("epsx5", summary.Eps[6]);
        Scalar("epsx6", summary.Eps[7]);

        // lines 1242-1246
        Scalar("gap_coefficient", summary.Qmcoef);
        Scalar("bridges_per_particle", (float)_state.IbridgeTotal / _state.Fi);
        Scalar("pocket_bridge_ratio", _state.NnTotal / _state.Fi);
        Scalar("jammed_fraction", _state.JammedTotal / _state.Fi);

        // lines 1262-1281: the oxidiser block.
        Metres("dok43_analytic", DokM);
        Metres("dok43_all_v1", summary.Alldok43);
        Metres("dok43_all_v2", summary.Alldok432);
        Metres("dok43_base", summary.Dok43b);
        Metres("dok43_surrounding", summary.Dok43s);
        Scalar("eps_all_dok", summary.Epsx3);
        Scalar("eps_dok_base", summary.Epsx1);
        Scalar("eps_dok_surrounding", summary.Epsx2);
        Metres("dok_max", _ddokmax);

        // line 1281. The report prints DOKSD**0.5 - the accumulator is the variance,
        // and the square root happens in the print statement.
        Metres("dok43_sd", (float)Math.Pow(DokSd, 0.5));

        // line 1286
        Scalar("fine_oxidiser_fraction", (float)(_configuration.Coefficients.HomogenisedOxidiser
            + ((double)Gdokleft / _ggg)));

        // lines 1291-1311: the pocket block.
        Scalar("eps_pocket_distribution", summary.Epsy);
        Scalar("eps_pocket_moment4", summary.EpsMd4);
        Scalar("eps_pocket_moment3", summary.EpsMd3);
        Metres("dkarm43_v1", summary.Dp43);
        Metres("dkarm43_v2", summary.D432);
        Metres("dkarm43_sd", summary.SdevP43);
        Metres("dkarm43_cor1", summary.Dkarm43Cor);
        Metres("dkarm43_sd_cor1", summary.SdevP43Cor);
        Metres("dkarm43_cor2", summary.Dfmk432);
        Metres("dkarm43_sd_cor2", summary.SdevP243);
        Metres("dkarm10", summary.Dqkarm);
        Metres("dkarm10_cor", summary.DqkarmCor);

        // lines 1318-1321. Written as the original writes them: one minus the binder
        // share, not a share accumulated directly.
        Scalar("zkarm", 1f - summary.DolM1);
        Scalar("zkarm_cor1", 1f - summary.DolM2);
        Scalar("zkarm_cor2", 1f - summary.DolM3);

        // lines 1326-1331. Each agglomerate diameter is a pocket diameter times mp,
        // multiplied here in the same single precision the print statement used.
        Scalar("da_coef", DaCoefficient);
        Metres("dagg43_v1", summary.Dp43 * DaCoefficient);
        Metres("dagg43_v2", summary.D432 * DaCoefficient);
        Metres("dagg43_cor1", summary.Dkarm43Cor * DaCoefficient);
        Metres("dagg43_cor2", summary.Dfmk432 * DaCoefficient);

        // lines 1335-1337
        Scalar("dqmkm1", summary.Dqmkm1);
        Scalar("dqmkm2", summary.Dqmkm2);

        // lines 1225-1230: counters the report prints as plain numbers rather than as
        // model quantities. They are here as well as in the diagnostics because the
        // report prints them and a reader quotes them by these names.
        Scalar("cycles_done", _passesDone);
        Scalar("nkarm", _state.Qkss);
        Scalar("nfx", _state.Nfx);
        Scalar("nfy", _state.Nfy);
        Scalar("nfq", _state.Nfq);
        Scalar("nfw", _state.Nfw);
        Scalar("nbase_per_cycle", _n);
        Scalar("nbase_accepted_cumulative", _state.Fi);

        return new StructureResult
        {
            Run = snapshot,
            Printed = printed,
            PrintedLengths = lengths,
            Distributions = BuildDistributions(),
            PocketWallDistributions = BuildPocketWallBlock(summary, status),
            Diagnostics = BuildDiagnostics(summary),
        };
    }

    /// <summary>The ten distributions, each built exactly as its print statement builds it.</summary>
    /// <remarks>
    /// ⚠ The three corrected pocket distributions are normalised over the <b>whole</b>
    /// accumulator (<c>sum(fmkarm_cor)</c>, length Nkarm) while only the first
    /// <c>int(DPmax_cor/Di)+2</c> cells are printed. Normalising over the printed
    /// prefix instead would give a distribution that sums to one and is wrong.
    /// </remarks>
    private IReadOnlyList<Distribution> BuildDistributions()
    {
        var grid = (double)_di * 1e6;
        var pocketCells = (int)(_state.DpMax / _di) + 2;
        var correctedCells = (int)(_state.DpMaxCor / _di) + 2;
        var byLength = new DistributionGrid.ByLength(Length.Zero, Length.FromMeters(_di));

        return
        [
            Sized("fmdok", byLength, Scaled(_state.AllVDokSo, _state.Ndok, 1.0 / grid)),
            Sized("fmkarm", byLength, Scaled(_state.Vkso, pocketCells, 1.0 / grid)),
            Sized("fmkarm_cor", byLength, Shared(_state.FmkarmCor, _state.Nkarm, correctedCells, 1.0 / grid)),
            Sized("fmkarm_cor2", byLength, Shared(_state.Fmkarm2, _state.Nkarm, correctedCells, 1.0 / grid)),
            Sized("fqkarm", byLength, Scaled(_state.Qks1, pocketCells, 1.0 / grid)),
            Sized("fqkarm_cor", byLength, Shared(_state.FqkarmCor, _state.Nkarm, correctedCells, 1.0 / grid)),
            Ratio("fqmkm1", Shared(_state.Qmkm1, RunState.NC, _state.Qmkm1Nmax + 2, 1.0)),
            Ratio("fqmkm2", Shared(_state.Qmkm2, RunState.NC, _state.Qmkm2Nmax + 2, 1.0)),
            Ratio("coef", Shared(_state.Coef, RunState.NC, _state.CoefNmax + 2, 1.0)),
            Sized("pdoksmall", byLength, Scaled(_state.PDokSmall, _state.Ndok - 1, 1.0)),
        ];

        // ⚠ Not named Length: a local function of that name shadows UnitsNet.Length
        // for the whole method, and the shadowing is silent until something in the
        // same body tries to use the type.
        static Distribution Sized(string name, DistributionGrid.ByLength grid, double[] values) =>
            new(name, grid, PrintedArrays.Distributions[name], values);

        static Distribution Ratio(string name, double[] values) =>
            new(
                name,
                new DistributionGrid.ByRatio(0.0, PrintedArrays.RatioGridSteps[name]),
                PrintedArrays.Distributions[name],
                values);
    }

    /// <summary>
    /// <c>Dkarmcat</c>, <c>dokkarm43</c>, <c>dokkarm10</c> and the per-category
    /// distributions of lines 1391-1416.
    /// </summary>
    /// <remarks>
    /// The lengths are stored in metres. The report multiplies them by
    /// <c>1.00001e6</c> rather than <c>1e6</c> - a nudge that keeps
    /// <c>int()</c> from truncating a value that lands a hair under a whole
    /// micrometre - and that nudge belongs to the printing, not to the length.
    /// </remarks>
    private ConditionalOxidiserByPocketSize BuildPocketWallBlock(PassSummary summary, Verification status)
    {
        var grid = (double)_di * 1e6;
        var byLength = new DistributionGrid.ByLength(Length.Zero, Length.FromMeters(_di));

        var categories = new List<Length>(summary.Dprow);
        var massMean = new List<Verified<Length>>(summary.Dprow);
        var mean = new List<Verified<Length>>(summary.Dprow);
        var rows = new List<Distribution>(summary.Dprow);

        for (var irow = 1; irow <= summary.Dprow; irow++)
        {
            categories.Add(Length.FromMeters(summary.Dpockets[irow]));
            massMean.Add(new Verified<Length>(Length.FromMeters(summary.Dokp43[irow]), status));
            mean.Add(new Verified<Length>(Length.FromMeters(summary.Qdokkarm[irow]), status));

            var cells = new double[_state.Ndok];
            for (var iks = 1; iks <= _state.Ndok; iks++)
            {
                cells[iks - 1] = (float)((double)summary.Qdokso[irow, iks] / grid);
            }

            rows.Add(new Distribution(
                $"fqdokkarm({irow},:)",
                byLength,
                DistributionNormalisation.DensityPerGridUnit,
                cells));
        }

        return new ConditionalOxidiserByPocketSize(categories, massMean, mean, rows);
    }

    /// <summary>How the run went, as opposed to what it computed.</summary>
    private RunDiagnostics BuildDiagnostics(PassSummary summary)
    {
        var perPass = (double)_state.Fi + _n;

        return new RunDiagnostics
        {
            PocketsTotal = _state.Qkss,
            BaseParticleDraws = _state.Nfx,
            SurroundingParticleDraws = _state.Nfy,
            PocketSizeDraws = _state.Nfq,
            BridgeDraws = _state.Nfw,
            PassesDone = _passesDone,
            BaseParticlesPerCycle = _n,
            BaseParticlesAccepted = _state.Fi,
            RandomStreamStatistics =
            [
                summary.Eps[1], summary.Eps[2], summary.Eps[3],
                summary.Eps[4], summary.Eps[6], summary.Eps[7],
            ],
            FractionDrawAccuracy = Scaled(summary.Epsalldok, _state.FractionCount, 1.0),
            Convergence = BuildConvergence(),

            // ⚠ The two groups carry different divisors, and both are the original's.
            // Conditions 1-5 count on every pass including the preparatory one, so they
            // are divided by FI+N; conditions 6-9 count only on working passes and are
            // divided by FI. See Kernel/BOOT.md.
            Conditions = new ConditionCounters(
                (float)_state.Conditions[1] / perPass,
                (float)_state.Conditions[2] / perPass,
                (float)_state.Conditions[3] / perPass,
                (float)_state.Conditions[4] / perPass,
                (float)_state.Conditions[5] / perPass,
                (float)_state.Conditions[6] / _state.Fi,
                (float)_state.Conditions[7] / _state.Fi,
                (float)_state.Conditions[8] / _state.Fi,
                (float)_state.Conditions[9] / _state.Fi),

            AttemptsTotal = Verified<long>.Unprinted(_attempts),
            PocketSizeClampCount = Verified<long>.Unprinted(_pocketSizes.ClampCount),
            BridgeGapExceedsPocket = Verified<long>.Unprinted(_bridgeGapExceedsPocket),
            BridgeWidthNegative = Verified<long>.Unprinted(_bridgeWidthNegative),
            BridgeVolumeNaNCount = Verified<long>.Unprinted(_bridgeVolumeNaN),
            CyclesRequested = _kxx,
        };
    }

    /// <summary>
    /// The six <c>*_n</c> arrays, transposed into one row per working pass.
    /// </summary>
    /// <remarks>
    /// ⚠ These are the only printed arrays the original indexes from 1 to <c>KXX</c>
    /// rather than over a histogram grid, so <see cref="Scaled"/> — which skips cell
    /// zero — does not apply to them. A single-pass run returns nothing, matching the
    /// <c>KXX &gt; 1</c> guard the original prints them under.
    /// </remarks>
    private PassConvergence[] BuildConvergence()
    {
        var rows = new PassConvergence[_state.PassPocketDistribution.Length];
        for (var i = 0; i < rows.Length; i++)
        {
            rows[i] = new PassConvergence(
                _state.PassPocketDistribution[i],
                _state.PassPocketMoment3[i],
                _state.PassPocketMoment4[i],
                _state.PassOxidiserMean[i],
                _state.PassOxidiserVariance[i],
                _state.PassPocketMassFraction[i]);
        }

        return rows;
    }

    /// <summary>One printed array: cells 1..<paramref name="count"/>, times a factor.</summary>
    private static double[] Scaled(float[] values, int count, double factor)
    {
        var result = new double[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = (float)((double)values[i + 1] * factor);
        }

        return result;
    }

    /// <summary>
    /// One printed array normalised over <paramref name="over"/> cells while only
    /// <paramref name="count"/> of them are printed.
    /// </summary>
    private static double[] Shared(float[] values, int over, int count, double factor)
    {
        var total = 0.0;
        for (var i = 1; i <= over; i++)
        {
            total += values[i];
        }

        var result = new double[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = (float)((double)values[i + 1] / (float)total * factor);
        }

        return result;
    }

    /// <inheritdoc cref="Shared(float[], int, int, double)"/>
    /// <remarks>
    /// ⚠ Both operands are narrowed to single before the division: the numerator by
    /// the original's explicit <c>REAL()</c>, the denominator because an INTEGER*8
    /// <c>SUM</c> meeting a REAL*4 is converted to the other operand's kind. Past 2^24
    /// that quantises a count, which is exactly what <c>epsdokfr</c> is sensitive to.
    /// </remarks>
    private static double[] Shared(long[] values, int over, int count, double factor)
    {
        var total = 0L;
        for (var i = 1; i <= over; i++)
        {
            total += values[i];
        }

        var result = new double[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = (float)((double)(float)values[i + 1] / (float)total * factor);
        }

        return result;
    }
}
