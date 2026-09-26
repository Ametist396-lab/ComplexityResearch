using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// №7. Quick Sort (быстрая сортировка) — O(n log n) в среднем.
/// </summary>
/// <remarks>
/// <para>
/// Собственная реализация (не вызов System.Array.Sort):
/// выбор опорного элемента медианой трёх (первый, средний, последний),
/// разделение по схеме Седжвика, рекурсия по меньшей части и цикл по большей
/// (глубина стека O(log n)), вставки для коротких отрезков (&lt; 16 элементов).
/// Медиана трёх защищает от худшего случая O(n²) на упорядоченных данных.
/// </para>
/// <para>
/// Худший случай O(n²) теоретически возможен (специально подобранные данные),
/// но на случайных данных с фиксированным seed не встречается.
/// </para>
/// <para>
/// Память: до 10⁸ элементов int = 400 МБ (+ копия для каждого запуска).
/// Алгоритм работает в прямом режиме; при нехватке памяти пользователь получит
/// понятное сообщение (обработка OutOfMemoryException в BenchmarkService).
/// Разделение на блоки для сортировок нечестно (n log n не декомпозируется
/// линейно), поэтому масштабированный режим для сортировок не применяется.
/// </para>
/// <para>
/// Комментарий по рекурсии: рекурсивные вызовы не создают объектов в куче,
/// кадры стека — непрерывная память, поэтому GC не влияет на измерение.
/// </para>
/// </remarks>
public sealed class QuickSortAlgorithm : AlgorithmBase
{
    /// <summary>Отсечки вставочной сортировки для коротких отрезков.</summary>
    private const int InsertionCutoff = 16;

    /// <inheritdoc />
    public override string Name => "№7. Quick Sort — O(n log n)";

    /// <inheritdoc />
    public override string Description =>
        "Быстрая сортировка: опорный элемент — медиана трёх, разделение массива на части «меньше/больше» " +
        "и рекурсивная сортировка частей. Средний случай O(n log n), худший (редкий) O(n²). " +
        "Вход: случайные данные с фиксированным seed.";

    /// <inheritdoc />
    public override string Complexity => "O(n log n) в среднем, O(n²) худший";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n log n)";

    /// <inheritdoc />
    public override string Application =>
        "Универсальная быстрая сортировка: std::sort (C++), Arrays.sort (примитивы, Java), " +
        "сортировка в базах данных и поисковых индексах.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) ≈ 1.39·n·log₂(n)·C_срав + 0.25·n·log₂(n)·C_обмен + ... — среднее число сравнений " +
        "быстрой сортировки на случайных данных ≈ 1.39·n·log₂(n)";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 10⁸ элементов (400 МБ): ~10 с на максимальной точке.
        // Верхняя граница по времени и памяти подобрана эмпирически.
        StartN = 10_000_000L,
        EndN = 100_000_000L,
        StepN = 10_000_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => MeasurementMode.Direct;

    /// <inheritdoc />
    public override object PrepareInput(long n) =>
        new SortInput(DataPreparationService.RandomInt32Array((int)n, seedOffset: 7));

    /// <summary>Свежая копия несортированного массива на каждый запуск (вне таймера).</summary>
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

    /// <summary>Ядро алгоритма: быстрая сортировка массива (публично — для unit-тестов).</summary>
    public static void SortArray(int[] a) => Sort(a, 0, a.Length - 1);

    private static void Sort(int[] a, int lo, int hi)
    {
        // Цикл вместо второго рекурсивного вызова: глубина стека O(log n).
        while (lo < hi)
        {
            if (hi - lo + 1 <= InsertionCutoff)
            {
                InsertionSort(a, lo, hi);
                return;
            }
            int p = Partition(a, lo, hi);
            if (p - lo < hi - p)
            {
                Sort(a, lo, p - 1);
                lo = p + 1;
            }
            else
            {
                Sort(a, p + 1, hi);
                hi = p - 1;
            }
        }
    }

    /// <summary>
    /// Разделение по Седжвику с медианой трёх. Возвращает индекс опорного
    /// элемента после разделения.
    /// </summary>
    private static int Partition(int[] a, int lo, int hi)
    {
        int mid = lo + (hi - lo) / 2;
        // Медиана трёх: упорядочиваем a[lo] ≤ a[mid] ≤ a[hi].
        if (a[mid] < a[lo]) Swap(a, mid, lo);
        if (a[hi] < a[lo]) Swap(a, hi, lo);
        if (a[hi] < a[mid]) Swap(a, hi, mid);
        // Опорный элемент прячем в hi−1 (длина отрезка здесь всегда > 16).
        Swap(a, mid, hi - 1);
        int pivot = a[hi - 1];

        int i = lo, j = hi - 1;
        while (true)
        {
            // Гарантированные барьеры a[lo] ≤ pivot ≤ a[hi] не дают индексам выйти за границы.
            while (a[++i] < pivot) { }
            while (a[--j] > pivot) { }
            if (i >= j) break;
            Swap(a, i, j);
        }
        Swap(a, i, hi - 1);
        return i;
    }

    private static void InsertionSort(int[] a, int lo, int hi)
    {
        for (int i = lo + 1; i <= hi; i++)
        {
            int key = a[i];
            int j = i - 1;
            while (j >= lo && a[j] > key)
            {
                a[j + 1] = a[j];
                j--;
            }
            a[j + 1] = key;
        }
    }

    private static void Swap(int[] a, int i, int j) => (a[i], a[j]) = (a[j], a[i]);

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        // Средние оценки для случайных данных (классический анализ быстрой сортировки):
        // сравнения ≈ 1.39·n·log₂n, обмены ≈ 0.25·n·log₂n.
        double nl = n * Math.Log2(Math.Max(2, n));
        double comparisons = 1.39 * nl;
        double swaps = 0.25 * nl;
        return new OperationCounts(
            Addition: nl,                    // сканы и счётчики разделения
            Multiplication: 0,
            Comparison: comparisons,
            Assignment: 3 * swaps,
            Swap: swaps,
            ArrayAccess: 2 * comparisons + 4 * swaps);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n * Math.Log2(Math.Max(2, n));
}
