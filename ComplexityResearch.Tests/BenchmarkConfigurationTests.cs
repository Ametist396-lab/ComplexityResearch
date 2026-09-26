using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;
using ComplexityResearch.Services;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Тесты генерации точек n (Nmax/step), лимита времени точки и защиты от некорректных конфигураций.
/// </summary>
public class BenchmarkConfigurationTests
{
    [Fact]
    public void GenerateSizes_Linear_Progression_Includes_Nmax()
    {
        var cfg = new BenchmarkConfiguration { StartN = 10, EndN = 100, StepN = 20, RunsPerPoint = 5 };
        var sizes = BenchmarkService.GenerateSizes(cfg);

        // 10, 30, 50, 70, 90 + Nmax=100 всегда включён.
        Assert.Equal(new long[] { 10, 30, 50, 70, 90, 100 }, sizes);
    }

    [Fact]
    public void GenerateSizes_Exact_Progression_Does_Not_Duplicate_Nmax()
    {
        var cfg = new BenchmarkConfiguration { StartN = 10, EndN = 90, StepN = 20 };
        var sizes = BenchmarkService.GenerateSizes(cfg);
        Assert.Equal(new long[] { 10, 30, 50, 70, 90 }, sizes);
    }

    [Fact]
    public void GenerateSizes_Single_Point_When_Step_Reaches_Nmax()
    {
        var cfg = new BenchmarkConfiguration { StartN = 5, EndN = 7, StepN = 10 };
        var sizes = BenchmarkService.GenerateSizes(cfg);
        Assert.Equal(new long[] { 5, 7 }, sizes);
    }

    [Theory]
    [InlineData(0, 100, 10)]     // StartN = 0
    [InlineData(10, 10, 1)]      // Nmax <= StartN
    [InlineData(10, 100, 0)]     // step = 0
    [InlineData(10, 100, -5)]    // отрицательный шаг
    [InlineData(1, 10000, 1)]    // слишком много точек (> 60)
    public void Validate_Rejects_Invalid_Configurations(long startN, long endN, long step)
    {
        var cfg = new BenchmarkConfiguration { StartN = startN, EndN = endN, StepN = step };
        Assert.Throws<ArgumentException>(() => BenchmarkService.ValidateConfigurationPublic(cfg));
    }

    [Fact]
    public void Validate_Rejects_Invalid_Runs_And_Time_Limit()
    {
        Assert.Throws<ArgumentException>(() => BenchmarkService.ValidateConfigurationPublic(
            new BenchmarkConfiguration { StartN = 1, EndN = 100, StepN = 50, RunsPerPoint = 0 }));
        Assert.Throws<ArgumentException>(() => BenchmarkService.ValidateConfigurationPublic(
            new BenchmarkConfiguration { StartN = 1, EndN = 100, StepN = 50, MaxSecondsPerPoint = -1 }));
    }

    [Fact]
    public void Default_Runs_Per_Point_Is_Five()
    {
        var cfg = new BenchmarkConfiguration();
        Assert.Equal(5, cfg.RunsPerPoint);
    }

    [Fact]
    public void Point_Timeout_Exception_Has_Informative_Message()
    {
        var ex = new PointTimeoutException(n: 50_000, elapsedNs: 20e9, limitSeconds: 15);
        // Форматирование числа зависит от культуры (разделитель групп может быть
        // неразрывным пробелом), поэтому проверяем ключевые части сообщения.
        Assert.Contains("000", ex.Message);
        Assert.Contains("20,0", ex.Message);
        Assert.Contains("15", ex.Message);
        Assert.Equal(50_000, ex.N);
    }

    [Fact]
    public void Default_Configurations_Are_Valid_And_Bounded()
    {
        foreach (var algo in AlgorithmRegistry.CreateDefaultAlgorithms())
        {
            var cfg = algo.DefaultConfiguration;
            Exception? exception = Record.Exception(new Action(() => BenchmarkService.ValidateConfigurationPublic(cfg)));
            Assert.True(exception == null, $"{algo.Name}: некорректная конфигурация по умолчанию ({exception?.Message})");
            Assert.True(cfg.RunsPerPoint >= 5, $"{algo.Name}: запусков должно быть минимум 5");
        }
    }
}
