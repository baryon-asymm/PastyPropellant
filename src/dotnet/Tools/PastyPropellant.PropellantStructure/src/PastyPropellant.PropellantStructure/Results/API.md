# API.md — Results

✅ **Узел реализован** (2026-08-15). Ниже — описание существующего кода. Вместе с
ним реализованы `Sampling/` (генератор, обращение распределения фракций, розыгрыш
размера кармана), `Geometry/` (объём межкарманной перемычки) и `Configuration/`
(вход, валидация, запись фактического прогона). Не написан `Kernel/`, поэтому
результат пока никто не заполняет: проверено, что форма не даёт заполнить её
вводящим в заблуждение образом, а не что числа верны — это L2.

Типы публичные: результат — часть внешнего контракта сборки.

## Устройство: одно хранилище, типизированные проекции сверху

Тип обслуживает две работы, которые тянут в разные стороны. Он мишень сверки L2
и обязан нести **каждое** напечатанное число; он же контракт для потребителя и
обязан быть маленьким и не давать подменить ветвь поправки. Первый черновик
пытался быть тем и другим одним набором полей — и вышел неполным (не было ни
целых счётчиков, ни `da_coef`, ни двумерного блока) и одновременно неточным
(ветвь потерялась у массовой доли карманов).

Разделено так:

- **`Printed` / `PrintedLengths`** — полная запись напечатанного, под именами
  оригинала, в СИ, полной точности. Сверка на L2 — обход ключей `expected.scalars`
  из `runs.json`; отсутствующая величина падает тестом, а не замечается на ревью.
- **типизированные проекции** — головные величины с ветвью, единицами и признаком
  подтверждённости. Собственного хранения у них нет: это чтение из записи, а не
  вторая копия, поэтому разъехаться они не могут.

## Ветвь поправки

```csharp
public enum Correction
{
    None     = 0,   // без поправки
    Variant1 = 1,   // «cor. var. #1» отчёта
    Variant2 = 2,   // «cor. var. #2» отчёта
}
```

⚠ Варианты **независимы**: это параллельные накопители, а не стадии одной
поправки — ни один не считается от результата другого. «Более исправленного»
варианта не существует, выбрать один за вызывающего нельзя. Сравнивать составы
можно только по одной ветви — см. [BOOT.md](./BOOT.md).

Набор ветвей у величин **разный**, и словарь выражает именно его:

| Величина | Ветви | Величины оригинала |
|---|---|---|
| массовая доля карманов | `None`, `Variant1`, `Variant2` | `zkarm`, `zkarm_cor1`, `zkarm_cor2` = `1−DolM1/2/3` |
| `D43` кармана | `None`, `Variant1`, `Variant2` | `dkarm43_v2`, `dkarm43_cor1`, `dkarm43_cor2` |
| `D43` агломерата | `None`, `Variant1`, `Variant2` | те же, умноженные на `da_coef` |
| `D10` кармана | `None`, `Variant1` | `dkarm10`, `dkarm10_cor` |

Запрос ветви, которой у величины нет, — `BranchMismatchException`, а не пустой
результат.

### Почему оценщик — не вторая ось ключа

Отчёт печатает размер кармана в двух «вариантах»: `Dkarm43(1)` = `DP43` =
`DP41/DP31`, отношение моментов прямо по выборке, и `Dkarm43(2)` = `D432`, тот же
момент по гистограмме `VKSO`. Это **два оценщика одной величины**, а не два
результата. Обе поправки (`Dkarm43_cor` по `fmkarm_cor`, `Dfmk432` по `fmkarm2`)
построены на гистограмме, поэтому:

- ключом служит только `Correction`, и всегда по гистограммному оценщику — иначе
  неисправленное значение сравнивалось бы с исправленным, посчитанным иначе, а
  это ровно то смешение, от которого узел заведён;
- `dkarm43_v1` доступен в `Printed`, под своим именем, ветвью не предлагается.

Основание количественное: `v1` и `v2` расходятся в 13 прогонах из 43 максимум на
1·10⁻⁴ относительных, при разрешении печати `F7.2` в 4.8·10⁻⁵ — то есть на два-три
последних знака. Ось поправок на том же поле даёт до 110 % (`v1` против `cor2`,
прогон `r3`). Один ключ на обе оси ставил бы разницу округления и разницу вдвое в
один ряд.

## Признак подтверждённости

```csharp
public enum Verification
{
    ConfirmedByArchivedRun,   // совпало с архивным прогоном
    NotPrintedByOriginal,     // оригинал такого не печатал: подтвердить нечем
    NotCoveredByAnyRun,       // печатал бы, но ни один архивный прогон сюда не заходит
}

public readonly record struct Verified<T>(T Value, Verification Status)
{
    public static Verified<T> Reference(T value)   => new(value, Verification.ConfirmedByArchivedRun);
    public static Verified<T> Unprinted(T value)   => new(value, Verification.NotPrintedByOriginal);
    public static Verified<T> Uncovered(T value)   => new(value, Verification.NotCoveredByAnyRun);
}
```

⚠ Состояний три, а не два. С 2026-08-15 оригинал **запускается** под wine, и
«не подтверждено» перестало значить «подтвердить невозможно»: для непокрытой
ветки фикстура генерируется (процедура — в `reference/propstruct/README.md`).
Булев флаг склеивал бы «эталона нет» и «эталон не снят», и по мере появления
фикстур начал бы врать.

## Результат

```csharp
public sealed record StructureResult
{
    public required ResolvedRunRecordSnapshot Run { get; init; }

    // полная запись напечатанного: ключи = имена величин оригинала
    public required IReadOnlyDictionary<string, Verified<double>> Printed        { get; init; }
    public required IReadOnlyDictionary<string, Verified<Length>> PrintedLengths { get; init; }

    public required IReadOnlyList<Distribution>        Distributions          { get; init; }
    public required ConditionalOxidiserByPocketSize    PocketWallDistributions { get; init; }
    public required RunDiagnostics                     Diagnostics            { get; init; }

    // проекции; собственного хранения нет
    public IReadOnlyDictionary<Correction, Verified<double>> PocketMassFraction    { get; }
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter43      { get; }
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter43Sd    { get; }
    public IReadOnlyDictionary<Correction, Verified<Length>> AgglomerateDiameter43 { get; }
    public IReadOnlyDictionary<Correction, Verified<Length>> PocketDiameter10      { get; }

    public Verified<double> PocketToAgglomerateCoefficient  { get; }  // da_coef
    public Verified<Length> PocketWallDiameter43            { get; }  // dok43_surrounding
    public Verified<double> MeanBridgeToParticleRatio       { get; }  // dqmkm1
    public Verified<double> MeanBridgeToPocketRatio         { get; }  // dqmkm2
    public Verified<double> HomogenisedOxidiserFraction     { get; }  // fine_oxidiser_fraction

    /// <summary>имена печатаемых скаляров, значение которых — длина</summary>
    public static IReadOnlySet<string> LengthValuedQuantities { get; }
}
```

⚠ **Исправлено при кодировании 2026-08-15: `dqmkm1`/`dqmkm2` — не доли окислителя.**
В черновике они назывались `SmallOxidiserInPocketMassShare` и
`…VolumeShare`. Отчёт (строки 1383–1387) называет их «Medium MKM/Dok size between
Dok particles» и «Medium MKM/Dol size between pockets» — это средние отношения
перемычка/частица и перемычка/карман, геометрия перемычек, а не содержание
окислителя в кармане. Ошибка поймана сверкой с оператором печати, а не с именем
поля. Долю гомогенизированного окислителя несёт отдельная величина
`fine_oxidiser_fraction` (строка 1409, `gdokns + gdokleft/GGG`).

`LengthValuedQuantities` — машинная форма списка ниже. Заведена, чтобы ядро и
тесты не выводили разбиение на два словаря по отдельности каждый.

## Отказы

| Ситуация | Поведение |
|---|---|
| запрошена ветвь, которой у величины нет (`PocketDiameter10[Variant2]`) | `BranchMismatchException` с перечнем имеющихся ветвей |
| сравнение составов по ветви, которой нет хотя бы у одного | `BranchMismatchException` с номером результата |
| в записи нет величины, нужной проекции | `InvalidOperationException`, называющий и величину, и проекцию |

⚠ Набор ветвей фиксирован для каждой величины, поэтому отсутствие ключа значит,
что **порт не выдал** величину, а не что у неё меньше ветвей. Вернуть в этом
случае словарь покороче — значит превратить сломанный результат в правдоподобный.

⚠ **`PocketMassFraction` — словарь, а не одно число.** Это та самая величина, ради
которой узел заведён, и в первом черновике она единственная шла без ветви.
`zkarm` расходится с `zkarm_cor2` **во всех 43 прогонах**, максимум 66 %; с
`zkarm_cor1` — в пяти прогонах, но до 75 %. И `d·Z_p` — произведение **двух**
ветвлённых величин: подменить ветвь можно в любом из множителей.

### Единицы в записи напечатанного

Оригинал хранил метры и печатал микрометры. Запись разделена на два словаря
именно поэтому: `PrintedLengths` несёт `Length` (внутри — метры), `Printed` —
безразмерное. Ключ живёт ровно в одном из двух; ключ, которого нет ни в одном, —
ошибка порта, а не пустое значение.

В `PrintedLengths` попадают: `dok43_analytic`, `dok43_base`, `dok43_surrounding`,
`dok43_sd`, `dok_max`, `dkarm43_v1`, `dkarm43_v2`, `dkarm43_sd`, `dkarm43_cor1`,
`dkarm43_sd_cor1`, `dkarm43_cor2`, `dkarm43_sd_cor2`, `dkarm10`, `dkarm10_cor`,
`dagg43_v1`, `dagg43_v2`, `dagg43_cor1`, `dagg43_cor2`, `dok43_all_v1`,
`dok43_all_v2` — двадцать из пятидесяти одной. Остальные — в `Printed`.

## Распределения

```csharp
public abstract record DistributionGrid
{
    public sealed record ByLength(Length Origin, Length Step)   : DistributionGrid;
    public sealed record ByRatio(double Origin, double Step)    : DistributionGrid;
    public sealed record ByCategory(IReadOnlyList<string> Labels) : DistributionGrid;

    private DistributionGrid() { }   // иерархия закрыта: switch обязан быть исчерпывающим
}

public enum DistributionNormalisation
{
    DensityPerGridUnit,  // сумма * шаг = 1
    ProbabilityPerBin,   // сумма = 1
    Unnormalised,        // каждая ячейка — самостоятельная величина
}

public sealed record Distribution(
    string Name,
    DistributionGrid Grid,
    DistributionNormalisation Normalisation,
    IReadOnlyList<double> Values);
```

Таблица ниже не пересказывается в коде — она **и есть** код:

```csharp
public static class PrintedArrays
{
    // какой из четырнадцати печатных массивов — распределение и с какой нормировкой
    public static IReadOnlyDictionary<string, DistributionNormalisation> Distributions { get; }

    // четыре, которые распределениями не являются, — названы, а не подразумеваются
    public static IReadOnlySet<string> NotDistributions { get; }

    // шаг ячейки для сеток по отношению — из строки, которая в массив разбивает,
    // а не из заголовка, который над ним печатается
    public static IReadOnlyDictionary<string, double> RatioGridSteps { get; }
}
```

`Kernel/` строит распределения по этой таблице, и тест, проверяющий нормировку,
читает её же. ⚠ Раньше копия таблицы лежала в тесте, и опасность была не
теоретической: тест проверял бы ту метку, которую несёт сам, а не ту, с которой
собран результат, — то есть неверно размеченный массив прошёл бы.

⚠ **Сетка не всегда длина, и нормировка не всегда одна из двух.** Первый черновик
объявлял `Length GridStep` и две нормировки; ни того, ни другого не хватает:

| Массив | Сетка | Нормировка |
|---|---|---|
| `fmdok`, `fmkarm`, `fmkarm_cor`, `fmkarm_cor2`, `fqkarm`, `fqkarm_cor` | `ByLength`, шаг `Di` | `DensityPerGridUnit` |
| `fqmkm1`, `fqmkm2`, `coef` | `ByRatio` | `ProbabilityPerBin` |
| `pdoksmall` | `ByLength`, шаг `Di` | `Unnormalised` — вероятность «карман в кармане» для своей ячейки размера базовой частицы, между ячейками не нормирована |

Иерархия закрыта приватным конструктором: чтение сетки обязано быть исчерпывающим
`switch`, и добавить четвёртый вид, не пройдя по всем местам чтения, нельзя.

⚠ **У `fqmkm1` напечатанный шаг не равен настоящему.** Отчёт объявляет `step =
0.01`, фактический — `0.001`; расхождение в десять раз. `runs.json` хранит оба
(`array_steps.fqmkm1` и `array_steps.fqmkm1_as_printed`). Заполнять `Grid` из
отчёта нельзя — только из `Di` и разбора `runs.json`.

⚠ **Четырнадцать массивов оригинала — не четырнадцать распределений.** Здесь их
десять. `Dkarmcat`, `dokkarm43`, `dokkarm10` — не распределения, а кривые длин по
категориям карманов, и живут в блоке ниже; `epsdokfr` — точность розыгрыша по
фракциям, диагностика. Шесть массивов `*_n` (`epsfkarm_n` … `epszkarm_n`) — тоже
диагностика: они печатаются только при `KXX > 1`, индексируются номером прохода, а
не сеткой, и лежат в `RunDiagnostics.Convergence`. Так что состав `Distributions`
от числа проходов по-прежнему не зависит.

## Условное распределение по категориям карманов

```csharp
public sealed record ConditionalOxidiserByPocketSize(
    IReadOnlyList<Length> Categories,                 // Dkarmcat
    IReadOnlyList<Verified<Length>> MassMeanOxidiser, // dokkarm43
    IReadOnlyList<Verified<Length>> MeanOxidiser,     // dokkarm10
    IReadOnlyList<Distribution> Rows);                // fqdokkarm(i,:), по строке на категорию
```

Сорок категорий, в каждой — распределение размеров окружающих частиц по 33
ячейкам. Двумерный блок в `IReadOnlyList<Distribution>` не ложился, и в первом
черновике ему не было места вовсе — при том, что именно он несёт утверждение
`BOOT.md` «крупные карманы окружены крупными частицами», от которого там
оставался только момент `PocketWallDiameter43`.

Все четыре члена индексируются одним и тем же порядком категорий; длины совпадают
по построению.

## Диагностика прогона

```csharp
public sealed record RunDiagnostics
{
    // печаталось оригиналом: обязано совпасть точно
    public required long PocketsTotal              { get; init; }  // nkarm
    public required long BaseParticleDraws         { get; init; }  // nfx
    public required long SurroundingParticleDraws  { get; init; }  // nfy
    public required long PocketSizeDraws           { get; init; }  // nfq
    public required long BridgeDraws               { get; init; }  // nfw
    public required int  PassesDone                { get; init; }  // cycles_done
    public required long BaseParticlesPerCycle     { get; init; }  // nbase_per_cycle
    public required long BaseParticlesAccepted     { get; init; }  // nbase_accepted_cumulative
    public required IReadOnlyList<double> RandomStreamStatistics { get; init; }  // epsx1..6
    public required IReadOnlyList<double> FractionDrawAccuracy   { get; init; }  // epsdokfr
    public required IReadOnlyList<PassConvergence> Convergence   { get; init; }  // шесть массивов *_n

    public required ConditionCounters Conditions   { get; init; }

    // оригиналом не печаталось: подтвердить нечем
    public required Verified<long> AttemptsTotal          { get; init; }  // == NFX, см. ниже
    public required Verified<long> PocketSizeClampCount   { get; init; }
    public required Verified<long> BridgeGapExceedsPocket { get; init; }  // VM: A >= 2*RK
    public required Verified<long> BridgeWidthNegative    { get; init; }  // VM: BB < 0
    public required Verified<long> BridgeVolumeNaNCount   { get; init; }  // в эталоне всегда 0

    public required int CyclesRequested { get; init; }   // KXX как задан, до нормировки
}
```

⚠ **`nbase_per_cycle` и `nbase_accepted_cumulative` — два разных числа.** Во всех
43 прогонах они равны, но это **находка**, а не основание для одного поля: равенство
держится ровно потому, что `KXX > 1` в архиве не встречается.

⚠ Непечатавшиеся счётчики помечены `Verified<>` не для симметрии. Два из них
приходят из `Geometry/`, чей `API.md` **не гарантирует** побитового совпадения:
`ACOS`/`SIN`/`TAN` там разрешаются в одинарные интринсики Compaq, а порт зовёт
двойные `Math.*`. Лежи они без признака рядом с `nkarm`, который обязан совпасть
точно, — их сравнили бы одним правилом.

### Сходимость по проходам

```csharp
public readonly record struct PassConvergence(
    double PocketDistributionError,   // par1, epsfkarm_n
    double PocketMoment3Error,        // par2, epsm3karm_n
    double PocketMoment4Error,        // par3, epsm4karm_n
    double OxidiserMeanError,         // par4, epsdok43_n  — со знаком
    double OxidiserVarianceError,     // par5, epsdoksd_n  — со знаком, по дисперсиям
    double PocketMassFraction);       // par6, epszkarm_n  — вообще не погрешность
```

Одна строка на один рабочий проход; у однопроходного прогона список **пуст**, а не
длиной в одну запись, — оригинал печатает эти массивы под условием `KXX > 1`, и
запись, которой нет ни в одном отчёте, порт придумывать не должен. Массивы, а не
строки, потому что читают их по проходу: шесть параллельных списков позволяют
сравнить проход *i* одного прогона с проходом *j* другого.

⚠ **Шесть имён на `eps…_n`, а величины три разные.** Первые три — **статистическая**
погрешность самого прогона, трёхсигмовая полуширина `3·√(D/Q)/M` по накопленным
карманам: она падает как `1/√Q` независимо от того, верен ответ или нет, и именно по
ней видно, нужен ли ещё проход. Следующие две — погрешность **против рецептуры**:
разыгранные среднее и дисперсия против аналитических `DokM` и `DokSD`; они **со
знаком**, тогда как печатные скаляры (`epsx1..6`, `epsalldok`) взяты по модулю, и
`epsdoksd_n` считается **по дисперсиям** (`DokSD` — квадрат, отчёт печатает
`Dok43sd` как `DOKSD**0.5`), то есть идёт примерно вдвое выше относительной
погрешности разброса. Последняя — **не погрешность вовсе**: `1 - DolM2` это сама
доля массы в карманах, и равна она `zkarm_cor1`, а не `zkarm`.

⚠ **`epsdoksd_n` не имеет тех шести знаков, которыми напечатан.** Он сокращается
дважды: сверху две дисперсии сходятся до трёх процентов, а внутри `ALLDOKsd =
ALLDOK243 - ALLDOK432**2` (строка 812) два вторых момента сходятся в одиннадцать
раз. Один последний бит `REAL*4`-величины `ALLDOK243` приходит в напечатанное число
как **1.36e-6** абсолютных — против 0.024 это 5.8e-5, на порядок шире тех 5e-6,
которые обещает формат. Порт ложится в 0.58 и 0.93 этого бита на двух проходах.
Это свойство величины, а не порта: в одинарной точности точнее её не определить ни
на какой машине. Вывод и допуск — в `ProbeRun3.VarianceErrorFloor`.

```csharp
public sealed record ConditionCounters(
    // делитель FI + N = 2N
    double DokAboveDmax, double DokBelowDmin, double BaseBelowHalfDok,
    double BaseAboveTwoDok, double GapAbove4p7Base,
    // делитель FI = N
    double NoPockets, double FewerThanTwoBridges,
    double PocketBridgeRatioBelowMin, double PocketBridgeRatioAboveMax);
```

⚠ Обе группы нормированы на число базовых частиц, но **разными** делителями:
первые пять на `FI+N`, последние четыре на `FI`, причём `FI = N` во всех 43
прогонах. У первой пятёрки делитель вдвое завышен — это дефект оригинала, а не
вторая осмысленная нормировка. Группировка вынесена в порядок полей и в
комментарий раздела, а не в имена: складывать и сравнивать группы между собой
нельзя.

## Операции

```csharp
public static class StructureResultComparison
{
    /// <summary>d·Z_p по одной ветви для всех составов.</summary>
    /// <exception cref="BranchMismatchException">
    /// если запрошенной ветви нет хотя бы у одного результата
    /// </exception>
    public static IReadOnlyList<Length> PocketSizeTimesMassFraction(
        IReadOnlyList<StructureResult> results,
        Correction correction);
}

public sealed class BranchMismatchException : Exception;
```

Возврат — `Length`, не `double`: у `d·Z_p` размерность длины, опубликованная
тройка (80.19 / 73.27 / 52.37) записана в микрометрах, а табу узла — не хранить
микрометры. Голое число здесь было бы ровно той неоднозначностью, которая уже
однажды дала ошибку прочтения.

Одна `Correction` применяется к **обоим** множителям. Взять размер по одной ветви,
а долю по другой через этот метод невозможно.

## Сериализация

Опции и конвертеры — те же, что у `Configuration/`: `ConfigurationJson`, с
`LengthMetresConverter` и записью величин голыми числами СИ. Своих здесь не
заводится: зависимость от `Configuration/` уже есть (`Run`), а вторая копия
конвертеров — это два файла, которые разъедутся.

Равенство — по значению, включая словари и списки. У `record` со словарём
равенство ссылочное по умолчанию; на этом уже дважды ловились
`StructureRunConfiguration` и `ResolvedRunRecordSnapshot`.

⚠ **Величины сравниваются в СИ, и это не украшение.** Собственное равенство
UnitsNet сверяет значение **и единицу**: `Length.FromMicrometers(10)` не равно
`Length.FromMeters(1e-5)`. Конвертер пишет метры, значит с диска величина
приходит в метрах — и круговой ход падал бы на каждой длине результата при
полностью верном содержимом файла. Сравнение и хеш идут через `.Meters`
(`QuantityComparer`, `internal`). Заодно обезврежен известный факт про последний
бит: `Length.FromMicrometers(10).Meters` = 9.999999999999999e-06, и именно это
число лежит в файле, значит его и надо сравнивать.

## Признак подтверждённости: чем он назначается

Правило одно и вычислимо в момент сборки результата: у величины, которую
оригинал печатал, статус `ConfirmedByArchivedRun`, если в
`Run.UnverifiedSettings` пусто, и `NotCoveredByAnyRun` иначе. Величины, которых
оригинал не печатал, всегда `NotPrintedByOriginal`.

⚠ Читать это следует как «архив в состоянии судить о таком прогоне», а не как
«это число сверено с прогоном». Сверка — работа L2; признак говорит лишь, есть ли
с чем сверять. Более сильное утверждение («в архиве есть прогон ровно с такой
рецептурой») тип проверить не может, и заявлять его не будет.

## Гарантии

- неизменяемость и сериализуемость в JSON с круговым ходом без потерь;
- каждая величина несёт признак подтверждённости, и он сериализуется;
- каждое распределение несёт свою сетку и свою нормировку, и вид сетки читается
  исчерпывающим `switch`;
- ни одну ветвлённую величину нельзя получить, не назвав ветвь;
- `Length` наружу — UnitsNet; внутри ядра — метры.

## Не предоставляется

- ветвь поправки «по умолчанию»;
- пересчёт в величины основного репозитория (`f_s`, δ, его собственный `Z_p`);
- оценщик `Dkarm43(1)` как ветвь — он есть в `Printed`, но ключом не является.
