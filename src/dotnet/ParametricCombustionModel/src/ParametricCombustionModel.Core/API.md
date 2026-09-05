# API.md — ParametricCombustionModel.Core

Контракт сборки — описание топлива, читаемое из JSON. Типы верхнего уровня
живут в узле [Models](./Models/API.md); собственных типов у каталога сборки нет.

## Children

- [Models/API.md](./Models/API.md) — `Propellant`, `PressureFrame`,
  `ConfidenceInterval`, разборщик компонентов;
- [Models/GasPhases/API.md](./Models/GasPhases/API.md) — газовые фазы;
- [Models/PropellantComponents/API.md](./Models/PropellantComponents/API.md) —
  компоненты рецептуры.

## Consumed as

Сборка отдаёт только данные, точки входа у неё нет. Вызывающая сторона
десериализует файл как `List<Propellant>` с регистронезависимым сопоставлением
имён; ничего иного от сборки не требуется и никакого статуса реализации здесь
не объявляется.
