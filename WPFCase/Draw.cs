using BestDelivery;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WPFCase;

public class Draw
{
    private readonly Canvas routeCanvas;
    private double minX, maxX, minY, maxY, rangeX, rangeY;
    private double canvasWidth, canvasHeight;
    private const double Padding = 10;

    private double zoom = 1.0;
    private double offsetX = 0;
    private double offsetY = 0;

    public Draw(Canvas canvas)
    {
        routeCanvas = canvas;
    }

    public void SetZoomAndOffset(double newZoom, double newOffsetX, double newOffsetY)
    {
        zoom = Math.Max(0.1, newZoom);
        offsetX = newOffsetX;
        offsetY = newOffsetY;
    }

    public (double zoom, double offsetX, double offsetY) AutoZoomAndCenter(Order[] orders, BestDelivery.Point depot)
    {
        if (orders == null || orders.Length == 0 || routeCanvas.ActualWidth == 0 || routeCanvas.ActualHeight == 0)
            return (zoom, offsetX, offsetY);

        var allPoints = orders.Select(o => o.Destination).Append(depot).ToArray();

        double minRawX = allPoints.Min(p => p.X);
        double maxRawX = allPoints.Max(p => p.X);
        double minRawY = allPoints.Min(p => p.Y);
        double maxRawY = allPoints.Max(p => p.Y);

        double rawRangeX = maxRawX - minRawX;
        double rawRangeY = maxRawY - minRawY;

        if (rawRangeX == 0) rawRangeX = 1;
        if (rawRangeY == 0) rawRangeY = 1;

        double drawableWidth = routeCanvas.ActualWidth - 2 * Padding;
        double drawableHeight = routeCanvas.ActualHeight - 2 * Padding;

        double scaleX = drawableWidth / rawRangeX;
        double scaleY = drawableHeight / rawRangeY;
        double newZoom = Math.Min(scaleX, scaleY);

        double centerX = (minRawX + maxRawX) / 2;
        double centerY = (minRawY + maxRawY) / 2;

        double canvasCenterX = routeCanvas.ActualWidth / 2;
        double canvasCenterY = routeCanvas.ActualHeight / 2;

        double newOffsetX = canvasCenterX - centerX * newZoom;
        double newOffsetY = canvasCenterY + centerY * newZoom;

        // Сохраняем диапазоны для преобразований
        minX = centerX - drawableWidth / newZoom / 2.0;
        maxX = centerX + drawableWidth / newZoom / 2.0;
        minY = centerY - drawableHeight / newZoom / 2.0;
        maxY = centerY + drawableHeight / newZoom / 2.0;
        rangeX = maxX - minX;
        rangeY = maxY - minY;

        SetZoomAndOffset(newZoom, newOffsetX, newOffsetY);

        return (newZoom, newOffsetX, newOffsetY);
    }

    public void DrawPointsOnCanvas(Order[] orders, BestDelivery.Point depot)
    {
        routeCanvas.Children.Clear();
        if (orders == null || orders.Length == 0) return;

        canvasWidth = routeCanvas.ActualWidth;
        canvasHeight = routeCanvas.ActualHeight;
        if (canvasWidth == 0 || canvasHeight == 0) return;

        double rawMinX = orders.Min(o => o.Destination.X);
        double rawMaxX = orders.Max(o => o.Destination.X);
        double rawMinY = orders.Min(o => o.Destination.Y);
        double rawMaxY = orders.Max(o => o.Destination.Y);

        // Вычисляем диапазоны
        double rawRangeX = rawMaxX - rawMinX;
        double rawRangeY = rawMaxY - rawMinY;

        if (rawRangeX == 0) rawRangeX = 1;
        if (rawRangeY == 0) rawRangeY = 1;

        // Получаем размеры канваса с отступами
        double drawableWidth = canvasWidth - 2 * Padding;
        double drawableHeight = canvasHeight - 2 * Padding;

        // Рассчитаем коэффициенты масштабирования по X и Y
        double scaleX = drawableWidth / rawRangeX;
        double scaleY = drawableHeight / rawRangeY;

        // Выбираем меньший масштаб, чтобы всё помещалось и не выходило за канвас
        double scale = Math.Min(scaleX, scaleY);

        // Теперь вычислим новые диапазоны, исходя из выбранного масштаба
        rangeX = drawableWidth / scale;
        rangeY = drawableHeight / scale;

        // Центрируем координаты вокруг середины исходного диапазона
        double centerX = (rawMinX + rawMaxX) / 2.0;
        double centerY = (rawMinY + rawMaxY) / 2.0;

        minX = centerX - rangeX / 2.0;
        maxX = centerX + rangeX / 2.0;
        minY = centerY - rangeY / 2.0;
        maxY = centerY + rangeY / 2.0;

        // Рисуем заказы (белые)
        foreach (var order in orders)
        {
            var (x, y) = TransformPoint(order.Destination.X, order.Destination.Y);
            DrawEllipse(x, y, 8, Brushes.White);
        }

        // Рисуем склад (синий)
        var (dx, dy) = TransformPoint(depot.X, depot.Y);
        Brush customBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a4ac3"));
        DrawEllipse(dx, dy, 10, customBrush);
    }

    public void DrawRouteWithArrows(BestDelivery.Point depot, Order[] orders, int[] route)
    {
        Brush customBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f7e59c"));
        Brush customBrush1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fbceb1"));
        if (orders == null || route == null || route.Length == 0) return;

        DrawPointsOnCanvas(orders, depot);

        List<BestDelivery.Point> path = new() { depot };
        foreach (int id in route)
        {
            var match = orders.FirstOrDefault(o => o.ID == id);
            if (match.ID != 0)
            {
                path.Add(match.Destination);
            }
        }
        path.Add(depot);

        for (int i = 0; i < path.Count - 1; i++)
        {
            var (x1, y1) = TransformPoint(path[i].X, path[i].Y);
            var (x2, y2) = TransformPoint(path[i + 1].X, path[i + 1].Y);

            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = customBrush,
                StrokeThickness = 2
            };

            routeCanvas.Children.Add(line);
            DrawArrowHead(x1, y1, x2, y2, customBrush1);
        }
    }

    private (double x, double y) TransformPoint(double xRaw, double yRaw)
    {
        double x = (xRaw - minX) / rangeX * (canvasWidth - 2 * Padding) + Padding;
        double y = canvasHeight - ((yRaw - minY) / rangeY * (canvasHeight - 2 * Padding)) - Padding;

        x = x * zoom + offsetX;
        y = y * zoom + offsetY;

        return (x, y);
    }

    public (double xRaw, double yRaw) InverseTransformPoint(double xCanvas, double yCanvas)
    {
        double x = (xCanvas - offsetX) / zoom;
        double y = (yCanvas - offsetY) / zoom;

        x = (x - Padding) / (canvasWidth - 2 * Padding) * rangeX + minX;
        y = ((canvasHeight - y - Padding) / (canvasHeight - 2 * Padding)) * rangeY + minY;

        return (x, y);
    }

    private void DrawEllipse(double x, double y, double size, Brush color)
    {
        var ellipse = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = color,
            Stroke = Brushes.Black,
            StrokeThickness = 1
        };
        Canvas.SetLeft(ellipse, x - size / 2);
        Canvas.SetTop(ellipse, y - size / 2);
        routeCanvas.Children.Add(ellipse);
    }

    private void DrawArrowHead(double x1, double y1, double x2, double y2, Brush color)
    {
        const double arrowLength = 10;
        const double arrowWidth = 6;

        Vector direction = new Vector(x2 - x1, y2 - y1);
        direction.Normalize();

        Vector orthogonal = new Vector(-direction.Y, direction.X);

        System.Windows.Point end = new(x2, y2);
        System.Windows.Point p1 = end - direction * arrowLength + orthogonal * arrowWidth / 2;
        System.Windows.Point p2 = end - direction * arrowLength - orthogonal * arrowWidth / 2;

        var arrow = new Polygon
        {
            Points = new PointCollection { end, p1, p2 },
            Fill = color
        };

        routeCanvas.Children.Add(arrow);
    }

}
