using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;
using GraphTheoristSketchpad.Interface;
using System.Collections;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        readonly double[] Xs = Generate.RandomAscending(10);
        readonly double[] Ys = Generate.RandomSample(10);
        readonly GraphRenderer graphRenderer;
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

            // make adjacencyMatrix with numEdges edges.
            int numEdges = 7;
            BitArray adjacencyMatrix = new BitArray(Xs.Length*numEdges);
            for (int i = 0; i < numEdges; ++i)
            {
                // endpoint represented as two vertex indices
                int[] edge = Generate.RandomIntegers(2, Xs.Length);

                // make sure endpoints arent same vertex index.
                if (edge[0] == edge[1])
                {
                    edge[1] = (edge[1] + 1) % Xs.Length;
                }

                // add edge to matrix
                adjacencyMatrix[numEdges * edge[0] + i] = true;
                adjacencyMatrix[numEdges * edge[1] + i] = true;
            }

            graphRenderer = new GraphRenderer(Xs, Ys, adjacencyMatrix);
            //graphRenderer.LineWidth = 2;
            //graphRenderer.MarkerSize = 10;
            //graphRenderer.Smooth = true;

            GraphView.Plot.Add.Plottable(graphRenderer);

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
