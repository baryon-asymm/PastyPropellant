using System.Reflection;
using System.Text;
using PastyPropellant.PropellantStructure.Configuration;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// Checks the two things the project's own protocol asks for but nothing enforced:
/// that every node of the tree carries its pair of documents, and that the public
/// surface of the assembly has not moved without anyone saying so.
/// </summary>
/// <remarks>
/// Both were listed as open questions in the appendix of <c>AGENTS.md</c>. Neither
/// test claims the documents are <em>correct</em> — no test can. They claim the
/// documents exist and that a change to the contract shows up in a diff instead of
/// slipping through, which is the part a machine can hold.
/// </remarks>
public sealed class ProtocolTests
{
    private static string PortRoot => RepositoryPaths.Resolve(
        "src", "dotnet", "Tools", "PastyPropellant.PropellantStructure");

    /// <summary>
    /// The tree invariant of <c>AGENTS.md</c> §1: a source directory is a node, and
    /// a node without its pair of documents is a level nobody described.
    /// </summary>
    [Fact]
    public void EverySourceDirectoryCarriesBootAndApi()
    {
        var missing = new List<string>();

        foreach (var directory in SourceDirectories())
        {
            foreach (var document in (string[])["BOOT.md", "API.md"])
            {
                if (!File.Exists(Path.Combine(directory, document)))
                {
                    missing.Add(Path.GetRelativePath(PortRoot, Path.Combine(directory, document)));
                }
            }
        }

        Assert.True(
            missing.Count == 0,
            "AGENTS.md §1 requires BOOT.md and API.md in every source directory. Missing:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, missing.Order(StringComparer.Ordinal)));
    }

    /// <summary>
    /// A tripwire on the assembly's public surface.
    /// </summary>
    /// <remarks>
    /// This is <b>not</b> a substitute for <c>API.md</c> — a generated list cannot
    /// carry intent, and the protocol's contract is the document. What it does is
    /// make a contract change impossible to commit silently: the baseline moves in
    /// the same diff, so a reviewer sees that the surface changed and can ask
    /// whether <c>API.md</c> moved with it.
    /// </remarks>
    [Fact]
    public void ThePublicSurfaceMatchesItsBaseline()
    {
        var baselinePath = Path.Combine(
            PortRoot, "tests", "PastyPropellant.PropellantStructure.Tests", "PublicSurface.approved.txt");

        var actual = Describe(typeof(StructureRunConfiguration).Assembly);

        if (!File.Exists(baselinePath))
        {
            File.WriteAllText(baselinePath, actual);
            Assert.Fail(
                $"No baseline of the public surface existed, so one was written to {baselinePath}. "
                + "Read it, satisfy yourself that it is the contract API.md describes, and re-run.");
        }

        var approved = File.ReadAllText(baselinePath);
        if (string.Equals(Normalise(approved), Normalise(actual), StringComparison.Ordinal))
        {
            return;
        }

        var rejectedPath = Path.ChangeExtension(baselinePath, ".actual.txt");
        File.WriteAllText(rejectedPath, actual);
        Assert.Fail(
            "The assembly's public surface no longer matches PublicSurface.approved.txt."
            + Environment.NewLine
            + $"What it is now was written to {rejectedPath}."
            + Environment.NewLine
            + "If the change is intended, update API.md of the node that owns the type first, then "
            + "replace the baseline with the .actual.txt file in the same commit.");
    }

    /// <summary>
    /// Every test that replays an archived run carries
    /// <c>[Trait("Category", "LongRunning")]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The repository's own rule (root <c>CLAUDE.md</c>, this node's <c>BOOT.md</c>
    /// §Ограничения) was a convention, and a convention is exactly the wrong shape for
    /// it: the cost of forgetting the trait lands on whoever next runs the fast suite,
    /// not on whoever omitted it, and it lands as "the fast suite got slow" rather than
    /// as a failure pointing at a method. The fast suite is the signal this subtree runs
    /// on dozens of times a day, and it stays fast only if this holds.
    /// </para>
    /// <para>
    /// ⚠ This is a proxy, not a proof. A replay is recognised by taking
    /// <see cref="ArchivedRunFixture.ReplayableRuns"/> as member data, which is how all
    /// four L2 groups reach the archive, plus one named exception below.
    /// </para>
    /// <para>
    /// ⚠ The obvious wider rule — every test in a class that takes the fixture — was
    /// tried and is wrong. It flags
    /// <c>APreparatoryPassRunsBeforeEveryWorkingOne</c> and
    /// <c>TheToleranceTableClassifiesEveryPrintedScalarExactlyOnce</c>, which sit in
    /// the L2 class but replay nothing and finish in milliseconds. Marking those
    /// <c>LongRunning</c> to satisfy this test would cost the fast suite two real
    /// checks — the rule would have made the thing it guards worse.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryArchivedReplayIsMarkedLongRunning()
    {
        var unmarked = new List<string>();

        foreach (var type in typeof(ProtocolTests).Assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!IsTest(method))
                {
                    continue;
                }

                var byMemberData = method
                    .GetCustomAttributesData()
                    .Any(attribute =>
                        attribute.AttributeType.Name == "MemberDataAttribute"
                        && attribute.ConstructorArguments.Count > 0
                        && (attribute.ConstructorArguments[0].Value as string)
                            == nameof(ArchivedRunFixture.ReplayableRuns));

                if (byMemberData && !IsLongRunning(method))
                {
                    unmarked.Add($"{type.Name}.{method.Name}");
                }
            }
        }

        // ⚠ The tests that replay the archive without going through the member data:
        // each names its run itself. Listed through nameof so a rename moves the entry
        // here rather than silently dropping it from the rule.
        (Type Type, string Method)[] byName =
        [
            (typeof(PropellantStructureModelTests),
                nameof(PropellantStructureModelTests.TheEntryPointReproducesAnArchivedRunsCounters)),
            (typeof(PropellantJsonProvenanceTests),
                nameof(PropellantJsonProvenanceTests.TheBimodalRecipeIsRunHp1)),
            (typeof(PropellantJsonProvenanceTests),
                nameof(PropellantJsonProvenanceTests.TheFineRecipeIsRunHp1050)),
            (typeof(PropellantJsonProvenanceTests),
                nameof(PropellantJsonProvenanceTests.TheCoarseRecipeIsRunHp180)),
        ];

        foreach (var (type, name) in byName)
        {
            if (!IsLongRunning(type.GetMethod(name)!))
            {
                unmarked.Add($"{type.Name}.{name}");
            }
        }

        Assert.True(
            unmarked.Count == 0,
            "these replay an archived run and so belong in the LongRunning category, or the "
            + "fast suite silently stops being fast:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, unmarked.Order(StringComparer.Ordinal)));
    }

    /// <summary>
    /// The reference archive still holds every run the tests were written against.
    /// </summary>
    /// <remarks>
    /// A floor rather than an equality, and the asymmetry is the whole design: the
    /// archive is meant to grow — <c>reference/propstruct/README.md</c> describes how a
    /// run is added — and a test failing on growth would make growing it a chore, which
    /// is how an archive stops growing. Shrinking is the other thing entirely. It
    /// removes evidence from tests that go on claiming to rest on it, and it removes it
    /// silently, because a test reading only the runs that remain still passes.
    /// </remarks>
    [Fact]
    public void TheReferenceArchiveHasNotShrunk()
    {
        Assert.True(
            ReferenceRuns.All.Count >= ReferenceRuns.ArchivedRunsAtLeast,
            $"runs.json holds {ReferenceRuns.All.Count} runs; the tests were written against "
            + $"{ReferenceRuns.ArchivedRunsAtLeast}. Runs may be added freely - if one was "
            + "genuinely dropped, say why in reference/propstruct/README.md and lower the floor "
            + "in the same commit.");

        // and the ids are distinct, since ById takes the first match: two runs sharing an
        // id would leave one of them unreachable while the count still looked right.
        var duplicates = ReferenceRuns.All
            .GroupBy(run => run.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} x{group.Count()}")
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.True(duplicates.Count == 0, $"duplicate run ids: {string.Join(", ", duplicates)}");
    }

    private static bool IsTest(MethodInfo method) =>
        method.GetCustomAttributesData().Any(attribute =>
            attribute.AttributeType.Name is "FactAttribute" or "TheoryAttribute");

    private static bool IsLongRunning(MethodInfo method) =>
        method.GetCustomAttributesData().Any(attribute =>
            attribute.AttributeType.Name == "TraitAttribute"
            && attribute.ConstructorArguments.Count == 2
            && (attribute.ConstructorArguments[0].Value as string) == "Category"
            && (attribute.ConstructorArguments[1].Value as string) == "LongRunning");

    private static IEnumerable<string> SourceDirectories()
    {
        foreach (var file in Directory.EnumerateFiles(PortRoot, "*.cs", SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(file)!;
            if (directory.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || directory.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || directory.EndsWith($"{Path.DirectorySeparatorChar}obj", StringComparison.Ordinal)
                || directory.EndsWith($"{Path.DirectorySeparatorChar}bin", StringComparison.Ordinal))
            {
                continue;
            }

            yield return directory;
        }
    }

    private static string Describe(Assembly assembly)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Public surface of " + assembly.GetName().Name);
        builder.AppendLine("# Generated by ProtocolTests.ThePublicSurfaceMatchesItsBaseline.");
        builder.AppendLine("# A tripwire, not a contract: the contract is the API.md of each node.");

        foreach (var type in assembly.GetExportedTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            builder.AppendLine();
            builder.AppendLine(type.FullName);

            var members = type
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(IsWorthListing)
                .Select(Describe)
                .Order(StringComparer.Ordinal);

            foreach (var member in members)
            {
                builder.AppendLine("    " + member);
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Drops what the compiler wrote rather than the author: property accessors
    /// (already reported through the property), the record clone helper, and the
    /// backing field of an enum. Everything else stays, operators and
    /// <c>Deconstruct</c> included — those are surface a caller can bind to.
    /// </summary>
    private static bool IsWorthListing(MemberInfo member) => member switch
    {
        MethodInfo method when method.IsSpecialName
                               && (method.Name.StartsWith("get_", StringComparison.Ordinal)
                                   || method.Name.StartsWith("set_", StringComparison.Ordinal)) => false,
        MethodInfo { Name: "<Clone>$" } => false,
        FieldInfo { Name: "value__" } => false,
        _ => true,
    };

    private static string Describe(MemberInfo member) => member switch
    {
        MethodInfo method =>
            $"{Name(method.ReturnType)} {method.Name}({string.Join(", ", method.GetParameters().Select(Describe))})",
        ConstructorInfo constructor =>
            $".ctor({string.Join(", ", constructor.GetParameters().Select(Describe))})",
        PropertyInfo property =>
            $"{Name(property.PropertyType)} {property.Name} {{ {(property.CanRead ? "get; " : string.Empty)}"
            + $"{Setter(property)}}}",
        FieldInfo field => $"{Name(field.FieldType)} {field.Name}",
        EventInfo @event => $"event {Name(@event.EventHandlerType!)} {@event.Name}",
        Type nested => $"nested {nested.Name}",
        _ => $"{member.MemberType} {member.Name}",
    };

    private static string Describe(ParameterInfo parameter) => $"{Name(parameter.ParameterType)} {parameter.Name}";

    /// <summary>
    /// Tells <c>init</c> from <c>set</c>. Reflection reports both as writable, but
    /// the difference is exactly the immutability this tree keeps insisting on, so a
    /// baseline that hid it would miss the change worth catching.
    /// </summary>
    private static string Setter(PropertyInfo property)
    {
        var setter = property.SetMethod;
        if (setter is null || !setter.IsPublic)
        {
            return string.Empty;
        }

        var initOnly = setter.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

        return initOnly ? "init; " : "set; ";
    }

    private static string Name(Type type) =>
        type.IsGenericType
            ? type.Name[..type.Name.IndexOf('`')]
              + "<" + string.Join(", ", type.GetGenericArguments().Select(Name)) + ">"
            : type.Name;

    private static string Normalise(string text) => text.ReplaceLineEndings("\n").TrimEnd();
}
