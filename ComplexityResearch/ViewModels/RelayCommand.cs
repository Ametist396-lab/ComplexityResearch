using System.Windows.Input;

namespace ComplexityResearch.ViewModels;

/// <summary>
/// Команда для привязки в XAML (стандартный RelayCommand с поддержкой async).
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;

    /// <summary>Создаёт асинхронную команду.</summary>
    public RelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }

    /// <summary>Создаёт синхронную команду.</summary>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        : this(o => { execute(o); return Task.CompletedTask; }, canExecute)
    {
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) =>
        !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try
        {
            await _executeAsync(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>Уведомляет WPF о необходимости пересчитать CanExecute.</summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
