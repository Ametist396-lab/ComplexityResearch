using ComplexityResearch.Algorithms;
using ComplexityResearch.Models;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Тесты классического умножения матриц (O(n³)).
/// </summary>
public class MatrixTests
{
    [Fact]
    public void Multiply_By_Identity_Gives_Same_Matrix()
    {
        int n = 5;
        var a = new int[n, n];
        var identity = new int[n, n];
        var rng = new Random(7);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                a[i, j] = rng.Next(-50, 50);
            }
            identity[i, i] = 1;
        }

        var c = new int[n, n];
        MatrixMultiplicationAlgorithm.Multiply(a, identity, c);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Assert.Equal(a[i, j], c[i, j]);
            }
        }
    }

    [Fact]
    public void Multiply_Matches_Reference_Implementation()
    {
        int n = 12;
        var rng = new Random(20260914);
        var a = new int[n, n];
        var b = new int[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                a[i, j] = rng.Next(-10, 10);
                b[i, j] = rng.Next(-10, 10);
            }
        }

        var expected = ReferenceMultiply(a, b);
        var actual = new int[n, n];
        MatrixMultiplicationAlgorithm.Multiply(a, b, actual);

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Assert.Equal(expected[i, j], actual[i, j]);
            }
        }
    }

    [Fact]
    public void Multiply_Small_Known_Case()
    {
        var a = new int[2, 2] { { 1, 2 }, { 3, 4 } };
        var b = new int[2, 2] { { 5, 6 }, { 7, 8 } };
        var c = new int[2, 2];
        MatrixMultiplicationAlgorithm.Multiply(a, b, c);
        // [[1·5+2·7, 1·6+2·8], [3·5+4·7, 3·6+4·8]] = [[19, 22], [43, 50]]
        Assert.Equal(19, c[0, 0]);
        Assert.Equal(22, c[0, 1]);
        Assert.Equal(43, c[1, 0]);
        Assert.Equal(50, c[1, 1]);
    }

    private static int[,] ReferenceMultiply(int[,] a, int[,] b)
    {
        int n = a.GetLength(0);
        var c = new int[n, n];
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                int sum = 0;
                for (int k = 0; k < n; k++)
                {
                    sum += a[i, k] * b[k, j];
                }
                c[i, j] = sum;
            }
        }
        return c;
    }
}
