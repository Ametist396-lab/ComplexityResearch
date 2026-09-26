using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Входные данные алгоритмов вычисления многочлена.
/// </summary>
/// <param name="Coefficients">Массив коэффициентов a0..an (размер n+1).</param>
/// <param name="X">Точка вычисления многочлена.</param>
/// <param name="Passes">Количество проходов (масштабированный режим).</param>
public sealed record PolynomialInput(double[] Coefficients, double X, long Passes);

/// <summary>
/// №4a. Вычисление многочлена наивным способом — O(n²).
/// </summary>
/// <remarks>
/// <para>
/// P(x) = a0 + a1·x + a2·x² + ... + an·xⁿ.
/// Наивный способ: для КАЖДОГО слагаемого степень x вычисляется заново
/// отдельным внутренним циклом (предыдущая степень не переиспользуется).
/// Это даёт Σ i ≈ n²/2 умножений — сложность O(n²).
/// </para>
/// <para>
/// Используется тип double: произведение x^k для x = 1.5 и k до 50000
/// быстро выходит за пределы даже long, а для измерения времени значение
/// степени важно лишь с точки зрения количества операций. Арифметика
/// с плавающей точкой переполняется в +Inf без исключений, что не влияет
/// на число выполняемых операций.
/// </para>
/// </remarks>
public sealed class PolynomialNaiveAlgorithm : AlgorithmBase
{
    /// <summary>Максимальный физический размер массива коэффициентов.</summary>
    public const int BlockLimit = 50_000_000;

    /// <inheritdoc />
    public override string Name => "№4a. Многочлен (наивно) — O(n²)";

    /// <inheritdoc />
    public override string Description =>
        "P(x) = a0 + a1·x + ... + an·xⁿ. Для каждого слагаемого степень x вычисляется заново " +
        "внутренним циклом — Σ i ≈ n²/2 умножений. На графике чётко отличается от метода Горнера (O(n)).";

    /// <inheritdoc />
    public override string Complexity => "O(n²)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n^2)";

    /// <inheritdoc />
    public override string Application =>
        "Учебный «плохой» вариант; наивная интерполяция, быстрые черновые расчёты малых степеней.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) ≈ (n²/2)·(C_умн + C_слож + C_срав) + n·C_дост — по одному умножению, сложению и " +
        "сравнению на каждую пару (слагаемое, внутренняя степень)";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 50 000: n²/2 ≈ 1.25·10⁹ умножений ≈ 1.5 с на максимальной точке.
        StartN = 5_000L,
        EndN = 50_000L,
        StepN = 5_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => n <= BlockLimit ? MeasurementMode.Direct : MeasurementMode.Scaled;

    /// <inheritdoc />
    public override object PrepareInput(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical);
        double[] coeffs = DataPreparationService.RandomDoubleArray((int)physical, seedOffset: 4);
        return new PolynomialInput(coeffs, X: 1.5, passes);
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var s = (PolynomialInput)input;
        double result = 0;
        for (long p = 0; p < s.Passes; p++)
        {
            result = ComputeNaive(s.Coefficients, s.X);
        }
        PreventOptimization(result);
    }

    /// <summary>
    /// Ядро алгоритма: наивное вычисление многочлена (публично — для unit-тестов).
    /// Степень вычисляется ЗАНОВО для каждого слагаемого — это даёт O(n²).
    /// </summary>
    public static double ComputeNaive(double[] coeffs, double x)
    {
        double result = 0;
        int len = coeffs.Length;
        for (int i = 0; i < len; i++)
        {
            double xk = 1.0;
            for (int j = 1; j <= i; j++)
            {
                xk *= x;
            }
            result += coeffs[i] * xk;
        }
        return result;
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical);
        double N = physical * (double)passes;
        double inner = N * (N + 1) / 2.0; // Σ i ≈ n²/2 итераций внутреннего цикла
        return new OperationCounts(
            Addition: inner,        // xk *= x (накопление) + result += ...
            Multiplication: inner,  // xk *= x + coeffs[i] * xk (≈ по одному на итерацию)
            Comparison: inner + N,
            Assignment: inner + N,
            Swap: 0,
            ArrayAccess: N + 1);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n * n;
}
