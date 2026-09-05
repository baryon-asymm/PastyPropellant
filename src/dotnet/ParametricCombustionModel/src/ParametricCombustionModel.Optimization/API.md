# API.md — ParametricCombustionModel.Optimization

Контракт сборки поиска. Наружу она отдаёт три вещи: оптимизатор, который
одновременно является функцией приспособленности, настройки поиска и групповой
результат. Собственных типов у каталога сборки нет.

## How the assembly is used

Хост собирает настройки построителем из узла [Settings](./Settings/API.md),
готовит матрицу задач `[воркер, группа]` из узла [Models](./Models/API.md) с
набором оценщиков штрафа из узла
[ConstraintPenaltyEvaluators](./ConstraintPenaltyEvaluators/API.md), передаёт
всё это оптимизатору из узла [Optimizers](./Optimizers/API.md) и получает
результат узла [Results](./Results/API.md). Тот же оптимизатор умеет посчитать
один вектор без поиска.

## Children

- [ConstraintPenaltyEvaluators/API.md](./ConstraintPenaltyEvaluators/API.md) —
  штрафы за нарушенные ограничения;
- [ConstraintPenaltyEvaluators/Interfaces/API.md](./ConstraintPenaltyEvaluators/Interfaces/API.md)
  — контракт оценщика штрафа;
- [Extensions/API.md](./Extensions/API.md) — эталонная скорость горения;
- [FitnessFunctionEvaluators/API.md](./FitnessFunctionEvaluators/API.md) —
  целевая функция;
- [Interfaces/API.md](./Interfaces/API.md) — контракт посетителя;
- [Models/API.md](./Models/API.md) — задача оптимизации;
- [Optimizers/API.md](./Optimizers/API.md) — групповой оптимизатор;
- [Results/API.md](./Results/API.md) — групповой результат и переходник;
- [Settings/API.md](./Settings/API.md) — настройки поиска;
- [Utils/API.md](./Utils/API.md) — текстовый формат вектора.
