using System.Collections.ObjectModel;
using System.Reflection;
using PastyPropellant.ConsoleApp.Helpers;
using PastyPropellant.Core.Models.Events.Logs;
using PastyPropellant.Core.Utils;
using PastyPropellant.Interop;
using PastyPropellant.ProcessHandling.Models.Events.Logs;
using PastyPropellant.RegionMapper.Mappers;
using PastyPropellant.RegionMapper.Models;
using UnitsNet;

namespace PastyPropellant.ConsoleApp.Tests;

/// <summary>
/// Covers <see cref="PreparePropellantDataHelper"/> itself.
///
/// <para>The class has an awkward test surface: its three Python collaborators are constructed
/// concretely in the constructor, so there is no seam through which a fake can be injected without
/// changing production code. The coverage here is therefore split three ways —</para>
/// <list type="bullet">
///   <item>constructor contract and eager argument handling, which need no process at all;</item>
///   <item><c>GetPreparedPropellantDataCollection</c>, the one piece of genuine pure logic in the
///     class (a three-way join by propellant name), reached by reflection because it is private and
///     unreachable otherwise;</item>
///   <item>the failure path of <c>PrepareAsync</c>, which does launch the interpreter but fails on
///     the first script invocation, so it costs milliseconds rather than the hours the success path
///     costs.</item>
/// </list>
///
/// <para>The single success-path test remains marked <c>LongRunning</c> and is unchanged.</para>
/// </summary>
public class PreparePropellantDataHelperTest
{
    public static readonly string ArtifactDirectoryPath = PythonRuntime.ResolveRepositoryPath("artifacts/output_prepare");
    public static readonly string PyMapperScriptPath = PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/src/main.py");
    public static readonly string PyThermodynamicsScriptPath = PythonRuntime.ResolveRepositoryPath("externals/src/python/AerospacePropellantThermodynamics/src/main.py");
    public static readonly string PyPorosityScriptPath = PythonRuntime.ResolveRepositoryPath("src/python/PorosityCalculation/src/main.py");

    // Very long running test
    [Fact]
    [Trait("Category", "LongRunning")]
    public async Task PrepareAsync_ShouldSuccessReturnOperationResult()
    {
        // Arrange
        var pressures = GetPressures();
        var preparePropellantHelper = new PreparePropellantDataHelper(
            ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, pressures);

        var propellantsFilePath = PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/data/propellants.json");
        var componentsFilePath = PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/data/components.json");
        var combustionProductsFilePath = PythonRuntime.ResolveRepositoryPath("externals/src/python/AerospacePropellantThermodynamics/data/combustion_products.json");

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            propellantsFilePath, componentsFilePath, combustionProductsFilePath);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task PrepareAsync_ShouldExceptionReturnOperationResult()
    {
        // Arrange
        var preparePropellantHelper = MakeHelper();

        // Act
        var result = await preparePropellantHelper.PrepareAsync(
            FakePropellantsFilePath, FakeComponentsFilePath, FakeCombustionProductsFilePath);

        // Assert
        Assert.False(result.IsSuccess);

        // The failure has to arrive as a carried exception, not as a null-valued success: the caller
        // (ConstructPropellantJsonHelper's driver) dereferences Value! on the success branch.
        Assert.NotNull(result.Exception);
        Assert.Null(result.Value);
    }

    // ---------------------------------------------------------------------------------------------
    // Constructor contract — no interpreter, no filesystem.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void Constructor_RejectsANullPressureSequence()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new PreparePropellantDataHelper(
            ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, null!));

        Assert.Equal("pressures", exception.ParamName);
    }

    /// <summary>
    /// Characterisation: the constructor validates nothing about the four paths it is handed — not
    /// the artifact directory, not any of the three script paths. Every path error is deferred to
    /// <c>PrepareAsync</c>, where it surfaces as a failed <c>OperationResult</c>. Pinned because the
    /// deferral is what makes the failure-path tests below possible at all.
    /// </summary>
    [Fact]
    public void Constructor_DoesNotValidateAnyOfItsPaths()
    {
        var nonexistent = Path.Combine(Path.GetTempPath(), $"pastypropellant_absent_{Guid.NewGuid():N}");

        var helper = new PreparePropellantDataHelper(
            Path.Combine(nonexistent, "artifacts"),
            Path.Combine(nonexistent, "mapper.py"),
            Path.Combine(nonexistent, "thermodynamics.py"),
            Path.Combine(nonexistent, "porosity.py"),
            GetPressures());

        Assert.NotNull(helper);
        Assert.False(Directory.Exists(nonexistent), "the constructor must not create anything on disk");
    }

    /// <summary>
    /// The pressure sequence is enumerated exactly once, during construction (it is copied into the
    /// region mapper's <c>ReadOnlyCollection</c>). That matters because a caller may hand in a LINQ
    /// projection: a class that re-enumerated it per pressure frame would silently do the work N
    /// times, and one that deferred enumeration would move an argument error into a background solve.
    /// </summary>
    [Fact]
    public void Constructor_EnumeratesThePressureSequenceExactlyOnce()
    {
        var enumerations = 0;

        IEnumerable<Pressure> CountingPressures()
        {
            enumerations++;
            yield return Pressure.FromMegapascals(1);
            yield return Pressure.FromMegapascals(6.5);
        }

        _ = new PreparePropellantDataHelper(
            ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath,
            CountingPressures());

        Assert.Equal(1, enumerations);
    }

    // ---------------------------------------------------------------------------------------------
    // The three-way join. Private, so reached by reflection: giving it a seam would mean editing the
    // class under test, which this change is not allowed to do. The MethodInfo lookup asserts rather
    // than NREs so a rename fails with a readable message instead of a null dereference.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The join is keyed on propellant name, and the output order follows the <i>region-mapped</i>
    /// collection — not the thermodynamics or porosity collections, which the parallel/sequential
    /// stages above may well produce in a different order. The inputs here are deliberately shuffled
    /// relative to each other so an implementation that zipped by index would fail.
    /// </summary>
    [Fact]
    public void GetPreparedPropellantDataCollection_JoinsByName_AndFollowsTheRegionMappedOrder()
    {
        var regionMapped = new ReadOnlyCollection<PropellantRegionMapped>(
        [
            MakeRegionMapped("Bas_0"),
            MakeRegionMapped("Bas_1"),
            MakeRegionMapped("Bas_2")
        ]);

        var thermodynamics = new ReadOnlyCollection<ThermodynamicsPropellantResult>(
        [
            MakeThermodynamics("Bas_2"),
            MakeThermodynamics("Bas_0"),
            MakeThermodynamics("Bas_1")
        ]);

        var porosity = new ReadOnlyCollection<PorosityPropellantResult>(
        [
            MakePorosity("Bas_1"),
            MakePorosity("Bas_2"),
            MakePorosity("Bas_0")
        ]);

        var prepared = InvokeGetPreparedPropellantDataCollection(regionMapped, thermodynamics, porosity);

        Assert.Equal(["Bas_0", "Bas_1", "Bas_2"], prepared.Select(x => x.Name));

        // Each entry must carry ITS OWN propellant's thermodynamics and porosity. The marker paths
        // embed the name, so a mis-join shows up as a mismatched path rather than a count mismatch.
        foreach (var entry in prepared)
        {
            Assert.Equal($"{entry.Name}/inter_pocket.json", entry.PressureFrameThermodynamics[0].InterPocketFilePath);
            Assert.Equal($"{entry.Name}/porosity.json", entry.PorosityPropellants[0].PorosityFilePath);
        }
    }

    /// <summary>
    /// Characterisation of the <c>First(...)</c> the join uses: a propellant present in the region
    /// map but absent from one of the downstream collections throws rather than being skipped or
    /// yielding an empty entry. Documented here because it is the same fail-loud convention the
    /// scenario relies on when it partitions the propellants file by name.
    /// </summary>
    [Fact]
    public void GetPreparedPropellantDataCollection_ThrowsWhenADownstreamResultIsMissing()
    {
        var regionMapped = new ReadOnlyCollection<PropellantRegionMapped>(
            [MakeRegionMapped("Bas_0"), MakeRegionMapped("Bas_1")]);

        var thermodynamics = new ReadOnlyCollection<ThermodynamicsPropellantResult>(
            [MakeThermodynamics("Bas_0")]);

        var porosity = new ReadOnlyCollection<PorosityPropellantResult>(
            [MakePorosity("Bas_0"), MakePorosity("Bas_1")]);

        Assert.Throws<InvalidOperationException>(
            () => InvokeGetPreparedPropellantDataCollection(regionMapped, thermodynamics, porosity));
    }

    [Fact]
    public void GetPreparedPropellantDataCollection_OnEmptyInput_ReturnsAnEmptyCollection()
    {
        var prepared = InvokeGetPreparedPropellantDataCollection(
            new ReadOnlyCollection<PropellantRegionMapped>([]),
            new ReadOnlyCollection<ThermodynamicsPropellantResult>([]),
            new ReadOnlyCollection<PorosityPropellantResult>([]));

        Assert.Empty(prepared);
    }

    // ---------------------------------------------------------------------------------------------
    // Event-bus behaviour on the failure path.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// The stage banners are the only progress signal an operator gets during the multi-hour prepare,
    /// so the first one must be published <i>before</i> the region mapper is invoked — otherwise a run
    /// that dies inside region mapping looks like a run that never started.
    /// </summary>
    [Fact]
    public async Task PrepareAsync_PublishesTheMappingStageBanner_BeforeItFails()
    {
        var messages = new List<string>();
        using var scope = new BusScope<InfoLogEvent>();

        scope.Subscribe(e =>
        {
            if (e.Sender == nameof(PreparePropellantDataHelper))
                lock (messages) messages.Add(e.Message);
        });

        var result = await MakeHelper().PrepareAsync(
            FakePropellantsFilePath, FakeComponentsFilePath, FakeCombustionProductsFilePath);

        Assert.False(result.IsSuccess);

        // Region mapping failed, so ONLY the first banner may have been published — reaching the
        // porosity or thermodynamics banner would mean the failure was not short-circuited.
        Assert.Equal(["Mapping regions..."], messages);
    }

    /// <summary>
    /// ⚠ CHARACTERISATION OF A BUG — asserts what the code does today, not what it should do.
    ///
    /// <para><c>TryPrepareAsync</c> subscribes <c>RegionMapperProcessInfoLogEventHandler</c> to the
    /// process-wide <c>EventBus&lt;ProcessInfoLogEvent&gt;</c>, and unsubscribes it on the line
    /// <i>after</i> the early <c>return</c> that handles a failed region-mapping result. There is no
    /// <c>try</c>/<c>finally</c>. So every failed <c>PrepareAsync</c> leaves one handler permanently
    /// registered on a static bus, and the leak accumulates per call: after N failures, every
    /// <c>ProcessInfoLogEvent</c> published anywhere in the process — by any component, not just the
    /// region mapper — is re-published N extra times as an <c>InfoLogEvent</c> attributed to
    /// <c>PythonRegionMapper</c>. The same shape exists on the porosity failure branch.</para>
    ///
    /// <para>In the console host each failure ends the run, so today the blast radius is limited to
    /// duplicated console lines. It is recorded rather than fixed because fixing it means editing the
    /// class under test.</para>
    ///
    /// <para>Assembly-level test parallelisation is disabled (see <c>TestParallelization.cs</c>),
    /// which is what makes the exact <c>before + 1</c> count below deterministic against a static bus.</para>
    /// </summary>
    [Fact]
    public async Task PrepareAsync_WhenRegionMappingFails_LeavesItsProcessLogHandlerSubscribed()
    {
        const string marker = "relay-probe-4f2a";

        var relays = 0;
        using var scope = new BusScope<InfoLogEvent>();

        scope.Subscribe(e =>
        {
            // Only count relays OF THE PROBE: the failing run itself emits real region-mapper output
            // through the same handler, which must not be mistaken for a leak.
            if (e.Sender == nameof(PythonRegionMapper) && e.Message == marker)
                Interlocked.Increment(ref relays);
        });

        int ProbeRelayCount()
        {
            var before = Volatile.Read(ref relays);
            EventBus<ProcessInfoLogEvent>.Publish(new ProcessInfoLogEvent(marker, "probe"));
            return Volatile.Read(ref relays) - before;
        }

        var leakedBefore = ProbeRelayCount();

        var result = await MakeHelper().PrepareAsync(
            FakePropellantsFilePath, FakeComponentsFilePath, FakeCombustionProductsFilePath);

        Assert.False(result.IsSuccess);

        var leakedAfter = ProbeRelayCount();

        Assert.Equal(leakedBefore + 1, leakedAfter);
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static string FakePropellantsFilePath =>
        PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/fake/propellants.json");

    private static string FakeComponentsFilePath =>
        PythonRuntime.ResolveRepositoryPath("src/python/RegionMapper/fake/components.json");

    private static string FakeCombustionProductsFilePath =>
        PythonRuntime.ResolveRepositoryPath("externals/src/python/AerospacePropellantThermodynamics/fake/combustion_products.json");

    private static PreparePropellantDataHelper MakeHelper() => new(
        ArtifactDirectoryPath, PyMapperScriptPath, PyThermodynamicsScriptPath, PyPorosityScriptPath, GetPressures());

    private static ReadOnlyCollection<Pressure> GetPressures()
    {
        var maxPressure = Pressure.FromMegapascals(6.5);
        var minPressure = Pressure.FromMegapascals(1);
        const int pressurePoints = 2;
        return new ReadOnlyCollection<Pressure>(Enumerable.Range(0, pressurePoints)
            .Select(x => minPressure + (maxPressure - minPressure) / (pressurePoints - 1) * x)
            .ToArray());
    }

    private static readonly Pressure ProbePressure = Pressure.FromMegapascals(1);

    private static PropellantRegionMapped MakeRegionMapped(string name) =>
        new(name, new ReadOnlyCollection<PressureFrame>(
        [
            new PressureFrame(
                ProbePressure,
                $"{name}/mapped_inter_pocket.json",
                $"{name}/mapped_pocket_without_skeleton.json",
                $"{name}/mapped_pocket_with_skeleton.json",
                $"{name}/mapped_diffusion.json")
        ]));

    private static ThermodynamicsPropellantResult MakeThermodynamics(string name) =>
        new(name, new ReadOnlyCollection<PressureFrameThermodynamics>(
        [
            new PressureFrameThermodynamics(
                ProbePressure,
                $"{name}/inter_pocket.json",
                $"{name}/pocket_without_skeleton.json",
                $"{name}/pocket_with_skeleton.json",
                $"{name}/diffusion.json")
        ]));

    private static PorosityPropellantResult MakePorosity(string name) =>
        new(name, new ReadOnlyCollection<PressureFramePorosity>(
        [
            new PressureFramePorosity(ProbePressure, $"{name}/porosity.json")
        ]));

    private static ReadOnlyCollection<PreparedPropellantData> InvokeGetPreparedPropellantDataCollection(
        ReadOnlyCollection<PropellantRegionMapped> regionMapped,
        ReadOnlyCollection<ThermodynamicsPropellantResult> thermodynamics,
        ReadOnlyCollection<PorosityPropellantResult> porosity)
    {
        const string methodName = "GetPreparedPropellantDataCollection";

        var method = typeof(PreparePropellantDataHelper)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.True(method != null,
                    $"{nameof(PreparePropellantDataHelper)}.{methodName} was not found — if it was renamed or "
                    + "made public, update this test rather than deleting it.");

        try
        {
            return (ReadOnlyCollection<PreparedPropellantData>)method!.Invoke(
                MakeHelper(), [regionMapped, thermodynamics, porosity])!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Surface the real exception so Assert.Throws<T> sees the type the production code threw.
            throw ex.InnerException;
        }
    }

    /// <summary>
    /// Removes every handler it registered on the process-wide <see cref="EventBus{TEvent}"/>, even if
    /// the test body throws. A leaked test handler would corrupt whichever test ran next.
    /// </summary>
    private sealed class BusScope<TEvent> : IDisposable
        where TEvent : struct
    {
        private readonly List<Action<TEvent>> _handlers = [];

        public void Subscribe(Action<TEvent> handler)
        {
            EventBus<TEvent>.Subscribe(handler);
            _handlers.Add(handler);
        }

        public void Dispose()
        {
            foreach (var handler in _handlers)
                EventBus<TEvent>.Unsubscribe(handler);

            _handlers.Clear();
        }
    }
}
