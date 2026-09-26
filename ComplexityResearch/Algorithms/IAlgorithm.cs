using ComplexityResearch.Models;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Общий интерфейс алгоритма, участвующего в исследовании.
/// </summary>
/// <remarks>
/// Чтобы добавить новый алгоритм в приложение, достаточно:
/// 1) создать класс, унаследованный от <see cref="AlgorithmBase"/>;
/// 2) реализовать обязательные члены;
/// 3) зарегистрировать экземпляр в <see cref="AlgorithmRegistry"/>.
/// Сервисы бенчмаркинга, статистики и построения графиков менять не нужно.
/// </remarks>
public interface IAlgorithm
{
    /// <summary>Название алгоритма (отображается в списке и легенде).</summary>
    string Name { get; }

    /// <summary>Краткое описание идеи алгоритма.</summary>
    string Description { get; }

    /// <summary>Теоретическая сложность в нотации O-большое, например "O(n log n)".</summary>
    string Complexity { get; }

    /// <summary>Класс сложности — ключ для функции аппроксимации f(n).</summary>
    string ComplexityClass { get; }

    /// <summary>Практическое применение алгоритма.</summary>
    string Application { get; }

    /// <summary>Формула теоретического времени (текстом, для интерфейса и отчёта).</summary>
    string TheoreticalFormula { get; }

    /// <summary>Конфигурация бенчмарка по умолчанию (диапазон n и число точек по ТЗ).</summary>
    BenchmarkConfiguration DefaultConfiguration { get; }

    /// <summary>
    /// Определяет режим измерения для данного n: прямой (реальный массив размера n)
    /// или масштабированный (повторяющийся блок фиксированного размера).
    /// </summary>
    MeasurementMode ModeFor(long n);

    /// <summary>
    /// Готовит входные данные для размера n. Вызывается ВНЕ измеряемого участка:
    /// генерация данных не должна попадать в замер времени.
    /// </summary>
    object PrepareInput(long n);

    /// <summary>
    /// Готовит данные одного запуска (например, копию несортированного массива,
    /// чтобы алгоритм каждый раз работал с одинаковыми исходными данными).
    /// Вызывается вне измеряемого участка. По умолчанию — те же данные.
    /// </summary>
    object PrepareRunInput(object sharedInput);

    /// <summary>Оценка количества элементарных операций каждого типа для размера n.</summary>
    OperationCounts EstimateOperationCounts(long n);

    /// <summary>Суммарная оценка количества элементарных операций.</summary>
    long EstimateOperations(long n);

    /// <summary>Теоретическое время выполнения (наносекунды) по модели стоимости операций.</summary>
    double EstimateTheoreticalTimeNs(long n, OperationCostModel costModel);

    /// <summary>Функция сложности f(n) для аппроксимации y = a·f(n) + b.</summary>
    Func<double, double> ComplexityFunction { get; }
}
