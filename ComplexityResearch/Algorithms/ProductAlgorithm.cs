using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// №3. Произведение элементов массива — O(n).
/// </summary>
/// <remarks>
/// <para>
/// Прямое произведение 3·10^9 элементов переполнит любой целочисленный тип.
/// Для корректного и воспроизводимого бенчмарка используется МОДУЛЯРНАЯ
/// арифметика: произведение вычисляется по модулю простого числа
/// P = 2^31 − 1 с накоплением в 64-битном типе. Каждый шаг — одно умножение
/// и один остаток от деления (стоимость остатка приближённо учитывается
/// как ещё одно умножение). Сложность остаётся O(n).
/// </para>
/// <para>
/// Как и в <see cref="SumAlgorithm"/>, для больших n применяется
/// масштабированный режим с блоками по <see cref="BlockLimit"/> элементов.
/// </para>
/// </remarks>
public sealed class ProductAlgorithm : AlgorithmBase
{
    /// <summary>Максимальный физический размер массива, элементы.</summary>
    public const int BlockLimit = 50_000_000;

    /// <summary>Модуль для контролируемого произведения (простое число Мерссена 2^31 − 1).</summary>
    public const long Modulus = 2_147_483_647L;

    /// <inheritdoc />
    public override string Name => "№3. Произведение элементов — O(n)";

    /// <inheritdoc />
    public override string Description =>
        "Последовательный проход по массиву с накоплением произведения по модулю простого числа " +
        "(переполнение контролируется модульной арифметикой). Для больших n — масштабированный режим.";

    /// <inheritdoc />
    public override string Complexity => "O(n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public override string Application =>
        "Вычисление вероятностей совместных событий, хэш-функции (полиномиальные хэши), " +
        "модульная арифметика в криптографии.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = n · (2·C_умн + C_слож + C_срав + 2·C_присв + C_дост) — умножение, остаток " +
        "(≈ умножение), инкремент, сравнение, присваивания и доступ к массиву на элемент";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 10⁹: остаток от деления дорог, ~5 с на максимальной точке
        // (эмпирически; большее Nmax «зависало» бы на 15–20 с).
        StartN = 100_000_000L,
        EndN = 1_000_000_000L,
        StepN = 100_000_000L,
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
        // Значения 1..99: ноль обнулил бы произведение, единицы дают «холостой» умножитель.
        int[] data = DataPreparationService.RandomInt32Array((int)physical, seedOffset: 3, minValue: 1, maxValue: 100);
        return new StreamInput(data, passes);
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var s = (StreamInput)input;
        int[] data = s.Data;
        long passes = s.Passes;
        long product = 1;
        for (long p = 0; p < passes; p++)
        {
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                // Модульная арифметика: переполнение исключено, значения в [0, Modulus).
                product = product * data[i] % Modulus;
            }
        }
        PreventOptimization(product);
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical);
        double elements = physical * (double)passes;
        return new OperationCounts(
            Addition: elements,          // инкремент счётчика
            Multiplication: 2 * elements, // умножение + остаток (≈ умножение)
            Comparison: elements,
            Assignment: 2 * elements,
            Swap: 0,
            ArrayAccess: elements);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n;
}
