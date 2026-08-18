namespace PastyPropellant.PropellantStructure.Kernel;

/// <summary>
/// The processing the original does once a pass has ended — lines 770-996 and
/// 1086-1152 — turning the accumulators into the numbers the report prints.
/// </summary>
/// <remarks>
/// <para>
/// It runs after <b>every</b> pass, preparatory one included, and the run reports
/// what the last pass left. That is why the code below reads like it recomputes
/// everything from nothing: it does, from accumulators that were never reset.
/// </para>
/// <para>
/// ⚠ Two parts of it are not pure. The category merge rewrites <c>QDOKS</c>,
/// <c>VDOKS</c>, <c>DOKP41</c>, <c>DOKP31</c> and <c>QDOK</c> in place, and the
/// corrections divide <c>Vdok_total</c> and <c>Vdok_total2</c> in place — so on a
/// second working pass the merge starts from already-merged categories and the
/// division is applied a second time. Both are the original's behaviour and neither
/// is a rounding detail: they change what a multi-cycle run prints.
/// </para>
/// </remarks>
internal sealed partial class StructureSimulation
{
    private PassSummary Summarise()
    {
        var summary = new PassSummary(_state.Ncat, _state.Ndok, _state.Nkarm);

        SummariseGeneratorQuality(summary);
        SummariseOxidiser(summary);
        SummariseOxidiserByPocketSize(summary);
        SummarisePockets(summary);
        RecomputeFineOxidiserModel();
        SummariseCorrections(summary);

        return summary;
    }

    /// <summary>Lines 770-784: how far each stream's mean sits from one half.</summary>
    private void SummariseGeneratorQuality(PassSummary summary)
    {
        // The pairing of stream to counter is the original's and is not one-to-one:
        // streams 2 and 3 are both drawn against NFZ, which is why seven departures
        // come out of five counters.
        Span<double> sums =
        [
            _state.Xss0, _state.Xss1, _state.Xss2, _state.Xss3, _state.Xss4, _state.Xss5, _state.Xss6,
        ];
        Span<long> counts =
        [
            _state.Nfx, _state.Nfx, _state.Nfy, _state.Nfz, _state.Nfz, _state.Nfq, _state.Nfw,
        ];

        for (var i = 0; i < 7; i++)
        {
            summary.Xsr[i] = (float)(sums[i] / counts[i]);
            summary.Eps[i + 1] = (float)Math.Abs(((double)summary.Xsr[i] - 0.5) / 0.5);
        }
    }

    /// <summary>Lines 789-819: sizes of the oxidiser particles the run drew.</summary>
    private void SummariseOxidiser(PassSummary summary)
    {
        var ndok = _state.Ndok;

        summary.Dok43b = (float)(_state.Sd4 / _state.Sd3);
        summary.Dok43s = (float)(_state.D41 / _state.D31);

        // lines 792-796. ⚠ ALLDOK is allocated over Ndok (line 281) — the size
        // histogram — while ALLDOK_FRACT next to it is allocated over NMM, the
        // fractions. The sum is therefore over every size cell, and summing only the
        // first NMM of them makes the denominator far too small: caught by
        // epsdokfr[0] coming out 23000x the oracle.
        var alldokq = 0L;
        for (var i = 1; i <= ndok; i++)
        {
            alldokq += _state.AllDok[i];
        }

        summary.Alldokq = alldokq;
        summary.Epsalldok = new float[_state.FractionCount + 1];
        for (var i = 1; i <= _state.FractionCount; i++)
        {
            // ⚠ Both operands are rounded to single before the division, and neither
            // rounding is the subtree's usual "evaluate in double, round on assignment".
            // The numerator is narrowed by an explicit REAL() intrinsic; the denominator
            // is INTEGER*8 meeting a REAL*4, so Fortran converts it to the other
            // operand's kind. On this run both counts run to 1e8, well past the 2^24
            // where a single stops holding integers exactly, so the quantisation is
            // real: about 1e-7 relative. That would be invisible anywhere else, but the
            // deviation this line goes on to measure is itself only 4e-6, so single
            // precision moves the printed number by a per cent.
            var share = (float)((double)(float)_state.AllDokFract[i] / (float)alldokq);
            var expected = _fractions.Probabilities[i - 1];
            summary.Epsalldok[i] = (float)(Math.Abs(share - expected) / expected);
            _state.EpsAllDok[i] = summary.Epsalldok[i];
        }

        // lines 798-809: the mass density and its running integral. Both feed the
        // fine-oxidiser model below, so this has to precede it.
        var allVDokSum = 0f;
        for (var i = 1; i <= ndok; i++)
        {
            allVDokSum = (float)(allVDokSum + _state.AllVDok[i]);
        }

        summary.Allvdoks = allVDokSum;

        var alldok432 = 0f;
        var alldok243 = 0f;
        _state.Fmdok[1] = 0f;
        for (var i = 1; i <= ndok; i++)
        {
            _state.AllVDokSo[i] = (float)((double)_state.AllVDok[i] / allVDokSum);
            // The two associations are the original's, and they differ from each other:
            // line 805 reads ALLVDOKSO*(kilo-0.5)*Di, left to right, while line 806 reads
            // ALLVDOKSO*((kilo-0.5)*Di)**2 - the cell centre squared first, the share
            // multiplying the square. Writing the second one as (share*centre)*centre
            // moves nothing on any fixture in the tree, so this is transcription rather
            // than a fix; it is written this way so the line can be read against the
            // listing. ⚠ ALLDOK243 does have a quantity downstream sharp enough to feel a
            // last bit - see PassConvergence and ProbeRun3.
            var centre = (i - 0.5) * (double)_di;
            alldok432 = (float)(alldok432 + ((double)_state.AllVDokSo[i] * (i - 0.5) * _di));
            alldok243 = (float)(alldok243 + ((double)_state.AllVDokSo[i] * (centre * centre)));
            _state.Fmdok[i + 1] = (float)((double)_state.Fmdok[i] + _state.AllVDokSo[i]);
        }

        summary.Alldok432 = alldok432;
        summary.Alldok243 = alldok243;
        summary.Alldok43 = (float)((_state.DokBase41 + _state.DokSur41) / (_state.DokBase31 + _state.DokSur31));
        summary.Alldoksd = (float)((double)alldok243 - ((double)alldok432 * alldok432));

        // lines 814-819
        summary.Epsx1 = (float)Math.Abs((DokM - (_state.DokBase41 / _state.DokBase31)) / DokM);
        summary.Epsx2 = (float)Math.Abs((DokM - (_state.DokSur41 / _state.DokSur31)) / DokM);
        summary.Epsx3 = (float)Math.Abs(((double)DokM - summary.Alldok43) / DokM);
    }

    /// <summary>
    /// Lines 825-959: the oxidiser size distribution conditioned on pocket size, and
    /// the merge that widens a category until it is accurate enough.
    /// </summary>
    /// <remarks>
    /// ⚠ The merge is destructive and cumulative. It folds neighbouring categories of
    /// <c>QDOKS</c>/<c>VDOKS</c> together in place, so a category boundary lost on one
    /// pass is lost for the rest of the run — the next pass keeps accumulating into
    /// the merged bins and merges those further. Rebuilding the categories per pass
    /// would be a repair, not a port.
    /// </remarks>
    private void SummariseOxidiserByPocketSize(PassSummary summary)
    {
        var ndok = _state.Ndok;
        var epsDok = (float)_configuration.Coefficients.Accuracy;

        var dprow = (int)((double)_state.DpMax / _dj) + 1;
        for (var irow = 1; irow <= dprow; irow++)
        {
            _state.Dpockets[irow] = (float)(irow * (double)_dj);
        }

        while (true)
        {
            // label 600: every derived array starts again from nothing, over its whole
            // allocation and not merely over the surviving categories.
            Array.Clear(summary.Mdok3);
            Array.Clear(summary.Mdok4);
            Array.Clear(summary.Ddok3);
            Array.Clear(summary.Ddok4);
            Array.Clear(summary.Qdokss);
            Array.Clear(summary.Vvdoks);
            Array.Clear(summary.Dokp432);
            Array.Clear(summary.Qdokkarm);

            for (var irow = 1; irow <= dprow; irow++)
            {
                SummariseOneCategory(summary, irow, ndok);
            }

            if (dprow <= 1)
            {
                break;
            }

            var shifted = false;
            for (var irow = 1; irow <= dprow - 1; irow++)
            {
                if (!shifted)
                {
                    if (summary.Epsydok[irow] > epsDok)
                    {
                        MergeCategory(irow, irow, irow + 1, ndok);
                        shifted = true;
                    }
                }
                else
                {
                    MergeCategory(irow, irow + 1, null, ndok);
                }
            }

            if (!shifted)
            {
                break;
            }

            dprow--;
        }

        // lines 951-959: the last category has no successor to merge with, so it folds
        // backwards into its predecessor instead — once, without repeating the pass.
        if (summary.Epsydok[dprow] > epsDok && dprow > 1)
        {
            MergeCategory(dprow - 1, dprow - 1, dprow, ndok);
            dprow--;
        }

        summary.Dprow = dprow;
        summary.Dpockets = _state.Dpockets;
    }

    /// <summary>Lines 838-947 for one pocket-size category.</summary>
    private void SummariseOneCategory(PassSummary summary, int irow, int ndok)
    {
        for (var iks = 1; iks <= ndok; iks++)
        {
            summary.Qdokss[irow] += _state.Qdoks[irow, iks];
        }

        var count = summary.Qdokss[irow];
        for (var iks = 1; iks <= ndok; iks++)
        {
            summary.Qdoks1[irow, iks] = count == 0
                ? 0f
                : (float)((double)(float)_state.Qdoks[irow, iks] / (float)count);
        }

        for (var iks = 1; iks <= ndok; iks++)
        {
            var size = (double)_di * (iks - 0.5);
            summary.Mdok4[irow] += Math.Pow(size, 4.0) * summary.Qdoks1[irow, iks];
            summary.Mdok3[irow] += Math.Pow(size, 3.0) * summary.Qdoks1[irow, iks];
        }

        summary.Dokp431[irow] = summary.Mdok3[irow] <= 1e-30
            ? 0f
            : (float)(summary.Mdok4[irow] / summary.Mdok3[irow]);

        for (var iks = 1; iks <= ndok; iks++)
        {
            var size = (double)_di * (iks - 0.5);
            var d4 = Math.Pow(size, 4.0) - summary.Mdok4[irow];
            var d3 = Math.Pow(size, 3.0) - summary.Mdok3[irow];
            summary.Ddok4[irow] += d4 * d4 * summary.Qdoks1[irow, iks];
            summary.Ddok3[irow] += d3 * d3 * summary.Qdoks1[irow, iks];
        }

        if (count == 0 || summary.Mdok3[irow] <= 1e-30 || summary.Mdok4[irow] <= 1e-30)
        {
            summary.Epsydok[irow] = 0f;
            summary.Epsmdok3[irow] = 0f;
            summary.Epsmdok4[irow] = 0f;
        }
        else
        {
            var inverse = 1.0 / (float)count;
            summary.Epsydok[irow] = (float)(3.0 * Math.Sqrt(inverse
                * ((summary.Ddok4[irow] / (summary.Mdok4[irow] * summary.Mdok4[irow]))
                    + (summary.Ddok3[irow] / (summary.Mdok3[irow] * summary.Mdok3[irow])))));
            summary.Epsmdok3[irow] = (float)(3.0 / summary.Mdok3[irow] * Math.Sqrt(inverse * summary.Ddok3[irow]));
            summary.Epsmdok4[irow] = (float)(3.0 / summary.Mdok4[irow] * Math.Sqrt(inverse * summary.Ddok4[irow]));
        }

        for (var iks = 1; iks <= ndok; iks++)
        {
            summary.Vvdoks[irow] += _state.Vdoks[irow, iks];
        }

        for (var iks = 1; iks <= ndok; iks++)
        {
            summary.Vdokso[irow, iks] = summary.Vvdoks[irow] <= 1e-30
                ? 0f
                : (float)(_state.Vdoks[irow, iks] / summary.Vvdoks[irow]);

            summary.Dokp432[irow] = (float)(summary.Dokp432[irow]
                + ((double)summary.Vdokso[irow, iks] * _di * (iks - 0.5)));

            summary.Qdokso[irow, iks] = count == 0
                ? 0f
                : (float)((double)(float)_state.Qdoks[irow, iks] / (float)count);

            summary.Qdokkarm[irow] = (float)(summary.Qdokkarm[irow]
                + ((double)summary.Qdokso[irow, iks] * _di * (iks - 0.5)));
        }

        summary.Dokp43[irow] = _state.Dokp31[irow] <= 1e-30
            ? 0f
            : (float)((double)_state.Dokp41[irow] / _state.Dokp31[irow]);
    }

    /// <summary>
    /// Folds one category into another, or shifts it down — lines 929-943 and
    /// 944-950 are the same six assignments differing only in whether the target is
    /// added to or overwritten.
    /// </summary>
    private void MergeCategory(int target, int keep, int? add, int ndok)
    {
        var source = add ?? keep;
        _state.Dpockets[target] = _state.Dpockets[source];

        if (add is null)
        {
            _state.Dokp41[target] = _state.Dokp41[keep];
            _state.Dokp31[target] = _state.Dokp31[keep];
            _state.Qdok[target] = _state.Qdok[keep];
            for (var iks = 1; iks <= ndok; iks++)
            {
                _state.Vdoks[target, iks] = _state.Vdoks[keep, iks];
                _state.Qdoks[target, iks] = _state.Qdoks[keep, iks];
            }

            return;
        }

        _state.Dokp41[target] = (float)((double)_state.Dokp41[keep] + _state.Dokp41[add.Value]);
        _state.Dokp31[target] = (float)((double)_state.Dokp31[keep] + _state.Dokp31[add.Value]);
        _state.Qdok[target] = _state.Qdok[keep] + _state.Qdok[add.Value];
        for (var iks = 1; iks <= ndok; iks++)
        {
            _state.Vdoks[target, iks] = _state.Vdoks[keep, iks] + _state.Vdoks[add.Value, iks];
            _state.Qdoks[target, iks] = _state.Qdoks[keep, iks] + _state.Qdoks[add.Value, iks];
        }
    }

    /// <summary>Lines 963-996: the pocket size distribution and its accuracy.</summary>
    private void SummarisePockets(PassSummary summary)
    {
        var nkarm = _state.Nkarm;

        for (var iks = 1; iks <= nkarm; iks++)
        {
            var size = (double)_di * (iks - 0.5);
            summary.Md4 += Math.Pow(size, 4.0) * _state.Qks1[iks];
            summary.Md3 += Math.Pow(size, 3.0) * _state.Qks1[iks];
        }

        for (var iks = 1; iks <= nkarm; iks++)
        {
            var size = (double)_di * (iks - 0.5);
            var d4 = Math.Pow(size, 4.0) - summary.Md4;
            var d3 = Math.Pow(size, 3.0) - summary.Md3;
            summary.Dd4 += d4 * d4 * _state.Qks1[iks];
            summary.Dd3 += d3 * d3 * _state.Qks1[iks];
        }

        // ⚠ `1./QKSS` divides a REAL*4 one by an INTEGER*8 count, so the count goes
        // through REAL*4 first and loses its low bits above 2^24. Faithful, and it is
        // reached on every run large enough to matter.
        var inverse = 1.0 / (float)_state.Qkss;
        summary.Epsy = (float)(3.0 * Math.Sqrt(inverse
            * ((summary.Dd4 / (summary.Md4 * summary.Md4)) + (summary.Dd3 / (summary.Md3 * summary.Md3)))));
        summary.D43 = (float)(summary.Md4 / summary.Md3);
        summary.EpsMd4 = (float)(3.0 / summary.Md4 * Math.Sqrt(inverse * summary.Dd4));
        summary.EpsMd3 = (float)(3.0 / summary.Md3 * Math.Sqrt(inverse * summary.Dd3));

        var vvks = 0.0;
        for (var kilo = 1; kilo <= nkarm; kilo++)
        {
            vvks += _state.Vks[kilo];
        }

        summary.Vvks = vvks;

        var d432 = 0f;
        var d243 = 0f;
        var dqkarm = 0f;
        for (var kilo = 1; kilo <= nkarm; kilo++)
        {
            _state.Vkso[kilo] = (float)(_state.Vks[kilo] / vvks);
            var size = (double)_di * (kilo - 0.5);
            d432 = (float)(d432 + ((double)_state.Vkso[kilo] * size));
            d243 = (float)(d243 + ((double)_state.Vkso[kilo] * size * size));
            dqkarm = (float)(dqkarm + ((double)_state.Qks1[kilo] * size));
        }

        summary.D432 = d432;
        summary.D243 = d243;
        summary.Dqkarm = dqkarm;
        summary.Dp43 = (float)(_state.Dp41 / _state.Dp31);
        summary.SdevP43 = (float)Math.Pow((double)d243 - ((double)d432 * d432), 0.5);
    }

    /// <summary>Lines 1086-1152: the two corrected pocket distributions and the three pocket mass fractions.</summary>
    private void SummariseCorrections(PassSummary summary)
    {
        var nkarm = _state.Nkarm;
        var ndok = _state.Ndok;

        var fmkarmCorSum = Sum(_state.FmkarmCor, nkarm);
        var fqkarmCorSum = Sum(_state.FqkarmCor, nkarm);
        var fmkarm2Sum = Sum(_state.Fmkarm2, nkarm);

        var dkarm43Cor = 0f;
        var dkarm243Cor = 0f;
        var dqkarmCor = 0f;
        var dfmk432 = 0f;
        var dfmk243 = 0f;
        for (var kilo = 1; kilo <= nkarm; kilo++)
        {
            var size = (double)_di * (kilo - 0.5);
            dkarm43Cor = (float)(dkarm43Cor + ((double)_state.FmkarmCor[kilo] / fmkarmCorSum * size));
            dkarm243Cor = (float)(dkarm243Cor + ((double)_state.FmkarmCor[kilo] / fmkarmCorSum * size * size));
            dqkarmCor = (float)(dqkarmCor + ((double)(float)_state.FqkarmCor[kilo] / (float)fqkarmCorSum * size));
            dfmk432 = (float)(dfmk432 + ((double)_state.Fmkarm2[kilo] / fmkarm2Sum * size));
            dfmk243 = (float)(dfmk243 + ((double)_state.Fmkarm2[kilo] / fmkarm2Sum * size * size));
        }

        summary.Dkarm43Cor = dkarm43Cor;
        summary.Dkarm243Cor = dkarm243Cor;
        summary.DqkarmCor = dqkarmCor;
        summary.SdevP43Cor = (float)Math.Pow((double)dkarm243Cor - ((double)dkarm43Cor * dkarm43Cor), 0.5);
        summary.Dfmk432 = dfmk432;
        summary.Dfmk243 = dfmk243;
        summary.SdevP243 = (float)Math.Pow((double)dfmk243 - ((double)dfmk432 * dfmk432), 0.5);

        // lines 1106-1112: the bridge histograms carry their own grids - 0.001 for the
        // particle ratio, 0.01 for the pocket ratio and the gap coefficient.
        var qmkm1Sum = Sum(_state.Qmkm1, RunState.NC);
        var qmkm2Sum = Sum(_state.Qmkm2, RunState.NC);
        var coefSum = Sum(_state.Coef, RunState.NC);

        var dqmkm1 = 0f;
        var dqmkm2 = 0f;
        var qmcoef = 0f;
        for (var kilo = 1; kilo <= RunState.NC; kilo++)
        {
            dqmkm1 = (float)(dqmkm1
                + ((double)(float)_state.Qmkm1[kilo] / (float)qmkm1Sum * 0.001 * (kilo - 0.5)));
            dqmkm2 = (float)(dqmkm2
                + ((double)(float)_state.Qmkm2[kilo] / (float)qmkm2Sum * 0.01 * (kilo - 0.5)));
            qmcoef = (float)(qmcoef
                + ((double)(float)_state.Coef[kilo] / (float)coefSum * 0.01 * (kilo - 0.5)));
        }

        summary.Dqmkm1 = dqmkm1;
        summary.Dqmkm2 = dqmkm2;
        summary.Qmcoef = qmcoef;

        // lines 1119-1133
        summary.Gk0 = new float[ndok + 1];
        summary.Gk1 = new float[ndok + 1];
        for (var kilo = 1; kilo <= ndok; kilo++)
        {
            var sumvsmkm = 0f;
            var sumsvd = 0f;
            var sumvmkmt = 0f;
            var sumvdokt = 0f;
            for (var iks = 1; iks <= kilo; iks++)
            {
                sumvsmkm = (float)(sumvsmkm + _state.Vsmkm[iks]);
                sumsvd = (float)(sumsvd + _state.Svd[iks]);
                sumvmkmt = (float)(sumvmkmt + _state.VmkmTotal[iks]);
                sumvdokt = (float)(sumvdokt + _state.VdokTotal[iks]);
            }

            summary.Gk0[kilo] = (float)(1.0 - ((double)sumvsmkm * Plotsmdok * (_ggg - Gdokleft)
                / (sumsvd * (double)_plot1 * (1.0 - _ggg + Gdokleft))));
            summary.Gk1[kilo] = (float)(1.0 - ((double)sumvmkmt * Plotsmdok * (_ggg - Gdokleft)
                / (sumvdokt * (double)_plot1 * (1.0 - _ggg + Gdokleft))));
            _state.Gk0[kilo] = summary.Gk0[kilo];
            _state.Gk1[kilo] = summary.Gk1[kilo];
        }

        // lines 1137-1152
        summary.Plotsm = (float)((1.0 - _ggg) / ((1.0 / _plot2) - (_ggg / _plot1)));

        var vsmkmSum = Sum(_state.Vsmkm, ndok);
        var svdSum = Sum(_state.Svd, ndok);
        var vmkmTotalSum = Sum(_state.VmkmTotal, ndok);
        var vdokTotalSum = Sum(_state.VdokTotal, ndok);

        summary.DolM1 = (float)((double)vsmkmSum * Plotsmdok * (_ggg - Gdokleft)
            / (svdSum * (double)_plot1 * (1.0 - _ggg + Gdokleft)));

        var vdokTotal0 = (float)((double)vdokTotalSum + ((double)vmkmTotalSum * Vdokleft));
        var vdokTotal1 = (float)((double)vdokTotalSum / (1.0 - ((double)Gdokleft / _ggg)));
        summary.DeltaVdok = (float)(((double)vdokTotal1 - vdokTotal0) / vdokTotal1);

        // ⚠ In place, and therefore once per pass: a second working pass divides the
        // already-divided array again.
        for (var kilo = 1; kilo <= ndok; kilo++)
        {
            _state.VdokTotal[kilo] = (float)((double)_state.VdokTotal[kilo] / (1.0 - ((double)Gdokleft / _ggg)));
        }

        var vdokTotalDivided = Sum(_state.VdokTotal, ndok);
        summary.DolM2 = (float)((double)vmkmTotalSum * (1.0 - Vdokleft)
            / ((double)vdokTotalDivided * _plot1 * (1.0 - _ggg) / _ggg / summary.Plotsm));

        _state.VdokTotal2 = (float)((double)_state.VdokTotal2 / (1.0 - ((double)Gdokleft / _ggg)));
        summary.DolM3 = (float)((double)_state.VmkmTotal2 * (1.0 - Vdokleft)
            / ((double)_state.VdokTotal2 * _plot1 * (1.0 - _ggg) / _ggg / summary.Plotsm));
    }

    /// <summary>
    /// The <c>SUM</c> intrinsic over a REAL*4 array.
    /// </summary>
    /// <remarks>
    /// Accumulated in <see cref="double"/> and rounded once, on the reading that the
    /// compiler keeps its running total in an x87 register and stores it a single
    /// time. ⚠ Untested either way: on the probe both forms give bit-identical
    /// results, so this is a choice of reading, not a measured fact.
    /// </remarks>
    private static float Sum(float[] values, int count)
    {
        var total = 0.0;
        for (var i = 1; i <= count; i++)
        {
            total += values[i];
        }

        return (float)total;
    }

    private static long Sum(long[] values, int count)
    {
        var total = 0L;
        for (var i = 1; i <= count; i++)
        {
            total += values[i];
        }

        return total;
    }
}
