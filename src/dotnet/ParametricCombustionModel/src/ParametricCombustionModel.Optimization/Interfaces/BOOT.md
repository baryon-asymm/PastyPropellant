# BOOT.md — Interfaces

## Purpose

Контракт посетителя на уровне оптимизации: `IFitnessFunctionVisitor` считает
целевую функцию по задаче, `IOptimizationVisitable` — задача, которая
посетителя принимает. Тот же образец, что в численном ядре, но на другом
уровне.

## Invariants

- Оба метода объявлены дважды, по ярусу. Поиск идёт по `ByDoubles`, отчёт — по
  `ByUnits`, и результат обязан совпадать.
- Посетитель **пишет результат в задачу** (`FitnessFunctionValue`,
  `TotalEvaluatedPenalty`), а не возвращает его. Это то, что позволяет
  оптимизатору не знать, из чего целевая функция составлена.
- Параметры решателя передаются `in`: это `ref struct`.

## Dependencies

- [KnownParams](../../ParametricCombustionModel.Computation/Models/KnownParams/API.md)
  — векторы параметров решателя в сигнатурах;
- [Models](../Models/API.md) — `OptimizationProblemByUnits` и
  `OptimizationProblemByDoubles` как посещаемые задачи.

## Constraints

Унаследованы от [Optimization](../BOOT.md).

## Acceptance criteria

- [x] Оба яруса реализованы (2026-09-05, по коду
      [FitnessFunctionEvaluators](../FitnessFunctionEvaluators/API.md);
      расхождение — ошибка компиляции).
- [ ] Совпадение целевой функции между ярусами тестом не закреплено; на
      практике его держит то, что финальная оценка и прямой расчёт вектора идут
      одним путём.

## Taboos

- Не возвращать целевую функцию из метода: два места записи результата
  разойдутся.
