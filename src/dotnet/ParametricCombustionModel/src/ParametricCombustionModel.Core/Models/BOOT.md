# BOOT.md — Models

## Purpose

Корень описания топлива: запись `Propellant` (рецептура, измеренный закон
скорости, коэффициенты поверхностной доли каркаса, доля кармана), кадр по
давлению `PressureFrame` (пористость и газовые фазы при одном давлении) и
разборщик компонентов. Уровень существует, чтобы весь входной файл
`data/propellants*.json` имел один типизированный корень.

## Invariants

- `Propellant.A` и `Propellant.Nu` — коэффициенты **измеренного** закона Вьеля
  `r = A·p^ν`; модель их не подгоняет, а сверяется с ними.
- `PocketSurfaceFractionCoefficients` — те же 22 числа, что
  `Aluminum.AgglomerationCoefficients` в
  [PropellantComponents](./PropellantComponents/API.md). ⚠ Копии обязаны
  меняться вместе; машина этого не проверяет.
- `PressureFrames` и `ConfidenceIntervals` необязательны (`null`), всё
  остальное помечено `[JsonRequired]`.
- `PropellantComponentJsonConverter` читает **объект**, а не массив: имя
  JSON-свойства обязано совпадать с именем типа компонента (`nameof`).
  Неизвестное имя, свойство не там или неразобравшийся компонент — всегда
  `JsonException`, никогда не пропуск.
- `ConfidenceInterval.SizeOfConfidenceInterval` — полная высота уса, а `XValue`
  для агломерации в МПа. Половина размера и единица давления — самая частая
  ошибка потребителя, поэтому названы здесь.

## Dependencies

None. `GasPhases/` и `PropellantComponents/` — дети этого узла, а не соседи;
родитель ими владеет.

## Constraints

Унаследованы от [Core](../BOOT.md). Дополнительно: разбор идёт
`System.Text.Json` без регистрозависимости (её включает вызывающая сторона), а
файл десериализуется как `List<Propellant>`.

## Acceptance criteria

- [x] `PropellantParsingTest.ParsePropellant` читает реальный
      `data/propellants.json` и получает непустой список топлив с разобранными
      компонентами (2026-09-05, `ParametricCombustionModel.Core.Tests`).
- [ ] Неизвестное имя компонента даёт `JsonException` — следует из кода
      разборщика, тестом не закреплено.

## Taboos

- Не заводить второй разборщик компонентов: имя типа как ключ JSON — единая
  точка соответствия файла и кода.
- Не расходиться копиями коэффициентов: правка одной без другой меняет модель
  молча.
