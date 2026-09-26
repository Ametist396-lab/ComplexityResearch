using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;

namespace ComplexityResearch.Services;

/// <summary>
/// Статистическая обработка результатов измерений.
/// </summary>
public static class StatisticsService
{
    /// <summary>
    /// Вычисляет статистику по серии запусков одной точки.
    /// </summary>
    public static StatisticsResult Compute(IReadOnlyList<double> timesNs, OperationCounts operations, OperationCostModel costModel)
    {
        double mean = BenchmarkTimer.Mean(timesNs);
        double theoretical = costModel.TotalTimeNs(operations);
        double absDiff = Math.Abs(mean - theoretical);
        double relDiff = theoretical > 0 ? absDiff / theoretical * 100.0 : 0;

        return new StatisticsResult
        {
            MeanNs = mean,
            MinNs = BenchmarkTimer.Min(timesNs),
            MaxNs = BenchmarkTimer.Max(timesNs),
            StdDevNs = BenchmarkTimer.StdDev(timesNs),
            TheoreticalNs = theoretical,
            AbsDiffNs = absDiff,
            RelDiffPercent = relDiff
        };
    }

    /// <summary>
    /// Формирует полный результат точки: статистика + отдельные запуски +
    /// режим измерения + конфигурация.
    /// </summary>
    public static BenchmarkResult BuildResult(
        AlgorithmBase algorithm,
        long n,
        IReadOnlyList<double> timesNs,
        int runsPerPoint,
        OperationCostModel costModel,
        bool fromCache = false)
    {
        return new BenchmarkResult
        {
            LogicalN = n,
            Mode = algorithm.ModeFor(n),
            RunsCount = runsPerPoint,
            Operations = algorithm.EstimateOperationCounts(n),
            Statistics = Compute(timesNs, algorithm.EstimateOperationCounts(n), costModel),
            RunTimesNs = timesNs.ToArray(),
            IsFromCache = fromCache
        };
    }
}
