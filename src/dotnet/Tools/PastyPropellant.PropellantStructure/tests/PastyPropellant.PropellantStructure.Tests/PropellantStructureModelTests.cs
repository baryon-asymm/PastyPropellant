using System.Reflection;
using PastyPropellant.PropellantStructure.Configuration;
using PastyPropellant.PropellantStructure.Kernel;
using PastyPropellant.PropellantStructure.Results;
using PastyPropellant.PropellantStructure.Sampling;
using Xunit.Abstractions;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// The assembly's own entry point: the five things the root node's <c>BOOT.md</c>
/// asks of it, none of which any other test was checking.
/// </summary>
/// <remarks>
/// The gap this file closes is narrow and was easy to miss. Every L2 test builds a
/// <see cref="StructureSimulation"/> directly, and every probe calls
/// <see cref="PropellantStructureModel"/> — so the composition step between them,
/// which picks the generator off the configuration, was replayed by the probes and
/// validated by nothing at archive scale.
/// </remarks>
public sealed class PropellantStructureModelTests(ITestOutputHelper output)
{
    /// <summary>
    /// The entry point reproduces an archived run's integer counters exactly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// L2 is the same evidence and far more of it, but every one of its tests builds a
    /// <see cref="StructureSimulation"/> by hand and hands it
    /// <see cref="RandomStreamSet.Historical"/> outright. That is precisely the choice
    /// the entry point exists to make, so L2's verdict was a verdict about the kernel
    /// and said nothing about the composition step above it. The <c>gsv = 1</c> lesson
    /// in the root <c>BOOT.md</c> is this failure exactly: a setting that never reached
    /// the kernel, with every number still right.
    /// </para>
    /// <para>
    /// One run, the cheapest with an exact input, and integers only — the counters are
    /// the group that has to match to the unit, and if the entry point picked the wrong
    /// generator not one of them would survive.
    /// </para>
    /// </remarks>
    [Fact]
    [Trait("Category", "LongRunning")]
    public void TheEntryPointReproducesAnArchivedRunsCounters()
    {
        var run = ReferenceRuns.ById(ArchivedRunFixture.RunId);
        var diagnostics = PropellantStructureModel.Run(run.ToConfiguration()).Diagnostics;

        var counters = new (string Name, long Value)[]
        {
            ("nfx", diagnostics.BaseParticleDraws),
            ("nfy", diagnostics.SurroundingParticleDraws),
            ("nfq", diagnostics.PocketSizeDraws),
            ("nfw", diagnostics.BridgeDraws),
            ("nkarm", diagnostics.PocketsTotal),
        };

        var problems = new List<string>();
        foreach (var (name, value) in counters)
        {
            var expected = (long)run.Scalars[name];
            output.WriteLine($"{name,-6} {value,14} oracle {expected,14}");
            if (value != expected)
            {
                problems.Add($"{name}: {value}, oracle {expected}.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The entry point and the path L2 replays are the same run, bit for bit — which is
    /// what carries L2's verdict up to the public surface without paying for it again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The archived replay above proves the composition step on one run; this proves it
    /// on the three probe recipes in milliseconds, and it is the one that will fail
    /// first when someone edits <c>StreamsFor</c>, because the fast suite runs it.
    /// The arm the archive takes is asserted rather than assumed — every archived
    /// configuration must ask for <see cref="GeneratorSelection.Random2"/>, or the
    /// equivalence proved here would be an equivalence about a different branch.
    /// </para>
    /// <para>
    /// Equality is the record's own, and it compares lists and dictionaries by value —
    /// see <c>Results/Structural.cs</c>. A reference comparison would pass here for the
    /// wrong reason.
    /// </para>
    /// </remarks>
    [Fact]
    public void TheEntryPointIsTheRunLevelTwoReplays()
    {
        // the arm the archive uses, so the equivalence below is about the right one
        Assert.NotEmpty(ReferenceRuns.All);
        foreach (var run in ReferenceRuns.All)
        {
            Assert.Equal(GeneratorSelection.Random2, run.ToConfiguration().Generator);
        }

        foreach (var configuration in Probes)
        {
            var throughTheEntryPoint = PropellantStructureModel.Run(configuration);
            var throughTheKernel =
                new StructureSimulation(configuration, RandomStreamSet.Historical()).Run();

            Assert.Equal(throughTheKernel, throughTheEntryPoint);
        }
    }

    /// <summary>
    /// The same configuration twice gives the same numbers, bit for bit.
    /// </summary>
    /// <remarks>
    /// The model holds no state between calls and takes no seed, so this is a
    /// consequence rather than a hope — but it is the consequence that would break
    /// first if a generator state were ever made static "to avoid re-seeding", which is
    /// the obvious optimisation to reach for and the one that would make every run
    /// after the first unreproducible.
    /// </remarks>
    [Fact]
    public void TheSameConfigurationTwiceGivesTheSameNumbers()
    {
        foreach (var configuration in Probes)
        {
            var first = PropellantStructureModel.Run(configuration);
            var second = PropellantStructureModel.Run(configuration);

            Assert.Equal(first, second);

            // ⚠ and to the bit, which record equality on doubles does give but is worth
            // saying: a result that agreed to fifteen digits would satisfy Equals on
            // nothing here, yet "same numbers" is the claim being made.
            foreach (var (name, value) in first.Printed)
            {
                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(value.Value),
                    BitConverter.DoubleToInt64Bits(second.Printed[name].Value));
            }
        }
    }

    /// <summary>
    /// A run killed halfway still leaves its resolved record behind.
    /// </summary>
    /// <remarks>
    /// This is the whole reason the record is written before the first draw rather than
    /// after the last. A six-hour run that is killed is exactly the run whose record
    /// someone needs, and writing it at the end produces a record only for the runs
    /// that did not need one. Cancelling from inside the progress callback is what
    /// makes "halfway" literal: the run is stopped on a base-particle boundary it has
    /// already reached, not before it started.
    /// </remarks>
    [Fact]
    public void AKilledRunStillLeavesItsResolvedRecord()
    {
        var directory = Directory.CreateTempSubdirectory("propstruct-killed-");
        try
        {
            var path = Path.Combine(directory.FullName, "structure_run.resolved.json");
            var configuration = ProbeRun.Configuration with { ResolvedRunPath = path };

            using var cancellation = new CancellationTokenSource();
            var reached = 0;
            var progress = new Progress<StructureProgress>(_ => { });

            Assert.Throws<OperationCanceledException>(() => PropellantStructureModel.Run(
                configuration,
                new CancelAfter(() => ++reached >= 5, cancellation),
                cancellation.Token));

            Assert.True(reached >= 5, "the run was cancelled before it started, not halfway.");
            Assert.True(File.Exists(path), $"{path} was not written before the run was killed.");

            // and it is a record, not a stub: it reads back as the configuration asked for
            Assert.Equal(configuration, ResolvedRunRecord.Read(path).Configuration);

            GC.KeepAlive(progress);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Nothing public mentions a type from <c>Sampling/</c>, <c>Geometry/</c> or
    /// <c>Kernel/</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule is not tidiness. A caller able to hold a <c>RunState</c> could read a
    /// half-finished Monte Carlo and mistake a result of <i>unknown</i> precision for
    /// one of lower precision; a caller able to hold a generator could produce runs no
    /// archived report can adjudicate while they look exactly like runs one can.
    /// </para>
    /// <para>
    /// ⚠ Checked over signatures — parameters, returns, generic arguments, base types
    /// and interfaces — of every public member of every public type, not over the
    /// namespaces the assembly happens to declare. The leak this guards against is one
    /// public method returning one internal-namespace type, which a namespace-level
    /// check would not see at all.
    /// </para>
    /// <para>
    /// ⚠ There is <b>one</b> allowed exception, <c>Sampling.SizeDistributionLaw</c>,
    /// and the root <c>BOOT.md</c> says the list may not grow: a type that has surfaced
    /// stops being free to change under the original. So the exception is named here
    /// rather than pattern-matched, and the test also fails if the name stops being
    /// exposed — an allowlist nobody can see decay is how a list of one becomes a list
    /// of three.
    /// </para>
    /// </remarks>
    [Fact]
    public void NoPublicMemberExposesAnInternalNode()
    {
        string[] hidden =
        [
            "PastyPropellant.PropellantStructure.Sampling",
            "PastyPropellant.PropellantStructure.Geometry",
            "PastyPropellant.PropellantStructure.Kernel",
        ];

        const string allowed = "PastyPropellant.PropellantStructure.Sampling.SizeDistributionLaw";

        var assembly = typeof(PropellantStructureModel).Assembly;
        var problems = new List<string>();
        var allowanceUsed = false;

        foreach (var type in assembly.GetExportedTypes())
        {
            Inspect(type.Name, type.BaseType);
            foreach (var contract in type.GetInterfaces())
            {
                Inspect(type.Name, contract);
            }

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                switch (member)
                {
                    case MethodInfo method:
                        Inspect($"{type.Name}.{method.Name}", method.ReturnType);
                        foreach (var parameter in method.GetParameters())
                        {
                            Inspect($"{type.Name}.{method.Name}({parameter.Name})", parameter.ParameterType);
                        }

                        break;

                    case PropertyInfo property:
                        Inspect($"{type.Name}.{property.Name}", property.PropertyType);
                        break;

                    case FieldInfo field:
                        Inspect($"{type.Name}.{field.Name}", field.FieldType);
                        break;

                    case ConstructorInfo constructor:
                        foreach (var parameter in constructor.GetParameters())
                        {
                            Inspect($"{type.Name}.ctor({parameter.Name})", parameter.ParameterType);
                        }

                        break;
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
        Assert.True(
            allowanceUsed,
            $"{allowed} is no longer exposed. Good news, but this allowance must go with it: "
                + "an unused exception is one nobody notices being reused.");
        return;

        void Inspect(string where, Type? candidate)
        {
            if (candidate is null)
            {
                return;
            }

            // a Task<T> or an IReadOnlyList<T> hides the leak one level down
            foreach (var argument in candidate.GetGenericArguments())
            {
                Inspect(where, argument);
            }

            if (candidate.Namespace is not { } space || !hidden.Contains(space))
            {
                return;
            }

            if (candidate.FullName == allowed)
            {
                allowanceUsed = true;
                return;
            }

            problems.Add($"{where} exposes {candidate.FullName}, which belongs to an internal node.");
        }
    }

    /// <summary>
    /// The assembly depends on nothing from the optimiser.
    /// </summary>
    /// <remarks>
    /// The model is a microstructure calculator and the optimiser is a consumer of it.
    /// A reference in this direction would make the dependency a cycle the moment the
    /// optimiser starts calling the model — which is the point of porting it — and
    /// would drag the whole combustion model into the test run of a calculator that
    /// does not need it.
    /// </remarks>
    [Fact]
    public void TheAssemblyReferencesNothingFromTheOptimiser()
    {
        var referenced = typeof(PropellantStructureModel).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToList();

        var offenders = referenced
            .Where(name => name.StartsWith("ParametricCombustionModel", StringComparison.Ordinal)
                || name.StartsWith("PastyPropellant.ConsoleApp", StringComparison.Ordinal)
                || name.StartsWith("PastyPropellant.ProcessHandling", StringComparison.Ordinal))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"the model references {string.Join(", ", offenders)}.");

        // ⚠ and the check is not vacuous: the assembly does reference things, so an
        // empty list would mean the reflection above found nothing to look at.
        Assert.NotEmpty(referenced);
    }

    /// <summary>The three probe recipes, which run in milliseconds and cover three branches.</summary>
    private static IEnumerable<StructureRunConfiguration> Probes =>
        [ProbeRun.Configuration, ProbeRun3.Configuration, ProbeRun8.Configuration];

    /// <summary>
    /// Cancels the run once the callback has fired often enough to have started.
    /// </summary>
    /// <remarks>
    /// A hand-written <see cref="IProgress{T}"/> rather than
    /// <see cref="Progress{T}"/>: the latter marshals to a synchronisation context and
    /// invokes asynchronously, so the cancellation would land at an unpredictable point
    /// or after the run had already finished.
    /// </remarks>
    private sealed class CancelAfter(Func<bool> ready, CancellationTokenSource cancellation)
        : IProgress<StructureProgress>
    {
        public void Report(StructureProgress value)
        {
            if (ready())
            {
                cancellation.Cancel();
            }
        }
    }
}
