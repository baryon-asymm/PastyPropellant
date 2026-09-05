# API.md — ParametricCombustionModel

Корень дерева. Собственного кода здесь нет; наружу дерево отдаёт шесть
библиотек.

## How the tree is used

Точка входа снаружи (`src/dotnet/Apps`) читает набор топлив типами узла
[Core](./src/ParametricCombustionModel.Core/API.md), строит матрицы контекстов
узлом [Computation](./src/ParametricCombustionModel.Computation/API.md) — по
копии на воркер, — и запускает поиск оптимизатором из узла
[Optimization](./src/ParametricCombustionModel.Optimization/API.md). Контексты
на горячем пути оборачиваются измеряющими из узла
[Telemetry](./src/ParametricCombustionModel.Telemetry/API.md). По сошедшемуся
32-вектору тот же оптимизатор считает итог тиром `ByUnits`; этот результат
рисует узел [PlotRenderer](./src/ParametricCombustionModel.PlotRenderer/API.md)
и печатает узел [ReportMaking](./src/ParametricCombustionModel.ReportMaking/API.md).
Файлы появляются в рабочем каталоге процесса.

Тот же путь проходит прямой расчёт одного заданного вектора: он зовёт тот же
публичный метод, что и конец оптимизации, и обязан давать побитово тот же
результат.

## Children

- [src/ParametricCombustionModel.Core/API.md](./src/ParametricCombustionModel.Core/API.md) —
  описание топлива и его компонентов;
- [src/ParametricCombustionModel.Computation/API.md](./src/ParametricCombustionModel.Computation/API.md) —
  численное ядро, контексты задачи, решатели;
- [src/ParametricCombustionModel.Optimization/API.md](./src/ParametricCombustionModel.Optimization/API.md) —
  оптимизаторы, оценщики штрафов, результаты;
- [src/ParametricCombustionModel.PlotRenderer/API.md](./src/ParametricCombustionModel.PlotRenderer/API.md) —
  графики скорости горения;
- [src/ParametricCombustionModel.ReportMaking/API.md](./src/ParametricCombustionModel.ReportMaking/API.md) —
  PDF-отчёт;
- [src/ParametricCombustionModel.Telemetry/API.md](./src/ParametricCombustionModel.Telemetry/API.md) —
  телеметрия прогона.

## Test nodes

- [tests/ParametricCombustionModel.Core.Tests/API.md](./tests/ParametricCombustionModel.Core.Tests/API.md);
- [tests/ParametricCombustionModel.Computation.Tests/API.md](./tests/ParametricCombustionModel.Computation.Tests/API.md);
- [tests/ParametricCombustionModel.Optimization.Tests/API.md](./tests/ParametricCombustionModel.Optimization.Tests/API.md);
- [tests/ParametricCombustionModel.PlotRenderer.Tests/API.md](./tests/ParametricCombustionModel.PlotRenderer.Tests/API.md);
- [tests/ParametricCombustionModel.ReportMaking.Tests/API.md](./tests/ParametricCombustionModel.ReportMaking.Tests/API.md);
- [tests/ParametricCombustionModel.Telemetry.Tests/API.md](./tests/ParametricCombustionModel.Telemetry.Tests/API.md).
