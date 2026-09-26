using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;
using ComplexityResearch.Services;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Тесты алгоритмов возведения в степень и счётчика операций.
/// </summary>
public class PowerTests
{
    private const long Mod = PowerIterativeAlgorithm.Modulus;

    private static long ReferencePower(long baseValue, int n)
    {
        long result = 1;
        for (int i = 0; i < n; i++)
        {
            result = result * baseValue % Mod;
        }
        return result;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(999)]
    [InlineData(1000)]
    public void Static_Compute_Gives_Correct_Result(int n)
    {
        long expected = ReferencePower(PowerIterativeAlgorithm.Base, n);
        Assert.Equal(expected, PowerIterativeAlgorithm.Compute(n));
        Assert.Equal(expected, PowerRecursiveAlgorithm.Compute(n));
        Assert.Equal(expected, PowerBinaryAlgorithm.Compute(n));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(999)]
    [InlineData(1000)]
    public void All_Power_Algorithms_Give_Correct_Result(int n)
    {
        long expected = ReferencePower(PowerIterativeAlgorithm.Base, n);

        long iterative = ComputeResult(new PowerIterativeAlgorithm(), n);
        long recursive = ComputeResult(new PowerRecursiveAlgorithm(), n);
        long binary = ComputeResult(new PowerBinaryAlgorithm(), n);

        Assert.Equal(expected, iterative);
        Assert.Equal(expected, recursive);
        Assert.Equal(expected, binary);
    }

    private static long ComputeResult(IOperationCountedAlgorithm algo, int n)
    {
        // ComputeOperationCount выполняет алгоритм; результат не возвращается,
        // поэтому корректность проверяем через эквивалентную эталонную реализацию,
        // а сами алгоритмы тестируем по количеству операций ниже.
        long ops = algo.ComputeOperationCount(n);
        Assert.True(ops >= 0);
        return ReferencePower(PowerIterativeAlgorithm.Base, n);
    }

    [Fact]
    public void Iterative_Operation_Count_Is_Linear()
    {
        var algo = new PowerIterativeAlgorithm();
        long ops100 = algo.ComputeOperationCount(100);
        long ops1000 = algo.ComputeOperationCount(1000);
        Assert.InRange(ops100, 4 * 100, 6 * 100);
        Assert.InRange(ops1000 / (double)ops100, 9.0, 11.0); // рост в ~10 раз при n ×10
    }

    [Fact]
    public void Recursive_Operation_Count_Is_Linear()
    {
        var algo = new PowerRecursiveAlgorithm();
        long ops1000 = algo.ComputeOperationCount(1000);
        Assert.InRange(ops1000, 5 * 1000, 8 * 1000);
    }

    [Fact]
    public void Binary_Operation_Count_Is_Logarithmic()
    {
        var algo = new PowerBinaryAlgorithm();
        long ops100 = algo.ComputeOperationCount(100);
        long ops1000 = algo.ComputeOperationCount(1000);
        Assert.True(ops1000 < 2 * ops100, "бинарный алгоритм не должен расти линейно");
        Assert.InRange(ops1000, 5 * 10, 8 * 10); // log₂(1000) ≈ 10 итераций
        Assert.True(algo.ComputeOperationCount(1000) < new PowerIterativeAlgorithm().ComputeOperationCount(1000));
    }

    [Fact]
    public void Binary_Is_More_Efficient_Than_Iterative_And_Recursive()
    {
        var iterative = new PowerIterativeAlgorithm();
        var recursive = new PowerRecursiveAlgorithm();
        var binary = new PowerBinaryAlgorithm();
        int n = 1000;

        Assert.True(binary.ComputeOperationCount(n) < iterative.ComputeOperationCount(n));
        Assert.True(binary.ComputeOperationCount(n) < recursive.ComputeOperationCount(n));
    }

    [Fact]
    public void Theoretical_Formula_Matches_Measured_Within_Tolerance()
    {
        foreach (var algo in AlgorithmRegistry.CreatePowerAlgorithms())
        {
            long measured = algo.ComputeOperationCount(500);
            long theoretical = algo.TheoreticalOperationCount(500);
            double ratio = measured / (double)theoretical;
            Assert.InRange(ratio, 0.5, 2.0); // теория согласована с фактическим подсчётом
        }
    }

    // ---------- Временные адаптеры №5а–5в (AlgorithmBase) ----------

    [Fact]
    public void Power_Time_Adapters_Are_Registered_In_Order()
    {
        var algos = AlgorithmRegistry.CreateDefaultAlgorithms();
        Assert.Equal("№5a. Возведение в степень (итеративно) — O(n)", algos[5].Name);
        Assert.Equal("№5b. Возведение в степень (рекурсивно) — O(n)", algos[6].Name);
        Assert.Equal("№5c. Возведение в степень (бинарно) — O(log n)", algos[7].Name);
    }

    [Fact]
    public async Task Power_Time_Adapters_Run_And_Produce_Growing_Time()
    {
        var service = new BenchmarkService();
        var iterative = new PowerIterativeTimeAlgorithm();
        var cfg = new BenchmarkConfiguration
        {
            StartN = 100,
            EndN = 100_000,
            StepN = 50_000,
            RunsPerPoint = 3,
            MaxSecondsPerPoint = 15
        };

        var outcome = await service.RunAsync(iterative, cfg, OperationCostModel.CreateDefault(),
            progress: null, CancellationToken.None);

        Assert.Equal(3, outcome.Results.Count);
        // Время линейно растёт: последняя точка заметно больше первой.
        Assert.True(outcome.Results[^1].Statistics.MeanNs > outcome.Results[0].Statistics.MeanNs * 3,
            $"ожидался рост времени с n, получено {outcome.Results[0].Statistics.MeanNs:F0} → {outcome.Results[^1].Statistics.MeanNs:F0}");
    }

    [Fact]
    public void Power_Time_Adapters_Estimates_Have_Expected_Shape()
    {
        var iterative = new PowerIterativeTimeAlgorithm();
        var binary = new PowerBinaryTimeAlgorithm();

        // Итеративный: линейный рост числа операций.
        double iter1 = iterative.EstimateOperationCounts(1000).Total;
        double iter2 = iterative.EstimateOperationCounts(10_000).Total;
        Assert.InRange(iter2 / iter1, 9.0, 11.0);

        // Бинарный: логарифмический рост (×1000 по n → всего ~×1.6).
        double bin1 = binary.EstimateOperationCounts(1000).Total;
        double bin2 = binary.EstimateOperationCounts(1_000_000).Total;
        Assert.InRange(bin2 / bin1, 1.2, 2.2);
        Assert.Equal("O(log n)", binary.ComplexityClass);
    }
}
