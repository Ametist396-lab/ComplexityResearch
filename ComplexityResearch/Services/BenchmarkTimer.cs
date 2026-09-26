using System.Diagnostics;

namespace ComplexityResearch.Services;

/// <summary>
/// Высокоточный таймер бенчмарка на основе <see cref="Stopwatch"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Stopwatch"/> использует счётчик времени высокого разрешения
/// (QueryPerformanceCounter), точность — доли микросекунды.
/// <see cref="DateTime.Now"/> для бенчмаркинга непригоден: его точность ~10–16 мс
/// и он корректируется системным временем.
/// </para>
/// <para>
/// Комментарий про JIT: первый вызов метода вызывает его JIT-компиляцию
/// (~миллисекунды). Поэтому первый запуск в каждой точке — прогревочный
/// и не учитывается в статистике, а перед всей серией BenchmarkService
/// дополнительно прогревает алгоритм на малом n.
/// </para>
/// </remarks>
public static class BenchmarkTimer
{
    /// <summary>Измеряет время выполнения действия в наносекундах.</summary>
    public static double MeasureNs(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed.TotalNanoseconds;
    }

    /// <summary>
    /// Выполняет серию измерений и возвращает массив времени в наносекундах.
    /// </summary>
    /// <param name="action">Измеряемое действие.</param>
    /// <param name="runs">Количество запусков.</param>
    /// <param name="beforeRun">
    /// Необязательное действие перед каждым запуском (например, подготовка
    /// копии данных) — выполняется ВНЕ измеряемого участка.
    /// </param>
    public static double[] MeasureSeries(Action action, int runs, Action<int>? beforeRun = null)
    {
        var results = new double[runs];
        for (int i = 0; i < runs; i++)
        {
            beforeRun?.Invoke(i);
            results[i] = MeasureNs(action);
        }
        return results;
    }

    /// <summary>Среднее арифметическое.</summary>
    public static double Mean(IReadOnlyList<double> values)
    {
        double sum = 0;
        for (int i = 0; i < values.Count; i++) sum += values[i];
        return values.Count == 0 ? 0 : sum / values.Count;
    }

    /// <summary>Минимум.</summary>
    public static double Min(IReadOnlyList<double> values)
    {
        double min = double.MaxValue;
        for (int i = 0; i < values.Count; i++) min = Math.Min(min, values[i]);
        return values.Count == 0 ? 0 : min;
    }

    /// <summary>Максимум.</summary>
    public static double Max(IReadOnlyList<double> values)
    {
        double max = double.MinValue;
        for (int i = 0; i < values.Count; i++) max = Math.Max(max, values[i]);
        return values.Count == 0 ? 0 : max;
    }

    /// <summary>
    /// Стандартное отклонение (по генеральной совокупности).
    /// Показывает «шум» измерения: разброс от планировщика ОС, кэша, GC.
    /// </summary>
    public static double StdDev(IReadOnlyList<double> values)
    {
        if (values.Count <= 1) return 0;
        double mean = Mean(values);
        double sumSquares = 0;
        for (int i = 0; i < values.Count; i++)
        {
            double d = values[i] - mean;
            sumSquares += d * d;
        }
        return Math.Sqrt(sumSquares / values.Count);
    }

    /// <summary>Перевод наносекунд в микросекунды.</summary>
    public static double NsToMicroseconds(double ns) => ns / 1e3;

    /// <summary>Перевод наносекунд в миллисекунды.</summary>
    public static double NsToMilliseconds(double ns) => ns / 1e6;

    /// <summary>Перевод наносекунд в секунды.</summary>
    public static double NsToSeconds(double ns) => ns / 1e9;
}
