# API.md — Resources

Каталог данных и генерации. Типов не объявляет и наружу ничего не отдаёт.

## Not part of the contract

- `Generate-Resources.ps1` — генератор классов из `.resx` (только Windows,
  только нейтральная культура, список из девяти имён внутри);
- `*.resx` — подписи в трёх локалях; `*.resources` и `ResGen.exe` —
  вкоммиченные артефакты и инструмент.

## Children

- [Generated/API.md](./Generated/API.md) — порождённые классы подписей.
