using ComplexityResearch.Models;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// №1. Постоянная функция — O(1).
/// </summary>
/// <remarks>
/// <para>
/// ЛОГИЧЕСКАЯ СЛОЖНОСТЬ: O(1) — алгоритм выполняет фиксированное количество
/// операций независимо от размера задачи.
/// </para>
/// <para>
/// ТЕХНИЧЕСКОЕ КОЛИЧЕСТВО ПОВТОРЕНИЙ: реальное время одной «константной»
/// операции (~1 нс) меньше разрешения Stopwatch, поэтому для получения
/// измеримого времени фиксированная операция повторяется n раз.
/// Здесь n — технический параметр «количество повторений», а НЕ размер
/// входных данных задачи. Этим пункт O(1) отличается от остальных:
/// в графиках по этому алгоритму ось X означает число повторений,
/// и график линейно растёт вместе с n — это ожидаемо, т.к. тело цикла
/// (одна операция) выполняется n раз.
/// </para>
/// </remarks>
public sealed class ConstantAlgorithm : AlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№1. Постоянная функция — O(1)";

    /// <inheritdoc />
    public override string Description =>
        "Алгоритм выполняет фиксированное количество операций независимо от размера задачи. " +
        "Так как время одной операции меньше разрешения таймера, фиксированная операция " +
        "(сложение) повторяется n раз — это ТЕХНИЧЕСКОЕ повторение, а не рост сложности.";

    /// <inheritdoc />
    public override string Complexity => "O(1)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(1)";

    /// <inheritdoc />
    public override string Application =>
        "Доступ к элементу массива по индексу, арифметические выражения, " +
        "обмен двух переменных, проверка флага.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = C · n, где C — время одной элементарной операции (сложение + сравнение + присваивание)";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 3·10⁹ повторений ≈ 1–2 с на максимальной точке (эмпирически).
        StartN = 100_000_000L,
        EndN = 3_000_000_000L,
        StepN = 300_000_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => MeasurementMode.Direct;

    /// <inheritdoc />
    public override object PrepareInput(long n) => n;

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        // Цикл — это техническое средство измерения, а не часть логической задачи O(1).
        // Одна итерация: 1 сложение (acc += 7) + 1 инкремент счётчика + 1 сравнение.
        long n = (long)input;
        long acc = 0;
        for (long i = 0; i < n; i++)
        {
            acc += 7;
        }
        PreventOptimization(acc);
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        double count = n;
        // Сложения: acc += 7 и инкремент i++. Сравнение: i < n. Присваивание: acc.
        return new OperationCounts(
            Addition: 2 * count,
            Multiplication: 0,
            Comparison: count,
            Assignment: count,
            Swap: 0,
            ArrayAccess: 0);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => _ => 1.0;
}
