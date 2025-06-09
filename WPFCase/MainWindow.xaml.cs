using BestDelivery;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPFCase;

namespace WPFCase
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<OrderPoint> Points { get; set; } = new ObservableCollection<OrderPoint>();
        private Draw drawHelper;
        private int nextId = 1;

        // Центр склада
        private readonly BestDelivery.Point depot = new BestDelivery.Point { X = 55.754, Y = 37.6201 };

        // Для панорамирования
        private System.Windows.Point lastMousePosition;
        private bool isPanning = false;

        // Параметры зума и смещения
        private double currentZoom = 1.0;
        private double offsetX = 0;
        private double offsetY = 0;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // Подписка на события мыши для канваса
            routeCanvas.MouseWheel += RouteCanvas_MouseWheel;
            routeCanvas.MouseLeftButtonDown += RouteCanvas_MouseLeftButtonDown;
            routeCanvas.MouseLeftButtonUp += RouteCanvas_MouseLeftButtonUp;
            routeCanvas.MouseMove += RouteCanvas_MouseMove;
            routeCanvas.MouseRightButtonUp += RouteCanvas_MouseRightButtonUp;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            pointsDataGrid.ItemsSource = Points;
            drawHelper = new Draw(routeCanvas);
            presetComboBox.SelectedIndex = 0;
            algorithmComboBox.SelectedIndex = 0;
            UpdateCanvas();
            

            routeCanvas.Clip = new RectangleGeometry(new Rect(0, 0, routeCanvas.ActualWidth, routeCanvas.ActualHeight));

            routeCanvas.SizeChanged += (s, e) =>
            {
                routeCanvas.Clip = new RectangleGeometry(new Rect(0, 0, routeCanvas.ActualWidth, routeCanvas.ActualHeight));
            };
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(txtX.Text, out double x) && double.TryParse(txtY.Text, out double y))
            {
                Points.Add(new OrderPoint { ID = nextId++, X = x, Y = y });
                txtX.Clear();
                txtY.Clear();

                UpdateCanvas(autoZoom: true);
            }
            else
            {
                MessageBox.Show("Введите корректные координаты X и Y.", "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            Points.Clear();
            nextId = 1;

            Order[] orders = presetComboBox.SelectedIndex switch
            {
                0 => OrderArrays.GetOrderArray1(),
                1 => OrderArrays.GetOrderArray2(),
                2 => OrderArrays.GetOrderArray3(),
                3 => OrderArrays.GetOrderArray4(),
                4 => OrderArrays.GetOrderArray5(),
                5 => OrderArrays.GetOrderArray6(),
                _ => null
            };

            if (orders != null)
            {
                foreach (var order in orders)
                {
                    Points.Add(new OrderPoint
                    {
                        ID = nextId++,
                        X = order.Destination.X,
                        Y = order.Destination.Y
                    });
                }

                drawHelper.DrawPointsOnCanvas(orders, depot);
                CalculateRoute(orders);
            }
        }

        private void CalculateRoute(Order[] orders = null)
        {
            if (Points.Count == 0)
            {
                totalCostLabel.Content = "Стоимость маршрута: -";
                routePathLabel.Content = "Маршрут: -";
                drawHelper.DrawPointsOnCanvas(new Order[0], depot); // Очистить или нарисовать пустой холст
                return;
            }

            try
            {
                if (orders == null)
                {
                    orders = Points.Select(p => new Order
                    {
                        ID = p.ID,
                        Destination = new BestDelivery.Point { X = p.X, Y = p.Y },
                        Priority = 1.0
                    }).ToArray();
                }

                string selectedAlgorithm = (algorithmComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Nearest Neighbor";

                var route = AlgorithClosedIndex.BuildRoute(depot, orders, selectedAlgorithm);

                if (RoutingTestLogic.TestRoutingSolution(depot, orders, route, out double routeCost))
                {
                    string routeText = string.Join(" → ", route);
                    totalCostLabel.Content = $"Стоимость маршрута: {routeCost:F2}";
                    routePathLabel.Content = $"Маршрут: {routeText}";

                    drawHelper.DrawRouteWithArrows(depot, orders, route);
                }
                else
                {
                    totalCostLabel.Content = "Ошибка маршрута.";
                    routePathLabel.Content = "Проверьте входные данные.";
                    drawHelper.DrawPointsOnCanvas(orders, depot); // Нарисовать только точки
                }
            }
            catch (Exception ex)
            {
                totalCostLabel.Content = "Ошибка маршрута.";
                routePathLabel.Content = ex.Message;
                drawHelper.DrawPointsOnCanvas(orders, depot); // Нарисовать только точки
            }
        }


        private void UpdateCanvas(bool autoZoom = false)
        {
            var orders = Points.Select(p => new Order
            {
                ID = p.ID,
                Destination = new BestDelivery.Point { X = p.X, Y = p.Y },
                Priority = 1.0
            }).ToArray();

            if (autoZoom)
            {
                var (zoom, offX, offY) = drawHelper.AutoZoomAndCenter(orders, depot);
                currentZoom = zoom;
                offsetX = offX;
                offsetY = offY;
            }

            drawHelper.SetZoomAndOffset(currentZoom, offsetX, offsetY);

            CalculateRoute(orders);
        }


        private void RouteCanvas_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var clickPosition = e.GetPosition(routeCanvas);
            var (xRaw, yRaw) = drawHelper.InverseTransformPoint(clickPosition.X, clickPosition.Y);

            Points.Add(new OrderPoint
            {
                ID = nextId++,
                X = Math.Round(xRaw, 6),
                Y = Math.Round(yRaw, 6)
            });

            UpdateCanvas(autoZoom: false);
        }


        private void RouteCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var pos = e.GetPosition(routeCanvas);

            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            double newZoom = currentZoom * zoomFactor;

            if (newZoom < 0.2) newZoom = 0.2;
            if (newZoom > 10) newZoom = 10;

            offsetX = (offsetX - pos.X) * (newZoom / currentZoom) + pos.X;
            offsetY = (offsetY - pos.Y) * (newZoom / currentZoom) + pos.Y;

            currentZoom = newZoom;

            UpdateCanvas();
        }

        private void RouteCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isPanning = true;
            lastMousePosition = e.GetPosition(this);
            routeCanvas.CaptureMouse();
        }

        private void RouteCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            isPanning = false;
            routeCanvas.ReleaseMouseCapture();
        }

        private void RouteCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (isPanning)
            {
                var pos = e.GetPosition(this);
                var delta = pos - lastMousePosition;
                lastMousePosition = pos;

                offsetX += delta.X;
                offsetY += delta.Y;

                UpdateCanvas();
            }
        }
    }
}
