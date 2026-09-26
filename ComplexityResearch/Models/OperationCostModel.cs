namespace ComplexityResearch.Models;

/// <summary>
/// Модель стоимости элементарных операций (в наносекундах на одну операцию).
/// </summary>
/// <remarks>
/// <para>
/// Теоретическое время работы алгоритма считается по формуле
/// T(n) = C · f(n), где f(n) — количество элементарных операций, а C —
/// константы времени из этой модели:
/// T(n) = Σ count_i · cost_i.
/// </para>
/// <para>
/// Константы можно изменить в коде (см. <see cref="CreateDefault"/>),
/// либо получить реалистичные значения кнопкой «Калибровка»
/// (<see cref="Services.CalibrationService"/>). Значения по умолчанию —
/// учебные, приблизительно соответствующие современному настольному CPU.
/// </para>
/// <para>
/// Важно: реальные стоимости сильно зависят от кэша процессора, конвейера,
/// векторизации (SIMD) и планировщика. Поэтому расхождение теории и практики
/// — нормальная часть исследования, а не ошибка.
/// </para>
/// </remarks>
public sealed class OperationCostModel
{
    /// <summary>Время одного сложения, нс.</summary>
    public double AdditionCost { get; set; } = 0.5;

    /// <summary>Время одного умножения, нс.</summary>
    public double MultiplicationCost { get; set; } = 1.0;

    /// <summary>Время одного сравнения, нс.</summary>
    public double ComparisonCost { get; set; } = 0.5;

    /// <summary>Время одного присваивания, нс.</summary>
    public double AssignmentCost { get; set; } = 0.3;

    /// <summary>Время одного полного обмена двух значений, нс.</summary>
    public double SwapCost { get; set; } = 1.5;

    /// <summary>Время одного обращения к элементу массива, нс.</summary>
    public double ArrayAccessCost { get; set; } = 1.0;

    /// <summary>Дата и время последней калибровки (null — используются значения по умолчанию).</summary>
    public DateTime? CalibratedAtUtc { get; set; }

    /// <summary>
    /// Создаёт копию модели (чтобы калибровка не меняла глобальные значения неожиданно).
    /// </summary>
    public OperationCostModel Clone() => new()
    {
        AdditionCost = AdditionCost,
        MultiplicationCost = MultiplicationCost,
        ComparisonCost = ComparisonCost,
        AssignmentCost = AssignmentCost,
        SwapCost = SwapCost,
        ArrayAccessCost = ArrayAccessCost,
        CalibratedAtUtc = CalibratedAtUtc
    };

    /// <summary>
    /// Вычисляет теоретическое время выполнения набора операций, в наносекундах.
    /// </summary>
    public double TotalTimeNs(OperationCounts counts) =>
        counts.Addition * AdditionCost +
        counts.Multiplication * MultiplicationCost +
        counts.Comparison * ComparisonCost +
        counts.Assignment * AssignmentCost +
        counts.Swap * SwapCost +
        counts.ArrayAccess * ArrayAccessCost;

    /// <summary>
    /// Создаёт модель с учебными константами по умолчанию.
    /// </summary>
    public static OperationCostModel CreateDefault() => new();

    /// <summary>Многострочное человекочитаемое описание констант (для панели интерфейса).</summary>
    public string ToDisplayString()
    {
        string calib = CalibratedAtUtc is null
            ? "значения по умолчанию (учебные)"
            : $"откалибровано {CalibratedAtUtc:yyyy-MM-dd HH:mm} UTC";
        return
            $"Константа сложения:      {AdditionCost:F3} нс" + Environment.NewLine +
            $"Константа умножения:     {MultiplicationCost:F3} нс" + Environment.NewLine +
            $"Константа сравнения:     {ComparisonCost:F3} нс" + Environment.NewLine +
            $"Константа присваивания:  {AssignmentCost:F3} нс" + Environment.NewLine +
            $"Константа обмена:        {SwapCost:F3} нс" + Environment.NewLine +
            $"Константа доступа к массиву: {ArrayAccessCost:F3} нс" + Environment.NewLine +
            $"({calib})";
    }
}
