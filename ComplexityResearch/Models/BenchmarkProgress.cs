namespace ComplexityResearch.Models;

/// <summary>
/// Прогресс выполнения бенчмарка (для ProgressBar и статусной строки).
/// </summary>
public sealed record BenchmarkProgress(
    string AlgorithmName,
    long CurrentN,
    int PointIndex,
    int PointCount,
    int RunIndex,
    int RunCount,
    int PercentComplete)
{
    /// <summary>Готовый текст статуса вида «Quick Sort | n = 50 000 000 | точка 6/10 | запуск 4/10».</summary>
    public string DetailsText =>
        RunIndex == 0
            ? $"{AlgorithmName} | n = {CurrentN:N0} | точка {PointIndex + 1}/{PointCount} | прогрев"
            : $"{AlgorithmName} | n = {CurrentN:N0} | точка {PointIndex + 1}/{PointCount} | запуск {RunIndex}/{RunCount}";
}
