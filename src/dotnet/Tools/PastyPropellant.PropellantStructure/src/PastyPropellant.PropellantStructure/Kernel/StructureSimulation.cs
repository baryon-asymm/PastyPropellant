using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Geometry;
using PastyPropellant.PropellantStructure.Sampling;

namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// The Monte-Carlo model itself: lines 408–1181 of <c>PropStructv3.for</c>.
/// </summary>
/// <remarks>
/// <para>
/// Read this file beside the listing. Method boundaries follow the original's loop
/// nesting — run, pass, base particle, surrounding particle — and every block carries
/// the line numbers it transcribes. Where the transcription is not literal, a ⚠ says
/// why.
/// </para>
/// <para>
/// **Numeric rule.** Expressions are evaluated in <see cref="double"/>; rounding to
/// <see cref="float"/> happens on assignment to a variable the original declares
/// <c>REAL*4</c>. A literal written <c>3.14159</c> in the original is a <c>REAL*4</c>
/// constant, which is why <see cref="Pi"/> is the double value of the float literal
/// and not the decimal one.
/// </para>
/// <para>
/// ⚠ **That rule is the closest C# can get, not a description of the original.** The
/// justification once written here — "the compiler ran x87 in 53-bit mode, so there
/// were no 80-bit intermediates to reproduce" — is false, and measurably so: the same
/// source built with <c>gfortran -O0 -mfpmath=387</c> reproduces the archive bit for
/// bit, while <c>-O0 -mfpmath=387 -ffloat-store</c> — same instructions, same library
/// routines, intermediates rounded to the declared precision, i.e. what this file does
/// — does not. The one place it costs anything is the <c>TU</c> budget loop in
/// <see cref="SurroundBaseParticle"/>; what that costs, and why it reaches no printed
/// quantity, is in <c>Kernel/BOOT.md</c> under "Предел воспроизводимости".
/// </para>
/// <para>
/// **Exponent rule.** <c>x**3</c> (integer) is repeated multiplication; <c>x**3.</c>
/// (real) goes through the library power function. The original uses both, sometimes
/// on adjacent lines, and they do not agree in the last bits.
/// </para>
/// </remarks>
internal sealed partial class StructureSimulation
{
    /// <summary>The main module's pi — a <c>REAL*4</c> literal, unlike Geometry's 3.14.</summary>
    private const double Pi = 3.14159f;

    /// <summary>TU's seed, line 430: the whole sphere, as the original spells it.</summary>
    private const double FullSolidAngle = 12.56636f;

    /// <summary>
    /// The exit threshold of line 716, <c>12.56636/200.</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ Both literals are default real, so the original folds this quotient in REAL*4
    /// and compares a REAL*4 TU against a REAL*4 constant. Dividing in double instead
    /// lands 0.24 ulp lower - 0.062831802368164061 against 0.062831804156303406 - and
    /// that gap has a sharp consequence rather than a vague one: the two readings decide
    /// differently in exactly one case, when TU lands ON the float, where the original
    /// exits and a double threshold does not. TU crosses within a decrement of ~0.05
    /// while the float step here is 7.45e-9, so that is about 1.5e-7 per realisation -
    /// of order one event in a run the size of hp1050. The cast forces the fold: C#
    /// permits float arithmetic to be carried at higher precision, but not through a
    /// cast.
    /// </remarks>
    internal const double TuExitThreshold = (float)(12.56636f / 200f);

    /// <summary>The exponent of line 501, written <c>(1/3.)</c> and therefore single precision.</summary>
    private const double OneThird = 1f / 3f;

    private readonly StructureRunConfiguration _configuration;
    private readonly RandomStreamSet _streams;
    private readonly FractionSampler _fractions;
    private readonly PocketSizeSampler _pocketSizes;
    private readonly RunState _state;
    private readonly bool[] _formsPockets;

    // --- input, in the original's names and units
    private readonly int _n;
    private readonly int _kxx;
    private readonly int _ivar;
    private readonly float _di;
    private readonly float _dj;
    private readonly float _dmin;
    private readonly float _dmax;
    private readonly float _ak1;
    private readonly float _ak2;
    private readonly float _ak3;
    private readonly float _ak4;
    private readonly float _plot1;
    private readonly float _plot2;
    private readonly float _ggg;
    private readonly float _gm;
    private readonly float _slamd;
    private readonly float _alpha;
    private readonly float _karmcoef;
    private readonly float _mkmcoef;
    private readonly float _nnMin;
    private readonly float _nnMax;
    private readonly float _eta;

    /// <summary>Ddokmax of lines 268-273: the widest fraction bound, which sizes every grid.</summary>
    private readonly float _ddokmax;

    // --- state of the realisation being built (the original's un-suffixed locals)
    private PassSummary? _summary;
    private int _ipris;
    private int _nfract;
    private float _dr;
    private float _db;
    private float _aa;
    private float _rrl;
    private float _vp;
    private float _tu;
    private int _ireset;
    private int _ipocketLoc;
    private int _ibridgeLoc;
    private int _ipocketLocCor;
    private int _ibridgeLocCor;
    private int _ibridgeLocCor2;
    private int _jammedLoc;
    private int _idokLocalAll;
    private float _vdokLoc;
    private float _vmkmLoc;
    private float _vdokLoc2;
    private float _vmkmLoc2;
    private float _svd1;
    private float _vsmkm1;
    private long _attempts;

    // Never printed by the original, so no archived run can adjudicate them. They exist
    // because the quantities they count are ones the original discards silently: a
    // rejected bridge leaves no trace in any accumulator, and a NaN volume leaves one
    // that looks like a number.
    private long _bridgeGapExceedsPocket;
    private long _bridgeWidthNegative;
    private long _bridgeVolumeNaN;
    private int _passesDone;

    internal StructureSimulation(StructureRunConfiguration configuration, RandomStreamSet streams)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(streams);
        StructureConfigurationValidator.Validate(configuration);

        _configuration = configuration;
        _streams = streams;

        _n = checked((int)configuration.BaseParticles);

        // lines 141-143: the original substitutes here, and this is the only place it
        // substitutes anything. Configuration/ deliberately does not - see BOOT.md.
        _kxx = configuration.Cycles > 1 ? configuration.Cycles : 1;
        _ivar = configuration.CalculationVariant;

        _di = (float)configuration.ParticleHistogramStep.Meters;
        _dj = (float)configuration.PocketHistogramStep.Meters;
        _dmin = (float)configuration.MinimumParticleSize.Meters;
        _ak1 = (float)configuration.Criteria.Ak1;
        _ak2 = (float)configuration.Criteria.Ak2;
        _ak3 = (float)configuration.Criteria.Ak3;
        _ak4 = (float)configuration.Criteria.Ak4;
        _plot1 = (float)configuration.OxidiserDensity.KilogramsPerCubicMeter;
        _plot2 = (float)configuration.BinderMetalDensity.KilogramsPerCubicMeter;
        _gm = (float)configuration.MetalMassFraction;
        _alpha = (float)configuration.Coefficients.SurroundingVolumeShare;
        _karmcoef = (float)configuration.Coefficients.PocketInPocket;
        _mkmcoef = (float)configuration.Coefficients.PocketInBridge;
        _nnMin = (float)configuration.Coefficients.PocketBridgeRatioMin;
        _nnMax = (float)configuration.Coefficients.PocketBridgeRatioMax;
        _eta = (float)configuration.AgglomeratedOxideShare;

        var fractionCount = configuration.Fractions.Count;
        var massFractions = new float[fractionCount];
        var bounds = new float[2 * fractionCount];
        _formsPockets = new bool[fractionCount + 1];
        for (var i = 0; i < fractionCount; i++)
        {
            massFractions[i] = (float)configuration.Fractions[i].MassFraction;
            bounds[(2 * i) + 0] = (float)configuration.Fractions[i].MinSize.Meters;
            bounds[(2 * i) + 1] = (float)configuration.Fractions[i].MaxSize.Meters;
            _formsPockets[i + 1] = configuration.Fractions[i].FormsPockets;
        }

        // line 376: the homogenised share is taken out of the oxidiser before anything
        // else sees it.
        var ggg0 = configuration.OxidiserMassFraction;
        _ggg = (float)(ggg0 - (configuration.Coefficients.HomogenisedOxidiser * ggg0));

        var alfa = configuration.Coefficients.StatisticalSignificance;
        _fractions = FractionSampler.Build(
            configuration.Law, massFractions, bounds, ref alfa, out _dmax);

        // line 378
        _slamd = (float)(_ggg / (double)_plot1 * _fractions.ProbabilitySum * _plot2);

        // lines 268-276: the grids are sized off the widest fraction bound, not off Dmax.
        var ddokmax = 0f;
        for (var i = 0; i < fractionCount; i++)
        {
            var upper = (float)configuration.Fractions[i].MaxSize.Meters;
            if (upper > ddokmax)
            {
                ddokmax = upper;
            }
        }

        _ddokmax = ddokmax;
        var ndok = (int)(ddokmax / _di) + 2;
        var nkarm = (int)(ddokmax * _ak4 / _di) + 2;
        var ncat = (int)(ddokmax * _ak4 / _dj) + 2;
        _state = new RunState(fractionCount, ndok, nkarm, ncat, _kxx);
        _pocketSizes = new PocketSizeSampler(nkarm);

        (DokM, DokSd) = MeanOxidiserSize(configuration.Law, massFractions, bounds, _fractions.Probabilities);
    }

    /// <summary>DOKM of lines 380-405: the recipe's own 4/3 mean, the yardstick for epsx.</summary>
    internal float DokM { get; }

    /// <summary>DOKSD of line 405.</summary>
    internal float DokSd { get; }

    internal RunState State => _state;

    /// <summary>
    /// What the last completed pass derived from the accumulators, or <c>null</c>
    /// before the first pass has ended.
    /// </summary>
    internal PassSummary? Summary => _summary;

    /// <summary>Base particles completed per pass — N of line 425.</summary>
    internal int BaseParticlesPerPass => _n;

    /// <summary>Passes this run will make: one preparatory plus KXX working ones.</summary>
    internal int TotalPasses => _kxx + 1;

    /// <summary>
    /// Runs every pass. Lines 408-1181.
    /// </summary>
    /// <remarks>
    /// ⚠ Progress and cancellation are checked on the base-particle boundary only, and
    /// neither touches a draw: a run watched and a run ignored produce the same numbers.
    /// </remarks>
    internal RunState Simulate(
        IProgress<StructureProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var kpris = 0;
        _ipris = 0;

        while (true)
        {
            RunOnePass(kpris, progress, cancellationToken);

            // lines 770-1152: everything derived from the accumulators, including the
            // fine-oxidiser model the next pass will consult. The last pass's summary
            // is the one the run reports.
            _summary = Summarise();

            // lines 1078-1082: leaving the preparatory pass.
            if (_ipris == 0)
            {
                _ipris = 1;
                continue;
            }

            // line 1154: FI is the first working pass's I-1, and only that pass's.
            if (kpris == 0)
            {
                _state.Fi = _n;
            }

            kpris++;
            _passesDone = kpris;
            RecordPassConvergence(kpris);

            // lines 1178-1181
            if (kpris >= _kxx)
            {
                return _state;
            }

            _state.Fi += _n;
        }
    }

    /// <summary>
    /// Lines 1168-1175: six numbers per working pass, the run's only record of how its
    /// own accuracy moved as base particles accumulated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The original guards the whole block with <c>KXX &gt; 1</c> and prints the six
    /// arrays under the same guard (1418-1427), so a single-pass run has no such
    /// history and the port keeps the arrays empty rather than one entry long.
    /// </para>
    /// <para>
    /// ⚠ Two of the six are <b>signed</b> where their scalar namesakes are not.
    /// <c>epsdok43_n</c> is <c>(ALLDOK43 - DokM) / DokM</c> while the scalar
    /// <c>epsalldok</c> is the absolute value of the same expression, and
    /// <c>epsdoksd_n</c> pairs with nothing printed at all. Taking the sign out would
    /// throw away the only statement the report makes about the <i>direction</i> the
    /// sample misses the analytic value in.
    /// </para>
    /// <para>
    /// ⚠ <c>epsdoksd_n</c> compares <b>variances</b>, not spreads: <c>ALLDOKsd</c> and
    /// <c>DokSD</c> are both second moments, and the report takes the square root only
    /// when it prints <c>Dok43sd</c>. And <c>epszkarm_n</c> is <c>1 - DolM2</c>, which
    /// is <c>zkarm_cor1</c> and not <c>zkarm</c> — the two happen to coincide on the
    /// probe, so only the source separates them.
    /// </para>
    /// </remarks>
    private void RecordPassConvergence(int kpris)
    {
        if (_kxx <= 1 || _summary is null)
        {
            return;
        }

        var i = kpris - 1;
        _state.PassPocketDistribution[i] = _summary.Epsy;
        _state.PassPocketMoment3[i] = _summary.EpsMd3;
        _state.PassPocketMoment4[i] = _summary.EpsMd4;
        _state.PassOxidiserMean[i] = (float)(((double)_summary.Alldok43 - DokM) / DokM);
        _state.PassOxidiserVariance[i] = (float)(((double)_summary.Alldoksd - DokSd) / DokSd);
        _state.PassPocketMassFraction[i] = (float)(1.0 - _summary.DolM2);
    }

    /// <summary>One pass: N base particles carried to completion. Lines 425-767.</summary>
    private void RunOnePass(
        int kpris,
        IProgress<StructureProgress>? progress,
        CancellationToken cancellationToken)
    {
        var pass = _ipris == 0 ? 0 : kpris + 1;

        for (var i = 1; i <= _n; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // ⚠ _attempts is NOT reset here, and used to be. Resetting it made
            // AttemptsTotal the attempt count of the run's last base particle - a sample
            // of one, under a name that promises a total. Nothing compares against it
            // (the original prints no such counter), so no oracle would have caught it.
            SimulateBaseParticle();

            progress?.Report(new StructureProgress(pass, TotalPasses, i, _n));
        }
    }

    /// <summary>
    /// One base particle, carried until a realisation is accepted. Lines 428-756.
    /// </summary>
    /// <remarks>
    /// ⚠ There is no attempt limit, and adding one is forbidden: it would change which
    /// realisations the model accepts. A degenerate recipe hangs here, exactly as the
    /// original hangs.
    /// </remarks>
    private void SimulateBaseParticle()
    {
        while (true)
        {
            // label 11, lines 429-445
            _attempts++;
            _tu = (float)FullSolidAngle;
            Array.Clear(_state.Fmkarm2Local);
            _ireset = 0;
            _ipocketLoc = 0;
            _ibridgeLoc = 0;
            _ipocketLocCor = 0;
            _ibridgeLocCor = 0;
            _ibridgeLocCor2 = 0;
            _jammedLoc = 0;
            _idokLocalAll = 0;
            _vdokLoc = 0f;
            _vmkmLoc = 0f;
            _vdokLoc2 = 0f;
            _vmkmLoc2 = 0f;
            _svd1 = 0f;
            _vsmkm1 = 0f;

            // lines 448-463
            var x = _streams.BaseFraction.Next();
            var x0 = _streams.BasePosition.Next();
            _dr = _fractions.Diameter(x, x0, ref _nfract);
            _state.Xss0 += x0;
            _state.Xss1 += x;
            _state.Nfx++;

            // lines 465-471: the base particle counts towards the oxidiser statistics
            // whether or not the realisation survives.
            RecordOxidiserParticle(_dr);
            _state.DokBase41 += Math.Pow(_dr, 4.0);
            _state.DokBase31 += Math.Pow(_dr, 3.0);

            // lines 473-481
            if (!_formsPockets[_nfract])
            {
                continue;
            }

            if (_dr >= _dmax)
            {
                // ⚠ Never fires: the draw cannot reach Dmax. The counter is shared with
                // line 530, whose test is the typo - see BOOT.md.
                _state.Conditions[1]++;
                continue;
            }

            if (_dr <= _dmin)
            {
                _state.Conditions[2]++;
                continue;
            }

            // lines 482-484
            _state.Sd4 += Math.Pow(_dr, 4.0);
            _state.Sd3 += Math.Pow(_dr, 3.0);
            _vp = (float)(Pi / 6.0 * Math.Pow(_dr, 3.0));

            if (SurroundBaseParticle() == NeighbourOutcome.RestartParticle)
            {
                continue;
            }

            // ⚠ lines 717-718 sit ABOVE the IPRIS guard on line 719, so the pocket
            // histogram is renormalised after every base particle in EVERY pass. The
            // preparatory one is not a spectator here: line 676 sits above the IPRIS
            // guard on line 680 too, so it fills QKS, and QKS is zeroed once at line
            // 306 - before the pass entry at label 700 - so every pass accumulates
            // into the same histogram.
            RenormalisePocketHistogram();

            // lines 719-756: only the working passes account for what was built.
            if (_ipris == 1 && !AccountForRealisation())
            {
                continue;
            }

            return;
        }
    }

    /// <summary>
    /// Takes surrounding particles until the budget is spent. Lines 487-716.
    /// </summary>
    private NeighbourOutcome SurroundBaseParticle()
    {
        while (true)
        {
            // label 501, lines 487-501. The shell grows on every neighbour, so coming
            // back here is not "try again in the same place".
            double x1;
            do
            {
                x1 = _streams.Spacing.Next();
            }
            while (x1 == 1.0);

            _state.Xss2 += x1;
            _state.Nfy++;

            var kcil = (-(Math.Log(1 - x1) * (1.0 / _slamd))) + _vp;
            _vp = (float)kcil;
            _rrl = (float)Math.Pow(3 * kcil / (4 * Pi), OneThird);

            // lines 503-518
            var x2 = _streams.NeighbourFraction.Next();
            var x21 = _streams.NeighbourPosition.Next();
            _db = _fractions.Diameter(x2, x21, ref _nfract);
            _state.Xss3 += x2;
            _state.Xss4 += x21;
            _state.Nfz++;

            // lines 520-526
            RecordOxidiserParticle(_db);
            _state.DokSur41 += Math.Pow(_db, 4.0);
            _state.DokSur31 += Math.Pow(_db, 3.0);

            var outcome = ClassifyNeighbour();
            if (outcome == NeighbourOutcome.RestartParticle)
            {
                return NeighbourOutcome.RestartParticle;
            }

            if (outcome == NeighbourOutcome.Next)
            {
                continue;
            }

            // label 550, lines 715-716: the budget, and the only exit that is not a
            // restart.
            //
            // ⚠ These two lines, not the AK3 fork, are where two builds of the ORIGINAL
            // part company - measured, see "Предел воспроизводимости" in BOOT.md. TU is a
            // REAL*4 accumulator taking ~70 of these decrements per realisation, and
            // `Db**2./rrL**2.` is written with REAL exponents (as is rrL's own
            // `**(1/3.)`), so an 80-bit intermediate reaches the REAL*4 store. Evaluated
            // in double instead, as here, TU drifts by tens of ulps over a realisation.
            // Late in one the shell is wide, so the decrement is small and the remaining
            // budget thin: on hp1050's particle 90870 the last comparison misses by
            // 2e-6 of one decrement and costs THREE extra neighbours, not one. C# has no
            // 80-bit type, so this is a limit of the port and not something to fix here.
            var decrement = Math.Pow(_db, 2.0) / Math.Pow(_rrl, 2.0);
            _tu = (float)(_tu - decrement);

            if (!(_tu > TuExitThreshold))
            {
                return NeighbourOutcome.Accounted;
            }
        }
    }

    /// <summary>
    /// One surrounding particle, from the first condition to the pocket/bridge split.
    /// Lines 528-711.
    /// </summary>
    private NeighbourOutcome ClassifyNeighbour()
    {
        // lines 528-536
        if (!_formsPockets[_nfract])
        {
            return NeighbourOutcome.Next;
        }

        if (_dr >= _dmax)
        {
            // ⚠ Line 529 tests Dr - the BASE particle - inside the loop over
            // surrounding ones, where line 533 below tests Db. That asymmetry is the
            // typo, and repairing it would add rejections the reference does not have.
            _state.Conditions[1]++;
            return NeighbourOutcome.Next;
        }

        if (_db <= _dmin)
        {
            _state.Conditions[2]++;
            return NeighbourOutcome.Next;
        }

        // lines 537-543
        _state.D41 += Math.Pow(_db, 4.0);
        _state.D31 += Math.Pow(_db, 3.0);
        _aa = (float)(_rrl - (_dr / 2.0) - (_db / 2.0));
        _idokLocalAll++;
        var bin = (int)(_db / _di) + 1;
        _state.VdokStr[bin] = (float)(_state.VdokStr[bin] + (Pi / 6 * Math.Pow(_db, 3.0)));
        _state.QdokStr[bin]++;

        // lines 547-555
        if (_dr < _ak1 * _db)
        {
            _state.Conditions[3]++;
            return _ivar == 0 ? NeighbourOutcome.RestartParticle : NeighbourOutcome.Next;
        }

        if (_dr > _ak2 * _db)
        {
            _state.Conditions[4]++;
            return NeighbourOutcome.Next;
        }

        // lines 560-564
        var larger = Math.Max((double)_dr, _db);
        var coefBin = (int)(((_aa / larger) + 1) * 100) + 1;
        if (coefBin <= RunState.NC)
        {
            _state.Coef[coefBin]++;
            if (coefBin > _state.CoefNmax)
            {
                _state.CoefNmax = coefBin;
            }
        }

        // lines 566-569: condition 5 restarts the realisation, it does not skip a
        // neighbour. See BOOT.md - the document had this wrong.
        if (_aa > larger * _ak4)
        {
            _state.Conditions[5]++;
            return NeighbourOutcome.RestartParticle;
        }

        // line 570
        //
        // ⚠ Do not re-open this comparison looking for the hp1050 pocket deficit. Four
        // rounding readings of it were instrumented and measured at N = 65000, and every
        // one disagrees with this line NOWHERE: the threshold single vs double, AA rounded
        // after each subtraction (2784161 values differ, 0 verdicts), both together, and
        // the pocket bin index. The reason is structural rather than lucky - each freedom
        // is at most 0.6 ulp of RRL while the narrowest gap in the whole pass is 1.63 ulp,
        // so nothing of rounding size can reach this fork. The divergence is in the TU
        // budget above; see BOOT.md, "Предел воспроизводимости".
        var ak3Threshold = larger * _ak3;

        if (_aa > ak3Threshold)
        {
            return RecordPocket();
        }

        // lines 572-576
        if (_aa <= 0.0f)
        {
            _rrl = (float)((_dr / 2.0) + (_db / 2.0));
            _aa = 0f;
            _jammedLoc++;
        }

        return RecordBridge();
    }

    /// <summary>The pocket branch: label 502, lines 671-711.</summary>
    private NeighbourOutcome RecordPocket()
    {
        _ipocketLoc++;
        var rk = _aa;
        if (rk > _state.DpMax)
        {
            _state.DpMax = rk;
        }

        if (rk > 0.0f)
        {
            // lines 675-679
            var bin = (int)(rk / _di) + 1;
            _state.Qks[bin]++;
            _state.Vks[bin] += Pi / 6 * ((double)rk * rk * rk);
            _state.Dp31 += Math.Pow(rk, 3.0);
            _state.Dp41 += Math.Pow(rk, 4.0);

            // line 680: the preparatory pass stops here.
            if (_ipris != 0)
            {
                RecordPocketCorrections(rk, bin);
            }
        }

        // label 333, lines 705-711: the conditional oxidiser distribution is filled on
        // the pocket branch only - the bridge branch jumps past it.
        var row = (int)(rk / _dj) + 1;
        _state.Qdok[row]++;
        var larger = Math.Max((double)_dr, _db);
        _state.Dokp41[row] = (float)(_state.Dokp41[row] + Math.Pow(larger, 4.0));
        _state.Dokp31[row] = (float)(_state.Dokp31[row] + Math.Pow(larger, 3.0));
        var column = (int)(larger / _di) + 1;
        var midpoint = _di * (column - .5);
        _state.Vdoks[row, column] += Pi / 6 * (midpoint * midpoint * midpoint);
        _state.Qdoks[row, column]++;

        return NeighbourOutcome.Accounted;
    }

    /// <summary>The two corrected pocket counts, lines 683-702.</summary>
    private void RecordPocketCorrections(float rk, int bin)
    {
        var fine = _state.PDokSmall[(int)(Math.Max((double)_dr, _db) / _di) + 1];

        // line 683
        if (fine >= 1.0f)
        {
            return;
        }

        if (rk > _state.DpMaxCor)
        {
            _state.DpMaxCor = rk;
        }

        _state.FmkarmCor[bin] = (float)(_state.FmkarmCor[bin] + (Pi / 6 * ((double)rk * rk * rk)));
        _state.FqkarmCor[bin]++;

        // lines 688-699
        var x4 = _streams.PocketAndCorrections.Next();
        _state.Xss6 += x4;
        _state.Nfw++;
        if (x4 < fine)
        {
            return;
        }

        if (rk > _state.DpMaxCor)
        {
            _state.DpMaxCor = rk;
        }

        _ipocketLocCor++;
        _state.Fmkarm2Local[bin] = (float)(_state.Fmkarm2Local[bin] + (Pi / 6 * ((double)rk * rk * rk)));
    }

    /// <summary>The bridge branch: lines 580-667.</summary>
    private NeighbourOutcome RecordBridge()
    {
        _ibridgeLoc++;

        // line 581
        if (_ipris == 0)
        {
            return NeighbourOutcome.Accounted;
        }

        // lines 582-585: the window is arithmetic on the base particle, so it stays
        // here; folding the histogram into an inverse function is Sampling's job.
        var mindk = (int)(_ak3 * _dr / _di) + 1;
        var maxdk = (int)(_ak4 * _dr / _di) + 1;
        if (!_pocketSizes.Rebuild(_state.Qks1, mindk, maxdk, _di))
        {
            // line 590: nothing in the window - the whole realisation goes.
            return NeighbourOutcome.RestartParticle;
        }

        float dkarm;
        float vmkm;
        float bb;
        BridgeOutcome built;
        do
        {
            // label 451, lines 612-625
            var x3 = _streams.PocketAndCorrections.Next();
            _state.Xss5 += x3;
            _state.Nfq++;
            dkarm = _pocketSizes.Draw(x3);
            built = BridgeVolume.Between(_dr / 2f, _db / 2f, dkarm / 2f, _aa, out vmkm, out bb);

            switch (built)
            {
                case BridgeOutcome.GapExceedsPocket:
                    _bridgeGapExceedsPocket++;
                    break;
                case BridgeOutcome.WidthNegative:
                    _bridgeWidthNegative++;
                    break;
                case BridgeOutcome.Built when float.IsNaN(vmkm):
                    _bridgeVolumeNaN++;
                    break;
            }
        }
        while (built != BridgeOutcome.Built);

        // lines 627-639
        var larger = Math.Max((double)_dr, _db);
        if (_aa > 0f)
        {
            var bin = (int)(_aa / larger * 1000) + 1;
            if (bin <= RunState.NC)
            {
                _state.Qmkm1[bin]++;
                if (bin > _state.Qmkm1Nmax)
                {
                    _state.Qmkm1Nmax = bin;
                }
            }
        }

        var widthBin = (int)(bb / larger * 100) + 1;
        if (widthBin <= RunState.NC)
        {
            _state.Qmkm2[widthBin]++;
            if (widthBin > _state.Qmkm2Nmax)
            {
                _state.Qmkm2Nmax = widthBin;
            }
        }

        // lines 642-650
        _vsmkm1 = (float)(_vsmkm1 + vmkm);
        _svd1 = (float)(_svd1 + (Pi / 6.0 * Math.Pow(_db, 3.0)));

        if (larger <= _state.Dmaxxx)
        {
            _ibridgeLocCor++;
            _vdokLoc = (float)(_vdokLoc + (Pi / 6.0 * Math.Pow(_db, 3.0)));
            _vmkmLoc = (float)(_vmkmLoc + vmkm);
        }

        // lines 652-666
        var x4 = _streams.PocketAndCorrections.Next();
        _state.Xss6 += x4;
        _state.Nfw++;
        if (x4 >= (double)_mkmcoef / _karmcoef * _state.PDokSmall[(int)(larger / _di) + 1])
        {
            _ibridgeLocCor2++;
            _vdokLoc2 = (float)(_vdokLoc2 + (Pi / 6.0 * Math.Pow(_db, 3.0)));
            _vmkmLoc2 = (float)(_vmkmLoc2 + vmkm);
        }

        return NeighbourOutcome.Accounted;
    }

    /// <summary>
    /// The end of a realisation in a working pass: lines 719-756.
    /// </summary>
    /// <returns><c>false</c> when the realisation is discarded and the particle redrawn.</returns>
    private bool AccountForRealisation()
    {
        // ⚠ line 720 stands ABOVE the four reset checks, so a discarded realisation
        // still leaves its pockets here. This is the only such leak, and it is the
        // reason fmkarm_cor2 and Dkarm43_cor(2) exist as separate quantities.
        for (var i = 1; i <= _state.Nkarm; i++)
        {
            _state.Fmkarm2[i] = (float)(_state.Fmkarm2[i] + _state.Fmkarm2Local[i]);
        }

        // lines 722-738. ⚠ `nn = real(ipocket_loc)/real(ibridge_loc)`: both operands are
        // narrowed by an explicit intrinsic before the division. Per realisation these
        // counts stay small enough for the narrowing to be exact, so this cannot move a
        // number today - it is written the original's way because nn decides conditions
        // 8 and 9, and a rule that only holds while the inputs stay small is not a rule.
        var nn = (float)((double)(float)_ipocketLoc / (float)_ibridgeLoc);
        if (_ipocketLoc == 0)
        {
            _state.Conditions[6]++;
            _ireset++;
        }

        if (_ibridgeLoc < 2)
        {
            _state.Conditions[7]++;
            _ireset++;
        }

        if (nn <= _nnMin)
        {
            _state.Conditions[8]++;
            _ireset++;
        }

        if (nn >= _nnMax)
        {
            _state.Conditions[9]++;
            _ireset++;
        }

        if (_ireset > 0)
        {
            return false;
        }

        // lines 741-755. ⚠ Line 741 is `real(jammed_loc)/real(ipocket_loc+ibridge_loc)`,
        // the same explicit narrowing of both operands as line 722. Note the sum is taken
        // in INTEGER first and only then converted.
        _state.JammedTotal = (float)(_state.JammedTotal
            + ((double)(float)_jammedLoc / (float)(_ipocketLoc + _ibridgeLoc)));
        _state.NnTotal = (float)(_state.NnTotal + nn);
        _state.IbridgeTotal += _ibridgeLoc;

        var bin = (int)(_dr / _di) + 1;
        _state.Vsmkm[bin] = (float)(_state.Vsmkm[bin] + _vsmkm1);
        _state.Svd[bin] = (float)(_state.Svd[bin]
            + (_alpha * (double)_svd1) + (Pi / 6.0 * Math.Pow(_dr, 3.0)));

        if (_ibridgeLocCor > 0)
        {
            _state.VmkmTotal[bin] = (float)(_state.VmkmTotal[bin] + _vmkmLoc);
            _state.VdokTotal[bin] = (float)(_state.VdokTotal[bin]
                + (_alpha * (double)_vdokLoc) + (Pi / 6.0 * Math.Pow(_dr, 3.0)));
        }

        if (_ibridgeLocCor2 > 0)
        {
            _state.VmkmTotal2 = (float)(_state.VmkmTotal2 + _vmkmLoc2);
            _state.VdokTotal2 = (float)(_state.VdokTotal2
                + (_alpha * (double)_vdokLoc2) + (Pi / 6.0 * Math.Pow(_dr, 3.0)));
        }

        return true;
    }

    /// <summary>
    /// Lines 717-718: QKSS and the normalised pocket histogram QKS1, recomputed after
    /// every base particle rather than once per pass.
    /// </summary>
    /// <remarks>
    /// ⚠ Before any pocket has been binned this divides by zero, and QKS1 fills with
    /// NaN exactly as the original's does. Guarding it would change which realisations
    /// the pocket-size draw can produce.
    /// </remarks>
    private void RenormalisePocketHistogram()
    {
        _state.Qkss = 0;
        for (var i = 1; i <= _state.Nkarm; i++)
        {
            _state.Qkss += _state.Qks[i];
        }

        for (var i = 1; i <= _state.Nkarm; i++)
        {
            // ⚠ line 718 is `QKS1 = QKS/real(QKSS)`, so BOTH operands reach the division
            // as REAL*4 - the total by the explicit intrinsic, the bin count because an
            // INTEGER meeting a REAL*4 converts to its kind. QKSS runs past 2^24 on every
            // full run (2.5e7 on hp1050), where a single no longer holds integers
            // exactly, so narrowing it is not a formality: it quantises the divisor by
            // about 1e-7 relative. Dividing by the exact total instead - what this line
            // used to do - is a different number.
            _state.Qks1[i] = (float)((double)(float)_state.Qks[i] / (float)_state.Qkss);
        }
    }

    /// <summary>Lines 465-469 and 520-524: one particle in whichever role.</summary>
    private void RecordOxidiserParticle(float diameter)
    {
        _state.AllDokFract[_nfract]++;
        _state.AllVDokFr[_nfract] = (float)(_state.AllVDokFr[_nfract]
            + (Pi / 6 * ((double)diameter * diameter * diameter)));
        var bin = (int)(diameter / _di) + 1;
        _state.AllDok[bin]++;
        _state.AllVDok[bin] = (float)(_state.AllVDok[bin]
            + (Pi / 6 * ((double)diameter * diameter * diameter)));
    }

    /// <summary>
    /// The fine-oxidiser model the next pass consults: lines 798-812 and 1001-1074.
    /// </summary>
    /// <remarks>
    /// ⚠ Rebuilt at the end of every pass and frozen for the duration of the next one.
    /// The preparatory pass exists to build it the first time; before that,
    /// <c>pdoksmall</c> is all zeros and <c>Dmaxxx</c> is zero, which is exactly why the
    /// preparatory pass skips every test that reads them.
    /// </remarks>
    private void RecomputeFineOxidiserModel()
    {
        var ndok = _state.Ndok;

        // Lines 798-809 used to sit here. They belong to SummariseOxidiser, which runs
        // first and leaves Fmdok and AllVDokSo ready for the gdokleft below.

        // lines 1001-1006
        var gdokleft = (float)((double)_state.Fmdok[(int)(_dmin / _di) + 1] * _ggg);
        var gdoksfr = 0f;
        for (var i = 1; i <= _state.FractionCount; i++)
        {
            if (!_formsPockets[i])
            {
                gdoksfr = (float)(gdoksfr + ((double)_configuration.Fractions[i - 1].MassFraction * _ggg));
            }
        }

        gdokleft = (float)((double)gdokleft + gdoksfr);

        // lines 1008-1016
        var plotsmdok = (float)((1.0 - (_ggg - (double)gdokleft))
            / ((1.0 / _plot2) - ((_ggg - (double)gdokleft) / _plot1)));
        var vdokleft = (float)((double)gdokleft / (1.0 - _ggg + gdokleft) * plotsmdok / _plot1);

        var mp = (float)(Pi / 6 * plotsmdok * _gm / (1 - _ggg + (double)gdokleft));
        mp = (float)(mp * (1.0 + (3.0 * 0.016 * _eta / ((2.0 * 0.027) + (3.0 * 0.016 * (1 - (double)_eta))))));
        mp = (float)(2.0 * Math.Pow(0.75 / Pi * mp * (((1 - (double)_eta) / 2000.0) + (_eta / 3000.0)), 0.3333));

        Gdokleft = gdokleft;
        Plotsmdok = plotsmdok;
        Vdokleft = vdokleft;
        DaCoefficient = mp;

        // lines 1022-1043
        var vdokstrSum = 0f;
        for (var i = 1; i <= ndok; i++)
        {
            vdokstrSum = (float)(vdokstrSum + _state.VdokStr[i]);
        }

        Array.Clear(_state.DDokSmall);
        Array.Clear(_state.ZDokSmall);
        for (var i = 1; i <= ndok; i++)
        {
            var xr = (float)((double)FortranMod(i, _ak2) / _ak2);
            var whole = (int)((double)i / _ak2);

            // line 1026: the whole-part loop is skipped below one full AK2 step.
            if ((float)((double)i / _ak2) >= 1)
            {
                for (var k = 1; k <= whole; k++)
                {
                    _state.ZDokSmall[i] = (float)(_state.ZDokSmall[i] + ((double)_state.VdokStr[k] / vdokstrSum));
                }
            }

            _state.ZDokSmall[i] = (float)(_state.ZDokSmall[i]
                + ((double)_state.VdokStr[whole + 1] / vdokstrSum * xr));

            if (_state.ZDokSmall[i] > 1e-5f)
            {
                for (var k = 1; k <= whole; k++)
                {
                    _state.DDokSmall[i] = (float)(_state.DDokSmall[i]
                        + ((double)_state.VdokStr[k] / vdokstrSum / _state.ZDokSmall[i] * _di * (k - 0.5)));
                }

                _state.DDokSmall[i] = (float)(_state.DDokSmall[i]
                    + ((double)_state.VdokStr[whole + 1] * xr / vdokstrSum / _state.ZDokSmall[i]
                       * _di * (whole + (0.5 * xr))));
            }
        }

        // lines 1047-1057
        for (var i = 1; i <= ndok; i++)
        {
            _state.ZDokSmall[i] = (float)((double)_state.ZDokSmall[i] * _ggg);
        }

        for (var i = 1; i <= ndok; i++)
        {
            var pl = (float)((1.0 - (_ggg - (double)gdokleft - _state.ZDokSmall[i]))
                / ((1.0 / _plot2) - ((_ggg - (double)gdokleft - _state.ZDokSmall[i]) / _plot1)));
            _state.VDokSmall[i] = (float)((double)_state.ZDokSmall[i]
                / (1 - _ggg + gdokleft + _state.ZDokSmall[i]) * pl / _plot1);
            _state.DDokSmall[i] = (float)((double)_state.DDokSmall[i] / (_di * (double)i));
        }

        // lines 1059-1065: the histogram is forced non-decreasing.
        for (var i = 2; i <= ndok; i++)
        {
            _state.PDokSmall[i] = (float)((double)_state.VDokSmall[i] * _state.DDokSmall[i] * _karmcoef);
            if (_state.PDokSmall[i] < _state.PDokSmall[i - 1])
            {
                _state.PDokSmall[i] = _state.PDokSmall[i - 1];
            }
        }

        // lines 1066-1074
        _state.Dmaxxx = _ddokmax;
        for (var i = 1; i <= ndok; i++)
        {
            if ((float)((double)_mkmcoef / _karmcoef * _state.PDokSmall[i]) >= 1.0f)
            {
                _state.Dmaxxx = (float)((double)_di * i);
                _state.ZDmaxxx = (float)(1 - (double)_state.Fmdok[i + 1]);
                break;
            }
        }
    }

    /// <summary>Fortran's <c>mod</c> on reals: the remainder with the sign of the dividend.</summary>
    private static float FortranMod(float a, float p) => (float)(a - ((int)((double)a / p) * (double)p));

    internal float Gdokleft { get; private set; }

    internal float Plotsmdok { get; private set; }

    internal float Vdokleft { get; private set; }

    /// <summary>mp of lines 1014-1016: the factor turning a pocket size into an agglomerate one.</summary>
    internal float DaCoefficient { get; private set; }

    /// <summary>Lines 380-405: the recipe's mean size, computed off the bounds, not the draws.</summary>
    private static (float Mean, float StandardDeviation) MeanOxidiserSize(
        SizeDistributionLaw law,
        ReadOnlySpan<float> massFractions,
        ReadOnlySpan<float> bounds,
        ReadOnlySpan<double> numberFractions)
    {
        var dokm = 0f;
        var doksd = 0f;

        if (law == SizeDistributionLaw.Surface)
        {
            for (var i = 0; i < massFractions.Length; i++)
            {
                var lower = bounds[2 * i];
                var upper = bounds[(2 * i) + 1];
                dokm = (float)(dokm + ((lower + (double)upper) * massFractions[i] / 2));
                doksd = (float)(doksd + (massFractions[i]
                    * (((double)lower * lower) + ((double)lower * upper) + ((double)upper * upper)) / 3));
            }
        }
        else
        {
            var dok4 = 0f;
            var dok3 = 0f;
            for (var i = 0; i < massFractions.Length; i++)
            {
                var lower = bounds[2 * i];
                var upper = bounds[(2 * i) + 1];

                // ⚠ The volume law weights by the number fraction ZX, not the mass one -
                // and it still uses the mass one for the deviation two lines down.
                var zx = numberFractions[i];
                dok4 = (float)(dok4 + ((Math.Pow(upper, 5.0) - Math.Pow(lower, 5.0)) * zx / (upper - (double)lower)));
                dok3 = (float)(dok3 + ((Math.Pow(upper, 4.0) - Math.Pow(lower, 4.0)) * zx / (upper - (double)lower)));
                doksd = (float)(doksd + (massFractions[i]
                    * (((double)lower * lower) + ((double)lower * upper) + ((double)upper * upper)) / 3.0));
            }

            dokm = (float)(0.8 * dok4 / dok3);
        }

        doksd = (float)(doksd - ((double)dokm * dokm));
        return (dokm, doksd);
    }
}
