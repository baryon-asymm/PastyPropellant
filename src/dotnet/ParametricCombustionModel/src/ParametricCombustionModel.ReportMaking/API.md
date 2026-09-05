# API.md — ParametricCombustionModel.ReportMaking

Контракт отчётности. Наружу сборка отдаёт сборщик отчёта, его входной DTO и
набор разделов; собственных типов у каталога сборки нет.

## How the assembly is used

Вызывающая сторона наполняет `GroupReportContextDto` из узла
[Models](./Models/API.md) — групповой результат, настройки ДЭ, измерения
производительности, путь к файлу топлив, готовые секции конфигурации запуска —
и передаёт его вместе с адаптером PDF в `GroupPdfReportMaker` из узла
[ReportMakers](./ReportMakers/API.md). Один вызов `MakeReport()` собирает все
разделы, исполняет их операции и генерирует файл в рабочем каталоге процесса.
Разделы из узла [Reports/Pdf](./Reports/Pdf/API.md) доступны и по отдельности:
каждый — преобразование без побочных эффектов, что и позволяет проверять их
числа без PDF.

## Children

- [Enums/API.md](./Enums/API.md) — стили текста;
- [Interfaces/API.md](./Interfaces/API.md) — контракты сборщика, операции и
  преобразования;
- [Models/API.md](./Models/API.md) — входной DTO, секции конфигурации,
  таблица по давлениям;
- [PdfOperations/API.md](./PdfOperations/API.md) — операции печати;
- [ReportMakers/API.md](./ReportMakers/API.md) — сборщик отчёта;
- [Reports/API.md](./Reports/API.md) — база разделов и все разделы PDF;
- [Resources/Generated/API.md](./Resources/Generated/API.md) — подписи из
  `.resx`.
