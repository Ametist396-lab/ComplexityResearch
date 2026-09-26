namespace ComplexityResearch.Algorithms;

/// <summary>
/// Алгоритм «операционного» бенчмарка: измеряется НЕ время, а количество
/// элементарных операций, подсчитываемое счётчиком прямо во время выполнения.
/// </summary>
/// <remarks>
/// Используется для сравнения итеративного, рекурсивного и бинарного
/// возведения в степень при n = 1..1000 (график: X = n, Y = количество
/// операций). Подсчёт ведётся честно — инкрементами счётчика в каждом
/// месте, где выполняется элементарная операция, а не по формуле.
/// </remarks>
public interface IOperationCountedAlgorithm
{
    /// <summary>Название (для графика и легенды).</summary>
    string Name { get; }

    /// <summary>Краткое описание.</summary>
    string Description { get; }

    /// <summary>Теоретическая сложность.</summary>
    string Complexity { get; }

    /// <summary>Класс сложности ("O(n)", "O(log n)").</summary>
    string ComplexityClass { get; }

    /// <summary>
    /// Выполняет алгоритм для показателя n и возвращает фактически
    /// подсчитанное количество элементарных операций.
    /// </summary>
    long ComputeOperationCount(int n);

    /// <summary>Теоретическая оценка количества операций для n (по формуле).</summary>
    long TheoreticalOperationCount(int n);

    /// <summary>Формула теоретического числа операций (текст).</summary>
    string TheoreticalFormula { get; }
}

/// <summary>
/// №11а. Итеративное возведение в степень — O(n).
/// </summary>
/// <remarks>
/// result ← 1; затем n раз result ← result·a. Число умножений линейно по n.
/// Основание 2, вычисления по модулю 1 000 000 007, чтобы не было переполнения
/// (на количество операций не влияет).
/// </remarks>
public sealed class PowerIterativeAlgorithm : IOperationCountedAlgorithm
{
    /// <summary>Модуль арифметики (исключает переполнение, на счёт операций не влияет).</summary>
    public const long Modulus = 1_000_000_007L;

    /// <summary>Основание степени.</summary>
    public const long Base = 2L;

    /// <inheritdoc />
    public string Name => "Степень: итеративно — O(n)";

    /// <inheritdoc />
    public string Description =>
        "result ← 1; n раз result ← result·a. Ровно n умножений — линейный рост числа операций.";

    /// <inheritdoc />
    public string Complexity => "O(n)";

    /// <inheritdoc />
    public string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public string TheoreticalFormula => "Операций ≈ 5n: n умножений, n присваиваний, n сравнений, n инкрементов";

    /// <summary>
    /// Чистое вычисление aⁿ mod m (без счёта операций) — используется
    /// временным бенчмарком №5а и unit-тестами.
    /// </summary>
    public static long Compute(int n)
    {
        long result = 1;
        for (int i = 0; i < n; i++)
        {
            result = result * Base % Modulus;
        }
        return result;
    }

    /// <inheritdoc />
    public long ComputeOperationCount(int n)
    {
        long ops = 0;
        long result = 1;
        for (int i = 0; i < n; i++)
        {
            result = result * Base % Modulus; // умножение + остаток
            ops += 2;                          // умножение и остаток (≈ умножение)
            ops++;                             // присваивание result
            ops++;                             // сравнение i < n
            ops++;                             // инкремент i
        }
        // Защита от удаления вычислений JIT-ом.
        _ = result;
        return ops;
    }

    /// <inheritdoc />
    public long TheoreticalOperationCount(int n) => 5L * n;
}

/// <summary>
/// №11б. Рекурсивное возведение в степень — O(n).
/// </summary>
/// <remarks>
/// power(a, k) = a · power(a, k−1), power(a, 0) = 1. Рекурсивная глубина n:
/// операций тоже линейно много, плюс накладные расходы на вызовы.
/// Для n = 1..1000 глубина стека безопасна.
/// </remarks>
public sealed class PowerRecursiveAlgorithm : IOperationCountedAlgorithm
{
    /// <inheritdoc />
    public string Name => "Степень: рекурсивно — O(n)";

    /// <inheritdoc />
    public string Description =>
        "power(a,k) = a·power(a,k−1); power(a,0) = 1. Глубина рекурсии n, операций линейно много " +
        "(как у итеративного), но каждый шаг — ещё и вызов функции.";

    /// <inheritdoc />
    public string Complexity => "O(n)";

    /// <inheritdoc />
    public string ComplexityClass => "O(n)";

    /// <inheritdoc />
    public string TheoreticalFormula => "Операций ≈ 6n: n умножений + по 2 операции на каждый вызов (сравнение, вычитание) + вызовы";

    /// <summary>
    /// Чистое рекурсивное вычисление aⁿ mod m — используется бенчмарком №5б
    /// и unit-тестами. Глубина рекурсии = n (безопасно до ~10 000).
    /// </summary>
    public static long Compute(int n) => ComputeRec(PowerIterativeAlgorithm.Base, n);

    private static long ComputeRec(long a, int k) =>
        k == 0 ? 1 : a * ComputeRec(a, k - 1) % PowerIterativeAlgorithm.Modulus;

    /// <inheritdoc />
    public long ComputeOperationCount(int n)
    {
        long ops = 0;
        long result = PowerRec(PowerIterativeAlgorithm.Base, n, ref ops);
        _ = result;
        return ops;
    }

    private long PowerRec(long a, int k, ref long ops)
    {
        ops++;                 // сравнение k == 0
        if (k == 0)
        {
            return 1;
        }
        ops++;                 // вычитание k − 1
        long sub = PowerRec(a, k - 1, ref ops);
        ops += 2;              // умножение a · sub + остаток
        ops++;                 // присваивание/возврат
        return a * sub % PowerIterativeAlgorithm.Modulus;
    }

    /// <inheritdoc />
    public long TheoreticalOperationCount(int n) => 6L * n;
}

/// <summary>
/// №11в. Бинарное (быстрое) возведение в степень — O(log n).
/// </summary>
/// <remarks>
/// Показатель разлагается в двоичную запись: каждый шаг — возведение основания
/// в квадрат, при единичном бите — домножение результата. Число итераций
/// ≈ log₂n, поэтому график операций растёт логарифмически и визуально
/// «прижимается» к оси X рядом с линейными вариантами.
/// </remarks>
public sealed class PowerBinaryAlgorithm : IOperationCountedAlgorithm
{
    /// <inheritdoc />
    public string Name => "Степень: бинарно — O(log n)";

    /// <inheritdoc />
    public string Description =>
        "Двоичное разложение показателя: на каждом шаге основание возводится в квадрат, " +
        "при единичном бите результат домножается. Всего ≈ log₂(n) итераций.";

    /// <inheritdoc />
    public string Complexity => "O(log n)";

    /// <inheritdoc />
    public string ComplexityClass => "O(log n)";

    /// <inheritdoc />
    public string TheoreticalFormula => "Операций ≈ 6·log₂(n) + 2·popcount(n): на итерацию сравнение, сдвиг, присваивание и возведение в квадрат (умножение + остаток)";

    /// <summary>
    /// Чистое бинарное вычисление aⁿ mod m — используется бенчмарком №5в
    /// и unit-тестами.
    /// </summary>
    public static long Compute(int n)
    {
        long result = 1;
        long b = PowerIterativeAlgorithm.Base % PowerIterativeAlgorithm.Modulus;
        long e = n;
        while (e > 0)
        {
            if ((e & 1) == 1)
            {
                result = result * b % PowerIterativeAlgorithm.Modulus;
            }
            b = b * b % PowerIterativeAlgorithm.Modulus;
            e >>= 1;
        }
        return result;
    }

    /// <inheritdoc />
    public long ComputeOperationCount(int n)
    {
        long ops = 0;
        long result = 1;
        long b = PowerIterativeAlgorithm.Base % PowerIterativeAlgorithm.Modulus;
        long e = n;
        while (e > 0)
        {
            ops++;                 // сравнение e > 0
            if ((e & 1) == 1)
            {
                ops += 2;          // умножение result·b + остаток
                result = result * b % PowerIterativeAlgorithm.Modulus;
                ops++;             // присваивание
            }
            ops += 2;              // умножение b·b + остаток
            b = b * b % PowerIterativeAlgorithm.Modulus;
            ops++;                 // присваивание
            ops++;                 // сдвиг e >> 1
            e >>= 1;
        }
        _ = result;
        return ops;
    }

    /// <inheritdoc />
    public long TheoreticalOperationCount(int n)
    {
        int log = n <= 0 ? 1 : (int)Math.Log2(n) + 1;
        int popcount = n == 0 ? 0 : System.Numerics.BitOperations.PopCount((uint)n);
        return 5L * log + 3L * popcount;
    }
}
