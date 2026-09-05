# API.md — ParametricCombustionModel.PlotRenderer

Контракт отрисовки. Наружу сборка отдаёт два групповых рисовальщика и объект
настроек графика; собственных типов у каталога сборки нет.

## How the assembly is used

Вызывающая сторона создаёт настройки из узла [Models](./Models/API.md), берёт
рисовальщик из узла [Renderers](./Renderers/API.md) и зовёт `Render` с
групповым результатом. Файл появляется в рабочем каталоге процесса под
фиксированным именем, которое затем ищет отчётность.

## Children

- [Drawers/API.md](./Drawers/API.md) — доверительные интервалы;
- [Extensions/API.md](./Extensions/API.md) — сброс позиции потока;
- [Interfaces/API.md](./Interfaces/API.md) — контракты рисовальщика;
- [Models/API.md](./Models/API.md) — настройки и серия данных;
- [Renderers/API.md](./Renderers/API.md) — сами рисовальщики.
