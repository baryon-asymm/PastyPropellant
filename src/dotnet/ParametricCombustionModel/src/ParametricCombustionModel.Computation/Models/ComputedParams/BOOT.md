# BOOT.md — ComputedParams

## Purpose

Результаты расчёта одной точки: что решатель записал по межкарманному
веществу, по карману, по кинетическому пламени и по смешанной скорости.
Уровень существует, чтобы выход решателя был структурой с именами, а не
набором `out`-параметров.

## Invariants

- Всё это **структуры с открытыми полями**, а не свойства. Поля нужны, чтобы
  потребитель брал `ref` на вложенную структуру и писал в неё на месте: путь
  вызывается миллионы раз за запуск, и копия здесь измерима.
- Пара `ByDoubles` / `ByUnits` полна по построению: одинаковый набор полей,
  одинаковый порядок, разница только в типе (`double` против `Speed`,
  `HeatFlux`, `Temperature`, …). Исключение одно и намеренное —
  `ConductiveThermalConductivityBalanceError` остаётся `double` в обоих
  ярусах, потому что это невязка, а не физическая величина.
- `BurnRateIsFound` — **единственный** признак сходимости. Скорость горения
  рядом с `BurnRateIsFound == false` не определена: она может остаться
  положительной с прошлой итерации, и потребитель обязан смотреть на флаг, а
  не на знак скорости.
- Структуры не валидируют себя и ничего не считают: любая формула — в
  [Solvers](../../Solvers/API.md).
- `PocketCombustionParams` содержит два `KineticFlameCombustionParams` —
  каркасное и внекаркасное пламя; `InterPocketCombustionParams` — одно.

## Dependencies

None. Наружу узел зависит только от UnitsNet, внешнего для дерева.

## Constraints

Унаследованы от [Computation](../../BOOT.md).

## Acceptance criteria

- [x] Флаг сходимости и все вычисленные поля совпадают между ярусами на
      сходящемся, нулевом и отрицательном случаях у всех трёх решателей
      (2026-09-05, `BurnRateIsFoundParityTests`, в частности
      `MixedSolver_ConvergingCase_EveryComputedFieldAgrees`, узел
      [Computation.Tests](../../../../tests/ParametricCombustionModel.Computation.Tests/BOOT.md)).
- [ ] Потребители, читающие скорость мимо флага, машиной не отлавливаются;
      в отчётах это закрыто тестами узла
      [ReportMaking.Tests](../../../../tests/ParametricCombustionModel.ReportMaking.Tests/BOOT.md).

## Taboos

- Не превращать поля в свойства: пропадёт `ref`-доступ, на котором держится
  горячий путь.
- Не читать `BurnRate`, не проверив `BurnRateIsFound`.
