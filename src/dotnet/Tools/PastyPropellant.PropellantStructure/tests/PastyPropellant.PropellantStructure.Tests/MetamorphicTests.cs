using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using PastyPropellant.PropellantStructure.Sampling;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// L3: properties the model must have for reasons that do not depend on any archived
/// run.
/// </summary>
/// <remarks>
/// <para>
/// L2 asks whether the port reproduces the original. These ask whether the thing
/// reproduced behaves like the physics it claims to model — a question no amount of
/// replaying archived runs can answer, because the archive is one corner of the
/// parameter space and every run in it shares the same recipe family.
/// </para>
/// <para>
/// ⚠ A metamorphic test is only worth its tolerance. Where an invariant is exact it is
/// asserted exactly, and where it is only statistical it is not asserted at all until
/// there is a yardstick for "close enough" that was not chosen to make the test pass.
/// See <see cref="FractionOrderIsNotTestedAndHereIsWhy"/>.
/// </para>
/// </remarks>
public sealed class MetamorphicTests
{
    /// <summary>
    /// Doubling every length in the input doubles every length in the result and changes
    /// nothing else: the decision path exactly, the numbers to single precision.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The model has no absolute length in it. Every comparison it makes is between two
    /// lengths (<c>Dr</c> against <c>AK1·Db</c>, the gap against <c>4.7·Dbase</c>), every
    /// bin index is a ratio of two lengths, and the one dimensioned constant it carries —
    /// <c>Di</c>, the histogram step — is an input. So a uniform rescaling must leave the
    /// entire decision path untouched: the same particles accepted, the same conditions
    /// fired, the same number of draws. That half <b>is</b> asserted exactly.
    /// </para>
    /// <para>
    /// The factor is <b>two</b>, and that is not arbitrary: a power of two rescales every
    /// float exactly, so nothing here is about the input's own rounding.
    /// </para>
    /// <para>
    /// ⚠ **The output half is not exact, and the first version of this test asserted that
    /// it was.** Measured, the deviation is at most one float ulp on ordinary quantities
    /// and about nine on the pocket sizes — and the split is not random: <b>every</b>
    /// quantity that moves reads a bridge volume, and every quantity that does not, does
    /// not. <c>Dkarm10</c>, a number-mean over sizes, holds exactly; <c>Dkarm43</c>, a
    /// mass-mean over volumes that include the bridges, does not. That is the same
    /// boundary and the same cause as
    /// <see cref="ProbeRun.ThreeScalarsInheritTheMathLibrarysLastBit"/>: line 1616 puts
    /// <c>GA</c> against π/2 where <c>TAN</c> has a derivative of about 1.6e6, so one ulp
    /// in becomes up to 6e-4 out, and the two runs need not round identically on the way
    /// there.
    /// </para>
    /// <para>
    /// The tolerances below are therefore the suite's existing ones — single precision's
    /// own resolution, and the amplified allowance for the three scalars that already
    /// have it — not numbers chosen to make this pass. What the test still catches is
    /// undiminished: a hard-coded micrometre would move a length by a factor, not by an
    /// ulp.
    /// </para>
    /// </remarks>
    [Fact]
    public void RescalingEveryLengthRescalesTheResultAndNothingElse()
    {
        const double factor = 2.0;

        var plain = PropellantStructureModel.Run(ProbeRun.Configuration);
        var scaled = PropellantStructureModel.Run(Rescaled(ProbeRun.Configuration, factor));

        var problems = new List<string>();

        // The decision path: identical, not merely similar.
        var a = plain.Diagnostics;
        var b = scaled.Diagnostics;
        Compare("nfx", a.BaseParticleDraws, b.BaseParticleDraws);
        Compare("nfy", a.SurroundingParticleDraws, b.SurroundingParticleDraws);
        Compare("nfq", a.PocketSizeDraws, b.PocketSizeDraws);
        Compare("nfw", a.BridgeDraws, b.BridgeDraws);
        Compare("nkarm", a.PocketsTotal, b.PocketsTotal);
        Compare("attempts", a.AttemptsTotal.Value, b.AttemptsTotal.Value);
        Compare("bridge gap rejections", a.BridgeGapExceedsPocket.Value, b.BridgeGapExceedsPocket.Value);
        Compare("bridge width rejections", a.BridgeWidthNegative.Value, b.BridgeWidthNegative.Value);
        Assert.Equal(a.Conditions, b.Conditions);

        // Dimensionless quantities: unchanged, to single precision's own resolution.
        foreach (var (name, value) in plain.Printed)
        {
            Close(name, value.Value, scaled.Printed[name].Value);
        }

        // Lengths: scaled by the factor, to the same resolution.
        foreach (var (name, value) in plain.PrintedLengths)
        {
            Close(name, value.Value.Meters * factor, scaled.PrintedLengths[name].Value.Meters);
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        return;

        void Close(string name, double expected, double actual)
        {
            // The three scalars that read a bridge volume get the allowance they are
            // given everywhere else in the suite; everything else gets single precision.
            var relative = name is "zkarm" or "zkarm_cor1" or "zkarm_cor2" ? 1e-5 : 1e-6;
            if (expected == 0.0 ? actual != 0.0 : Math.Abs(actual - expected) > Math.Abs(expected) * relative)
            {
                problems.Add($"{name}: {expected:R} unscaled x{factor}, {actual:R} scaled.");
            }
        }

        void Compare(string name, long left, long right)
        {
            if (left != right)
            {
                problems.Add($"{name}: {left} unscaled, {right} scaled — the decision path moved.");
            }
        }
    }

    /// <summary>
    /// <c>k5</c> moves the pocket mass fractions and touches nothing else — in
    /// particular it does <b>not</b> decide how many neighbours a base particle gets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because the node's own description got it wrong. <c>Kernel/BOOT.md</c>
    /// used to say the neighbour loop ran "until the volume share <c>k5</c> is used up".
    /// It does not: the loop is a budget of <c>TU</c>, seeded at 4π (line 430), each
    /// accepted neighbour subtracting <c>(Db/rrL)²</c> (715) until <c>TU &lt; 4π/200</c>
    /// (716). <c>k5</c> — <c>ALPHA</c> in the listing, <c>SurroundingVolumeShare</c> here
    /// — appears in exactly three places (747, 750, 754), all three of them the oxidiser
    /// term of a pocket-fraction accumulator.
    /// </para>
    /// <para>
    /// A description cannot be tested, so what is tested is its consequence, and the two
    /// readings differ sharply on it. Under the old reading <c>k5</c> steers the loop, so
    /// changing it must change how many neighbours are drawn — every draw counter, every
    /// condition counter, every size. Under the right one it changes three numbers and no
    /// others. Halving it here leaves <b>the whole decision path bit-identical</b> and
    /// moves exactly <c>zkarm</c>, <c>zkarm_cor1</c> and <c>zkarm_cor2</c>.
    /// </para>
    /// <para>
    /// ⚠ The three that move are asserted to move, not merely allowed to. A port that
    /// dropped <c>k5</c> altogether would satisfy "nothing else changed" perfectly.
    /// </para>
    /// </remarks>
    [Fact]
    public void K5MovesThePocketFractionsAndNotTheNeighbourLoop()
    {
        var baseline = ProbeRun.Configuration;
        var halved = baseline with
        {
            Coefficients = baseline.Coefficients with
            {
                SurroundingVolumeShare = baseline.Coefficients.SurroundingVolumeShare / 2.0,
            },
        };

        var plain = PropellantStructureModel.Run(baseline);
        var moved = PropellantStructureModel.Run(halved);

        // The decision path, exactly: k5 is read after every branch has been taken.
        Assert.Equal(plain.Diagnostics, moved.Diagnostics);

        string[] expected = ["zkarm", "zkarm_cor1", "zkarm_cor2"];

        var problems = new List<string>();
        foreach (var (name, value) in plain.Printed)
        {
            var changed = moved.Printed[name].Value != value.Value;
            if (changed != expected.Contains(name))
            {
                problems.Add($"{name}: {(changed ? "moved" : "held")} at half k5, expected the opposite.");
            }
        }

        foreach (var (name, value) in plain.PrintedLengths)
        {
            if (moved.PrintedLengths[name].Value.Meters != value.Value.Meters)
            {
                problems.Add($"{name}: moved at half k5, and no length depends on it.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Rejection is of two kinds, and the books balance to the unit: conditions 3 (under
    /// <c>ivar = 0</c>), 5, and the group 6–9 throw the whole realisation away; condition
    /// 4 only skips one neighbour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every arrival at label 11 begins a realisation and draws exactly one base
    /// particle, so <c>attempts − accepted</c> is the number of realisations thrown
    /// away, and the restart causes must add up to it <b>exactly</b> — there is nothing
    /// else for a realisation to have been. That makes the claim arithmetic rather than
    /// directional: a condition wrongly classified is not a shift in a statistic, it is
    /// a residue.
    /// </para>
    /// <para>
    /// On the probe, at <c>ivar = 0</c>: 915 realisations, 40 accepted, 875 discarded —
    /// and 633 (condition 3) + 198 (condition 5) + 44 (condition 8) = 875, to the unit.
    /// Condition 4, whose raw count is 16780, appears in no term. Were it a restart the
    /// left side would be twenty times larger; were 3 or 5 not restarts, the sum would
    /// fall short by its own count.
    /// </para>
    /// <para>
    /// ⚠ Conditions 6–9 count <b>events, not realisations</b> — the original tests all
    /// four and can increment several on one realisation (<c>IRESET</c> is a sum, and
    /// any positive value discards). They are summed here only because this fixture
    /// fires exactly one of the four, which the test asserts rather than assumes. On a
    /// fixture where two co-fire this identity would over-count, and that is a property
    /// of the original's bookkeeping, not a licence to relax the tolerance.
    /// </para>
    /// <para>
    /// ⚠ Two restart paths carry no counter at all — a base particle from a fraction
    /// that forms no pockets (line 473) and an empty pocket-size window (line 590). The
    /// identity closing exactly is what shows both are zero here; on a fixture where
    /// they are not, it would not close, and the residue would be their count.
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(0, 915, 633)]
    [InlineData(1, 386, 0)]
    public void DiscardedRealisationsAccountForThemselvesExactly(
        int ivar, long attempts, long fromConditionThree)
    {
        var configuration = ProbeRun.Configuration with { CalculationVariant = ivar };

        var result = PropellantStructureModel.Run(configuration);
        Assert.Equal(attempts, result.Diagnostics.AttemptsTotal.Value);

        var simulation = new StructureSimulation(configuration, RandomStreamSet.Historical());
        simulation.Simulate();
        var raw = simulation.State.Conditions;

        // Condition 3 fires either way; under ivar = 1 it stops being a restart, so the
        // term leaves the sum while the count stays. Asserting the count is non-zero in
        // both runs is what keeps the ivar = 1 case from passing vacuously.
        Assert.True(raw[3] > 0);
        Assert.Equal(fromConditionThree, ivar == 0 ? raw[3] : 0);

        // exactly one of the group fires on this fixture, so summing it is legitimate
        Assert.Equal(0, raw[6]);
        Assert.Equal(0, raw[7]);
        Assert.Equal(0, raw[9]);

        var accepted = ProbeRun.Configuration.BaseParticles * simulation.TotalPasses;
        var discarded = attempts - accepted;
        var accounted = raw[1] + raw[2] + fromConditionThree + raw[5] + raw[6] + raw[7] + raw[8] + raw[9];

        Assert.Equal(discarded, accounted);

        // and the one that is not a restart is large enough that mistaking it would be
        // impossible to miss
        Assert.True(
            raw[4] > discarded,
            $"condition 4 fired {raw[4]} times against {discarded} discarded realisations — "
            + "this fixture no longer separates a skipped neighbour from a discarded realisation.");
    }

    /// <summary>
    /// Every realisation draws exactly one base particle: <c>attempts</c> and <c>NFX</c>
    /// are the same number, always.
    /// </summary>
    /// <remarks>
    /// The two counters are deliberately kept apart even so. <c>NFX</c> is a property of
    /// the <b>generator</b> — how many times the base-particle stream was consumed, which
    /// is what the archive adjudicates — while <c>AttemptsTotal</c> is a property of the
    /// <b>search</b>: how many realisations were built to have one kept. Their equality is
    /// a statement about the model (a retry never reuses the base particle it already
    /// drew, and never draws twice before deciding), not a redundancy, and it is exactly
    /// what would break first if a restart path were ever moved above the draw.
    /// </remarks>
    [Fact]
    public void EveryRealisationDrawsExactlyOneBaseParticle()
    {
        foreach (var configuration in (StructureRunConfiguration[])
                 [ProbeRun.Configuration, ProbeRun3.Configuration, ProbeRun8.Configuration])
        {
            var diagnostics = PropellantStructureModel.Run(configuration).Diagnostics;
            Assert.Equal(diagnostics.BaseParticleDraws, diagnostics.AttemptsTotal.Value);
        }
    }

    /// <summary>
    /// A distribution's own normalisation rule holds on a run the port produced, not
    /// only on the archived arrays.
    /// </summary>
    /// <remarks>
    /// The sums were already checked against the reference's arrays. This checks the
    /// same rule against arrays the port built, which is a different claim: the first
    /// says the classification describes the original, the second says the port's own
    /// output obeys it. Both are needed — a packing bug that dropped the division by the
    /// grid step would leave the first passing.
    /// </remarks>
    [Fact]
    public void EveryDistributionObeysTheNormalisationItDeclares()
    {
        var result = PropellantStructureModel.Run(ProbeRun.Configuration);
        var step = ProbeRun.Configuration.ParticleHistogramStep.Micrometers;

        var problems = new List<string>();
        foreach (var distribution in result.Distributions)
        {
            var sum = distribution.Values.Sum();
            var (total, rule) = distribution.Normalisation switch
            {
                DistributionNormalisation.DensityPerGridUnit => (sum * step, "sum x step"),
                DistributionNormalisation.ProbabilityPerBin => (sum, "sum"),
                DistributionNormalisation.Unnormalised => (1.0, "not normalised"),
                _ => throw new InvalidOperationException($"unknown normalisation on {distribution.Name}."),
            };

            // The tolerance is the print format's: arrayprint writes E9.3, and the
            // reference arrays this rule was first checked against carry only what that
            // format preserved. A tighter one here would be measuring the reference's
            // rounding, not the port.
            if (Math.Abs(total - 1.0) > 5e-3)
            {
                problems.Add($"{distribution.Name}: {rule} = {total:R}, expected 1.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Watching a run does not change it, and neither does asking it to report progress.
    /// </summary>
    /// <remarks>
    /// The guarantee is in <c>Kernel/API.md</c> and it is the kind that decays quietly:
    /// a progress callback added inside the particle loop rather than at its boundary
    /// would still work, still report sensible numbers, and silently consume a draw.
    /// </remarks>
    [Fact]
    public void ProgressReportingDoesNotChangeTheNumbers()
    {
        var silent = PropellantStructureModel.Run(ProbeRun.Configuration);

        var seen = new List<StructureProgress>();
        var watched = PropellantStructureModel.Run(
            ProbeRun.Configuration,
            new Progress<StructureProgress>(seen.Add));

        Assert.Equal(silent.Diagnostics, watched.Diagnostics);
        Assert.Equal(silent.Printed.Count, watched.Printed.Count);
        Assert.All(
            silent.Printed,
            entry => Assert.Equal(entry.Value, watched.Printed[entry.Key]));
    }

    /// <summary>
    /// Permuting the oxidiser fractions is a real invariant of the physics and is
    /// deliberately <b>not</b> asserted. This records why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reordering the fractions in the input describes the same propellant, so every
    /// aggregate quantity should be unchanged — but only in distribution, never
    /// exactly. The sampler builds its cumulative table in input order, so a permuted
    /// run draws a different sequence from the first particle onward, and the two runs
    /// are two independent samples of the same population rather than two computations
    /// of the same number.
    /// </para>
    /// <para>
    /// ⚠ Asserting it therefore needs a tolerance, and there is nothing honest to derive
    /// one from. The generator seeds are fixed literals of the original, so the model
    /// cannot produce independent replicates, so the Monte-Carlo scatter that would be
    /// the natural yardstick cannot be measured. Any number put here would be one chosen
    /// because the test passed with it — which is the failure mode this whole subtree
    /// exists to avoid. The invariant stays written down and unasserted until either the
    /// scatter can be measured or the permuted run can be shown to be the same draw
    /// sequence.
    /// </para>
    /// </remarks>
    [Fact]
    public void FractionOrderIsNotTestedAndHereIsWhy()
    {
        var plain = PropellantStructureModel.Run(ProbeRun.Configuration);

        var reversed = ProbeRun.Configuration with
        {
            Fractions = [.. ProbeRun.Configuration.Fractions.Reverse()],
        };
        var permuted = PropellantStructureModel.Run(reversed);

        // The claim being recorded is the negative one: the draw sequence really does
        // move, so an exact assertion would be wrong rather than merely strict. If this
        // ever stops holding, the invariant becomes testable and this test should be
        // replaced by the real one.
        Assert.NotEqual(
            plain.Diagnostics.BaseParticleDraws,
            permuted.Diagnostics.BaseParticleDraws);
    }

    /// <summary>Every length in a configuration, multiplied by the same factor.</summary>
    private static StructureRunConfiguration Rescaled(StructureRunConfiguration configuration, double factor) =>
        configuration with
        {
            Fractions =
            [
                .. configuration.Fractions.Select(fraction => fraction with
                {
                    MinSize = Length.FromMeters(fraction.MinSize.Meters * factor),
                    MaxSize = Length.FromMeters(fraction.MaxSize.Meters * factor),
                }),
            ],
            MinimumParticleSize = Length.FromMeters(configuration.MinimumParticleSize.Meters * factor),
            ParticleHistogramStep = Length.FromMeters(configuration.ParticleHistogramStep.Meters * factor),
            PocketHistogramStep = Length.FromMeters(configuration.PocketHistogramStep.Meters * factor),
        };
}
