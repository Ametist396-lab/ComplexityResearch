using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ComplexityResearch.Views;

/// <summary>
/// Преобразует строку в Visibility: пустая/null → Collapsed, иначе Visible.
/// </summary>
public sealed class EmptyToCollapsedConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
