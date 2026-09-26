using System.IO;
using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;
using ComplexityResearch.Services;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Интеграционный тест: BenchmarkService + БД + кэш + аппроксимация + MSE
/// на малых конфигурациях (без «тяжёлых» вычислений).
/// </summary>
public class BenchmarkIntegrationTests : IDisposable
{
    private readonly string _dbPath;

    public BenchmarkIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_integration_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private static BenchmarkConfiguration SmallConfig() => new()
    {
        StartN = 1000,
        EndN = 5000,
        StepN = 1000,
        RunsPerPoint = 3,
        MaxSecondsPerPoint = 10
    };

    [Fact]
    public async Task Full_Run_Produces_Results_And_Persists_To_Db()
    {
        using var db = new BenchmarkDatabase(_dbPath);
        var service = new BenchmarkService();
        var algo = new BubbleSortAlgorithm();
        var cfg = SmallConfig();

        var outcome = await service.RunAsync(algo, cfg, OperationCostModel.CreateDefault(),
            progress: null, CancellationToken.None, database: db, useCache: true);

        Assert.Equal(5, outcome.Results.Count); // 1000..5000 с шагом 1000
        Assert.All(outcome.Results, r =>
        {
            Assert.True(r.Statistics.MeanNs > 0);
            Assert.Equal(cfg.RunsPerPoint, r.RunTimesNs.Count);
            Assert.False(r.IsFromCache);
            Assert.True(r.Operations.Total > 0);
        });

        // В БД: 5 точек × 3 запуска = 15 измерений, 1 эксперимент.
        Assert.Equal(1, db.CountExperiments());
        Assert.Equal(15, db.CountMeasurements());

        // Повторный запуск с кэшем — все точки из кэша, новых измерений нет.
        var outcome2 = await service.RunAsync(algo, cfg, OperationCostModel.CreateDefault(),
            progress: null, CancellationToken.None, database: db, useCache: true);
        Assert.All(outcome2.Results, r => Assert.True(r.IsFromCache));
        Assert.Equal(15, db.CountMeasurements()); // не выросло

        // Force recalculation: кэш ключа удалён, точки измерены заново.
        var outcome3 = await service.RunAsync(algo, cfg, OperationCostModel.CreateDefault(),
            progress: null, CancellationToken.None, database: db, useCache: true, forceRecalculation: true);
        Assert.All(outcome3.Results, r => Assert.False(r.IsFromCache));
        // Старые записи удалены: 1 эксперимент (новый) и 15 измерений, без смешивания.
        Assert.Equal(1, db.CountExperiments());
        Assert.Equal(15, db.CountMeasurements());
    }

    [Fact]
    public async Task Approximation_And_Mse_Work_On_Real_Run()
    {
        using var db = new BenchmarkDatabase(_dbPath);
        var service = new BenchmarkService();
        var algo = new SumAlgorithm();
        // n достаточно крупные, чтобы время было измеримо (микросекунды — шум).
        var cfg = new BenchmarkConfiguration
        {
            StartN = 200_000,
            EndN = 1_000_000,
            StepN = 200_000,
            RunsPerPoint = 3,
            MaxSecondsPerPoint = 10
        };

        var outcome = await service.RunAsync(algo, cfg, OperationCostModel.CreateDefault(),
            progress: null, CancellationToken.None, database: db);

        var fit = ApproximationService.Fit(
            outcome.Results.Select(r => ((double)r.LogicalN, r.Statistics.MeanNs)).ToList(),
            algo.ComplexityClass);

        Assert.True(fit.C > 0, "коэффициент C должен быть положительным");
        Assert.True(fit.R2 > 0.8, $"линейный алгоритм должен хорошо приближаться C·n (R² = {fit.R2})");
        Assert.True(fit.MSE >= 0);
        Assert.Contains("n", fit.Formula);
    }

    [Fact]
    public async Task Cancellation_Stops_Run_And_Keeps_Partial_Results()
    {
        using var db = new BenchmarkDatabase(_dbPath);
        var service = new BenchmarkService();
        var algo = new BubbleSortAlgorithm();
        var cfg = SmallConfig();

        using var cts = new CancellationTokenSource();
        var partial = new List<BenchmarkResult>();
        var task = service.RunAsync(algo, cfg, OperationCostModel.CreateDefault(),
            progress: null, cts.Token,
            onPointCompleted: r => partial.Add(r),
            database: db);

        // Отменяем после первой же точки.
        var first = await WaitForFirstPoint(partial, task);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(first >= 1, "должна быть завершена хотя бы одна точка до отмены");
    }

    private static async Task<int> WaitForFirstPoint(List<BenchmarkResult> partial, Task task)
    {
        // Ждём до 10 секунд появления первой точки, иначе — отмена теста.
        for (int i = 0; i < 100 && partial.Count == 0 && !task.IsCompleted; i++)
        {
            await Task.Delay(100);
        }
        return partial.Count;
    }

    [Fact]
    public async Task Operation_Benchmark_Measures_Operations_Not_Time()
    {
        using var db = new BenchmarkDatabase(_dbPath);
        var service = new OperationBenchmarkService();
        var cfg = new BenchmarkConfiguration { StartN = 1, EndN = 1000, StepN = 250, RunsPerPoint = 1 };

        var results = await service.RunAsync(cfg, progress: null, CancellationToken.None, database: db);

        Assert.Equal(3, results.Count); // итеративный, рекурсивный, бинарный
        var iterative = results.Single(r => r.Algorithm is PowerIterativeAlgorithm);
        var binary = results.Single(r => r.Algorithm is PowerBinaryAlgorithm);

        // Измеряются операции: последняя точка n=1000.
        var lastIter = iterative.Points[^1];
        var lastBin = binary.Points[^1];
        Assert.Equal(1000, lastIter.N);
        Assert.InRange(lastIter.MeasuredOperations, 4 * 1000, 8 * 1000);
        Assert.True(lastBin.MeasuredOperations < lastIter.MeasuredOperations / 3,
            "бинарный алгоритм должен давать на порядок меньше операций");
        Assert.True(iterative.Approximation.MSE >= 0);
        Assert.True(binary.Approximation.C > 0);
    }
}
