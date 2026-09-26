namespace ComplexityResearch.Models;

/// <summary>
/// Исключение: один запуск точки превысил максимально допустимое время
/// (BenchmarkConfiguration.MaxSecondsPerPoint). Эксперимент корректно
/// останавливается, частичные результаты сохраняются.
/// </summary>
public sealed class PointTimeoutException : Exception
{
    /// <summary>Размер n точки, превысившей лимит.</summary>
    public long N { get; }

    /// <summary>Фактическое время запуска, нс.</summary>
    public double ElapsedNs { get; }

    /// <summary>Лимит, секунды.</summary>
    public double LimitSeconds { get; }

    /// <summary>Создаёт исключение с понятным сообщением для пользователя.</summary>
    public PointTimeoutException(long n, double elapsedNs, double limitSeconds)
        : base($"Точка n = {n:N0} заняла {elapsedNs / 1e9:F1} с, что превышает лимит {limitSeconds:F1} с. " +
               "Уменьшите Nmax или увеличьте лимит времени точки.")
    {
        N = n;
        ElapsedNs = elapsedNs;
        LimitSeconds = limitSeconds;
    }
}
