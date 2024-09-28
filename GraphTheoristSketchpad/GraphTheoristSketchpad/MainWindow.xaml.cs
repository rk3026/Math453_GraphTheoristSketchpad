using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            GraphView.Plot.Grid.IsVisible = false; // make the grid background invisible

            // Hide the axis ticks and labels (may want to use):
            AxisManager axis = GraphView.Plot.Axes;
            axis.Left.IsVisible = false;
            axis.Bottom.IsVisible = false;
            axis.Right.IsVisible = false;
            axis.Top.IsVisible = false;

            Scatter = GraphView.Plot.Add.Scatter(Xs, Ys);
            Scatter.LineWidth = 2;
            Scatter.MarkerSize = 10;
            Scatter.Smooth = true;

            GraphView.MouseMove += FormsPlot1_MouseMove;
            GraphView.MouseDown += FormsPlot1_MouseDown;
            GraphView.MouseUp += FormsPlot1_MouseUp;
        }

        readonly double[] Xs = Generate.RandomAscending(10);
        readonly double[] Ys = Generate.RandomSample(10);
        readonly ScottPlot.Plottables.Scatter Scatter;
        int? IndexBeingDragged = null;

        // Helper method to get DPI scaling factor
        private double GetDpiScale()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }

        private void FormsPlot1_MouseDown(object? sender, MouseEventArgs e)
        {
            // Apply DPI scaling factor to mouse coordinates
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
            IndexBeingDragged = nearest.IsReal ? nearest.Index : null;

            if (IndexBeingDragged.HasValue)
                GraphView.Interaction.Disable();
        }

        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            IndexBeingDragged = null;
            GraphView.Interaction.Enable();
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            // Apply DPI scaling factor to mouse coordinates
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
            GraphView.Cursor = nearest.IsReal ? Cursors.Hand : Cursors.Arrow;

            if (IndexBeingDragged.HasValue)
            {
                Xs[IndexBeingDragged.Value] = mouseLocation.X;
                Ys[IndexBeingDragged.Value] = mouseLocation.Y;
                GraphView.Refresh();
            }
        }
    }
}
