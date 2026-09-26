using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ComplexityResearch.Controls;

/// <summary>Точка серии графика: координаты в пространстве данных.</summary>
public readonly record struct ChartPoint(double X, double Y);

/// <summary>
/// Серия графика: набор точек с настройками отображения.
/// </summary>
public sealed class ChartSeries
{
    /// <summary>Название серии (в легенде).</summary>
    public required string Title { get; init; }

    /// <summary>Цвет линии и точек.</summary>
    public required Color Color { get; init; }

    /// <summary>Точки серии (X = n, Y = время, с).</summary>
    public required IReadOnlyList<ChartPoint> Points { get; init; }

    /// <summary>Рисовать линию.</summary>
    public bool ShowLine { get; init; } = true;

    /// <summary>Рисовать маркеры точек.</summary>
    public bool ShowPoints { get; init; } = true;

    /// <summary>Толщина линии.</summary>
    public double Thickness { get; init; } = 2.0;

    /// <summary>Шаблон пунктира (null — сплошная линия).</summary>
    public DoubleCollection? DashPattern { get; init; }
}

/// <summary>
/// Учебный элемент управления для построения графиков зависимостей время/n.
/// </summary>
/// <remarks>
/// <para>
/// Возможности: заголовок, подписи осей, легенда, сетка, маркеры точек,
/// линейная и логарифмическая шкала по каждой оси, масштабирование колесом
/// мыши, панорамирование перетаскиванием, сброс двойным щелчком,
/// экспорт в PNG.
/// </para>
/// <para>
/// Собственная реализация выбрана, чтобы проект собирался без внешних
/// NuGet-зависимостей и студент мог изучить построение графиков «изнутри».
/// </para>
/// </remarks>
public class ChartControl : FrameworkElement
{
    private const double LeftMargin = 90;
    private const double RightMargin = 20;
    private const double TopMargin = 45;
    private const double BottomMargin = 55;

    private IReadOnlyList<ChartSeries> _series = Array.Empty<ChartSeries>();
    private string _title = string.Empty;
    private string _xTitle = "Размер входных данных n";
    private string _yTitle = "Время, с";
    private bool _logX;
    private bool _logY;

    // Видимая область в преобразованных координатах (логарифм — если включён).
    private double _vxMin, _vxMax, _vyMin, _vyMax;
    private bool _autoFit = true;
    private Point _dragOrigin;
    private bool _isDragging;

    /// <summary>Обновляет все данные графика.</summary>
    public void SetData(string title, string xTitle, string yTitle, IReadOnlyList<ChartSeries> series)
    {
        _title = title;
        _xTitle = xTitle;
        _yTitle = yTitle;
        _series = series;
        _autoFit = true;
        InvalidateVisual();
    }

    /// <summary>Переключает шкалы осей (true — логарифмическая).</summary>
    public void SetScale(bool logX, bool logY)
    {
        _logX = logX;
        _logY = logY;
        _autoFit = true;
        InvalidateVisual();
    }

    /// <summary>Очищает график.</summary>
    public void Clear()
    {
        _series = Array.Empty<ChartSeries>();
        _title = string.Empty;
        _autoFit = true;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        RenderChart(drawingContext, ActualWidth, ActualHeight, dpi);
    }

    /// <summary>
    /// Основная отрисовка. Используется и для экрана, и для экспорта в PNG.
    /// </summary>
    private void RenderChart(DrawingContext dc, double width, double height, double pixelsPerDip)
    {
        var background = Color.FromRgb(255, 255, 255);
        dc.DrawRectangle(new SolidColorBrush(background), null, new Rect(0, 0, width, height));

        var plot = new Rect(LeftMargin, TopMargin,
            Math.Max(10, width - LeftMargin - RightMargin),
            Math.Max(10, height - TopMargin - BottomMargin));

        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var typefaceBold = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        var textBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        var gridBrush = new SolidColorBrush(Color.FromRgb(225, 228, 232));
        var axisBrush = new SolidColorBrush(Color.FromRgb(120, 120, 120));
        textBrush.Freeze();
        gridBrush.Freeze();
        axisBrush.Freeze();

        FormattedText MakeText(string s, double size, bool bold = false) =>
            new(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, bold ? typefaceBold : typeface, size, textBrush, pixelsPerDip);

        var allPoints = _series.SelectMany(s => s.Points).Where(IsValid).ToList();

        if (allPoints.Count == 0)
        {
            var empty = MakeText("Нет данных — запустите эксперимент", 14);
            dc.DrawText(empty, new Point(width / 2 - empty.Width / 2, height / 2 - empty.Height / 2));
            return;
        }

        FitViewport(allPoints);

        double xRange = _vxMax - _vxMin;
        double yRange = _vyMax - _vyMin;
        if (xRange <= 0 || yRange <= 0) return;

        // --- Преобразование данных в экранные координаты ---
        double Tx(double v) => _logX ? Math.Log10(Math.Max(v, 1e-300)) : v;
        double Ty(double v) => _logY ? Math.Log10(Math.Max(v, 1e-300)) : v;
        double MapX(double data) => plot.X + (Tx(data) - _vxMin) / xRange * plot.Width;
        double MapY(double data) => plot.Y + plot.Height - (Ty(data) - _vyMin) / yRange * plot.Height;

        // --- Сетка и подписи осей ---
        IReadOnlyList<double> xTicks = _logX ? LogTicks(_vxMin, _vxMax) : NiceTicks(_vxMin, _vxMax, 8);
        IReadOnlyList<double> yTicks = _logY ? LogTicks(_vyMin, _vyMax) : NiceTicks(_vyMin, _vyMax, 7);
        var tickPen = new Pen(axisBrush, 1);
        tickPen.Freeze();

        foreach (double t in xTicks)
        {
            double x = plot.X + (t - _vxMin) / xRange * plot.Width;
            dc.DrawLine(GridPen(gridBrush), new Point(x, plot.Top), new Point(x, plot.Bottom));
            var label = MakeText(FormatValue(InverseTx(t)), 11);
            dc.DrawText(label, new Point(x - label.Width / 2, plot.Bottom + 6));
        }
        foreach (double t in yTicks)
        {
            double y = plot.Bottom - (t - _vyMin) / yRange * plot.Height;
            dc.DrawLine(GridPen(gridBrush), new Point(plot.Left, y), new Point(plot.Right, y));
            var label = MakeText(FormatValue(InverseTy(t)), 11);
            dc.DrawText(label, new Point(plot.Left - label.Width - 6, y - label.Height / 2));
        }

        // Рамка области построения.
        var framePen = new Pen(axisBrush, 1);
        framePen.Freeze();
        dc.DrawRectangle(null, framePen, plot);

        // --- Серии (с обрезкой по области построения) ---
        dc.PushClip(new RectangleGeometry(plot));
        foreach (var series in _series)
        {
            if (series.Points.Count == 0) continue;
            var brush = new SolidColorBrush(series.Color);
            brush.Freeze();
            var pen = new Pen(brush, series.Thickness);
            if (series.DashPattern != null && series.DashPattern.Count > 0)
            {
                pen.DashStyle = new DashStyle(series.DashPattern, 0);
            }
            pen.Freeze();

            if (series.ShowLine && series.Points.Count > 1)
            {
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    var first = series.Points[0];
                    ctx.BeginFigure(new Point(MapX(first.X), MapY(first.Y)), false, false);
                    var screenPoints = new List<Point>(series.Points.Count);
                    for (int i = 1; i < series.Points.Count; i++)
                    {
                        var p = series.Points[i];
                        screenPoints.Add(new Point(MapX(p.X), MapY(p.Y)));
                    }
                    ctx.PolyLineTo(screenPoints, true, false);
                }
                geometry.Freeze();
                dc.DrawGeometry(null, pen, geometry);
            }
            if (series.ShowPoints)
            {
                // При очень большом количестве точек маркеры прореживаются.
                int step = Math.Max(1, series.Points.Count / 200);
                for (int i = 0; i < series.Points.Count; i += step)
                {
                    var p = series.Points[i];
                    var center = new Point(MapX(p.X), MapY(p.Y));
                    dc.DrawEllipse(brush, null, center, 3, 3);
                }
            }
        }
        dc.Pop();

        // --- Заголовок и названия осей ---
        if (_title.Length > 0)
        {
            var title = MakeText(_title, 15, bold: true);
            dc.DrawText(title, new Point(width / 2 - title.Width / 2, 12));
        }
        var xLabel = MakeText(_xTitle, 12);
        dc.DrawText(xLabel, new Point(width / 2 - xLabel.Width / 2, height - xLabel.Height - 6));

        var yLabel = MakeText(_yTitle, 12);
        double ylx = 16, yly = height / 2 + yLabel.Width / 2;
        dc.PushTransform(new RotateTransform(-90, ylx, yly));
        dc.DrawText(yLabel, new Point(ylx, yly));
        dc.Pop();

        // --- Легенда ---
        var legendItems = _series.Where(s => s.Points.Count > 0).ToList();
        if (legendItems.Count > 0)
        {
            double lh = 20;
            double maxTextWidth = legendItems.Max(s => MakeText(s.Title, 12).Width);
            double legendWidth = maxTextWidth + 46;
            double legendHeight = legendItems.Count * lh + 10;
            var legendRect = new Rect(plot.Right - legendWidth - 8, plot.Top + 8, legendWidth, legendHeight);
            var legendBrush = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255));
            legendBrush.Freeze();
            dc.DrawRectangle(legendBrush, framePen, legendRect);

            for (int i = 0; i < legendItems.Count; i++)
            {
                var s = legendItems[i];
                double y = legendRect.Y + 8 + i * lh;
                var brush = new SolidColorBrush(s.Color);
                brush.Freeze();
                var samplePen = new Pen(brush, 2);
                if (s.DashPattern != null && s.DashPattern.Count > 0)
                {
                    samplePen.DashStyle = new DashStyle(s.DashPattern, 0);
                }
                samplePen.Freeze();
                dc.DrawLine(samplePen, new Point(legendRect.X + 8, y + 8), new Point(legendRect.X + 32, y + 8));
                if (s.ShowPoints)
                {
                    dc.DrawEllipse(brush, null, new Point(legendRect.X + 20, y + 8), 3, 3);
                }
                var text = MakeText(s.Title, 12);
                dc.DrawText(text, new Point(legendRect.X + 38, y));
            }
        }
    }

    private Pen GridPen(Brush brush)
    {
        var pen = new Pen(brush, 1);
        pen.Freeze();
        return pen;
    }

    private static bool IsValid(ChartPoint p) => double.IsFinite(p.X) && double.IsFinite(p.Y);

    /// <summary>Подгоняет видимую область под данные (с отступами 5%).</summary>
    private void FitViewport(List<ChartPoint> points)
    {
        if (!_autoFit) return;
        double txmin = double.MaxValue, txmax = double.MinValue;
        double tymin = double.MaxValue, tymax = double.MinValue;
        foreach (var p in points)
        {
            double tx = _logX ? Math.Log10(Math.Max(p.X, 1e-300)) : p.X;
            double ty = _logY ? Math.Log10(Math.Max(p.Y, 1e-300)) : p.Y;
            txmin = Math.Min(txmin, tx);
            txmax = Math.Max(txmax, tx);
            tymin = Math.Min(tymin, ty);
            tymax = Math.Max(tymax, ty);
        }
        // Если значения неотрицательны, начинаем ось от нуля — так нагляднее.
        if (!_logX && txmin > 0) txmin = 0;
        if (!_logY && tymin > 0) tymin = 0;
        Pad(ref txmin, ref txmax);
        Pad(ref tymin, ref tymax);
        _vxMin = txmin; _vxMax = txmax;
        _vyMin = tymin; _vyMax = tymax;
    }

    private static void Pad(ref double min, ref double max)
    {
        if (max - min < 1e-12)
        {
            max = min + Math.Max(1, Math.Abs(min) * 0.1);
        }
        double pad = (max - min) * 0.05;
        min -= pad;
        max += pad;
    }

    /// <summary>«Красивые» деления шкалы: шаги 1-2-5.</summary>
    private static IReadOnlyList<double> NiceTicks(double min, double max, int targetCount)
    {
        double range = max - min;
        if (range <= 0) return [min];
        double step0 = range / Math.Max(1, targetCount);
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(step0)));
        double normalized = step0 / magnitude;
        double step = normalized switch
        {
            < 1.5 => 1,
            < 3.5 => 2,
            < 7.5 => 5,
            _ => 10
        } * magnitude;

        var ticks = new List<double>();
        double first = Math.Ceiling(min / step) * step;
        for (double t = first; t <= max + step * 1e-6; t += step)
        {
            ticks.Add(t);
        }
        return ticks;
    }

    /// <summary>Деления логарифмической шкалы (декады, для узких диапазонов — 1/2/5).</summary>
    private static IReadOnlyList<double> LogTicks(double min, double max)
    {
        int e0 = (int)Math.Floor(min);
        int e1 = (int)Math.Ceiling(max);
        var ticks = new List<double>();
        int span = e1 - e0;
        for (int e = e0; e <= e1; e++)
        {
            double decade = Math.Pow(10, e);
            if (e >= min - 1e-9 && e <= max + 1e-9) ticks.Add(decade);
            if (span <= 2)
            {
                foreach (double m in new[] { 2.0, 5.0 })
                {
                    double logM = Math.Log10(m) + e;
                    if (logM >= min - 1e-9 && logM <= max + 1e-9)
                    {
                        ticks.Add(m * decade);
                    }
                }
            }
        }
        return ticks;
    }

    private double InverseTx(double transformed) => _logX ? Math.Pow(10, transformed) : transformed;
    private double InverseTy(double transformed) => _logY ? Math.Pow(10, transformed) : transformed;

    /// <summary>Форматирование чисел на осях: компактный общий формат, крайние значения — экспоненциальные.</summary>
    private static string FormatValue(double v)
    {
        if (v == 0) return "0";
        double abs = Math.Abs(v);
        if (abs >= 1e6 || abs < 1e-4) return v.ToString("G2", CultureInfo.CurrentCulture);
        return v.ToString("0.######", CultureInfo.CurrentCulture);
    }

    // --- Масштабирование и панорамирование ---

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (_series.Count == 0) return;
        var plot = GetPlotRect();
        var p = e.GetPosition(this);
        if (!plot.Contains(p)) return;

        double factor = e.Delta > 0 ? 1.0 / 1.25 : 1.25;
        double xRange = _vxMax - _vxMin;
        double yRange = _vyMax - _vyMin;
        double tx = _vxMin + (p.X - plot.X) / plot.Width * xRange;
        double ty = _vyMin + (plot.Bottom - p.Y) / plot.Height * yRange;

        _vxMin = tx - (tx - _vxMin) * factor;
        _vxMax = tx + (_vxMax - tx) * factor;
        _vyMin = ty - (ty - _vyMin) * factor;
        _vyMax = ty + (_vyMax - ty) * factor;
        _autoFit = false;
        InvalidateVisual();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // Двойной щелчок — сброс масштаба к автоматическому.
        if (e.ClickCount >= 2)
        {
            _autoFit = true;
            InvalidateVisual();
            return;
        }
        _dragOrigin = e.GetPosition(this);
        _isDragging = true;
        CaptureMouse();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_isDragging) return;
        var plot = GetPlotRect();
        var p = e.GetPosition(this);
        double dx = (p.X - _dragOrigin.X) / plot.Width * (_vxMax - _vxMin);
        double dy = (p.Y - _dragOrigin.Y) / plot.Height * (_vyMax - _vyMin);
        _vxMin -= dx;
        _vxMax -= dx;
        _vyMin += dy;
        _vyMax += dy;
        _dragOrigin = p;
        _autoFit = false;
        InvalidateVisual();
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }

    private Rect GetPlotRect() => new(LeftMargin, TopMargin,
        Math.Max(10, ActualWidth - LeftMargin - RightMargin),
        Math.Max(10, ActualHeight - TopMargin - BottomMargin));

    /// <summary>
    /// Экспортирует график в PNG.
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <param name="width">Ширина изображения.</param>
    /// <param name="height">Высота изображения.</param>
    public void ExportPng(string path, int width = 1600, int height = 900)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            RenderChart(dc, width, height, pixelsPerDip: 1.0);
        }
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }
}
