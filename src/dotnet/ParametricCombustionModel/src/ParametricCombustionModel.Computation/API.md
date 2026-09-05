# API.md — ParametricCombustionModel.Computation

Контракт численного ядра. Наружу сборка отдаёт три вещи: сборщик матрицы
контекстов, сами контексты и решатели, применяемые к ним через посетителя.
Собственных типов у каталога сборки нет.

## How the assembly is used

Порядок вызова, без объявлений: сборщик матрицы строит контексты «топливо ×
давление» по списку топлив и постоянным запуска, по отдельной копии на каждый
воркер; контекст точки принимает решатель методом `Accept`, передавая ему
вектор параметров; решатель пишет результат в поля того же контекста, откуда
его и читают, начиная с признака сходимости. Объявления — в узлах
[Builders](./Builders/API.md), [ProblemContexts](./Models/ProblemContexts/API.md)
и [Solvers](./Solvers/API.md).

## Children

- [Builders/API.md](./Builders/API.md) — сборка матрицы контекстов;
- [Common/API.md](./Common/API.md) — физические постоянные;
- [Extensions/API.md](./Extensions/API.md) — выводимое из рецептуры;
- [Interfaces/API.md](./Interfaces/API.md) — контракт посетителя;
- [Units/API.md](./Units/API.md) — размерные константы горения металла;
- [Models/ComputedParams/API.md](./Models/ComputedParams/API.md) — результаты;
- [Models/KnownParams/API.md](./Models/KnownParams/API.md) — вход и постоянные
  запуска;
- [Models/ProblemContexts/API.md](./Models/ProblemContexts/API.md) — контекст
  точки;
- [Solvers/API.md](./Solvers/API.md) — решатели.
