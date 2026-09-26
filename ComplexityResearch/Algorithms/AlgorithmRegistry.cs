namespace ComplexityResearch.Algorithms;

/// <summary>
/// Реестр алгоритмов исследования.
/// </summary>
/// <remarks>
/// <para>
/// ЕДИНАЯ точка регистрации алгоритмов. Чтобы добавить новый алгоритм:
/// 1) создать класс, унаследованный от <see cref="AlgorithmBase"/>;
/// 2) реализовать обязательные методы;
/// 3) добавить экземпляр в массив ниже.
/// </para>
/// <para>
/// Больше НИЧЕГО менять не нужно: BenchmarkService, StatisticsService,
/// ApproximationService, построение графика и таблица результатов работают
/// с любым алгоритмом одинаково (принцип открытости/закрытости).
/// </para>
/// </remarks>
public static class AlgorithmRegistry
{
    /// <summary>Возвращает список всех зарегистрированных алгоритмов (временной бенчмарк).</summary>
    public static IReadOnlyList<AlgorithmBase> CreateDefaultAlgorithms() =>
    [
        // Стандартные алгоритмы (№1–№9).
        new ConstantAlgorithm(),                 // №1
        new SumAlgorithm(),                      // №2
        new ProductAlgorithm(),                  // №3
        new PolynomialNaiveAlgorithm(),          // №4а
        new HornerAlgorithm(),                   // №4б
        new PowerIterativeTimeAlgorithm(),       // №5а
        new PowerRecursiveTimeAlgorithm(),       // №5б
        new PowerBinaryTimeAlgorithm(),          // №5в
        new BubbleSortAlgorithm(),               // №6
        new QuickSortAlgorithm(),                // №7
        new TimSortAlgorithm(),                  // №8
        new MatrixMultiplicationAlgorithm(),     // №9

        // Кастомные (индивидуальные) алгоритмы — в конце списка (№10–№12).
        new CustomAlgorithm(),                   // №10
        new KadaneAlgorithm(),                   // №11
        new ReverseArrayAlgorithm()              // №12
    ];

    /// <summary>
    /// Алгоритмы «операционного» бенчмарка возведения в степень: измеряется
    /// количество элементарных операций (не время), n = 1..1000.
    /// </summary>
    public static IReadOnlyList<IOperationCountedAlgorithm> CreatePowerAlgorithms() =>
    [
        new PowerIterativeAlgorithm(),
        new PowerRecursiveAlgorithm(),
        new PowerBinaryAlgorithm()
    ];
}
