# API.md — Resources/Generated

Пространство имён `ParametricCombustionModel.ReportMaking.Resources`
(не `…Resources.Generated` — см. [BOOT.md](./BOOT.md)).

## Not part of the contract

Девять `internal` классов подписей, порождённых из `.resx`:
`PropellantReportResources`, `CombustionSolverParamsReportResources`,
`ProblemContextReportResources`, `ConstraintPenaltyEvaluatorReportResources`,
`ParametricConstraintReportResources`, `FitnessFunctionEvaluatorReportResources`,
`PressureTablesReportResources`, `PerformanceMeterReportResources`,
`DifferentialEvolutionSettingsReportResources`.

У каждого — статические `ResourceManager` / `Culture` и по одному
статическому строковому свойству на ключ `.resx`. Контракта наружу сборки
нет: классы `internal`, а их состав целиком определяется `.resx`, поэтому
перечислять свойства здесь означало бы дублировать порождённый файл.
