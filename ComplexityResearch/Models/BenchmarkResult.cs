namespace ComplexityResearch.Models;

/// <summary>
/// Полный результат измерения для одного значения n.
/// Сохраняется в БД, отображается в DataGrid и экспортируется.
/// </summary>
public sealed class BenchmarkResult
{
    /// <summary>Логический размер входных данных n.</summary>
    public long LogicalN { get; init; }

    /// <summary>Режим измерения (прямой / масштабированный).</summary>
    public MeasurementMode Mode { get; init; }

    /// <summary>Количество учитываемых запусков (без прогревочного).</summary>
    public int RunsCount { get; init; }

    /// <summary>Количество элементарных операций каждого типа.</summary>
    public OperationCounts Operations { get; init; } = OperationCounts.Zero;

    /// <summary>Статистика времени.</summary>
    public StatisticsResult Statistics { get; init; } = new();

    /// <summary>
    /// Отдельные запуски точки (нс), без прогревочного. Хранятся в БД
    /// (по строке на запуск) и экспортируются в JSON.
    /// </summary>
    public IReadOnlyList<double> RunTimesNs { get; init; } = Array.Empty<double>();

    /// <summary>Результат получен из кэша БД, а не измерен заново.</summary>
    public bool IsFromCache { get; init; }

    /// <summary>
    /// Аппроксимированное время Tapprox(n) = C·f(n) (нс); заполняется после
    /// подбора коэффициента C по всей серии.
    /// </summary>
    public double ApproxNs { get; set; }

    /// <summary>Среднее время, мс.</summary>
    public double AverageMs => Statistics.MeanNs / 1e6;

    /// <summary>Минимальное время, мс.</summary>
    public double MinMs => Statistics.MinNs / 1e6;

    /// <summary>Максимальное время, мс.</summary>
    public double MaxMs => Statistics.MaxNs / 1e6;

    /// <summary>Стандартное отклонение, мс.</summary>
    public double StdDevMs => Statistics.StdDevNs / 1e6;

    /// <summary>Теоретическое время, мс.</summary>
    public double TheoreticalMs => Statistics.TheoreticalNs / 1e6;

    /// <summary>Аппроксимация C·f(n), мс.</summary>
    public double ApproxMs => ApproxNs / 1e6;

    /// <summary>Абсолютная ошибка |physical − theoretical|, мс.</summary>
    public double AbsDiffMs => Statistics.AbsDiffNs / 1e6;

    /// <summary>Относительная ошибка, %.</summary>
    public double ErrorPercent => Statistics.RelDiffPercent;

    /// <summary>Суммарное количество элементарных операций (StepCount для БД).</summary>
    public long OperationsTotal => (long)Math.Round(Operations.Total);

    /// <summary>Режим измерения текстом (для таблицы и отчёта).</summary>
    public string ModeText => IsFromCache
        ? "Из кэша"
        : Mode == MeasurementMode.Direct ? "Прямой" : "Масштабированный";
}
