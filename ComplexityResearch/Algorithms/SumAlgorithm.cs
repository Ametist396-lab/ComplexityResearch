using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Входные данные линейных «потоковых» алгоритмов (сумма, произведение, Горнер).
/// </summary>
/// <param name="Data">Физический массив данных.</param>
/// <param name="Passes">Количество проходов по массиву. В прямом режиме = 1.
/// В масштабированном режиме проходов = ceil(n / размер_массива), что даёт
/// количество обработанных элементов, пропорциональное логическому n.</param>
public sealed record StreamInput(int[] Data, long Passes);

/// <summary>
/// №2. Сумма элементов массива — O(n).
/// </summary>
/// <remarks>
/// <para>
/// Для n ≤ 50 000 000 — прямой режим: выделяется реальный массив размера n.
/// Для больших n — масштабированный режим: выделяется блок фиксированного
/// размера (<see cref="BlockLimit"/>) и выполняется ceil(n / блок) полных
/// проходов по нему. Суммарное количество обработанных элементов пропорционально
/// логическому n, поэтому измеренное время честно отражает рост O(n),
/// но массив размером n физически не выделяется.
/// </para>
/// <para>
/// Комментарий по GC: внутри измеряемого участка алгоритм не создаёт объектов
/// (только локальные переменные на стеке), поэтому сборщик мусора не влияет
/// на измерение.
/// </para>
/// </remarks>
public sealed class SumAlgorithm : AlgorithmBase
{
    /// <summary>Максимальный физический размер массива, элементы.</summary>
    public const int BlockLimit = 50_000_000;

    /// <inheritdoc />
    public override string Name => "№2. Сумма элементов — O(n)";

    /// <inheritdoc />
    public override string Description =>
        "Последовательный проход по массиву с накоплением суммы. Время линейно зависит от n. " +
        "Для n > 50 млн используется масштабированный режим: блок 50 млн элементов обрабатывается " +
        "многократно, число проходов пропорционально n.";

    /// <inheritdoc />
    public override string Complexity => "O(n)";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public override string Application =>
        "Агрегация данных (средние, итоги), предварительные проходы перед сортировкой, " +
        "подсчёт контрольных сумм.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) = n · (2·C_слож + C_срав + C_присв + C_дост) — по 2 сложения (сумма и счётчик), " +
        "1 сравнение, 1 присваивание и 1 доступ к массиву на элемент";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 3·10⁹ элементов: ~2 с на максимальной точке (эмпирически,
        // пропускная способность памяти ~12 ГБ/с).
        StartN = 100_000_000L,
        EndN = 3_000_000_000L,
        StepN = 300_000_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => n <= BlockLimit ? MeasurementMode.Direct : MeasurementMode.Scaled;

    /// <inheritdoc />
    public override object PrepareInput(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical); // ceil(n / physical)
        // Генерация данных выполняется здесь, ДО запуска таймера — в измеряемое
        // время она не попадает.
        int[] data = DataPreparationService.RandomInt32Array((int)physical, seedOffset: 2);
        return new StreamInput(data, passes);
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var s = (StreamInput)input;
        int[] data = s.Data;
        long passes = s.Passes;
        long sum = 0;
        for (long p = 0; p < passes; p++)
        {
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                sum += data[i];
            }
        }
        PreventOptimization(sum);
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        long physical = Math.Min(n, BlockLimit);
        long passes = Math.Max(1, (n + physical - 1) / physical);
        double elements = physical * (double)passes; // ≈ n обработанных элементов
        return new OperationCounts(
            Addition: 2 * elements,
            Multiplication: 0,
            Comparison: elements,
            Assignment: elements,
            Swap: 0,
            ArrayAccess: elements);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n;
}
