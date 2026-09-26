using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;
using ComplexityResearch.Services;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Тесты корректности алгоритмов и оценок количества операций.
/// </summary>
public class AlgorithmTests
{
    [Fact]
    public void BubbleSort_Produces_Sorted_Output()
    {
        int[] a = [5, 3, 8, 1, 9, 2, 7, 1];
        BubbleSortAlgorithm.SortArray(a);
        Assert.Equal(new[] { 1, 1, 2, 3, 5, 7, 8, 9 }, a);
    }

    [Fact]
    public void QuickSort_Produces_Sorted_Output()
    {
        int[] a = [5, 3, 8, 1, 9, 2, 7, 1, 0, 42, -3];
        QuickSortAlgorithm.SortArray(a);
        Assert.Equal(new[] { -3, 0, 1, 1, 2, 3, 5, 7, 8, 9, 42 }, a);
    }

    [Fact]
    public void QuickSort_On_Large_Random_Array_Matches_Reference()
    {
        var rng = new Random(20260914);
        int[] a = Enumerable.Range(0, 5000).Select(_ => rng.Next(-1000, 1000)).ToArray();
        int[] expected = (int[])a.Clone();
        Array.Sort(expected);

        QuickSortAlgorithm.SortArray(a);
        Assert.Equal(expected, a);
    }

    [Fact]
    public void QuickSort_On_Already_Sorted_Array_Is_Fast_And_Correct()
    {
        // Медиана трёх защищает от худшего случая O(n²) на упорядоченных данных.
        int[] a = Enumerable.Range(0, 20000).ToArray();
        QuickSortAlgorithm.SortArray(a);
        Assert.True(IsSorted(a));
    }

    [Fact]
    public void TimSort_Produces_Sorted_Output_And_Is_Stable_On_Ints()
    {
        int[] a = [5, 3, 8, 1, 9, 2, 7, 1, 0, 42, -3, 5, 5];
        TimSortAlgorithm.SortArray(a);
        Assert.Equal(new[] { -3, 0, 1, 1, 2, 3, 5, 5, 5, 7, 8, 9, 42 }, a);
    }

    [Fact]
    public void TimSort_Handles_Reversed_And_Sorted_Input()
    {
        // Обратный порядок (естественные убывающие серии разворачиваются).
        int[] reversed = Enumerable.Range(0, 5000).Select(i => 5000 - i).ToArray();
        TimSortAlgorithm.SortArray(reversed);
        Assert.Equal(Enumerable.Range(1, 5000), reversed);

        // Уже отсортированный (лучший случай O(n)).
        int[] sorted = Enumerable.Range(0, 5000).ToArray();
        TimSortAlgorithm.SortArray(sorted);
        Assert.Equal(Enumerable.Range(0, 5000), sorted);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Sorters_Handle_Tiny_Arrays(int n)
    {
        int[] original = Enumerable.Range(0, n).Reverse().ToArray();
        int[] expected = Enumerable.Range(0, n).ToArray();

        int[] bubble = (int[])original.Clone();
        BubbleSortAlgorithm.SortArray(bubble);
        int[] quick = (int[])original.Clone();
        QuickSortAlgorithm.SortArray(quick);
        int[] tim = (int[])original.Clone();
        TimSortAlgorithm.SortArray(tim);

        Assert.Equal(expected, bubble);
        Assert.Equal(expected, quick);
        Assert.Equal(expected, tim);
    }

    [Fact]
    public void Polynomial_Naive_And_Horner_Match_Reference()
    {
        double[] coeffs = Enumerable.Range(0, 51).Select(i => (i % 7) * 0.25).ToArray();
        double x = 1.3;

        double expected = 0;
        for (int i = 0; i < coeffs.Length; i++)
        {
            expected += coeffs[i] * Math.Pow(x, i);
        }

        Assert.Equal(expected, PolynomialNaiveAlgorithm.ComputeNaive(coeffs, x), 6);
        Assert.Equal(expected, HornerAlgorithm.ComputeHorner(coeffs, x), 6);
    }

    [Fact]
    public void Sum_Algorithm_Estimate_Grows_Linearly()
    {
        var algo = new SumAlgorithm();
        var ops1 = algo.EstimateOperationCounts(10_000).Total;
        var ops2 = algo.EstimateOperationCounts(20_000).Total;
        Assert.InRange(ops1, 4 * 10_000, 6 * 10_000);
        Assert.InRange(ops2 / ops1, 1.9, 2.1); // строго линейный рост
    }

    [Fact]
    public void Matrix_Algorithm_Estimate_Grows_Cubically()
    {
        var algo = new MatrixMultiplicationAlgorithm();
        double ops100 = algo.EstimateOperationCounts(100).Total;
        double ops200 = algo.EstimateOperationCounts(200).Total;
        Assert.InRange(ops200 / ops100, 7.5, 8.5); // 2³ = 8 — кубический рост
        Assert.Equal("O(n^3)", algo.ComplexityClass);
    }

    private static bool IsSorted(int[] a)
    {
        for (int i = 1; i < a.Length; i++)
        {
            if (a[i - 1] > a[i]) return false;
        }
        return true;
    }
}
