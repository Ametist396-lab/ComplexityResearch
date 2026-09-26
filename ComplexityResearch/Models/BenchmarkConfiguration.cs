namespace ComplexityResearch.Models;

/// <summary>
/// Централизованная конфигурация бенчмарка.
/// </summary>
/// <remarks>
/// <para>
/// Точки n генерируются линейно: StartN, StartN + StepN, StartN + 2·StepN, …,
/// причём Nmax (EndN) всегда включается последней точкой.
/// </para>
/// <para>
/// Nmax для каждого алгоритма подобран ЭМПИРИЧЕСКИ так, чтобы эксперимент
/// не «зависал»: время на максимальной точке ≈ 5–10 секунд (см.
/// Documentation/BenchmarkMethodology.md). Дополнительно пользователь может
/// задать MaxSecondsPerPoint — предельное время одной точки, при превышении
/// которого эксперимент корректно останавливается.
/// </para>
/// </remarks>
public sealed class BenchmarkConfiguration
{
    /// <summary>Начальное значение n (первая точка).</summary>
    public long StartN { get; set; } = 1;

    /// <summary>Максимальный размер входных данных (Nmax, последняя точка).</summary>
    public long EndN { get; set; } = 1000;

    /// <summary>Шаг между соседними точками n.</summary>
    public long StepN { get; set; } = 100;

    /// <summary>
    /// Количество учитываемых запусков в каждой точке. По умолчанию 5.
    /// Первый запуск каждой точки — прогревочный и в статистику не входит.
    /// </summary>
    public int RunsPerPoint { get; set; } = 5;

    /// <summary>
    /// Seed генератора случайных чисел. Фиксированный seed гарантирует
    /// повторяемость эксперимента.
    /// </summary>
    public int RandomSeed { get; set; } = 20260914;

    /// <summary>
    /// Максимально допустимое время одной точки, секунды. Если один запуск
    /// превышает лимит, эксперимент останавливается с понятным сообщением
    /// (защита от «зависания» при слишком большом Nmax). 0 — без ограничения.
    /// </summary>
    public double MaxSecondsPerPoint { get; set; } = 15.0;

    /// <summary>
    /// Максимальный размер физического массива (в элементах), который выделяется
    /// в прямом режиме. Для n больше этого предела линейные алгоритмы переходят
    /// в масштабированный режим. 50 000 000 элементов int ≈ 200 МБ памяти.
    /// </summary>
    public long PhysicalBlockLimit { get; set; } = 50_000_000;

    /// <summary>Создаёт глубокую копию конфигурации.</summary>
    public BenchmarkConfiguration Clone() => new()
    {
        StartN = StartN,
        EndN = EndN,
        StepN = StepN,
        RunsPerPoint = RunsPerPoint,
        RandomSeed = RandomSeed,
        MaxSecondsPerPoint = MaxSecondsPerPoint,
        PhysicalBlockLimit = PhysicalBlockLimit
    };
}
