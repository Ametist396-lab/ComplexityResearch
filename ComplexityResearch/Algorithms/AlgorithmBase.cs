using ComplexityResearch.Models;

namespace ComplexityResearch.Algorithms;

/// <summary>
/// Абстрактный базовый класс всех алгоритмов исследования.
/// </summary>
/// <remarks>
/// <para>
/// Содержит общую инфраструктуру:
/// метаданные (имя, описание, сложность), подготовку входных данных,
/// защиту от «выбрасывания» неиспользуемых результатов JIT-компилятором,
/// расчёт количества элементарных операций и теоретического времени.
/// </para>
/// <para>
/// Наследник обязан реализовать <see cref="ExecuteCore"/> — «чистый» алгоритм,
/// который и измеряется, и <see cref="EstimateOperationCounts"/> — теоретическую
/// оценку числа операций. Всё остальное (прогрев, таймер, статистика,
/// генерация данных) выполняется общими сервисами.
/// </para>
/// <para>
/// Комментарии про JIT: RyuJIT может удалить вычисления, результат которых
/// нигде не используется, или выполнить их во время компиляции. Чтобы этого
/// не произошло, каждый алгоритм в конце работы вызывает метод
/// <c>PreventOptimization</c> — результат «утекает» в статическое поле,
/// и компилятор обязан его вычислить.
/// </para>
/// </remarks>
public abstract class AlgorithmBase : IAlgorithm
{
    /// <summary>Статический «приёмник» результатов: защищает измеряемый код от удаления JIT-ом.</summary>
    private static long _sink;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public abstract string Complexity { get; }

    /// <inheritdoc />
    public abstract string ComplexityClass { get; }

    /// <inheritdoc />
    public abstract string Application { get; }

    /// <inheritdoc />
    public abstract string TheoreticalFormula { get; }

    /// <inheritdoc />
    public abstract BenchmarkConfiguration DefaultConfiguration { get; }

    /// <inheritdoc />
    public abstract MeasurementMode ModeFor(long n);

    /// <inheritdoc />
    public abstract object PrepareInput(long n);

    /// <inheritdoc />
    public virtual object PrepareRunInput(object sharedInput) => sharedInput;

    /// <summary>
    /// Сам алгоритм. Вызывается внутри таймера — здесь не должно быть
    /// генерации данных, аллокаций и вывода на экран.
    /// </summary>
    protected abstract void ExecuteCore(object input);

    /// <summary>
    /// Публичная точка запуска ядра алгоритма (используется BenchmarkService
    /// и калибровкой). Не измеряется здесь — измерение выполняет таймер снаружи.
    /// </summary>
    public void Run(object input) => ExecuteCore(input);

    /// <inheritdoc />
    public abstract OperationCounts EstimateOperationCounts(long n);

    /// <inheritdoc />
    public long EstimateOperations(long n) => (long)Math.Round(EstimateOperationCounts(n).Total);

    /// <inheritdoc />
    public double EstimateTheoreticalTimeNs(long n, OperationCostModel costModel) =>
        costModel.TotalTimeNs(EstimateOperationCounts(n));

    /// <inheritdoc />
    public abstract Func<double, double> ComplexityFunction { get; }

    /// <summary>
    /// Защита от оптимизации: помещает результат вычислений в статическое поле.
    /// JIT не может удалить вычисление, результат которого используется.
    /// </summary>
    protected static void PreventOptimization(long value) => _sink = value;

    /// <summary>Перегрузка для вещественных результатов.</summary>
    protected static void PreventOptimization(double value) => _sink = BitConverter.DoubleToInt64Bits(value);
}
