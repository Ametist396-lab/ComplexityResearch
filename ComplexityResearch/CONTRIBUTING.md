# CONTRIBUTING

Правила командной работы над проектом ComplexityResearch.

## Распределение работ между участниками

Проект разработан тремя участниками; зоны ответственности:

### Dev1 — скелет и основной код
- Каркас: `ComplexityResearch.sln`, `ComplexityResearch.csproj`, `App.xaml(.cs)`, `.gitignore`
- `Models/`: `BenchmarkConfiguration`, `BenchmarkResult`, `StatisticsResult`,
  `OperationCounts`, `OperationCostModel`, `MeasurementMode`, `BenchmarkProgress`,
  `PointTimeoutException`
- `Services/`: `BenchmarkTimer`, `BenchmarkService`, `StatisticsService`,
  `DataPreparationService`, `CalibrationService`, `BenchmarkDatabase` (БД + кэш)
- `ViewModels/`, `Views/`, `Controls/ChartControl.cs` (GUI целиком)
- Каркас алгоритмов: `IAlgorithm`, `AlgorithmBase`, `AlgorithmRegistry`
  и стандартные алгоритмы №1–№8: константа, сумма, произведение, многочлен 4а/4б,
  степени 5а–5в (`PowerTimeAlgorithms.cs`), Bubble/Quick/TimSort
  (`StreamInput`, `SortInput`, `TimSortInput`)
- Базовые тесты: сортировки, конфигурации, БД/кэш, степени

### Dev2 — дополнительный код + кастомные алгоритмы №10 и №11
- `Services/`: `ApproximationService` (аппроксимация C·f(n) + MSE),
  `ExportService` (CSV/JSON), `OperationBenchmarkService` (операционный бенчмарк
  возведения в степень)
- `Models/`: `OperationsPoint`, `ExperimentReport`, `ChartData`,
  `AlgorithmMetadata`, `ComplexityPoint`
- Алгоритмы: №10 `CustomAlgorithm` (быстрое возведение в степень),
  №11 `KadaneAlgorithm` (алгоритм Кадане)
- Тесты: аппроксимация/MSE, операционный бенчмарк, Кадане (`KadaneTests.cs`)

### Dev3 — кастомный алгоритм №12 + документация
- Алгоритм: №12 `ReverseArrayAlgorithm` (реверс массива)
- `Documentation/` целиком: `Algorithms.md`, `Architecture.md`, `Database.md`,
  `BenchmarkMethodology.md`, `flowcharts.md`, `Report.md`, `README.md`
- Тесты: `ReverseTests.cs`

Общая точка интеграции — `Algorithms/AlgorithmRegistry.cs`: каждый участник
добавляет одну строку со своими алгоритмами, не трогая чужой код.

## 1. Клонирование репозитория

```bash
git clone https://github.com/<organization>/ComplexityResearch.git
cd ComplexityResearch
dotnet build -c Release
dotnet run -c Release --project ComplexityResearch.csproj
```

Убедитесь, что проект собирается и запускается, прежде чем что-то менять.

## 2. Создание ветки

Структура веток:

```text
main      — стабильная версия, собирается всегда, только через Pull Request
develop   — основная ветка разработки
feature/* — ветки задач
```

Ветка создаётся от `develop`:

```bash
git checkout develop
git pull origin develop
git checkout -b feature/algorithm-my-alg
```

Имена веток: `feature/algorithm-<name>` (новый алгоритм), `feature/chart-<name>`,
`fix/<описание>`.

## 3. Добавление нового алгоритма

Новый алгоритм добавляется **без изменения** существующих алгоритмов и сервисов:

1. Создайте файл `Algorithms/MyAlgorithm.cs`, унаследуйте класс от `AlgorithmBase`.
2. Реализуйте обязательные члены:
   - `Name`, `Description`, `Complexity`, `ComplexityClass`, `Application`,
     `TheoreticalFormula` — метаданные для интерфейса и отчёта;
   - `DefaultConfiguration` — диапазон n и количество точек;
   - `ModeFor(n)` — прямой или масштабированный режим;
   - `PrepareInput(n)` — генерация данных (выполняется вне таймера);
   - `ExecuteCore(input)` — сам алгоритм (измеряется); без аллокаций и ввода-вывода;
   - `EstimateOperationCounts(n)` — теоретическое количество операций;
   - `ComplexityFunction` — функция f(n) для аппроксимации.
3. В конце `ExecuteCore` обязательно вызовите `PreventOptimization(result)`.
4. Зарегистрируйте алгоритм в `Algorithms/AlgorithmRegistry.cs` (одна строка).
5. Добавьте документацию в `Documentation/algorithms.md` и блок-схему
   в `Documentation/flowcharts.md`.
6. Проверьте сборку: `dotnet build -c Release` — 0 ошибок и 0 предупреждений.

## 4. Commit

- Осмысленные сообщения, стиль: `Add: bubble sort algorithm`, `Fix: log scale ticks`,
  `Docs: flowcharts for sorting`.
- Один коммит — одна логическая задача.
- Не коммитьте `bin/`, `obj/`, `*.user` (см. `.gitignore`).
- Перед коммитом: `git status`, `git diff` — убедитесь, что попадает только нужное.

```bash
git add Algorithms/MyAlgorithm.cs Algorithms/AlgorithmRegistry.cs Documentation/
git commit -m "Add: my algorithm (O(n log n))"
```

## 5. Push

```bash
git push -u origin feature/algorithm-my-alg
```

## 6. Pull Request

1. Откройте PR из `feature/...` в `develop` (не напрямую в `main`).
2. Заголовок и описание: что сделано, как проверить.
3. Проверки перед merge:
   - проект собирается в Release без предупреждений;
   - приложение запускается, новый алгоритм виден в списке;
   - эксперимент, график, таблица, экспорт CSV/JSON/PNG работают;
   - документация обновлена.
4. Ревью минимум одного участника команды, затем squash-merge в `develop`.
5. В `main` — только релизные merge из `develop`.
