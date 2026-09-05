# BOOT.md — Models

## Purpose

Задача оптимизации: матрица контекстов «топливо × давление», эталонные
скорости, решатель, набор оценщиков штрафа и место под результат. Плюс
`OptimizationResult` — результат по одной композиции. Уровень существует,
чтобы у целевой функции был один объект со всем нужным.

## Invariants

- Эталонные скорости считаются **один раз в конструкторе** по всей матрице и
  дальше не пересчитываются: они не зависят от вектора параметров.
- Число топлив и число давлений берутся из размеров переданной матрицы, а не
  задаются отдельно, — рассогласоваться не могут.
- **Задача изменяема и принадлежит одному воркеру.** `FitnessFunctionValue`,
  `TotalEvaluatedPenalty` и `EvaluatedPenalties` перезаписываются на каждой
  оценке.
- Каждый ярус поддерживает **только свой** `Accept`; вызов чужого бросает
  `NotSupportedException`.
- `null` вместо матрицы или решателя — `ArgumentNullException` в конструкторе.
- `OptimizationResult` отдаёт границы и найденный вектор как
  `ReadOnlySpan<double>` и умеет собрать из вектора размерные параметры
  решателя.

## Dependencies

- [Computation/Interfaces](../../ParametricCombustionModel.Computation/Interfaces/API.md)
  — `ISolverVisitor`, хранимый в задаче;
- [KnownParams](../../ParametricCombustionModel.Computation/Models/KnownParams/API.md)
  — векторы параметров решателя;
- [ProblemContexts](../../ParametricCombustionModel.Computation/Models/ProblemContexts/API.md)
  — матрица контекстов;
- [ConstraintPenaltyEvaluators/Interfaces](../ConstraintPenaltyEvaluators/Interfaces/API.md)
  — набор оценщиков штрафа;
- [Extensions](../Extensions/API.md) — эталонная скорость по закону Вьеля;
- [Optimization/Interfaces](../Interfaces/API.md) — реализуемый
  `IOptimizationVisitable`.

## Constraints

Унаследованы от [Optimization](../BOOT.md).

## Acceptance criteria

- [x] Индекс воркера никогда не выдаётся двум одновременным оценкам, то есть
      разделение задач по воркерам корректно (2026-09-05,
      `DifferentialEvolutionWorkerIndexContractTests`, узел
      [Optimization.Tests](../../../tests/ParametricCombustionModel.Optimization.Tests/BOOT.md)).
- [ ] Однократность расчёта эталонных скоростей тестом не закреплена.

## Taboos

- Не делить одну задачу между воркерами.
- Не пересчитывать эталонные скорости внутри оценки.
