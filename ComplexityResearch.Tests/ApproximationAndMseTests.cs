using ComplexityResearch.Services;
using Xunit;

namespace ComplexityResearch.Tests;

/// <summary>
/// Тесты аппроксимации Tapprox(n) = C·f(n) и MSE.
/// </summary>
public class ApproximationAndMseTests
{
    [Fact]
    public void Fit_Linear_Data_Recovers_Coefficient_Exactly()
    {
        // t = 3·n (без шума) → C = 3, MSE = 0.
        var points = Enumerable.Range(1, 20).Select(i => ((double)i * 10, 3.0 * i * 10)).ToList();
        var fit = ApproximationService.Fit(points, "O(n)");

        Assert.Equal(3.0, fit.C, 6);
        Assert.Equal(0.0, fit.MSE, 6);
        Assert.Equal(1.0, fit.R2, 6);
        Assert.Contains("n", fit.Formula);
    }

    [Fact]
    public void Fit_Quadratic_Data_Recovers_Coefficient()
    {
        // t = 2·n² (без шума) → C = 2, MSE = 0.
        var points = Enumerable.Range(1, 15).Select(i => ((double)i * 5, 2.0 * i * 5 * i * 5)).ToList();
        var fit = ApproximationService.Fit(points, "O(n^2)");

        Assert.Equal(2.0, fit.C, 6);
        Assert.Equal(0.0, fit.MSE, 6);
    }

    [Fact]
    public void Fit_Logarithmic_Data_Recovers_Coefficient()
    {
        // t = 5·log₂(n).
        var points = new List<(double, double)>();
        for (int n = 2; n <= 1024; n *= 2)
        {
            points.Add((n, 5.0 * Math.Log2(n)));
        }
        var fit = ApproximationService.Fit(points, "O(log n)");

        Assert.Equal(5.0, fit.C, 4);
        Assert.Equal(0.0, fit.MSE, 6);
    }

    [Fact]
    public void Fit_Cubic_Data_Recovers_Coefficient()
    {
        var points = Enumerable.Range(1, 10).Select(i => ((double)i * 10, 0.5 * i * 10 * i * 10 * i * 10)).ToList();
        var fit = ApproximationService.Fit(points, "O(n^3)");
        Assert.Equal(0.5, fit.C, 6);
    }

    [Fact]
    public void Fit_Noisy_Data_Produces_Nonzero_Mse()
    {
        var rng = new Random(42);
        var points = new List<(double, double)>();
        for (int i = 1; i <= 20; i++)
        {
            double noise = 1 + (rng.NextDouble() - 0.5) * 0.2; // ±10%
            points.Add((i * 10, 3.0 * i * 10 * noise));
        }
        var fit = ApproximationService.Fit(points, "O(n)");

        Assert.InRange(fit.C, 2.7, 3.3);   // коэффициент близок к истинному
        Assert.True(fit.MSE > 0, "на зашумлённых данных MSE должен быть > 0");
        Assert.True(fit.R2 > 0.99);
    }

    [Fact]
    public void Mse_Formula_Matches_Manual_Calculation()
    {
        // Ручной расчёт MSE для известных точек и кривой C·n.
        var points = new List<(double N, double T)>
        {
            (1, 3), (2, 5), (3, 10)   // t = {3, 5, 10}
        };
        var fit = ApproximationService.Fit(points, "O(n)");

        // C = Σ(n·t)/Σ(n²) = (3+10+30)/(1+4+9) = 43/14
        double c = 43.0 / 14.0;
        double expectedMse = (Math.Pow(3 - c * 1, 2) + Math.Pow(5 - c * 2, 2) + Math.Pow(10 - c * 3, 2)) / 3.0;

        Assert.Equal(c, fit.C, 10);
        Assert.Equal(expectedMse, fit.MSE, 10);
    }

    [Fact]
    public void Evaluate_Uses_Fitted_Coefficient()
    {
        var points = Enumerable.Range(1, 10).Select(i => ((double)i, 2.0 * i)).ToList();
        var fit = ApproximationService.Fit(points, "O(n)");
        Assert.Equal(2.0 * 100, fit.Evaluate(100), 6);
    }

    [Fact]
    public void Fit_With_Too_Few_Points_Is_Safe()
    {
        var fit = ApproximationService.Fit(new List<(double, double)> { (1, 1) }, "O(n)");
        Assert.Contains("недостаточно", fit.Formula);
    }

    [Fact]
    public void Wrong_Complexity_Class_Has_Higher_Mse()
    {
        // Линейные данные: модель O(n) должна приближать лучше, чем O(n²).
        var points = Enumerable.Range(1, 15).Select(i => ((double)i * 10, 2.0 * i * 10)).ToList();
        double mseLinear = ApproximationService.Fit(points, "O(n)").MSE;
        double mseQuadratic = ApproximationService.Fit(points, "O(n^2)").MSE;
        Assert.True(mseLinear < mseQuadratic,
            "правильная модель сложности должна давать меньший MSE");
    }
}
