using System.Diagnostics;
using ComplexityResearch.Models;

namespace ComplexityResearch.Services;

/// <summary>
/// Калибровка констант модели стоимости элементарных операций.
/// </summary>
/// <remarks>
/// <para>
/// Вместо полностью выдуманных чисел программа измеряет реальное время базовых
/// операций на данной машине: выполняются плотные циклы по K итераций,
/// и стоимость одной операции = (время цикла − время «пустого» цикла) / K.
/// </para>
/// <para>
/// Методика и допущения:
/// 1) «пустой» цикл содержит инкремент и сравнение — они вычитаются из всех
///    измерений, но его XOR-операция по стоимости ≈ сложению, поэтому
///    вычитание приближённо удаляет и её вклад;
/// 2) стоимость остатка от деления учитывается как умножение;
/// 3) стоимость обмена включает два присваивания (они вычитаются);
/// 4) доступ к массиву измеряется по горячим данным L1-кэша — для данных
///    из оперативной памяти фактическая стоимость будет выше (это одна из
///    причин расхождения теории и практики на больших n);
/// 5) JIT может применять SIMD-векторизацию к простым циклам, занижая
///    измеренные константы относительно «элементарной» операции.
/// </para>
/// </remarks>
public static class CalibrationService
{
    /// <summary>
    /// Выполняет калибровку и возвращает модель с измеренными константами (нс/операция).
    /// </summary>
    /// <param name="iterationsPerTest">Количество итераций на каждый тест.</param>
    public static OperationCostModel Calibrate(long iterationsPerTest = 100_000_000)
    {
        long k = iterationsPerTest;

        // Прогрев, чтобы JIT-компиляция не попала в измерения.
        WarmUp();

        // --- Базовый цикл: инкремент + сравнение + XOR (вычитается из всех тестов) ---
        double baselineNs = MeasurePerIteration(k, () =>
        {
            long dummy = 0;
            for (long i = 0; i < k; i++)
            {
                dummy ^= i;
            }
            return dummy;
        });

        // --- Сложение ---
        double additionNs = MeasurePerIteration(k, () =>
        {
            long x = 0;
            for (long i = 0; i < k; i++)
            {
                x += i;
            }
            return x;
        }) - baselineNs;

        // --- Умножение ---
        double multiplicationNs = MeasurePerIteration(k, () =>
        {
            long x = 1;
            for (long i = 0; i < k; i++)
            {
                unchecked { x *= 3; }
            }
            return x;
        }) - baselineNs;

        // --- Сравнение (предсказуемая ветвление — всегда истинно) ---
        double comparisonNs = MeasurePerIteration(k, () =>
        {
            long hits = 0;
            for (long i = 0; i < k; i++)
            {
                if (i < k) hits++;
            }
            return hits;
        }) - baselineNs;

        // --- Присваивание ---
        double assignmentNs = MeasurePerIteration(k, () =>
        {
            long a = 0;
            long b = 1;
            for (long i = 0; i < k; i++)
            {
                a = b;
            }
            return a;
        }) - baselineNs;

        // --- Обмен (включает 2 присваивания, вычитаем их) ---
        double swapRawNs = MeasurePerIteration(k, () =>
        {
            long a = 0;
            long b = 1;
            for (long i = 0; i < k; i++)
            {
                (a, b) = (b, a);
            }
            return a;
        }) - baselineNs;
        double swapNs = Math.Max(0.01, swapRawNs - 2 * assignmentNs);

        // --- Доступ к элементу массива (данные в L1-кэше) ---
        int[] array = new int[1024];
        double arrayRawNs = MeasurePerIteration(k, () =>
        {
            long s = 0;
            for (long i = 0; i < k; i++)
            {
                s += array[i & 1023];
            }
            return s;
        }) - baselineNs;
        double arrayNs = Math.Max(0.01, arrayRawNs - additionNs);

        return new OperationCostModel
        {
            AdditionCost = Clamp(additionNs),
            MultiplicationCost = Clamp(multiplicationNs),
            ComparisonCost = Clamp(comparisonNs),
            AssignmentCost = Clamp(assignmentNs),
            SwapCost = swapNs,
            ArrayAccessCost = arrayNs,
            CalibratedAtUtc = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Измеряет среднее время одной итерации цикла в наносекундах
    /// (несколько повторов, берётся минимальный — наименее «зашумлённый»).
    /// </summary>
    private static double MeasurePerIteration(long iterations, Func<long> body)
    {
        double best = double.MaxValue;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            var sw = Stopwatch.StartNew();
            long result = body();
            sw.Stop();
            if (result == long.MinValue) Console.Write(string.Empty); // защита от удаления результата
            double perIteration = sw.Elapsed.TotalNanoseconds / iterations;
            best = Math.Min(best, perIteration);
        }
        return best;
    }

    private static void WarmUp()
    {
        long acc = 0;
        for (long i = 0; i < 1_000_000; i++)
        {
            acc += i;
        }
        if (acc == long.MinValue) Console.Write(string.Empty);
    }

    /// <summary>Ограничивает константу разумным диапазоном (0.05..50 нс).</summary>
    private static double Clamp(double ns) => Math.Clamp(ns, 0.05, 50.0);
}
