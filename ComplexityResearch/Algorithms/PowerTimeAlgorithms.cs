using ComplexityResearch.Models;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Вход адаптеров возведения в степень: показатель степени и число повторений.
/// </summary>
/// <param name="Exponent">Показатель степени n.</param>
/// <param name="Repeats">Техническое число вычислений за один замер
/// (один вызов занимает микросекунды — меньше разрешения таймера).</param>
public sealed record PowerTimeInput(int Exponent, int Repeats);

/// <summary>
/// Базовый класс «временных» адаптеров возведения в степень (№5а–5в).
/// </summary>
/// <remarks>
/// Один вызов возведения — единицы микросекунд, поэтому за один замер
/// алгоритм выполняется фиксированное число раз (<see cref="RepeatsPerRun"/>);
/// замеренное время растёт согласно сложности алгоритма (n, n, log₂ n).
/// Сами вычисления — те же статические ядра, что проверены unit-тестами
/// и используются в операционном бенчмарке (кнопка «Возведение в степень»).
/// Больших массивов не требуется — всегда прямой режим.
/// </remarks>
public abstract class PowerTimeAlgorithmBase : AlgorithmBase
{
    /// <summary>Число вычислений за один замер (технический параметр).</summary>
    protected abstract int RepeatsPerRun { get; }

    /// <summary>Чистое вычисление aⁿ mod m.</summary>
    protected abstract long ComputeCore(int exponent);

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => MeasurementMode.Direct;

    /// <inheritdoc />
    public override object PrepareInput(long n) =>
        new PowerTimeInput((int)Math.Clamp(n, 1, 1_000_000_000L), RepeatsPerRun);

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var p = (PowerTimeInput)input;
        long acc = 0;
        for (int rep = 0; rep < p.Repeats; rep++)
        {
            acc ^= ComputeCore(p.Exponent);
        }
        PreventOptimization(acc);
    }
}

/// <summary>
/// №5а. Итеративное возведение в степень — O(n): n умножений с остатком.
/// </summary>
public sealed class PowerIterativeTimeAlgorithm : PowerTimeAlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№5a. Возведение в степень (итеративно) — O(n)";

    /// <inheritdoc />
    public override string Description =>
        "result ← 1; n раз result ← result·a mod m — ровно n умножений. " +
        "Время растёт линейно по показателю. Измеряется через серию повторений (1000 за замер).";

    /// <inheritdoc />
    public override string Complexity => "O(n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public override string Application =>
        "Учебный базовый вариант; там, где показатель мал, а код должен быть максимально простым.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = K·n·(2·C_умн + C_срав + 2·C_присв), K = 1000 повторений за замер";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Линейный рост: 1e6 → 1e8 умножений с остатком ≈ 0.02–2 с на точку.
        StartN = 100L,
        EndN = 100_000L,
        StepN = 10_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    protected override int RepeatsPerRun => 1000;

    /// <inheritdoc />
    protected override long ComputeCore(int exponent) => PowerIterativeAlgorithm.Compute(exponent);

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        double perCall = Math.Clamp(n, 1, 1_000_000_000L);
        double k = RepeatsPerRun;
        return new OperationCounts(
            Addition: k * perCall,          // счётчик цикла
            Multiplication: 2 * k * perCall, // умножение + остаток
            Comparison: k * perCall,
            Assignment: 2 * k * perCall,
            Swap: 0,
            ArrayAccess: 0);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n;
}

/// <summary>
/// №5б. Рекурсивное возведение в степень — O(n): глубина рекурсии n.
/// </summary>
/// <remarks>
/// Показатель ограничен 5000: глубина рекурсии n, кадр стека ~100 байт —
/// 5000 кадров ≈ 0.5 МБ, безопасно для стека 1 МБ.
/// </remarks>
public sealed class PowerRecursiveTimeAlgorithm : PowerTimeAlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№5b. Возведение в степень (рекурсивно) — O(n)";

    /// <inheritdoc />
    public override string Description =>
        "power(a,k) = a·power(a,k−1); power(a,0) = 1. Линейное число операций плюс накладные " +
        "расходы вызовов; глубина рекурсии = n (ограничена 5000 для безопасности стека).";

    /// <inheritdoc />
    public override string Complexity => "O(n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public override string Application =>
        "Учебная демонстрация рекурсии; разбор «разделяй и властвуй» перед бинарным вариантом.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = K·n·(2·C_умн + 2·C_срав + C_присв) + накладные расходы вызовов, K = 20000 повторений";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Ограничение по глубине рекурсии (безопасно до ~10000 кадров стека).
        StartN = 100L,
        EndN = 5_000L,
        StepN = 500L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    protected override int RepeatsPerRun => 20_000;

    /// <inheritdoc />
    protected override long ComputeCore(int exponent) => PowerRecursiveAlgorithm.Compute(exponent);

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        double perCall = Math.Clamp(n, 1, 1_000_000_000L);
        double k = RepeatsPerRun;
        return new OperationCounts(
            Addition: 2 * k * perCall,
            Multiplication: 2 * k * perCall,
            Comparison: 2 * k * perCall,
            Assignment: k * perCall,
            Swap: 0,
            ArrayAccess: 0);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n;
}

/// <summary>
/// №5в. Бинарное (быстрое) возведение в степень — O(log n).
/// </summary>
public sealed class PowerBinaryTimeAlgorithm : PowerTimeAlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№5c. Возведение в степень (бинарно) — O(log n)";

    /// <inheritdoc />
    public override string Description =>
        "Двоичное разложение показателя: на каждом шаге основание в квадрат, при единичном бите — " +
        "домножение. Всего ≈ log₂(n) итераций; на диапазоне 100 → 10⁹ время растёт всего ~4 раза.";

    /// <inheritdoc />
    public override string Complexity => "O(log n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(log n)";

    /// <inheritdoc />
    public override string Application =>
        "Криптография (RSA, Диффи–Хеллман), модульная арифметика больших степеней, " +
        "быстрые биномиальные коэффициенты.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = K·log₂(n)·(3·C_умн + C_срав + 2·C_присв), K = 1 000 000 повторений за замер";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Логарифмический рост: log₂ от 7 до 30 итераций — рост времени ~4x.
        StartN = 100L,
        EndN = 1_000_000_000L,
        StepN = 100_000_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    protected override int RepeatsPerRun => 1_000_000;

    /// <inheritdoc />
    protected override long ComputeCore(int exponent) => PowerBinaryAlgorithm.Compute(exponent);

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        double iterations = Math.Log2(Math.Clamp(n, 2, 1_000_000_000L));
        double k = RepeatsPerRun;
        return new OperationCounts(
            Addition: 2 * k * iterations,
            Multiplication: 3 * k * iterations,
            Comparison: k * iterations,
            Assignment: 2 * k * iterations,
            Swap: 0,
            ArrayAccess: 0);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => Math.Log2(Math.Max(2, n));
}
