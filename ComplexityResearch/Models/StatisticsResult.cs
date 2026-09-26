namespace ComplexityResearch.Models;

/// <summary>
/// Результат статистической обработки серии запусков для одного значения n.
/// </summary>
public sealed class StatisticsResult
{
    /// <summary>Среднее физическое время, нс.</summary>
    public double MeanNs { get; init; }

    /// <summary>Минимальное физическое время, нс (лучшая оценка — меньше помех).</summary>
    public double MinNs { get; init; }

    /// <summary>Максимальное физическое время, нс.</summary>
    public double MaxNs { get; init; }

    /// <summary>Стандартное отклонение времени, нс.</summary>
    public double StdDevNs { get; init; }

    /// <summary>Теоретическое время (по модели стоимости операций), нс.</summary>
    public double TheoreticalNs { get; init; }

    /// <summary>Абсолютная разница |physical − theoretical|, нс.</summary>
    public double AbsDiffNs { get; init; }

    /// <summary>Относительная разница в процентах: |physical − theoretical| / theoretical × 100.</summary>
    public double RelDiffPercent { get; init; }
}
