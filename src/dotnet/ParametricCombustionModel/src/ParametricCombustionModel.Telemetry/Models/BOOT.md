# BOOT.md — Models (Telemetry)

## Purpose

Измеряющая обёртка над контекстом задачи оптимизации: тот же контекст, но
каждый вызов решателя обёрнут кадром измерения.

## Invariants

- Обёртка — декоратор наследованием, а не композицией: она наследует
  `OptimizationProblemByDoubles` и переопределяет `Accept`. Поэтому она
  подставляется в матрицу контекстов вместо базового типа, и сценарий
  действительно её туда кладёт — измеряется весь боевой прогон, а не
  отдельный замер.
- Один измеритель на воркер, по `processorId`: имя и путь прибора включают
  номер процессора. Это и есть условие, при котором непотокобезопасный
  накапливающий измеритель корректен.
- ⚠ Перегрузка `Accept` по `CombustionSolverParamsByUnits` намеренно бросает
  `NotSupportedException`: измеряется только тир `ByDoubles`, тир `ByUnits`
  через измеряющий контекст не ходит.
- ⚠ Пространство имён — `ParametricCombustionModel.Optimization.Models`, а
  каталог `Telemetry/Models`. Тип входит в пространство имён чужой сборки,
  чтобы вызывающая сторона подставляла его без дополнительного `using`.
  Каталог и пространство имён не совпадают.
- `AggressiveOptimization` на обеих перегрузках: контекст на горячем пути.

## Dependencies

- [Instruments](../Instruments/API.md) — накапливающий измеритель;
- [Telemetry](../API.md) — `PerformanceMeter`, который его заводит;
- [Models](../../ParametricCombustionModel.Optimization/Models/API.md) —
  базовый контекст задачи;
- [Interfaces](../../ParametricCombustionModel.Optimization/Interfaces/API.md) —
  посетитель целевой функции;
- [Interfaces](../../ParametricCombustionModel.Computation/Interfaces/API.md) —
  посетитель решателя;
- [KnownParams](../../ParametricCombustionModel.Computation/Models/KnownParams/API.md),
  [ProblemContexts](../../ParametricCombustionModel.Computation/Models/ProblemContexts/API.md) —
  параметры и матрица контекстов;
- [ConstraintPenaltyEvaluators/Interfaces](../../ParametricCombustionModel.Optimization/ConstraintPenaltyEvaluators/Interfaces/API.md) —
  оценщики штрафов, передаваемые базе.

## Constraints

Матрица контекстов мутируется в ходе решения, поэтому у каждого воркера своя
копия. Обёртка это правило не меняет и не смеет его нарушать.

## Acceptance criteria

- [x] Сценарий подставляет обёртку в матрицу контекстов, и прогон
      измеряется (2026-09-05, по коду
      `GroupDifferentialEvolutionScenario`).
- [ ] Расхождение каталога и пространства имён ничем не проверяется и
      ломает наивное «узел = последний сегмент пространства имён».

## Taboos

- Не заводить один измеритель на несколько воркеров.
- Не добавлять в обёртку логику расчёта: она обязана оставаться прозрачной.
