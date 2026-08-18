# API.md — Configuration

✅ **Узел реализован** (2026-08-15). Всё ниже описывает существующий код; 155
тестов в `tests/…Tests/StructureRunConfigurationTests.cs` и
`ConfigurationJsonTests.cs`.

## Типы

```csharp
public sealed record StructureRunConfiguration
{
    // рецептура
    public required Density OxidiserDensity { get; init; }
    public required Density BinderMetalDensity { get; init; }
    public required double OxidiserMassFraction { get; init; }
    public required double MetalMassFraction { get; init; }
    public required IReadOnlyList<OxidiserFraction> Fractions { get; init; }

    // размер выборки
    public long BaseParticles { get; init; } = 100_000;

    // KXX оригинала, как он стоит во входном файле. Значение <= 1 ядро нормирует
    // в один проход - это поведение оригинала (строки 141-143), а не подстановка
    // умолчания здесь. В архивных .dat стоит 0.
    public int Cycles { get; init; } = 1;

    // ivar оригинала. 0 - условие 3 перезапускает всю базовую частицу;
    // иначе отбрасывается только окружающая. 43/43 прогонов идут с нулём.
    public int CalculationVariant { get; init; }

    // геометрические критерии; НЕ записывались оригиналом в .m
    public GeometricCriteria Criteria { get; init; } = GeometricCriteria.Historical;

    // сетки гистограмм
    public Length MinimumParticleSize { get; init; } = Length.FromMicrometers(10);
    public Length ParticleHistogramStep { get; init; } = Length.FromMicrometers(10);
    public Length PocketHistogramStep { get; init; } = Length.FromMicrometers(10);

    public ModelCoefficients Coefficients { get; init; } = ModelCoefficients.Historical;
    public GeneratorSelection Generator { get; init; } = GeneratorSelection.Random2;
    public int GeneratorWarmup { get; init; } = 6_000_000;     // NNZ; читается только при Toy
    public SizeDistributionLaw Law { get; init; } = SizeDistributionLaw.Surface;
    public double AgglomeratedOxideShare { get; init; }        // eta, по умолчанию 0

    public string? ResolvedRunPath { get; init; } = "structure_run.resolved.json";
}

public readonly record struct OxidiserFraction(
    double MassFraction,
    Length MinSize,
    Length MaxSize,
    bool FormsPockets = true);   // SFR оригинала: false исключает фракцию
                                 // из образования карманов, а её массу
                                 // переводит в гомогенизированный окислитель

public readonly record struct GeometricCriteria(double Ak1, double Ak2, double Ak3, double Ak4)
{
    public static GeometricCriteria Historical => new(0.5, 2.0, 0.27, 4.7);
}

public readonly record struct ModelCoefficients(
    double SurroundingVolumeShare,     // k5   = 0.25
    double PocketInPocket,             // k7   = 8.2
    double PocketInBridge,             // k8   = 7.73
    double Accuracy,                   // eps  = 0.05
    double StatisticalSignificance,    // alfa = 0
    double HomogenisedOxidiser,        // Zok* = 0
    double PocketBridgeRatioMin,       // 3
    double PocketBridgeRatioMax)       // 100
{
    public static ModelCoefficients Historical { get; }
}

public enum GeneratorSelection { Toy = 1, Random2 = 2, SystemSeeded = 3 }
```

⚠ **Два похожих имени означают разное, и в оригинале они различаются одной
буквой.** `alpha` — это `k5`, здесь `SurroundingVolumeShare`, историческое
значение 0.25; `alfa` — статистическая значимость, здесь
`StatisticalSignificance`, историческое значение 0.

⚠ **`k5` не управляет числом окружающих частиц.** Он входит ровно в три места
(строки 747, 750, 754 оригинала) — множителем при объёме окружающих частиц в трёх
накопителях доли карманов. Сколько соседей будет набрано, решает бюджет `TU`
(см. [Kernel/BOOT.md](../Kernel/BOOT.md), «Ход одного прогона»).

⚠ `SizeDistributionLaw` **определён не здесь**, а в
[Sampling/API.md](../Sampling/API.md) — он уже написан. Закон задаёт формулу
обращения, то есть принадлежит выборке; конфигурация его только переносит, и
зависимость идёт отсюда вниз, а не наоборот.

## Операции

```csharp
public static class StructureConfigurationValidator
{
    // бросает StructureConfigurationException; ничего не подставляет
    public static void Validate(StructureRunConfiguration configuration);
}

public static class StructureRunConfigurationFile
{
    // разбор; НЕ валидация - см. ниже, почему это разные вопросы
    public static StructureRunConfiguration Read(string path);
    public static StructureRunConfiguration Parse(string json);
}

public static class ResolvedRunRecord
{
    // пишется ДО первого розыгрыша; включает eta, AK1..AK4, SFR и ivar
    public static ResolvedRunRecordSnapshot Write(
        StructureRunConfiguration configuration, string? path);
    public static ResolvedRunRecordSnapshot Read(string path);
}

// то же содержимое, что попало в файл; вкладывается в StructureResult,
// чтобы результат нёс свой вход даже без файла на диске
public sealed record ResolvedRunRecordSnapshot(
    StructureRunConfiguration Configuration,
    IReadOnlyList<string> Warnings,          // напр. сумма долей фракций != 1
    IReadOnlyList<string> UnverifiedSettings) // параметры без эталонного покрытия
{
    public string Schema { get; init; } = "propstruct-resolved-run/1";
}

public sealed class StructureConfigurationException : Exception
{
    public IReadOnlyList<string> Problems { get; }   // все, а не первая
}
```

⚠ `StructureRunConfigurationFile` в первоначальной спецификации не значился, но
критерий приёмки «неизвестное поле в разбираемом JSON бросает исключение»
подразумевает разбор. Добавлен при реализации.

Разбор и валидация **разделены намеренно**: первый отвечает на вопрос «это вообще
конфигурация», вторая — «можно ли с этим запускать прогон». Оба бросают
`StructureConfigurationException`, так что вызывающему, которому нужно лишь
сообщить о плохом входе, ловить надо один тип.

`Schema` в спецификации тоже не было. Причина та же, что и у самой записи: файл,
не называющий собственной формы, через год нечитаем — а ровно этот дефект
оригинала узел и закрывает.

## Гарантии

- значения по умолчанию тождественны историческим — «без конфигурации» и
  «конфигурация с историческими значениями» неразличимы; исключение —
  `BaseParticles`, у которого единого исторического значения нет (архив идёт от
  5·10³ до 10⁶), поэтому умолчание там названо в `BOOT.md` как выбранное, а не
  унаследованное;
- `Cycles` **не** нормируется здесь: значение уходит в ядро как есть, нормировку
  `≤ 1 → 1` выполняет ядро, воспроизводя оригинал. `resolved.json` хранит
  заданное значение в поле, а фактическое число проходов называет строкой в
  `Warnings` — так «ничего не подставляется молча» и «порт воспроизводит
  оригинал» перестают противоречить друг другу;
- равенство типа — **по значению**, включая поэлементное сравнение фракций, и
  величины сравниваются в СИ, а не средствами UnitsNet. ⚠ Из-за этого 10 мкм и
  1e-5 м — **разные** ключи: перевод микрометров в метры даёт на один ulp меньше
  литерала. Разница 2·10⁻²¹ м не двигает ни одного результата, но делает два
  описания одного прогона неравными, если они пришли из разных источников
  (`.dat` пишет метры, отчёт — микрометры);
- неизвестный член JSON — ошибка, а не примечание: опечатка в имени иначе молча
  оставляет настройку в умолчании, а это ровно тот отказ, ради которого узел и
  существует;
- пропущенный `formsPockets` остаётся `true`. Держится это на явном
  `[JsonConstructor]`: без него десериализатор пошёл бы через беспараметрический
  конструктор структуры и превратил бы отсутствие ключа в `false` — фракцию,
  молча исключённую из образования карманов;
- `Validate` проверяет **всю** конфигурацию, включая неиспользуемые в этом
  прогоне ветки;
- сумма массовых долей фракций, отличная от единицы, — предупреждение, а не
  ошибка: четыре эталонных прогона идут с суммой 1.1 и 0.9885;
- ⚠ **`GeneratorSelection.Toy` реализован 2026-08-16 и больше не отвергается.**
  Здесь стояло, что обе не-`Random2` ветки отвергаются ядром, и что отказ от
  `Toy` — «ветка не реализована», снимаемая фикстурой плюс кодом. Так и вышло:
  фикстура снята (`reference/propstruct/probes/probe4.m`), `Random1Stream`
  написан, `RandomStreamSet.For` выбирает набор по конфигурации.

  Пока этого не было, ядро звало `Historical()` безусловно — то есть прогон,
  попросивший `gsv = 1`, получал числа `gsv = 2`, а разрешённая запись
  фиксировала просьбу. Это не «нереализованная ветка», это молчаливая подмена, и
  ровно её разрешённая запись обязана была не допустить. Урок для остальных
  непокрытых веток: **параметр, который никуда не доходит, хуже параметра,
  который отвергают**.

  `SystemSeeded` (`gsv = 3`) отвергается по-прежнему и навсегда: он берёт
  затравку извне программы (строка 409) и невоспроизводим даже в оригинале, так
  что эталона у него быть не может в принципе. Это `NotSupportedException` из
  `RandomStreamSet.For`, а не ошибка валидации, — настройка не невалидна, ей
  просто нечем быть проверенной.
- `GeneratorWarmup` (`NNZ`) читается всеми прогонами и используется только при
  `Toy`. ⚠ Отчёт оригинала печатает его **неверно**: строка 1199 гонит INTEGER
  через `E9.3`, поэтому 6 000 000 выходит как `0.841E-38` — битовый узор целого,
  прочитанный как денормализованный REAL*4. Значение берётся из `.dat`, а не из
  отчёта.

## Единицы

Внутри типа — СИ. `Length` и `Density` из UnitsNet на границе; ядро получает
метры и кг/м³ как `double`.

В JSON величины пишутся **голыми числами в СИ**, а не в формате UnitsNet
`{value, unit}`. Причина — ловушка, в которую уже попал оригинал: он хранил метры,
печатал микрометры и угадывал между ними эвристикой `.ge. 0.1`, которая молча
читает 0.05 мкм как метры. Поле, единица которого закреплена его именем, угадывать
не о чем.
