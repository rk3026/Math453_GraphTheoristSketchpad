using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;
using GraphTheoristSketchpad.Interface;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        readonly double[] Xs = Generate.RandomAscending(10);
        readonly double[] Ys = Generate.RandomSample(10);
        readonly GraphRenderer graphRenderer;
        //Scatter graphRenderer;
        int? IndexBeingDragged = null;

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

            graphRenderer = new GraphRenderer(Xs, Ys);
            //graphRenderer.LineWidth = 2;
            //graphRenderer.MarkerSize = 10;
            //graphRenderer.Smooth = true;

            GraphView.Plot.Add.Plottable(graphRenderer);
            //graphRenderer = GraphView.Plot.Add.ScatterPoints(Xs, Ys);

            GraphView.MouseMove += FormsPlot1_MouseMove;
            GraphView.MouseDown += FormsPlot1_MouseDown;
            GraphView.MouseUp += FormsPlot1_MouseUp;
        }

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
            DataPoint nearest = graphRenderer.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
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
            DataPoint nearest = graphRenderer.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
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
