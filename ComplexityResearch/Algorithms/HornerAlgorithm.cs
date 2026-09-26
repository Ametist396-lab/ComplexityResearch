using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// №4b. Вычисление многочлена по схеме Горнера — O(n).
/// </summary>
/// <remarks>
/// <para>
/// P(x) = (...((an·x + a(n−1))·x + a(n−2))·x ... + a0).
/// Вложенные скобки позволяют вычислить многочлен за один проход:
/// на каждый коэффициент — ровно одно умножение и одно сложение.
/// </para>
/// <para>
/// Диапазон n — до 1,5·10⁹ коэффициентов, поэтому используется
/// масштабированный режим с блоками по <see cref="BlockLimit"/> элементов:
/// схема Горнера выполняется над одним и тем же физическим блоком
/// многократно; число проходов пропорционально логическому n.
/// Режим отображается в таблице результатов и отчёте.
/// </para>
/// </remarks>
public sealed class HornerAlgorithm : AlgorithmBase
{
    /// <summary>Максимальный физический размер массива коэффициентов.</summary>
    public const int BlockLimit = 50_000_000;

    /// <inheritdoc />
    public override string Name => "№4b. Метод Горнера — O(n)";

    /// <inheritdoc />
    public override string Description =>
        "P(x) = (...((an·x + a(n−1))·x + ...) + a0). Один проход по коэффициентам: " +
        "одно умножение и одно сложение на коэффициент. Для сравнения с наивным способом O(n²). " +
        "Для больших n — масштабированный режим (блок 50 млн коэффициентов, проходы ∝ n).";

    /// <inheritdoc />
    public override string Complexity => "O(n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public override string Application =>
        "Быстрое вычисление многочленов, полиномиальные хэш-функции (RLP, rolling hash), " +
        "перевод чисел между системами счисления, FFT.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = n · (C_умн + C_слож + C_срав + C_присв + C_дост) — одно умножение, сложение, " +
        "сравнение, присваивание и доступ к массиву на коэффициент";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 1.5·10⁹ коэффициентов ≈ 6 с на максимальной точке (эмпирически).
        StartN = 100_000_000L,
        EndN = 1_500_000_000L,
        StepN = 150_000_000L,
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
        double[] coeffs = DataPreparationService.RandomDoubleArray((int)physical, seedOffset: 5);
        return new PolynomialInput(coeffs, X: 1.5, passes);
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var s = (PolynomialInput)input;
        double result = 0;
        for (long p = 0; p < s.Passes; p++)
        {
            result = ComputeHorner(s.Coefficients, s.X);
        }
        PreventOptimization(result);
    }

    /// <summary>
    /// Ядро алгоритма: схема Горнера (публично — для unit-тестов).
    /// Один проход от старшего коэффициента к младшему.
    /// </summary>
    public static double ComputeHorner(double[] coeffs, double x)
    {
        double result = coeffs[coeffs.Length - 1];
        for (int i = coeffs.Length - 2; i >= 0; i--)
        {
            result = result * x + coeffs[i];
        }
        return result;
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical);
        double elements = physical * (double)passes;
        return new OperationCounts(
            Addition: elements,
            Multiplication: elements,
            Comparison: elements,
            Assignment: elements,
            Swap: 0,
            ArrayAccess: elements + 1);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n;
}
