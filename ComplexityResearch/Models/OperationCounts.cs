namespace ComplexityResearch.Models;

/// <summary>
/// Количество элементарных операций, выполняемых алгоритмом для данного n.
/// </summary>
/// <remarks>
/// Значения — <see cref="double"/>, потому что среднее число операций
/// (например, среднее число обменов в Bubble Sort на случайных данных)
/// может быть дробным. Значения используются теоретической моделью
/// (<see cref="OperationCostModel"/>) для расчёта теоретического времени:
/// T(n) = Σ (количество_операций_типа_i × константа_времени_типа_i).
/// </remarks>
/// <param name="Addition">Сложения и инкременты (в т.ч. счётчики циклов).</param>
/// <param name="Multiplication">Умножения (включая приближённую оценку деления/остатка).</param>
/// <param name="Comparison">Сравнения (условия циклов и ветвлений).</param>
/// <param name="Assignment">Присваивания (включая пересылки регистров).</param>
/// <param name="Swap">Обмены значений двух переменных (полные обмены, а не отдельные присваивания).</param>
/// <param name="ArrayAccess">Обращения к элементам массива (чтение и запись по индексу).</param>
public sealed record OperationCounts(
    double Addition,
    double Multiplication,
    double Comparison,
    double Assignment,
    double Swap,
    double ArrayAccess)
{
    /// <summary>Нулевое количество операций.</summary>
    public static OperationCounts Zero { get; } = new(0, 0, 0, 0, 0, 0);

    /// <summary>Суммарное количество операций всех типов.</summary>
    public double Total => Addition + Multiplication + Comparison + Assignment + Swap + ArrayAccess;

    /// <summary>Поэлементное сложение двух наборов операций.</summary>
    public static OperationCounts operator +(OperationCounts a, OperationCounts b) =>
        new(a.Addition + b.Addition,
            a.Multiplication + b.Multiplication,
            a.Comparison + b.Comparison,
            a.Assignment + b.Assignment,
            a.Swap + b.Swap,
            a.ArrayAccess + b.ArrayAccess);

    /// <summary>Умножение всех количеств на коэффициент.</summary>
    public static OperationCounts operator *(OperationCounts a, double k) =>
        new(a.Addition * k,
            a.Multiplication * k,
            a.Comparison * k,
            a.Assignment * k,
            a.Swap * k,
            a.ArrayAccess * k);
}
