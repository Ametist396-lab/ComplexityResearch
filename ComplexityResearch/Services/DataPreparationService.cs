namespace ComplexityResearch.Services;

/// <summary>
/// Генерация входных данных для алгоритмов.
/// </summary>
/// <remarks>
/// <para>
/// Ключевые требования к генерации:
/// 1) воспроизводимость — фиксированный seed гарантирует одинаковые данные
///    при повторном эксперименте с теми же настройками;
/// 2) генерация выполняется ДО запуска таймера: в измеряемое время алгоритма
///    подготовка данных не входит.
/// </para>
/// <para>
/// Комментарий по GC: массивы создаются один раз на точку n и вне измеряемого
/// кода; сами алгоритмы в измеряемом участке не аллоцируют память, поэтому
/// сборщик мусора не искажает измерения.
/// </para>
/// </remarks>
public static class DataPreparationService
{
    /// <summary>
    /// Создаёт генератор случайных чисел с фиксированным seed.
    /// </summary>
    public static Random CreateRandom(int seed) => new(seed);

    /// <summary>
    /// Генерирует массив случайных int в диапазоне [minValue, maxValue).
    /// </summary>
    /// <param name="n">Размер массива.</param>
    /// <param name="seedOffset">
    /// Смещение seed, уникальное для каждого алгоритма, чтобы данные разных
    /// алгоритмов отличались, оставаясь воспроизводимыми.
    /// </param>
    /// <param name="minValue">Нижняя граница значений (включительно).</param>
    /// <param name="maxValue">Верхняя граница значений (не включается).</param>
    public static int[] RandomInt32Array(int n, int seedOffset, int minValue = 0, int maxValue = 1_000_000)
    {
        var rng = CreateRandom(20260914 + seedOffset);
        var array = new int[n];
        for (int i = 0; i < n; i++)
        {
            array[i] = rng.Next(minValue, maxValue);
        }
        return array;
    }

    /// <summary>Генерирует массив случайных double в диапазоне [min, max).</summary>
    public static double[] RandomDoubleArray(int n, int seedOffset, double min = 0.0, double max = 1.0)
    {
        var rng = CreateRandom(20260914 + seedOffset);
        var array = new double[n];
        for (int i = 0; i < n; i++)
        {
            array[i] = min + rng.NextDouble() * (max - min);
        }
        return array;
    }

    /// <summary>Генерирует «почти отсортированный» массив (немного случайных перестановок).</summary>
    public static int[] NearlySortedArray(int n, int seedOffset)
    {
        var rng = CreateRandom(20260914 + seedOffset);
        var array = new int[n];
        for (int i = 0; i < n; i++)
        {
            array[i] = i;
        }
        for (int k = 0; k < n / 100; k++)
        {
            int i = rng.Next(n), j = rng.Next(n);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return array;
    }
}
