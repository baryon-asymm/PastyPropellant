# BOOT.md — Extensions

## Purpose

Измеренная скорость горения топлива по закону Вьеля `r = A·p^ν` — то, с чем
модель сравнивается. Уровень существует, чтобы эталон вычислялся в одном месте
и одинаково в обоих ярусах.

## Invariants

- Это **эталон, а не модель**: `A` и `ν` берутся из записи топлива как
  измеренные и никогда не подгоняются.
- Давление на входе — паскали; размерная перегрузка возвращает `Speed`, ярус
  `double` — метры в секунду. Ярусы обязаны давать одно и то же число.
- Функция чистая: ни состояния, ни кэша. Целевая функция зовёт её один раз на
  точку при построении контекста задачи.

## Dependencies

- [Models](../../ParametricCombustionModel.Core/Models/API.md) — `Propellant`
  как расширяемый тип, откуда берутся `A` и `Nu`.

## Constraints

Унаследованы от [Optimization](../BOOT.md).

## Acceptance criteria

- [x] Экспериментальные коэффициенты в отчёте берутся из записи топлива, а не
      пересчитываются подгонкой (2026-09-05,
      `BurnRateVieilleFitReportTests.ExperimentalCoefficients_AreEchoedFromThePropellantNotRefitted`,
      узел [ReportMaking.Tests](../../../tests/ParametricCombustionModel.ReportMaking.Tests/BOOT.md)).
- [x] Эталонные скорости фикстуры равны ожидаемым (2026-09-05,
      `BurnRateErrorReportTests.Fixture_ExperimentalRatesAreOneTwoAndThreeMillimetresPerSecond`).

## Taboos

- Не подгонять `A` и `ν` здесь: это сделало бы эталон зависимым от модели.
