using ComplexityResearch.Models;
using ComplexityResearch.Services;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// №8. TimSort (собственная упрощённая реализация) — O(n log n).
/// </summary>
/// <remarks>
/// <para>
/// Собственная реализация (не вызов System.Array.Sort), отражающая ключевые
/// идеи оригинального TimSort (Tim Peters, 2002):
/// 1) поиск естественных упорядоченных серий (ascending / строго descending с разворотом);
/// 2) удлинение коротких серий вставочной сортировкой до minrun (32..63);
/// 3) слияние серий со стеком и инвариантами баланса (упрощённые правила
///    merge_collapse из CPython; galloping-режим для простоты опущен).
/// Стабильность обеспечивается слиянием с временным буфером.
/// </para>
/// <para>
/// Временный буфер и массивы стека серий создаются в <see cref="PrepareRunInput"/>
/// ВНЕ измеряемого участка — внутри таймера алгоритм не аллоцирует память.
/// </para>
/// <para>
/// Память: до 2·10⁸ элементов int = 800 МБ (+ копия и буфер). При нехватке
/// памяти пользователь получит понятное сообщение об ошибке.
/// </para>
/// </remarks>
public sealed class TimSortAlgorithm : AlgorithmBase
{
    /// <inheritdoc />
    public override string Name => "№8. TimSort — O(n log n)";

    /// <inheritdoc />
    public override string Description =>
        "Гибрид сортировок (собственная упрощённая реализация): естественные серии, " +
        "вставочная сортировка коротких серий (minrun 32..63), слияние со стеком серий. " +
        "Стабильна, лучший случай O(n) на отсортированных данных, худший O(n log n).";

    /// <inheritdoc />
    public override string Complexity => "O(n log n) (лучший случай O(n))";

    /// <inheritdoc />
    public override string ComplexityClass => "O(n log n)";

    /// <inheritdoc />
    public override string Application =>
        "Стандартная стабильная сортировка Python (list.sort) и Java (Object[] Arrays.sort), " +
        "реальные данные с частичной упорядоченностью.";

    /// <inheritdoc />
    public override string TheoreticalFormula =>
        "T(n) ≈ n·log₂(n)·(C_срав + 2·C_присв + 3·C_дост) — на каждом из log₂(n) уровней слияний " +
        "каждый элемент сравнивается и перемещается несколько раз";

    /// <inheritdoc />
    public override BenchmarkConfiguration DefaultConfiguration => new()
    {
        // Nmax = 7·10⁷ (280 МБ): ~9 с на максимальной точке (эмпирически;
        // 2·10⁷ давали больше 20 с и «подвешивали» эксперимент).
        StartN = 10_000_000L,
        EndN = 70_000_000L,
        StepN = 10_000_000L,
        RunsPerPoint = 5,
        MaxSecondsPerPoint = 15
    };

    /// <inheritdoc />
    public override MeasurementMode ModeFor(long n) => MeasurementMode.Direct;

    /// <inheritdoc />
    public override object PrepareInput(long n) =>
        new SortInput(DataPreparationService.RandomInt32Array((int)n, seedOffset: 8));

    /// <summary>
    /// Готовит данные запуска ВНЕ таймера: свежую копию исходного массива,
    /// буфер слияния (n/2) и массивы стека серий (максимум 85 серий —
    /// теоретический предел для массивов до 2⁶⁴ элементов).
    /// </summary>
    public override object PrepareRunInput(object sharedInput)
    {
        int[] original = ((SortInput)sharedInput).Data;
        return new TimSortInput(
            (int[])original.Clone(),
            new int[original.Length / 2 + 1],
            new int[85],
            new int[85]);
    }

    /// <inheritdoc />
    protected override void ExecuteCore(object input)
    {
        var s = (TimSortInput)input;
        Sort(s.Data, s.Buffer, s.RunStart, s.RunLength);
        PreventOptimization(s.Data.Length > 0 ? s.Data[0] : 0);
    }

    /// <summary>Ядро алгоритма: TimSort (публично — для unit-тестов).</summary>
    public static void SortArray(int[] a)
    {
        var buffer = new int[a.Length / 2 + 1];
        Sort(a, buffer, new int[85], new int[85]);
    }

    /// <summary>Основной цикл TimSort.</summary>
    private static void Sort(int[] a, int[] buffer, int[] runStart, int[] runLength)
    {
        int n = a.Length;
        if (n < 2) return;

        int minrun = CalcMinRun(n);
        int stackSize = 0;
        int i = 0;

        while (i < n)
        {
            int runEnd = FindRunEnd(a, i, n - 1);
            // Короткие естественные серии удлиняем вставочной сортировкой до minrun.
            int forcedLength = Math.Min(minrun, n - i);
            if (runEnd - i + 1 < forcedLength)
            {
                runEnd = i + forcedLength - 1;
                InsertionSort(a, i, runEnd);
            }
            runStart[stackSize] = i;
            runLength[stackSize] = runEnd - i + 1;
            stackSize++;
            // MergeCollapse может слить несколько серий — возвращаем новый размер стека.
            stackSize = MergeCollapse(a, buffer, runStart, runLength, stackSize);
            i = runEnd + 1;
        }

        // Финальное слияние всех серий в одну.
        while (stackSize > 1)
        {
            MergeAt(a, buffer, runStart, runLength, stackSize, stackSize - 2);
            stackSize--;
        }
    }

    /// <summary>
    /// minrun по правилу Tim Peters: делить n, пока оно не станет &lt; 64,
    /// запоминая отброшенные биты → результат 32..63.
    /// </summary>
    private static int CalcMinRun(int n)
    {
        int r = 0;
        while (n >= 64)
        {
            r |= n & 1;
            n >>= 1;
        }
        return n + r;
    }

    /// <summary>
    /// Находит конец естественной серии, начинающейся в start:
    /// неубывающей либо строго убывающей (убывающая разворачивается).
    /// </summary>
    private static int FindRunEnd(int[] a, int start, int last)
    {
        if (start >= last) return start;
        if (a[start + 1] < a[start])
        {
            int k = start + 1;
            while (k < last && a[k + 1] < a[k]) k++;
            Reverse(a, start, k);
            return k;
        }
        int j = start + 1;
        while (j < last && a[j + 1] >= a[j]) j++;
        return j;
    }

    /// <summary>
    /// Упрощённые инварианты стека серий (по мотивам CPython merge_collapse):
    /// поддерживаем сбалансированные длины соседних серий, чтобы слияния
    /// были близки к идеальному двоичному дереву.
    /// Условия проверяются от старших индексов к младшим, чтобы не обращаться
    /// к отрицательным индексам. Возвращает новый размер стека (часть серий
    /// сливается — количество элементов стека уменьшается).
    /// </summary>
    private static int MergeCollapse(int[] a, int[] buffer, int[] runStart, int[] runLength, int stackSize)
    {
        while (stackSize > 1)
        {
            int top = stackSize - 1;
            bool mergeWithTopMinus2 = top > 2 && runLength[top - 3] <= runLength[top - 2] + runLength[top - 1];
            bool mergeWithTopMinus1 = top > 1 && runLength[top - 2] <= runLength[top - 1] + runLength[top];

            if (mergeWithTopMinus2 && (!mergeWithTopMinus1 || runLength[top - 3] < runLength[top - 1]))
            {
                MergeAt(a, buffer, runStart, runLength, stackSize, top - 2);
                stackSize--;
            }
            else if (mergeWithTopMinus1 || runLength[top - 1] <= runLength[top])
            {
                MergeAt(a, buffer, runStart, runLength, stackSize, top - 1);
                stackSize--;
            }
            else
            {
                break;
            }
        }
        return stackSize;
    }

    /// <summary>Сливает серию i со следующей за ней серией i+1.</summary>
    private static void MergeAt(int[] a, int[] buffer, int[] runStart, int[] runLength, int stackSize, int i)
    {
        int lo = runStart[i];
        int mid = runStart[i] + runLength[i] - 1;
        int hi = runStart[i] + runLength[i] + runLength[i + 1] - 1;
        Merge(a, buffer, lo, mid, hi);
        runLength[i] += runLength[i + 1];
        // Удаляем запись i+1 сдвигом (в стеке не более 85 элементов).
        for (int k = i + 1; k < stackSize - 1; k++)
        {
            runStart[k] = runStart[k + 1];
            runLength[k] = runLength[k + 1];
        }
    }

    /// <summary>
    /// Стабильное слияние двух упорядоченных отрезков.
    /// В буфер копируется МЕНЬШАЯ из частей — это гарантирует, что буфера
    /// размера n/2 + 1 всегда достаточно, при любой конфигурации серий.
    /// </summary>
    private static void Merge(int[] a, int[] buffer, int lo, int mid, int hi)
    {
        int leftLen = mid - lo + 1;
        int rightLen = hi - mid;

        if (leftLen <= rightLen)
        {
            // Копируем левую часть, сливаем слева направо.
            // «≤» берёт элемент из левой части при равенстве — это даёт стабильность.
            for (int t = 0; t < leftLen; t++)
            {
                buffer[t] = a[lo + t];
            }
            int i = 0, j = mid + 1, k = lo;
            while (i < leftLen && j <= hi)
            {
                a[k++] = buffer[i] <= a[j] ? buffer[i++] : a[j++];
            }
            while (i < leftLen)
            {
                a[k++] = buffer[i++];
            }
        }
        else
        {
            // Копируем правую часть, сливаем справа налево.
            // При равенстве первым уходит элемент правой части (в старшую позицию),
            // левый остаётся раньше — стабильность сохраняется.
            for (int t = 0; t < rightLen; t++)
            {
                buffer[t] = a[mid + 1 + t];
            }
            int i = mid, j = rightLen - 1, k = hi;
            while (j >= 0 && i >= lo)
            {
                a[k--] = a[i] > buffer[j] ? a[i--] : buffer[j--];
            }
            while (j >= 0)
            {
                a[k--] = buffer[j--];
            }
        }
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

    private static void Reverse(int[] a, int lo, int hi)
    {
        while (lo < hi)
        {
            (a[lo], a[hi]) = (a[hi], a[lo]);
            lo++;
            hi--;
        }
    }

    /// <inheritdoc />
    public override OperationCounts EstimateOperationCounts(long n)
    {
        // Средние оценки: на каждом уровне слияний каждый элемент участвует
        // в 1 сравнении и 2 перемещениях (буфер + запись), уровней ≈ log₂n.
        double nl = n * Math.Log2(Math.Max(2, n));
        return new OperationCounts(
            Addition: nl,
            Multiplication: 0,
            Comparison: nl,
            Assignment: 2 * nl,
            Swap: 0,
            ArrayAccess: 3 * nl);
    }

    /// <inheritdoc />
    public override Func<double, double> ComplexityFunction => n => n * Math.Log2(Math.Max(2, n));
}
