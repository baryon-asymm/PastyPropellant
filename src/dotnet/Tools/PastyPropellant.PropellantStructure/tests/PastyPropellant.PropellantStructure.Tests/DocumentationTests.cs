using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using PastyPropellant.PropellantStructure.Configuration;

namespace PastyPropellant.PropellantStructure.Tests;

/// <summary>
/// Checks that the tree's documents still describe the tree.
/// </summary>
/// <remarks>
/// <para>
/// <c>ProtocolTests</c> holds two things: that every node carries its pair of
/// documents, and that the public surface has not moved silently. Neither says
/// anything about whether the documents are <em>true</em>. These tests take the part
/// of that question a machine can answer: a declared dependency that does not exist,
/// a dependency that exists and was not declared, a public type nobody described, a
/// declaration in an <c>API.md</c> marked as implemented that the assembly does not
/// have, a missing section, a dead link.
/// </para>
/// <para>
/// What they still cannot check is whether a document says the right thing about the
/// code it correctly names. That stays a human's job, and no amount of tooling moves
/// it.
/// </para>
/// </remarks>
public sealed class DocumentationTests
{
    private static string PortRoot => RepositoryPaths.Resolve(
        "src", "dotnet", "Tools", "PastyPropellant.PropellantStructure");

    private static string SourceRoot => Path.Combine(PortRoot, "src", "PastyPropellant.PropellantStructure");

    private static Assembly Port => typeof(StructureRunConfiguration).Assembly;

    /// <summary>
    /// The node at the root of the tree — the one whose directory contributes no
    /// segment to a namespace, and whose <c>API.md</c> a sibling spells `../API.md`.
    /// </summary>
    private const string RootNode = "PropellantStructure";

    /// <summary>
    /// A node's <c>BOOT.md</c> lists what it uses from its neighbours. The assembly
    /// knows what it actually uses.
    /// </summary>
    /// <remarks>
    /// This is the one check here that closes a hole something already fell into:
    /// <c>Results/BOOT.md</c> said "Зависимости: Нет" while <c>StructureResult.Run</c>
    /// had carried a type from <c>Configuration/</c> since the first draft of the
    /// contract. The lie survived because nothing but reading could catch it.
    /// </remarks>
    [Fact]
    public void EveryNodeDeclaresTheNeighboursItActuallyUses()
    {
        var problems = new List<string>();

        foreach (var (node, actual) in ActualDependencies())
        {
            var bootPath = Path.Combine(SourceRoot, node, "BOOT.md");
            if (!File.Exists(bootPath))
            {
                continue;
            }

            var declared = DeclaredDependencies(File.ReadAllText(bootPath));

            foreach (var used in actual.Except(declared).Order(StringComparer.Ordinal))
            {
                problems.Add(
                    $"{node}/BOOT.md does not declare {used}/, but {node}/ uses its types: "
                    + $"{string.Join(", ", TypesCrossing(node, used).Order(StringComparer.Ordinal))}.");
            }

            foreach (var unused in declared.Except(actual).Order(StringComparer.Ordinal))
            {
                problems.Add(
                    $"{node}/BOOT.md declares a dependency on {unused}/, but no type of {node}/ refers to it. "
                    + "Either the dependency went away and the document did not, or it was never real.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// A type visible outside the assembly that no <c>API.md</c> mentions is a
    /// contract nobody wrote down.
    /// </summary>
    /// <remarks>
    /// The public-surface baseline catches a type that <em>changes</em>; it cannot
    /// catch one that was added and left undescribed, because it is generated from
    /// the same code. This is the other half.
    /// </remarks>
    [Fact]
    public void EveryPublicTypeIsNamedByItsOwnNodeApi()
    {
        var problems = new List<string>();

        foreach (var type in Port.GetExportedTypes())
        {
            var node = type.Namespace?.Split('.').Last();
            if (node is null)
            {
                continue;
            }

            var apiPath = node == RootNode
                ? Path.Combine(SourceRoot, "API.md")
                : Path.Combine(SourceRoot, node, "API.md");

            if (!File.Exists(apiPath))
            {
                problems.Add($"{type.FullName} lives in {node}/, which has no API.md.");
                continue;
            }

            var name = SimpleName(type);
            if (!File.ReadAllText(apiPath).Contains(name, StringComparison.Ordinal))
            {
                problems.Add($"{apiPath} never names {name}, which it exports.");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Every declaration in a C# block of an <c>API.md</c> that is marked implemented
    /// has to exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Names only, not full signatures: the signatures are already pinned by
    /// <c>PublicSurface.approved.txt</c>, and a second copy of them in prose would be
    /// a second thing to keep in step.
    /// </para>
    /// <para>
    /// Strictness follows the status mark. A block under a ✅ heading is checked; one
    /// under ⏳ is a design sketch and is not. That makes the marks load-bearing:
    /// leaving ✅ on a node that has not been written turns its <c>API.md</c> into a
    /// failing test rather than a stale page.
    /// </para>
    /// </remarks>
    [Fact]
    public void EveryImplementedDeclarationInAnApiDocumentExists()
    {
        var byName = AllTypesByName();
        var problems = new List<string>();

        foreach (var path in Directory.EnumerateFiles(PortRoot, "API.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            foreach (var block in ImplementedCsharpBlocks(File.ReadAllText(path)))
            {
                Type? current = null;
                var currentIsEnum = false;
                var typesInBlock = new HashSet<string>(StringComparer.Ordinal);

                foreach (var declaration in Declarations(block))
                {
                    if (declaration.IsType)
                    {
                        typesInBlock.Add(declaration.Name);
                        if (!byName.TryGetValue(declaration.Name, out current))
                        {
                            problems.Add($"{Relative(path)}: no type named {declaration.Name} in the port.");
                        }

                        currentIsEnum = current?.IsEnum ?? false;
                        continue;
                    }

                    // A member named after a type declared in the same block is a
                    // constructor - of this type or of the one it is nested in, which
                    // is why the whole block's names count and not just the current
                    // type's. Reflection reports constructors under .ctor, so looking
                    // one up by its written name would always fail.
                    if (typesInBlock.Contains(declaration.Name))
                    {
                        continue;
                    }

                    if (current is null || (declaration.IsEnumMember && !currentIsEnum))
                    {
                        continue;
                    }

                    if (current.GetMember(
                            declaration.Name,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                            | BindingFlags.Static | BindingFlags.FlattenHierarchy).Length == 0
                        && current.GetNestedType(declaration.Name, BindingFlags.Public | BindingFlags.NonPublic) is null)
                    {
                        problems.Add($"{Relative(path)}: {SimpleName(current)} has no member named {declaration.Name}.");
                    }
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// The six sections <c>AGENTS.md</c> §6 makes the condition for leaving design
    /// mode.
    /// </summary>
    [Fact]
    public void EveryBootDocumentCarriesTheSectionsTheProtocolRequires()
    {
        string[] required = ["Назначение", "Инварианты", "Зависимости", "Ограничения", "Критерии приёмки", "Табу"];
        var problems = new List<string>();

        foreach (var path in Directory.EnumerateFiles(PortRoot, "BOOT.md", SearchOption.AllDirectories))
        {
            var headings = File.ReadLines(path)
                .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
                .Select(line => line[3..].Trim())
                .ToHashSet(StringComparer.Ordinal);

            foreach (var section in required.Where(section => !headings.Contains(section)))
            {
                problems.Add($"{Relative(path)} has no '## {section}' section (AGENTS.md §6).");
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    /// <summary>
    /// Every relative link between the tree's documents resolves to a file.
    /// </summary>
    /// <remarks>
    /// Some of these climb six levels to reach <c>reference/propstruct/</c>, and this
    /// repository has been bitten by long relative paths before: the ones hardcoded in
    /// the Tools tests broke against the centralised <c>artifacts/</c> layout and left
    /// the tests "passing" in 110 ms without running anything.
    /// </remarks>
    [Fact]
    public void EveryRelativeLinkBetweenDocumentsResolves()
    {
        var problems = new List<string>();

        foreach (var path in Directory.EnumerateFiles(PortRoot, "*.md", SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(path)!;

            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"\]\(([^)\s]+)\)"))
            {
                var target = match.Groups[1].Value;
                if (target.StartsWith("http", StringComparison.Ordinal) || target.StartsWith('#'))
                {
                    continue;
                }

                var withoutAnchor = target.Split('#')[0];
                if (withoutAnchor.Length == 0)
                {
                    continue;
                }

                var resolved = Path.GetFullPath(Path.Combine(directory, withoutAnchor));
                if (!File.Exists(resolved) && !Directory.Exists(resolved))
                {
                    problems.Add($"{Relative(path)}: the link to {target} resolves to nothing ({resolved}).");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<(string Node, HashSet<string> Uses)> ActualDependencies()
    {
        foreach (var group in Port.GetTypes().Where(type => type.Namespace is not null).GroupBy(NodeOf))
        {
            var uses = new HashSet<string>(StringComparer.Ordinal);
            foreach (var referenced in group.SelectMany(ReferencedTypes))
            {
                var node = NodeOf(referenced);
                if (node != group.Key)
                {
                    uses.Add(node);
                }
            }

            yield return (group.Key, uses);
        }
    }

    private static IEnumerable<string> TypesCrossing(string node, string used) =>
        Port.GetTypes()
            .Where(type => NodeOf(type) == node && ReferencedTypes(type).Any(other => NodeOf(other) == used))
            .Select(SimpleName)
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Every type of this assembly that the given type mentions — in its shape (base
    /// type, interfaces, and the types of everything it declares) and in the bodies of
    /// its methods.
    /// </summary>
    /// <remarks>
    /// ⚠ The bodies are not an embellishment. Reading shapes alone, this method missed
    /// <c>Kernel/</c>'s use of <c>Geometry/</c> entirely, because
    /// <c>BridgeVolume.Between</c> is a static call whose name appears in no signature —
    /// and reported the true declaration in <c>Kernel/BOOT.md</c> as a dependency that
    /// "was never real". The same blindness runs the other way, which is the dangerous
    /// one: a node reaching into a neighbour only through static calls would have passed
    /// the check undeclared, which is the very hole this test exists to close.
    /// </remarks>
    private static IEnumerable<Type> ReferencedTypes(Type type)
    {
        foreach (var candidate in Shape(type).Concat(Bodies(type)))
        {
            foreach (var unwrapped in Unwrap(candidate))
            {
                // A type with no namespace is the compiler's own furniture —
                // <PrivateImplementationDetails>, which holds the blob behind every array
                // literal, is what a node reaches through when it declares one. It belongs
                // to no node, so counting it invents a dependency on a directory named "/".
                if (unwrapped.Assembly == Port && !unwrapped.IsGenericParameter && unwrapped.Namespace is not null)
                {
                    yield return unwrapped;
                }
            }
        }

        static IEnumerable<Type> Shape(Type type)
        {
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                     | BindingFlags.Static | BindingFlags.DeclaredOnly;

            if (type.BaseType is not null)
            {
                yield return type.BaseType;
            }

            foreach (var contract in type.GetInterfaces())
            {
                yield return contract;
            }

            foreach (var field in type.GetFields(All))
            {
                yield return field.FieldType;
            }

            foreach (var property in type.GetProperties(All))
            {
                yield return property.PropertyType;
            }

            foreach (var method in type.GetMethods(All))
            {
                yield return method.ReturnType;
                foreach (var parameter in method.GetParameters())
                {
                    yield return parameter.ParameterType;
                }
            }

            foreach (var constructor in type.GetConstructors(All))
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    yield return parameter.ParameterType;
                }
            }
        }

        static IEnumerable<Type> Bodies(Type type)
        {
            const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                                     | BindingFlags.Static | BindingFlags.DeclaredOnly;

            var context = type.IsGenericTypeDefinition ? type.GetGenericArguments() : null;

            IEnumerable<MethodBase> members = type.GetMethods(All).Cast<MethodBase>()
                .Concat(type.GetConstructors(All))
                .Concat(type.TypeInitializer is { } initializer ? [initializer] : []);

            foreach (var member in members)
            {
                foreach (var referenced in TypesTouchedBy(member, context))
                {
                    yield return referenced;
                }
            }
        }

        static IEnumerable<Type> Unwrap(Type type)
        {
            var bare = type.IsByRef || type.IsArray || type.IsPointer ? type.GetElementType() ?? type : type;
            yield return bare;

            foreach (var argument in bare.IsGenericType ? bare.GetGenericArguments() : [])
            {
                foreach (var nested in Unwrap(argument))
                {
                    yield return nested;
                }
            }
        }
    }

    /// <summary>
    /// Every type named by the metadata tokens in one method's IL — what it calls, the
    /// fields it touches, the types it casts to, boxes, or takes a handle of.
    /// </summary>
    /// <remarks>
    /// Walking IL needs the operand width of every opcode, and the runtime already
    /// carries that table: <see cref="OpCodes"/> exposes one static field per opcode,
    /// each knowing its own <see cref="OpCode.OperandType"/>. Reading the table off the
    /// runtime rather than transcribing it keeps this honest — a transcription is one
    /// more thing that can be wrong, and a wrong operand width desynchronises the walk
    /// and yields garbage tokens rather than failing loudly.
    /// </remarks>
    private static IEnumerable<Type> TypesTouchedBy(MethodBase method, Type[]? typeContext)
    {
        byte[] il;
        try
        {
            il = method.GetMethodBody()?.GetILAsByteArray() ?? [];
        }
        catch (Exception)
        {
            yield break;
        }

        var methodContext = method.IsGenericMethodDefinition ? method.GetGenericArguments() : null;
        var module = method.Module;

        var offset = 0;
        while (offset < il.Length)
        {
            var code = il[offset++];
            OpCode opcode;
            if (code == 0xFE)
            {
                if (offset >= il.Length || !TwoByte.TryGetValue(il[offset++], out opcode))
                {
                    yield break;
                }
            }
            else if (!OneByte.TryGetValue(code, out opcode))
            {
                yield break;
            }

            var width = opcode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 or OperandType.InlineR => 8,
                OperandType.InlineSwitch => 4 + (4 * BitConverter.ToInt32(il, offset)),
                _ => 4,
            };

            Type? resolved = null;
            if (opcode.OperandType is OperandType.InlineMethod or OperandType.InlineField
                or OperandType.InlineType or OperandType.InlineTok)
            {
                try
                {
                    var member = module.ResolveMember(BitConverter.ToInt32(il, offset), typeContext, methodContext);
                    resolved = member as Type ?? member?.DeclaringType;
                }
                catch (Exception)
                {
                    // A token this reflection context cannot resolve names nothing we can
                    // attribute to a node; the walk stays in step either way.
                }
            }

            offset += width;
            if (resolved is not null)
            {
                yield return resolved;
            }
        }
    }

    private static Dictionary<byte, OpCode> OneByte { get; } = OpCodeTable(single: true);

    private static Dictionary<byte, OpCode> TwoByte { get; } = OpCodeTable(single: false);

    private static Dictionary<byte, OpCode> OpCodeTable(bool single)
    {
        var table = new Dictionary<byte, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is not OpCode opcode)
            {
                continue;
            }

            var value = (ushort)opcode.Value;
            if (single ? value <= 0xFF && opcode.Value != 0xFE : value > 0xFF)
            {
                table[(byte)(value & 0xFF)] = opcode;
            }
        }

        return table;
    }

    private static string NodeOf(Type type)
    {
        var declaring = type;
        while (declaring.DeclaringType is not null)
        {
            declaring = declaring.DeclaringType;
        }

        return declaring.Namespace?.Split('.').Last() ?? string.Empty;
    }

    private static HashSet<string> DeclaredDependencies(string boot)
    {
        var section = Regex.Match(boot, @"^## Зависимости\s*$(.*?)(?=^## |\z)", RegexOptions.Multiline | RegexOptions.Singleline);
        var declared = new HashSet<string>(StringComparer.Ordinal);
        if (!section.Success)
        {
            return declared;
        }

        // `../Sampling/API.md` names a sibling; `../API.md` names the root node, whose
        // own directory has no name to spell. Without the optional segment the root is
        // simply inexpressible, and a node that uses `StructureProgress` could never
        // declare where it came from.
        foreach (Match match in Regex.Matches(section.Groups[1].Value, @"\.\./(?:(\w+)/)?API\.md"))
        {
            declared.Add(match.Groups[1].Success ? match.Groups[1].Value : RootNode);
        }

        return declared;
    }

    /// <summary>
    /// The C# blocks of a document that sit under a heading marked implemented.
    /// </summary>
    private static IEnumerable<string> ImplementedCsharpBlocks(string document)
    {
        var implemented = true;
        var inside = false;
        var block = new List<string>();

        foreach (var line in document.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                if (inside)
                {
                    if (implemented)
                    {
                        yield return string.Join("\n", block);
                    }

                    block.Clear();
                    inside = false;
                }
                else if (line.Contains("csharp", StringComparison.Ordinal))
                {
                    inside = true;
                }

                continue;
            }

            if (inside)
            {
                block.Add(line);
                continue;
            }

            // The nearest status mark before a block decides whether it is a promise
            // or a description. A file with no mark at all is taken as a description.
            if (line.Contains('⏳', StringComparison.Ordinal))
            {
                implemented = false;
            }
            else if (line.Contains('✅', StringComparison.Ordinal))
            {
                implemented = true;
            }
        }
    }

    private static IEnumerable<Declaration> Declarations(string block)
    {
        var lines = block.Split('\n');
        for (var index = 0; index < lines.Length; index++)
        {
            var line = StripComment(lines[index]);
            if (line.Length == 0)
            {
                continue;
            }

            var type = Regex.Match(
                line,
                @"\b(?:readonly\s+)?(?:record\s+struct|record\s+class|abstract\s+record|sealed\s+record|record|class|struct|enum|interface)\s+(\w+)");
            if (type.Success && Regex.IsMatch(line, @"\b(class|record|struct|enum|interface)\b"))
            {
                yield return new Declaration(type.Groups[1].Value, IsType: true, IsEnumMember: false);

                // A positional record declares its properties in its parameter list,
                // which is routinely spread over several lines.
                var parameters = ParameterList(lines, ref index, line);
                foreach (var parameter in parameters)
                {
                    yield return new Declaration(parameter, IsType: false, IsEnumMember: false);
                }

                continue;
            }

            var property = Regex.Match(line, @"\b(\w+)\s*\{\s*get");
            if (property.Success)
            {
                yield return new Declaration(property.Groups[1].Value, IsType: false, IsEnumMember: false);
                continue;
            }

            var method = Regex.Match(line, @"\b(\w+)\s*\(");
            if (method.Success && !IsKeyword(method.Groups[1].Value))
            {
                yield return new Declaration(method.Groups[1].Value, IsType: false, IsEnumMember: false);
                continue;
            }

            var field = Regex.Match(line, @"\b(?:const|readonly)\s+[\w<>\[\],\s]*?(\w+)\s*[=;]");
            if (field.Success)
            {
                yield return new Declaration(field.Groups[1].Value, IsType: false, IsEnumMember: false);
                continue;
            }

            var enumMember = Regex.Match(line, @"^\s*(\w+)\s*(?:=\s*[^,]+)?,\s*$");
            if (enumMember.Success && !IsKeyword(enumMember.Groups[1].Value))
            {
                yield return new Declaration(enumMember.Groups[1].Value, IsType: false, IsEnumMember: true);
            }
        }
    }

    /// <summary>
    /// The parameter names of a positional record, following the list across lines.
    /// </summary>
    private static List<string> ParameterList(string[] lines, ref int index, string first)
    {
        var names = new List<string>();
        if (!first.Contains('(', StringComparison.Ordinal))
        {
            return names;
        }

        var text = first;
        var depth = Depth(first);
        while (depth > 0 && index + 1 < lines.Length)
        {
            index++;
            var next = StripComment(lines[index]);
            text += " " + next;
            depth += Depth(next);
        }

        var open = text.IndexOf('(', StringComparison.Ordinal);
        var close = text.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return names;
        }

        foreach (var parameter in text[(open + 1)..close].Split(','))
        {
            var withoutDefault = parameter.Split('=')[0].Trim();
            var name = Regex.Match(withoutDefault, @"(\w+)\s*$");
            if (name.Success && char.IsUpper(name.Groups[1].Value[0]))
            {
                names.Add(name.Groups[1].Value);
            }
        }

        return names;

        static int Depth(string line) => line.Count(c => c == '(') - line.Count(c => c == ')');
    }

    private static string StripComment(string line)
    {
        var trimmed = line.TrimStart();
        if (trimmed.StartsWith("///", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var comment = line.IndexOf("//", StringComparison.Ordinal);
        return (comment >= 0 ? line[..comment] : line).TrimEnd();
    }

    private static bool IsKeyword(string word) =>
        word is "if" or "for" or "foreach" or "while" or "switch" or "return" or "new" or "get" or "set" or "init"
            or "throw" or "using" or "nameof" or "typeof";

    private static Dictionary<string, Type> AllTypesByName()
    {
        var byName = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in Port.GetTypes().Concat(typeof(DocumentationTests).Assembly.GetTypes()))
        {
            byName.TryAdd(SimpleName(type), type);
        }

        return byName;
    }

    private static string SimpleName(Type type)
    {
        var name = type.Name;
        var arity = name.IndexOf('`');
        return arity < 0 ? name : name[..arity];
    }

    private static string Relative(string path) => Path.GetRelativePath(PortRoot, path);

    private readonly record struct Declaration(string Name, bool IsType, bool IsEnumMember);
}
