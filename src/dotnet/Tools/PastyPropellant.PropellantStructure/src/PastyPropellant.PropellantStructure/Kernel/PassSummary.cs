namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// Everything the original derives from the accumulators once a pass has ended —
/// lines 770-996 and 1086-1152.
/// </summary>
/// <remarks>
/// <para>
/// Names are the original's, because every one of these ends up printed under that
/// name and the report is the comparison target. Types follow the original's own
/// declarations rather than what the arithmetic looks like it needs: <c>MD3</c>,
/// <c>MD4</c>, <c>DD3</c>, <c>DD4</c> and <c>VVKS</c> are REAL*8 there and are
/// <see cref="double"/> here, while their neighbours are REAL*4 by implicit typing.
/// </para>
/// <para>
/// ⚠ A summary is recomputed from scratch after <b>every</b> pass, including the
/// preparatory one, and the run reports the last. It is not cumulative — but the
/// accumulators it reads are, and two of them it also mutates in place (see
/// <c>Vdok_total</c> in the corrections block).
/// </para>
/// </remarks>
internal sealed class PassSummary
{
    internal PassSummary(int ncat, int ndok, int nkarm)
    {
        Qdokss = new long[ncat + 1];
        Qdoks1 = new float[ncat + 1, ndok + 1];
        Qdokso = new float[ncat + 1, ndok + 1];
        Vdokso = new float[ncat + 1, ndok + 1];
        Mdok3 = new double[ncat + 1];
        Mdok4 = new double[ncat + 1];
        Ddok3 = new double[ncat + 1];
        Ddok4 = new double[ncat + 1];
        Dokp431 = new float[ncat + 1];
        Dokp432 = new float[ncat + 1];
        Dokp43 = new float[ncat + 1];
        Epsydok = new float[ncat + 1];
        Epsmdok3 = new float[ncat + 1];
        Epsmdok4 = new float[ncat + 1];
        Vvdoks = new double[ncat + 1];
        Qdokkarm = new float[ncat + 1];
        Nkarm = nkarm;
    }

    // --- Generator quality, lines 770-784. -------------------------------------

    /// <summary>Mean of each stream's draws; 0.5 is the ideal.</summary>
    /// <remarks>
    /// ⚠ <see cref="float"/>, not <see cref="double"/>, and that is not a detail.
    /// <c>XSR0..XSR6</c> are the only variables in this arithmetic the original never
    /// declares, and it has no <c>IMPLICIT</c> statement — so they are REAL*4 by the
    /// default rule for names beginning with X, and the REAL*8 quotient
    /// <c>XSS/NF</c> is rounded on the way in. Keeping the quotient in double moves
    /// three of the six printed <c>epsx</c> values by about half a single-precision
    /// ulp at 0.5, which is what caught it.
    /// </remarks>
    internal float[] Xsr { get; } = new float[7];

    /// <summary>
    /// Relative departure of each stream's mean from 0.5 — <c>EPS1..EPS7</c>.
    /// </summary>
    /// <remarks>
    /// ⚠ Seven are computed and six are printed: the report's <c>epsx(5)</c> is
    /// <c>EPS6</c> and its <c>epsx(6)</c> is <c>EPS7</c>, so <c>EPS5</c> — the second
    /// stream drawn against <c>NFZ</c> — appears nowhere. Kept anyway, because
    /// dropping it would renumber the rest and turn a reader's reasonable assumption
    /// into a wrong one.
    /// </remarks>
    internal float[] Eps { get; } = new float[8];

    // --- Oxidiser particle sizes, lines 789-819. -------------------------------

    /// <summary><c>DOK43b</c>: mass-mean size over base particles only.</summary>
    internal float Dok43b { get; set; }

    /// <summary><c>DOK43s</c>: mass-mean size over surrounding particles only.</summary>
    internal float Dok43s { get; set; }

    /// <summary><c>ALLDOKQ</c>: how many particles of any role were drawn.</summary>
    internal long Alldokq { get; set; }

    /// <summary><c>ALLVDOKS</c>: their total volume.</summary>
    internal float Allvdoks { get; set; }

    /// <summary><c>ALLDOK432</c> and <c>ALLDOK243</c>: first two moments of the mass distribution.</summary>
    internal float Alldok432 { get; set; }

    /// <inheritdoc cref="Alldok432"/>
    internal float Alldok243 { get; set; }

    /// <summary><c>ALLDOK43</c>: mass-mean size over every particle.</summary>
    internal float Alldok43 { get; set; }

    /// <summary>
    /// <c>ALLDOKsd</c>: the variance of that distribution — <b>not</b> its standard
    /// deviation.
    /// </summary>
    /// <remarks>
    /// ⚠ Line 812 assigns <c>ALLDOK243 - ALLDOK432**2</c>, and the report prints
    /// <c>DOKSD**0.5</c>. The name says deviation, the value is a variance, and the
    /// square root happens at the print statement. Reproduced as written.
    /// </remarks>
    internal float Alldoksd { get; set; }

    /// <summary>Relative error of the reproduced fraction shares, per fraction.</summary>
    internal float[] Epsalldok { get; set; } = [];

    /// <summary><c>EPSX1</c>: base-particle mean against the analytic one.</summary>
    internal float Epsx1 { get; set; }

    /// <summary><c>EPSX2</c>: the same for surrounding particles.</summary>
    internal float Epsx2 { get; set; }

    /// <summary><c>EPSX3</c>: the same over every particle.</summary>
    internal float Epsx3 { get; set; }

    // --- Oxidiser resolved by pocket size, lines 825-959. ----------------------

    /// <summary>
    /// <c>DPRow</c>: how many pocket-size categories survived the merge.
    /// </summary>
    internal int Dprow { get; set; }

    /// <summary><c>Dpockets</c>: the upper bound of each surviving category.</summary>
    internal float[] Dpockets { get; set; } = [];

    /// <summary><c>QDOKSS</c>: particles counted in each category.</summary>
    internal long[] Qdokss { get; }

    /// <summary><c>QDOKS1</c> and <c>QDOKSO</c>: the category's size distribution by number.</summary>
    internal float[,] Qdoks1 { get; }

    /// <inheritdoc cref="Qdoks1"/>
    internal float[,] Qdokso { get; }

    /// <summary><c>VDOKSO</c>: the same by volume.</summary>
    internal float[,] Vdokso { get; }

    /// <summary><c>MDOK3</c>, <c>MDOK4</c>: third and fourth moments per category.</summary>
    internal double[] Mdok3 { get; }

    /// <inheritdoc cref="Mdok3"/>
    internal double[] Mdok4 { get; }

    /// <summary><c>DDOK3</c>, <c>DDOK4</c>: their variances.</summary>
    internal double[] Ddok3 { get; }

    /// <inheritdoc cref="Ddok3"/>
    internal double[] Ddok4 { get; }

    /// <summary><c>DOKP431</c>, <c>DOKP432</c>, <c>DOKP43</c>: three mean sizes per category.</summary>
    internal float[] Dokp431 { get; }

    /// <inheritdoc cref="Dokp431"/>
    internal float[] Dokp432 { get; }

    /// <inheritdoc cref="Dokp431"/>
    internal float[] Dokp43 { get; }

    /// <summary><c>Epsydok</c>: the accuracy that drives the merge.</summary>
    internal float[] Epsydok { get; }

    /// <summary><c>Epsmdok3</c>, <c>Epsmdok4</c>: accuracy of each moment.</summary>
    internal float[] Epsmdok3 { get; }

    /// <inheritdoc cref="Epsmdok3"/>
    internal float[] Epsmdok4 { get; }

    /// <summary><c>VVDOKS</c>: category volume.</summary>
    internal double[] Vvdoks { get; }

    /// <summary><c>qdokkarm</c>: mean particle size per category, by number.</summary>
    internal float[] Qdokkarm { get; }

    // --- Pocket size distribution, lines 963-996. ------------------------------

    /// <summary><c>MD3</c>, <c>MD4</c>: moments of the pocket distribution.</summary>
    internal double Md3 { get; set; }

    /// <inheritdoc cref="Md3"/>
    internal double Md4 { get; set; }

    /// <summary><c>DD3</c>, <c>DD4</c>: their variances.</summary>
    internal double Dd3 { get; set; }

    /// <inheritdoc cref="Dd3"/>
    internal double Dd4 { get; set; }

    /// <summary><c>EPSY</c>: accuracy of the pocket distribution.</summary>
    internal float Epsy { get; set; }

    /// <summary><c>D43</c>: mass-mean pocket size from the moments.</summary>
    internal float D43 { get; set; }

    /// <summary><c>epsMD3</c>, <c>epsMD4</c>: accuracy of each moment.</summary>
    internal float EpsMd3 { get; set; }

    /// <inheritdoc cref="EpsMd3"/>
    internal float EpsMd4 { get; set; }

    /// <summary><c>VVKS</c>: total pocket volume.</summary>
    internal double Vvks { get; set; }

    /// <summary><c>D432</c>, <c>D243</c>: first two moments of the pocket mass distribution.</summary>
    internal float D432 { get; set; }

    /// <inheritdoc cref="D432"/>
    internal float D243 { get; set; }

    /// <summary><c>Dqkarm</c>: mean pocket size by number.</summary>
    internal float Dqkarm { get; set; }

    /// <summary><c>DP43</c>: mass-mean pocket size from the running sums.</summary>
    internal float Dp43 { get; set; }

    /// <summary><c>sdevP43</c>: standard deviation of the pocket mass distribution.</summary>
    internal float SdevP43 { get; set; }

    /// <summary>How many pocket histogram cells the run allocated.</summary>
    internal int Nkarm { get; }

    // --- Corrections, lines 1086-1152. ----------------------------------------

    /// <summary><c>Dkarm43_cor</c>, <c>Dkarm243_cor</c>: the same moments under correction 1.</summary>
    internal float Dkarm43Cor { get; set; }

    /// <inheritdoc cref="Dkarm43Cor"/>
    internal float Dkarm243Cor { get; set; }

    /// <summary><c>Dqkarm_cor</c>: mean pocket size by number, under correction 1.</summary>
    internal float DqkarmCor { get; set; }

    /// <summary><c>sdevP43_cor</c>: its standard deviation.</summary>
    internal float SdevP43Cor { get; set; }

    /// <summary><c>Dfmk432</c>, <c>Dfmk243</c>: the same moments under correction 2.</summary>
    internal float Dfmk432 { get; set; }

    /// <inheritdoc cref="Dfmk432"/>
    internal float Dfmk243 { get; set; }

    /// <summary><c>sdevP243</c>: its standard deviation.</summary>
    internal float SdevP243 { get; set; }

    /// <summary><c>Dqmkm1</c>: mean bridge size between oxidiser particles, relative.</summary>
    internal float Dqmkm1 { get; set; }

    /// <summary><c>Dqmkm2</c>: mean bridge size between pockets, relative.</summary>
    internal float Dqmkm2 { get; set; }

    /// <summary><c>qmcoef</c>: mean gap coefficient <c>(Lij/Ddok)+1</c>.</summary>
    internal float Qmcoef { get; set; }

    /// <summary><c>PLOTsm</c>: density of the binder-metal composition.</summary>
    internal float Plotsm { get; set; }

    /// <summary><c>gk0</c>, <c>gk1</c>: pocket share resolved by base-particle size.</summary>
    internal float[] Gk0 { get; set; } = [];

    /// <inheritdoc cref="Gk0"/>
    internal float[] Gk1 { get; set; } = [];

    /// <summary>
    /// <c>DolM1</c>, <c>DolM2</c>, <c>DolM3</c>: the complement of the pocket mass
    /// fraction under each of the three variants.
    /// </summary>
    /// <remarks>
    /// The report prints <c>1 - DolM</c>, not these. <c>Zkarm</c> is
    /// <c>1 - DolM1</c>, and <c>Zkarm_cor</c> is the pair <c>1 - DolM2</c>,
    /// <c>1 - DolM3</c>.
    /// </remarks>
    internal float DolM1 { get; set; }

    /// <inheritdoc cref="DolM1"/>
    internal float DolM2 { get; set; }

    /// <inheritdoc cref="DolM1"/>
    internal float DolM3 { get; set; }

    /// <summary><c>deltaVdok</c>: relative gap between two ways of totalling oxidiser volume.</summary>
    internal float DeltaVdok { get; set; }
}
