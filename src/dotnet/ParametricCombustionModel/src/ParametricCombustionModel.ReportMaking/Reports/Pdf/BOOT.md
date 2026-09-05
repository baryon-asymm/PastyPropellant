# BOOT.md — Reports/Pdf

## Purpose

Все разделы PDF-отчёта. Каждый раздел — чистое преобразование результата
оптимизации в очередь операций печати; ни один не открывает файл, не знает о
странице и не решает, в каком порядке разделы идут. Порядок собирает
[ReportMakers](../../ReportMakers/API.md).

## Invariants

- Раздел не рисует. Он возвращает `Queue<IPdfOperation>` (или, у
  `PressureTablesReport`, коллекцию таблиц) — исполняет их
  [PdfOperations](../../PdfOperations/API.md). Поэтому раздел тестируется без
  PDF-библиотеки, чем и пользуются все шесть тестов узла.
- Три семейства разделов, и это не единый корень:
  - наследники [PerCompositionPerFuelPdfReport](./API.md) обходят групповой
    результат «композиция → топливо» и дают только различающийся текст;
  - наследники [BaseReport](../API.md) читают одиночный склеенный результат;
  - остальные (`ReportHeaderReport`, `RunConfigurationReport`,
    `DifferentialEvolutionSettingsReport`, `ReproductionVectorReport`,
    `GroupCombustionSolverParamsReport`, `GroupPerformanceMeterReport`) берут
    свой вход напрямую и не наследуют ничего.
- Порядок композиций задан один раз и не является выбором раздела:
  `CompositionNames => CompositionGroups.ReportNames` из внешнего
  `PastyPropellant.Core`. Раскладка 32-вектора и порядок разделов совпадают
  потому, что источник один.
- Несошедшийся расчёт печатается маркером, а не числом.
  `MixedCombustionParams.BurnRate` присваивается только при сходимости обоих
  подрешений, а контексты переиспользуются между вычислениями — значит поле
  может держать чужое значение. Единственный достоверный признак —
  `BurnRateIsFound`; `BurnRateConvergence.FormatMixedBurnRate` — единственное
  место, где это решается, и печатает `NOT CONVERGED`.
- Целевая функция, отказавшая на композиции, приходит как `double.MaxValue`.
  `BurnRateErrorReport` считает всё, что не конечно или ≥ `1e6`, этим
  сигналом и печатает словами: законная цель — O(0.01…1).
- Разбивка ошибки сходится с целевой функцией по построению: цель =
  `mean_fuel( sqrt( mean_pressure( ((calc−exp)/exp)² ) ) )`, поэтому
  показанный RMS топлива и есть его вклад, значение композиции — среднее по её
  топливам, общее — среднее по трём композициям, то есть
  `AggregatedFitness`. Раздел работает по групповому результату, а не по
  склеенной матрице, иначе группа `Bas_2+Bas_3+Bas_4` потеряла бы свой
  общий вес 1/3 и числа перестали бы сходиться.
- Ген у границы помечается «railed» с относительным допуском `1e-3`,
  масштабированным на `max(|bound|, 1)` — иначе углы O(1) и предэкспоненты
  O(1e13) судились бы по разным меркам.
- Числа параметров в `GroupCombustionSolverParamsReport` округлены для
  показа и негодны для воспроизведения: замыкание радиационной температуры
  усиливает округление. Воспроизводит только блок
  `ReproductionVectorReport`, печатающий вектор в полной точности.
- `RunConfigurationReport` получает уже отформатированный текст
  (`RunConfigurationSection`), а не объект конфигурации: библиотека отчётов
  не должна знать консольный хост.

## Dependencies

- [Results](../../../ParametricCombustionModel.Optimization/Results/API.md) —
  `GroupOptimizationResult`, `OptimizationResult`.
- [Models](../../../ParametricCombustionModel.Optimization/Models/API.md) —
  `OptimizationProblemByUnits`, `GroupCombustionSolverParamsByDoubles`.
- [Settings](../../../ParametricCombustionModel.Optimization/Settings/API.md) —
  `DifferentialEvolutionSettings`.
- [ComputedParams](../../../ParametricCombustionModel.Computation/Models/ComputedParams/API.md) —
  `MixedCombustionParams` для признака сходимости.
- [PdfOperations](../../PdfOperations/API.md) — операции, из которых собран
  выход.
- [Enums](../../Enums/API.md) — `TextStyle`.
- [Interfaces](../../Interfaces/API.md) — `ITransformable<T>`.
- [Models](../../Models/API.md) — `GroupReportContextDto`,
  `RunConfigurationSection`, `PressureTable`.
- [Reports](../API.md) — база `BaseReport`.

## Constraints

- Маркер несходимости — намеренно ASCII-литерал, а не ресурс: это диагностика
  расчёта, а не подпись, и она должна читаться одинаково в любой локали,
  чтобы несошедшуюся точку можно было найти грепом в любом отчёте.
- Единицы величин — UnitsNet; форматирование чисел — `CultureInfo.InvariantCulture`.

## Acceptance criteria

- [x] `BurnRateErrorReportTests` — разбивка ошибки сходится с целевой
      функцией и вырождается в маркер на несошедшемся расчёте.
- [x] `BurnRateVieilleFitReportTests` — подгонка `U = A·p^ν`.
- [x] `FlameStructureReportTests` — разложение теплового потока.
- [x] `PressureTablesReportTests` — таблицы по давлениям.
- [x] `RailFlagTests` — пометка гена у границы, включая масштаб допуска.
- [x] `CompositionBlockAttributionTests` — блок композиции подписан своей
      композицией, а не соседней.
- [ ] Малые потоки в PDF печатаются в кВт/м² с подписью Вт/м² — расхождение
      «как есть / как должно быть», числа надо брать из
      `forward_penalties.txt`.

## Taboos

- Не печатать `MixedCombustionParams.BurnRate`, не спросив
  `BurnRateIsFound`, и не заводить второе место, где это решается.
- Не открывать файлы и не вызывать PDF-библиотеку из раздела.
- Не выбирать в разделе свой порядок композиций.
- Не переносить округлённые показанные параметры в вектор воспроизведения.
