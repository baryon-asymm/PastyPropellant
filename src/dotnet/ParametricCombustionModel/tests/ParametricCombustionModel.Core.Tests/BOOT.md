# BOOT.md — ParametricCombustionModel.Core.Tests

## Purpose

Одна проверка: описание топлива разбирается из JSON и кадры давлений
читаются в объявленном порядке.

## Invariants

- Проверяется десериализация **одиночного** `Propellant`, а не списка:
  боевой вход — список, и его разбирает хост, но контракт имён полей
  проверяется здесь, на одном объекте.
- Порядок кадров давления значим и проверяется по индексу: расчёт и отчёт
  ходят по кадрам позиционно.
- Давления в файле — паскали (`4e6`, `7e6`), а не мегапаскали.
- Файл `data/propellants.json` — локальный маленький набор этого проекта,
  а не боевой `propellants.01234.json`. Единственный, кто его читает, — этот
  тест.

## Dependencies

- [Core](../../src/ParametricCombustionModel.Core/API.md) — разбираемые типы.

Вне дерева: xunit 2.9.2, `Microsoft.NET.Test.Sdk` 17.12.0, coverlet.

## Constraints

Проект входит в `PastyPropellant.sln` и исполняется обычным `dotnet test`.

## Acceptance criteria

- [x] `PropellantParsingTest.ParsePropellant` зелёный (2026-09-05).
- [ ] Покрытие узла [Core](../../src/ParametricCombustionModel.Core/API.md)
      одним тестом на разбор: ни `Aluminum`, ни коэффициенты покрытия, ни
      доверительные интервалы здесь не проверяются. Часть этого закрыта в
      узле [Computation.Tests](../ParametricCombustionModel.Computation.Tests/BOOT.md),
      но как проверка данных, а не типов.

## Taboos

- Не подменять локальный набор боевым: тесты боевого набора живут в
  `Computation.Tests` и подключают его ссылкой.
