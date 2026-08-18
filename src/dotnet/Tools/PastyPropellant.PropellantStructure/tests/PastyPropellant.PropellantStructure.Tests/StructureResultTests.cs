using System.Text.Json;
using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Results;
using UnitsNet;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The result type's own guarantees: a branched quantity cannot be read unbranched,
/// a branch a quantity lacks is refused rather than answered, and a result survives
/// a round trip through JSON unchanged.
/// </summary>
/// <remarks>
/// These are not L2. Nothing here checks that a computed number is right — the kernel
/// does not exist yet. They check that the shape the kernel will fill cannot be
/// filled in a way that misleads.
/// </remarks>
public sealed class StructureResultTests
{
    [Fact]
    public void PocketMassFractionIsBranchedLikeEveryOtherBranchedQuantity()
    {
        var result = Build(ReferenceRuns.ById("hp1"));

        Assert.Equal(
            [Correction.None, Correction.Variant1, Correction.Variant2],
            result.PocketMassFraction.Keys.Order());

        // The three come from three independent accumulators, so nothing here should
        // be assumed about their order or spacing - only that they are told apart.
        Assert.Equal(result.Printed["zkarm"], result.PocketMassFraction[Correction.None]);
        Assert.Equal(result.Printed["zkarm_cor1"], result.PocketMassFraction[Correction.Variant1]);
        Assert.Equal(result.Printed["zkarm_cor2"], result.PocketMassFraction[Correction.Variant2]);
    }

    [Fact]
    public void ThePocketDiameterUsesTheHistogramEstimatorAndLeavesTheOtherInThePrintedRecord()
    {
        var result = Build(ReferenceRuns.ById("hp1"));

        Assert.Equal(result.PrintedLengths["dkarm43_v2"], result.PocketDiameter43[Correction.None]);

        // The sample-moment estimator stays reachable, just not as a branch.
        Assert.True(result.PrintedLengths.ContainsKey("dkarm43_v1"));
    }

    [Fact]
    public void AQuantityCarriesOnlyTheBranchesItHasAndSaysSoWhenAskedForAnother()
    {
        var result = Build(ReferenceRuns.ById("hp1"));

        Assert.Equal(2, result.PocketDiameter10.Count);

        var mismatch = Assert.Throws<BranchMismatchException>(() => result.PocketDiameter10[Correction.Variant2]);
        Assert.Contains("PocketDiameter10", mismatch.Message, StringComparison.Ordinal);
        Assert.Contains("None, Variant1", mismatch.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The mapping from branch to the original's name is 14 string literals, and a
    /// typo in any of them would surface as a missing quantity at the first read.
    /// Walking all 43 archived runs is what pins them.
    /// </summary>
    [Fact]
    public void EveryNameTheProjectionsNeedIsPrintedByAllArchivedRuns()
    {
        foreach (var run in ReferenceRuns.All)
        {
            var result = Build(run);

            Assert.Equal(3, result.PocketMassFraction.Count);
            Assert.Equal(3, result.PocketDiameter43.Count);
            Assert.Equal(3, result.PocketDiameter43Sd.Count);
            Assert.Equal(3, result.AgglomerateDiameter43.Count);
            Assert.Equal(2, result.PocketDiameter10.Count);

            _ = result.PocketToAgglomerateCoefficient;
            _ = result.PocketWallDiameter43;
            _ = result.MeanBridgeToParticleRatio;
            _ = result.MeanBridgeToPocketRatio;
            _ = result.HomogenisedOxidiserFraction;
        }
    }

    /// <summary>
    /// Every name in <see cref="StructureResult.LengthValuedQuantities"/> has to be a
    /// name the original actually prints; an invented one would send a real quantity
    /// to the wrong half of the record for good.
    /// </summary>
    [Fact]
    public void TheLengthValuedNamesAreAllPrintedNames()
    {
        var printed = ReferenceRuns.All
            .SelectMany(run => run.Scalars.Keys)
            .ToHashSet(StringComparer.Ordinal);

        var invented = StructureResult.LengthValuedQuantities.Where(name => !printed.Contains(name)).Order();
        Assert.Empty(invented);
    }

    [Fact]
    public void AMissingQuantityNamesItselfAndWhatNeededIt()
    {
        var run = ReferenceRuns.ById("hp1");
        var complete = Build(run);
        var withoutOneBranch = complete with
        {
            Printed = complete.Printed
                .Where(entry => entry.Key != "zkarm_cor1")
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
        };

        var failure = Assert.Throws<InvalidOperationException>(() => withoutOneBranch.PocketMassFraction);
        Assert.Contains("zkarm_cor1", failure.Message, StringComparison.Ordinal);
        Assert.Contains("PocketMassFraction", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ComparingCompositionsTakesOneBranchForBothFactors()
    {
        var results = (IReadOnlyList<StructureResult>)
        [
            Build(ReferenceRuns.ById("hp1")),
            Build(ReferenceRuns.ById("hp50b")),
            Build(ReferenceRuns.ById("r3")),
        ];

        var uncorrected = StructureResultComparison.PocketSizeTimesMassFraction(results, Correction.None);
        var corrected = StructureResultComparison.PocketSizeTimesMassFraction(results, Correction.Variant2);

        for (var index = 0; index < results.Count; index++)
        {
            var expected = results[index].PocketDiameter43[Correction.None].Value
                           * results[index].PocketMassFraction[Correction.None].Value;
            Assert.Equal(expected.Meters, uncorrected[index].Meters, 12);
        }

        // The point of the operation: the branch is not a detail of presentation.
        Assert.NotEqual(uncorrected[0].Meters, corrected[0].Meters, 6);
    }

    [Fact]
    public void ARoundTripThroughJsonGivesTheSameResultBackBitForBit()
    {
        var result = Build(ReferenceRuns.ById("hp1"));

        var text = JsonSerializer.Serialize(result, ConfigurationJson.Options);
        var read = JsonSerializer.Deserialize<StructureResult>(text, ConfigurationJson.Options);

        Assert.Equal(result, read);
        Assert.Equal(text, JsonSerializer.Serialize(read, ConfigurationJson.Options));

        // Value equality is the claim; this is the bit-for-bit half of it.
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(result.Printed["zkarm"].Value),
            BitConverter.DoubleToInt64Bits(read!.Printed["zkarm"].Value));
        Assert.Equal(
            BitConverter.DoubleToInt64Bits(result.PrintedLengths["dkarm43_v2"].Value.Meters),
            BitConverter.DoubleToInt64Bits(read.PrintedLengths["dkarm43_v2"].Value.Meters));
    }

    /// <summary>
    /// A projection has no setter, so serialising it would write a member that cannot be
    /// read back — and with <c>UnmappedMemberHandling.Disallow</c> that turns the round
    /// trip into an exception rather than a wrong value.
    /// </summary>
    /// <remarks>
    /// ⚠ Read off the <b>root object's own members</b>, not off the text. The earlier
    /// substring form said "this name appears nowhere in the document", which is a
    /// different and much stronger claim than the one intended — and a false one, since
    /// <c>pocketMassFraction</c> is also a member of <c>PassConvergence</c>, nested two
    /// levels down and perfectly serialisable. Names repeat across records; positions
    /// do not.
    /// </remarks>
    [Fact]
    public void TheProjectionsAreNotSerialisedTwice()
    {
        var text = JsonSerializer.Serialize(Build(ReferenceRuns.ById("hp1")), ConfigurationJson.Options);

        var members = JsonDocument.Parse(text).RootElement
            .EnumerateObject()
            .Select(member => member.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("pocketMassFraction", members);
        Assert.DoesNotContain("pocketDiameter43", members);

        // and the round trip the assertion above exists to protect
        Assert.NotNull(JsonSerializer.Deserialize<StructureResult>(text, ConfigurationJson.Options));
    }

    [Fact]
    public void EachGridKindComesBackAsItself()
    {
        var result = Build(ReferenceRuns.ById("hp1"));
        var read = JsonSerializer.Deserialize<StructureResult>(
            JsonSerializer.Serialize(result, ConfigurationJson.Options),
            ConfigurationJson.Options)!;

        Assert.Collection(
            read.Distributions,
            first => Assert.IsType<DistributionGrid.ByLength>(first.Grid),
            second => Assert.IsType<DistributionGrid.ByRatio>(second.Grid),
            third => Assert.IsType<DistributionGrid.ByCategory>(third.Grid));

        Assert.Equal(result.Distributions, read.Distributions);
    }

    [Fact]
    public void EqualityComparesMapsAndListsByValueNotByIdentity()
    {
        var run = ReferenceRuns.ById("hp1");
        var left = Build(run);
        var right = Build(run);

        Assert.NotSame(left.Printed, right.Printed);
        Assert.Equal(left, right);
        Assert.Equal(left.GetHashCode(), right.GetHashCode());

        var moved = right with
        {
            Printed = right.Printed.ToDictionary(
                entry => entry.Key,
                entry => entry.Key == "zkarm"
                    ? entry.Value with { Value = entry.Value.Value + 1e-9 }
                    : entry.Value,
                StringComparer.Ordinal),
        };
        Assert.NotEqual(left, moved);
    }

    /// <summary>
    /// A value's status must survive the trip, or a number read back from disk would
    /// look confirmed when it is not.
    /// </summary>
    [Fact]
    public void TheVerificationStatusSurvivesTheRoundTrip()
    {
        var result = Build(ReferenceRuns.ById("hp1"));
        var read = JsonSerializer.Deserialize<StructureResult>(
            JsonSerializer.Serialize(result, ConfigurationJson.Options),
            ConfigurationJson.Options)!;

        Assert.Equal(Verification.NotPrintedByOriginal, read.Diagnostics.AttemptsTotal.Status);
        Assert.Equal(result.Printed["zkarm"].Status, read.Printed["zkarm"].Status);
    }

    /// <summary>
    /// The status of a printed quantity follows the resolved record: a run that moved
    /// a setting off the value the archive pins has nothing to be compared against.
    /// </summary>
    [Fact]
    public void ARunOnAnUncoveredSettingCarriesUncoveredValues()
    {
        var covered = ReferenceRuns.ById("hp1").ToConfiguration();
        var uncovered = covered with { CalculationVariant = 1 };

        Assert.Equal(
            Verification.ConfirmedByArchivedRun,
            VerificationPolicy.ForPrintedQuantity(ResolvedRunRecord.Write(covered, null)));
        Assert.Equal(
            Verification.NotCoveredByAnyRun,
            VerificationPolicy.ForPrintedQuantity(ResolvedRunRecord.Write(uncovered, null)));
    }

    /// <summary>
    /// Every array the contract calls a distribution adds up the way the contract
    /// says it does, on all 43 archived runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what makes the three normalisations more than a label. The tolerance
    /// comes from the print format and not from what passes: <c>arrayprint</c> writes
    /// <c>E9.3</c>, three significant digits, so a cell carries at most 5·10⁻³
    /// relative error and a sum of cells inherits the same bound. The worst observed
    /// departure over the 43 runs is 1.7·10⁻³.
    /// </para>
    /// <para>
    /// The table below is the machine-readable form of the one in
    /// <c>Results/API.md</c>. When <c>Kernel/</c> lands it will need the same
    /// classification to build the distributions, and the two should become one
    /// source rather than two.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryDistributionAddsUpTheWayItsNormalisationSaysItShould()
    {
        var problems = new List<string>();

        foreach (var run in ReferenceRuns.All)
        {
            foreach (var (name, normalisation) in Distributions)
            {
                if (!run.Arrays.TryGetValue(name, out var values))
                {
                    problems.Add($"{run.Id}: no array {name}.");
                    continue;
                }

                var sum = values.Sum();
                switch (normalisation)
                {
                    case DistributionNormalisation.DensityPerGridUnit:
                        var total = sum * run.ArraySteps[name];
                        if (Math.Abs(total - 1.0) > 5e-3)
                        {
                            problems.Add($"{run.Id}/{name}: sum times step is {total:R}, not 1.");
                        }

                        break;

                    case DistributionNormalisation.ProbabilityPerBin:
                        if (Math.Abs(sum - 1.0) > 5e-3)
                        {
                            problems.Add($"{run.Id}/{name}: cells sum to {sum:R}, not 1.");
                        }

                        break;

                    case DistributionNormalisation.Unnormalised:
                        break;

                    default:
                        problems.Add($"{run.Id}/{name}: no rule for {normalisation}.");
                        break;
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The third normalisation is not a formality: <c>pdoksmall</c> really does not
    /// add up to anything.
    /// </summary>
    /// <remarks>
    /// Without this the <see cref="DistributionNormalisation.Unnormalised"/> arm above
    /// would be an untested escape hatch, and a distribution wrongly classified into
    /// it would be checked by nothing.
    /// </remarks>
    [Fact]
    public void TheUnnormalisedArrayIsGenuinelyUnnormalised()
    {
        var sums = ReferenceRuns.All
            .Where(run => run.Arrays.ContainsKey("pdoksmall"))
            .Select(run => run.Arrays["pdoksmall"].Sum())
            .ToArray();

        Assert.NotEmpty(sums);
        Assert.True(
            sums.Any(sum => Math.Abs(sum - 1.0) > 0.5),
            $"pdoksmall sums to {string.Join(", ", sums.Select(sum => sum.ToString("R")))} — all near 1, so it "
            + "may be a normalised distribution after all and the classification needs re-reading.");
    }

    /// <summary>
    /// Ten of the original's fourteen arrays are distributions, and the four that are
    /// not stay out.
    /// </summary>
    [Fact]
    public void TheFourArraysThatAreNotDistributionsAreNotTreatedAsOne()
    {
        Assert.Equal(10, Distributions.Count);
        Assert.Equal(4, PrintedArrays.NotDistributions.Count);

        foreach (var name in PrintedArrays.NotDistributions)
        {
            Assert.All(
                ReferenceRuns.All,
                run => Assert.True(run.Arrays.ContainsKey(name), $"{run.Id}: {name} vanished from the reference."));
            Assert.DoesNotContain(name, Distributions.Keys);
        }
    }

    /// <summary>
    /// Which of the original's arrays is a distribution, and how its cells add up.
    /// </summary>
    /// <remarks>
    /// Read from the shipped table, not restated here. Until the kernel started
    /// building distributions the classification lived in two places, and the risk was
    /// exact: this test would have checked the label the test itself carried rather
    /// than the one the result was built with, so a mislabelled array would pass.
    /// </remarks>
    private static IReadOnlyDictionary<string, DistributionNormalisation> Distributions =>
        PrintedArrays.Distributions;

    /// <summary>
    /// Builds a result from an archived run's printed scalars.
    /// </summary>
    /// <remarks>
    /// Only the scalars are real: the distributions and the conditional block are
    /// stand-ins, because <c>ReferenceRuns</c> does not load the arrays yet and these
    /// tests are about the shape rather than the numbers. The scalars have to be real
    /// — they are what pins the name mapping.
    /// </remarks>
    private static StructureResult Build(ReferenceRun run)
    {
        var snapshot = ResolvedRunRecord.Write(run.ToConfiguration(), null);
        var status = VerificationPolicy.ForPrintedQuantity(snapshot);

        var printed = new Dictionary<string, Verified<double>>(StringComparer.Ordinal);
        var printedLengths = new Dictionary<string, Verified<Length>>(StringComparer.Ordinal);
        foreach (var (name, value) in run.Scalars)
        {
            if (StructureResult.LengthValuedQuantities.Contains(name))
            {
                // The report prints micrometres; the record holds metres.
                printedLengths[name] = new Verified<Length>(Length.FromMicrometers(value), status);
            }
            else
            {
                printed[name] = new Verified<double>(value, status);
            }
        }

        var step = Length.FromMicrometers(10);
        return new StructureResult
        {
            Run = snapshot,
            Printed = printed,
            PrintedLengths = printedLengths,
            Distributions =
            [
                new Distribution(
                    "fmkarm",
                    new DistributionGrid.ByLength(Length.Zero, step),
                    DistributionNormalisation.DensityPerGridUnit,
                    [0.02, 0.05, 0.03]),
                new Distribution(
                    "fqmkm1",
                    new DistributionGrid.ByRatio(0.0, 0.001),
                    DistributionNormalisation.ProbabilityPerBin,
                    [0.4, 0.6]),
                new Distribution(
                    "epsdokfr",
                    new DistributionGrid.ByCategory(["0", "1", "2"]),
                    DistributionNormalisation.Unnormalised,
                    [3.78e-06, 0.000974, 0.000763]),
            ],
            PocketWallDistributions = new ConditionalOxidiserByPocketSize(
                [step, step * 2],
                [new Verified<Length>(Length.FromMicrometers(21.6), status)],
                [new Verified<Length>(Length.FromMicrometers(15.4), status)],
                [
                    new Distribution(
                        "fqdokkarm(1,:)",
                        new DistributionGrid.ByLength(Length.Zero, step),
                        DistributionNormalisation.DensityPerGridUnit,
                        [0.1, 0.9]),
                ]),
            Diagnostics = new RunDiagnostics
            {
                PocketsTotal = (long)run.Scalars["nkarm"],
                BaseParticleDraws = (long)run.Scalars["nfx"],
                SurroundingParticleDraws = (long)run.Scalars["nfy"],
                PocketSizeDraws = (long)run.Scalars["nfq"],
                BridgeDraws = (long)run.Scalars["nfw"],
                PassesDone = (int)run.Scalars["cycles_done"],
                BaseParticlesPerCycle = (long)run.Scalars["nbase_per_cycle"],
                BaseParticlesAccepted = (long)run.Scalars["nbase_accepted_cumulative"],
                RandomStreamStatistics =
                [
                    run.Scalars["epsx1"], run.Scalars["epsx2"], run.Scalars["epsx3"],
                    run.Scalars["epsx4"], run.Scalars["epsx5"], run.Scalars["epsx6"],
                ],
                FractionDrawAccuracy = [3.78e-06, 0.000974, 0.000763],

                // ⚠ Deliberately not empty, although every archived run is single-pass
                // and would carry no history: this fixture is a shape, not a replay -
                // its distributions are made-up numbers too - and an empty list would
                // let the round trip and the equality check pass over the member
                // without touching it.
                Convergence =
                [
                    new PassConvergence(0.11, 0.065, 0.089, 4.9e-3, 3.1e-2, 0.605),
                    new PassConvergence(0.09, 0.051, 0.070, 4.6e-4, 2.4e-2, 0.587),
                ],
                Conditions = new ConditionCounters(0, 0.00155, 306.8599, 11633.05, 5.7514, 0.0013, 0.0194, 0.8964, 0.0027),
                AttemptsTotal = Verified<long>.Unprinted(0),
                PocketSizeClampCount = Verified<long>.Unprinted(0),
                BridgeGapExceedsPocket = Verified<long>.Unprinted(0),
                BridgeWidthNegative = Verified<long>.Unprinted(0),
                BridgeVolumeNaNCount = Verified<long>.Unprinted(0),
                CyclesRequested = run.Cycles,
            },
        };
    }
}
