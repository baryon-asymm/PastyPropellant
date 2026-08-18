namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// Every accumulator the original carries across a run.
/// </summary>
/// <remarks>
/// <para>
/// One object per run; there is no parallelism inside a run, so nothing here is
/// synchronised. The fields keep the original's names, because the only way to check
/// this file is to read it beside <c>PropStructv3.for</c>.
/// </para>
/// <para>
/// Element types are the original's, not the ones that would be convenient:
/// <c>REAL*8</c> becomes <see cref="double"/>, <c>REAL</c> becomes <see cref="float"/>,
/// <c>INTEGER*8</c> becomes <see cref="long"/>. Widening a <c>REAL</c> accumulator to
/// double would be a different model, not a more accurate one.
/// </para>
/// <para>
/// ⚠ Arrays are allocated one element longer than the original and indexed from 1, so
/// that index expressions can be read straight off the listing. Element 0 exists and
/// is never used. The same convention holds in <c>Sampling/</c>.
/// </para>
/// <para>
/// ⚠ Nothing here is re-zeroed between passes. That is the original's behaviour
/// (label 700 sits below all initialisation), and it is load-bearing: the working pass
/// continues the preliminary one. See <c>BOOT.md</c>, «Накопители обнуляются один раз».
/// </para>
/// </remarks>
internal sealed class RunState
{
    internal RunState(int fractionCount, int ndok, int nkarm, int ncat, int workingPasses)
    {
        FractionCount = fractionCount;
        Ndok = ndok;
        Nkarm = nkarm;
        Ncat = ncat;

        // par1..par6 of line 140: one entry per working pass, and the only arrays here
        // that the original allocates over KXX rather than over a grid. They stay empty
        // at KXX = 1, which is what the report does with them.
        var history = workingPasses > 1 ? workingPasses : 0;
        PassPocketDistribution = new float[history];
        PassPocketMoment3 = new float[history];
        PassPocketMoment4 = new float[history];
        PassOxidiserMean = new float[history];
        PassOxidiserVariance = new float[history];
        PassPocketMassFraction = new float[history];

        AllDokFract = new long[fractionCount + 1];
        EpsAllDok = new float[fractionCount + 1];

        AllDok = new long[ndok + 1];
        AllVDok = new float[ndok + 1];
        AllVDokSo = new float[ndok + 1];
        AllVDokFr = new float[ndok + 1];
        Fmdok = new float[ndok + 2];
        Vsmkm = new float[ndok + 1];
        Svd = new float[ndok + 1];
        VdokTotal = new float[ndok + 1];
        VmkmTotal = new float[ndok + 1];
        VdokStr = new float[ndok + 1];
        QdokStr = new long[ndok + 1];
        ZDokSmall = new float[ndok + 1];
        VDokSmall = new float[ndok + 1];
        DDokSmall = new float[ndok + 1];
        PDokSmall = new float[ndok + 1];
        Gk0 = new float[ndok + 1];
        Gk1 = new float[ndok + 1];

        Vkso = new float[nkarm + 1];
        Qks1 = new float[nkarm + 1];
        Fqks = new float[nkarm + 2];
        Dpoc = new float[nkarm + 2];
        Qks = new long[nkarm + 1];
        Vks = new double[nkarm + 1];
        FmkarmCor = new float[nkarm + 1];
        FqkarmCor = new long[nkarm + 1];
        Fmkarm2 = new float[nkarm + 1];
        Fmkarm2Local = new float[nkarm + 1];

        Qmkm1 = new long[NC + 1];
        Qmkm2 = new long[NC + 1];
        Coef = new long[NC + 1];

        Qdok = new long[ncat + 1];
        Dokp31 = new float[ncat + 1];
        Dokp41 = new float[ncat + 1];
        Dpockets = new float[ncat + 1];
        Vdoks = new double[ncat + 1, ndok + 1];
        Qdoks = new long[ncat + 1, ndok + 1];
    }

    /// <summary>Nc of line 277: the fixed width of the three ratio histograms.</summary>
    internal const int NC = 1000;

    internal int FractionCount { get; }

    internal int Ndok { get; }

    internal int Nkarm { get; }

    internal int Ncat { get; }

    // --- draw counters and sums, lines 461-463, 497-498, 516-518, 621-622, 661-662, 697-698
    internal double Xss0 { get; set; }

    internal double Xss1 { get; set; }

    internal double Xss2 { get; set; }

    internal double Xss3 { get; set; }

    internal double Xss4 { get; set; }

    internal double Xss5 { get; set; }

    internal double Xss6 { get; set; }

    internal long Nfx { get; set; }

    internal long Nfy { get; set; }

    internal long Nfz { get; set; }

    internal long Nfq { get; set; }

    internal long Nfw { get; set; }

    // --- moments of the base and surrounding particles, lines 470-471, 482-483, 525-526, 537-538
    internal double DokBase41 { get; set; }

    internal double DokBase31 { get; set; }

    internal double DokSur41 { get; set; }

    internal double DokSur31 { get; set; }

    internal double Sd4 { get; set; }

    internal double Sd3 { get; set; }

    internal double D41 { get; set; }

    internal double D31 { get; set; }

    // --- pockets, lines 673-679
    internal double Dp41 { get; set; }

    internal double Dp31 { get; set; }

    internal float DpMax { get; set; }

    internal float DpMaxCor { get; set; }

    // --- local structure, lines 741-755
    internal float JammedTotal { get; set; }

    internal float NnTotal { get; set; }

    internal int IbridgeTotal { get; set; }

    internal float VmkmTotal2 { get; set; }

    internal float VdokTotal2 { get; set; }

    /// <summary>QKSS of line 717: the running count of binned pockets.</summary>
    internal long Qkss { get; set; }

    /// <summary>
    /// The nine condition counters, indexed 1..9 as in the original's <c>conditions(10)</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ Conditions 1–5 are counted in every pass, conditions 6–9 only in working ones
    /// (the guard on line 719). That is why the printed denominators differ, and neither
    /// of them is wrong. See <c>BOOT.md</c>.
    /// </remarks>
    internal long[] Conditions { get; } = new long[11];

    /// <summary>
    /// FI of line 1154: base particles completed in the working passes so far. Set after
    /// the first working pass, then grown by N per further pass (line 1179).
    /// </summary>
    /// <remarks>
    /// ⚠ The original never zeroes it — it is zero only because it lives in static
    /// storage. Initialising it is the port's job, not an embellishment.
    /// </remarks>
    internal long Fi { get; set; }

    /// <summary>Largest particle that still counts as fine, line 1066; zero until the first pass ends.</summary>
    internal float Dmaxxx { get; set; }

    internal float ZDmaxxx { get; set; }

    internal long[] AllDokFract { get; }

    internal float[] EpsAllDok { get; }

    internal long[] AllDok { get; }

    internal float[] AllVDok { get; }

    internal float[] AllVDokSo { get; }

    internal float[] AllVDokFr { get; }

    internal float[] Fmdok { get; }

    internal float[] Vsmkm { get; }

    internal float[] Svd { get; }

    internal float[] VdokTotal { get; }

    internal float[] VmkmTotal { get; }

    internal float[] VdokStr { get; }

    internal long[] QdokStr { get; }

    internal float[] ZDokSmall { get; }

    internal float[] VDokSmall { get; }

    internal float[] DDokSmall { get; }

    internal float[] PDokSmall { get; }

    internal float[] Gk0 { get; }

    internal float[] Gk1 { get; }

    internal float[] Vkso { get; }

    internal float[] Qks1 { get; }

    /// <summary><c>par1</c>: EPSY after each working pass — <c>epsfkarm_n</c>.</summary>
    internal float[] PassPocketDistribution { get; }

    /// <summary><c>par2</c>: epsMD3 after each working pass — <c>epsm3karm_n</c>.</summary>
    internal float[] PassPocketMoment3 { get; }

    /// <summary><c>par3</c>: epsMD4 after each working pass — <c>epsm4karm_n</c>.</summary>
    internal float[] PassPocketMoment4 { get; }

    /// <summary><c>par4</c>: signed <c>epsdok43_n</c>.</summary>
    internal float[] PassOxidiserMean { get; }

    /// <summary><c>par5</c>: signed <c>epsdoksd_n</c>, over variances.</summary>
    internal float[] PassOxidiserVariance { get; }

    /// <summary><c>par6</c>: <c>1 - DolM2</c> after each working pass — <c>epszkarm_n</c>.</summary>
    internal float[] PassPocketMassFraction { get; }

    internal float[] Fqks { get; }

    internal float[] Dpoc { get; }

    internal long[] Qks { get; }

    internal double[] Vks { get; }

    internal float[] FmkarmCor { get; }

    internal long[] FqkarmCor { get; }

    internal float[] Fmkarm2 { get; }

    /// <summary>
    /// fmkarm2_loc: the one local buffer that reaches the result even when the
    /// realisation is discarded (line 720 sits above the reset checks).
    /// </summary>
    internal float[] Fmkarm2Local { get; }

    internal long[] Qmkm1 { get; }

    internal long[] Qmkm2 { get; }

    internal long[] Coef { get; }

    internal int Qmkm1Nmax { get; set; }

    internal int Qmkm2Nmax { get; set; }

    internal int CoefNmax { get; set; }

    internal long[] Qdok { get; }

    internal float[] Dokp31 { get; }

    internal float[] Dokp41 { get; }

    internal float[] Dpockets { get; }

    internal double[,] Vdoks { get; }

    internal long[,] Qdoks { get; }
}
