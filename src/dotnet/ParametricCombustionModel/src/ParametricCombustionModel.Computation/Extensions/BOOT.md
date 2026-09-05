# BOOT.md — Extensions

## Purpose

Всё, что выводится из рецептуры топлива до начала расчёта: объёмные доли двух
областей (карман и межкарманное вещество), диаметр частиц окислителя, массовая
доля тонкого окислителя, поверхностная доля каркаса под выбранным замыканием и
температура кипения металла. Уровень существует, чтобы эти выводы делались
один раз и одинаково для оба ярусов расчёта.

## Invariants

- Обе области построены из одного и того же соединения (алюминий + горючее
  связующее + мелкая фракция перхлората аммония) и различаются только его
  долей: карман берёт `PocketMassFraction`, межкарманное вещество —
  дополнение `1 − PocketMassFraction`. Внутренние пропорции соединения
  считаются одинаковыми в обеих областях.
- Отсутствие любого из трёх обязательных компонентов — исключение
  (`ArgumentNullException` с именем компонента), а не ноль.
- Полиномы берут давление в **МПа** (`pressure / 1e6`), хотя аргумент — Па.
  Это относится и к `GetPocketSurfaceFraction`, и к
  `GetMetalBoilingTemperature`.
- `GetPocketSurfaceFraction` делит полином на `PocketMassFraction`: полином
  описывает измеренную `Z_a^m` от всего топлива, а модели нужна доля от
  площади кармана.
- `GetSkeletonCoverage` — **единственная точка входа** к поверхностной доле
  каркаса для сборщиков контекста. `null` в настройках означает исторический
  полином; кинетическое замыкание даёт постоянную кривую; равновесное берёт
  кривую из таблицы и бросает, если топлива или давления в ней нет.
- `MetalMeltingTemperatureKelvins = 1300` здесь — **только значение по
  умолчанию** для `ModelConstants`. ⚠ Читать эту постоянную из решателя или
  сборщика запрещено: настроенный запуск тогда молча считал бы при 1300 К.
- Коэффициенты полинома кипения металла (7 чисел) вписаны в код. Это данные, а
  не конфигурация, и они не выведены наружу.

## Dependencies

- [Models](../../ParametricCombustionModel.Core/Models/API.md) — `Propellant`
  как расширяемый тип;
- [PropellantComponents](../../ParametricCombustionModel.Core/Models/PropellantComponents/API.md)
  — `Aluminum`, `CombustibleBinder`, `AmmoniumPerchlorate`;
- [KnownParams](../Models/KnownParams/API.md) —
  `SkeletonSurfaceFractionSettings`, `SkeletonCoverageCurve`,
  `KineticCoverageSettings` в `GetSkeletonCoverage`.

## Constraints

Унаследованы от [Computation](../BOOT.md). Дополнительно: расширения обязаны
оставаться чистыми функциями от записи топлива — никакого состояния и никакого
кэша, потому что их зовут из всех воркеров.

## Acceptance criteria

- [x] Замыкание по умолчанию даёт ровно исторический полином при любой
      температуре поверхности (2026-09-05,
      `SkeletonSurfaceFractionTests.DefaultClosureIsTheHistoricalPolynomialAtEverySurfaceTemperature`).
- [x] Кинетическое замыкание воспроизводит измеренную поверхностную долю своего
      калибровочного набора, остаётся долей везде и падает с давлением только
      через тонкий окислитель (2026-09-05, `KineticCoverageTests`).
- [x] Отсутствие топлива или давления в равновесной таблице бросает
      (2026-09-05, `SkeletonSurfaceFractionTests.APropellantMissingFromTheTableThrows`,
      `...APressureMissingFromTheTableThrows`).
- [ ] Совпадение объёмных долей двух областей с независимым расчётом
      микроструктуры не проверяется здесь: это предмет отдельного дерева
      (порт PropStructV3).

Все перечисленные тесты живут в узле
[Computation.Tests](../../../tests/ParametricCombustionModel.Computation.Tests/BOOT.md).

## Taboos

- Не читать `MetalMeltingTemperatureKelvins` из расчётного кода: только через
  `ModelConstants`.
- Не заводить второй путь к поверхностной доле каркаса в обход
  `GetSkeletonCoverage`: два яруса тогда разойдутся в том, какая модель в силе.
