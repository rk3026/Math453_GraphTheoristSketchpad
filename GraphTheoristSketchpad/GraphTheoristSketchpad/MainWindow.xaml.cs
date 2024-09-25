using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        private WpfPlot formsPlot1;
        public MainWindow()
        {
            InitializeComponent();
            formsPlot1 = Graph1;

            formsPlot1.Plot.Grid.IsVisible = false; // make the grid background invisible

            // Hide the axis ticks and labels (may want to use):
            AxisManager axis = formsPlot1.Plot.Axes;
            axis.Left.IsVisible = false;
            axis.Bottom.IsVisible = false;
            axis.Right.IsVisible = false;
            axis.Top.IsVisible = false;

            Scatter = Graph1.Plot.Add.Scatter(Xs, Ys);
            Scatter.LineWidth = 2;
            Scatter.MarkerSize = 10;
            Scatter.Smooth = true;

            formsPlot1.MouseMove += FormsPlot1_MouseMove;
            formsPlot1.MouseDown += FormsPlot1_MouseDown;
            formsPlot1.MouseUp += FormsPlot1_MouseUp;
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
            Pixel mousePixel = new Pixel(e.GetPosition(formsPlot1).X * dpiScale, e.GetPosition(formsPlot1).Y * dpiScale);

            Coordinates mouseLocation = formsPlot1.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, Graph1.Plot.LastRender);
            IndexBeingDragged = nearest.IsReal ? nearest.Index : null;

            if (IndexBeingDragged.HasValue)
                Graph1.Interaction.Disable();
        }

        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            IndexBeingDragged = null;
            formsPlot1.Interaction.Enable();
            formsPlot1.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            // Apply DPI scaling factor to mouse coordinates
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(formsPlot1).X * dpiScale, e.GetPosition(formsPlot1).Y * dpiScale);

            Coordinates mouseLocation = formsPlot1.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, formsPlot1.Plot.LastRender);
            formsPlot1.Cursor = nearest.IsReal ? Cursors.Hand : Cursors.Arrow;

            if (IndexBeingDragged.HasValue)
            {
                Xs[IndexBeingDragged.Value] = mouseLocation.X;
                Ys[IndexBeingDragged.Value] = mouseLocation.Y;
                formsPlot1.Refresh();
            }
        }
    }
}
