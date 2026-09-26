using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Входные данные сортировок: копия несортированного массива на каждый запуск.
/// </summary>
/// <param name="Data">Массив для сортировки.</param>
public sealed record SortInput(int[] Data);

/// <summary>
/// Входные данные сортировки с временным буфером (TimSort).
/// </summary>
/// <param name="Data">Массив для сортировки.</param>
/// <param name="Buffer">Вспомогательный буфер слияния (размер n/2).</param>
/// <param name="RunStart">Массив начал серий в стеке серий TimSort.</param>
/// <param name="RunLength">Массив длин серий в стеке серий TimSort.</param>
public sealed record TimSortInput(int[] Data, int[] Buffer, int[] RunStart, int[] RunLength);

/// <summary>
/// №6. Bubble Sort (сортировка пузырьком) — O(n²).
/// </summary>
/// <remarks>
/// <para>
/// Классическая сортировка обменами: последовательные проходы по массиву,
/// соседние элементы сравниваются и при нарушении порядка меняются местами.
/// Оптимизация «ранний выход» (флаг swapped) даёт лучший случай O(n)
/// на уже отсортированных данных; на случайных данных средний и худший
/// случаи — O(n²).
/// </para>
/// <para>
/// Для воспроизводимости эксперимента вход — массив случайных чисел,
/// сгенерированный с фиксированным seed. Каждый запуск получает свежую копию
/// исходного несортированного массива (<see cref="PrepareRunInput"/>);
/// копирование выполняется ВНЕ таймера. Сортировка «на месте» — внутри
/// измеряемого кода нет аллокаций, поэтому GC не влияет на измерение.
/// </para>
/// </remarks>
public sealed class BubbleSortAlgorithm : AlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№6. Bubble Sort — O(n²)";

    /// <inheritdoc />
    public override string Description =>
        "Сортировка обменами: проходы по массиву, сравнение и обмен соседних элементов. " +
        "С флагом раннего выхода лучший случай O(n), средний и худший — O(n²). " +
        "Вход: случайные данные (фиксированный seed), каждая точка воспроизводима.";

    /// <inheritdoc />
    public override string Complexity => "O(n²) (лучший случай O(n))";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n^2)";

    /// <inheritdoc />
    public override string Application =>
        "Учебные задачи, почти отсортированные данные малых размеров, проверка корректности " +
        "более сложных сортировок.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) ≈ (n(n−1)/2)·C_срав + (n(n−1)/4)·C_обмен + ... — средний случай на случайных данных: " +
        "все n(n−1)/2 сравнений и в среднем половина из них (число инверсий) — обмены";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 50 000: n²/2 ≈ 1.25·10⁹ сравнений ≈ 2–3 с на максимальной точке.
        StartN = 5_000L,
        EndN = 50_000L,
        StepN = 5_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => MeasurementMode.Direct;

    /// <inheritdoc />
    public override object PrepareInput(long n) =>
        new SortInput(DataPreparationService.RandomInt32Array((int)n, seedOffset: 6));

    /// <summary>
    /// Каждый запуск получает свежую копию исходного массива: после первого
    /// запуска массив уже отсортирован, и повторная сортировка заняла бы O(n)
    /// вместо O(n²). Копирование происходит вне измеряемого участка.
    /// </summary>
    public override object PrepareRunInput(object sharedInput)
    {
        int[] original = ((SortInput)sharedInput).Data;
        return new SortInput((int[])original.Clone());
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        int[] a = ((SortInput)input).Data;
        SortArray(a);
        PreventOptimization(a.Length > 0 ? a[0] : 0);
    }

    /// <summary>Ядро алгоритма: пузырьковая сортировка (публично — для unit-тестов).</summary>
    public static void SortArray(int[] a)
    {
        int n = a.Length;
        for (int end = n - 1; end > 0; end--)
        {
            bool swapped = false;
            for (int j = 0; j < end; j++)
            {
                if (a[j] > a[j + 1])
                {
                    (a[j], a[j + 1]) = (a[j + 1], a[j]);
                    swapped = true;
                }
            }
            // Оптимизация раннего выхода: если за проход не было ни одного
            // обмена, массив уже отсортирован — лучший случай O(n).
            if (!swapped)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        // Оценки для СРЕДНЕГО случая (случайные данные):
        // сравнений — всегда n(n−1)/2; обменов — в среднем число инверсий n(n−1)/4.
        double comparisons = n * (double)(n - 1) / 2.0;
        double swaps = comparisons / 2.0;
        return new OperationCounts(
            Addition: comparisons,          // инкремент j
            Multiplication: 0,
            Comparison: comparisons + n,    // a[j] > a[j+1] + проверка swapped
            Assignment: 3 * swaps,          // temp-обмен: 3 присваивания
            Swap: swaps,
            ArrayAccess: 2 * comparisons + 4 * swaps);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n * n;
}
