using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;
using System.Collections;
using GraphTheoristSketchpad.Interface;
using System.Windows.Controls;
using System.Windows.Media;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        private static double VERTEX_LABEL_OFFSET_Y = 0.1;
        enum ToolMode { None, AddVertex, AddEdge, Erase, View }
        ToolMode currentMode = ToolMode.View;
        private List<Button> toolbarButtons;
        private double[] Xs = Generate.RandomAscending(10);
        private double[] Ys = Generate.RandomSample(10);
        readonly GraphRenderer graphRenderer;
        int? IndexBeingDragged = null;
        private Dictionary<int, Text> vertexIndexAndLabels = new Dictionary<int, Text>();

        public MainWindow()
        {
            InitializeComponent();
            toolbarButtons = new List<Button>
            {
                btnAddVertex,
                btnAddEdge,
                btnErase,
                btnView // Include any other buttons you want to manage
            };
            SetButtonSelected(btnView); // Select the view button

            GraphView.Plot.Grid.IsVisible = false;

            AxisManager axis = GraphView.Plot.Axes;
            axis.Left.IsVisible = false;
            axis.Bottom.IsVisible = false;
            axis.Right.IsVisible = false;
            axis.Top.IsVisible = false;

            // Make adjacencyMatrix with numEdges edges.
            int numEdges = 7;
            BitArray adjacencyMatrix = new BitArray(Xs.Length * numEdges);
            for (int i = 0; i < numEdges; ++i)
            {
                int[] edge = Generate.RandomIntegers(2, Xs.Length);
                if (edge[0] == edge[1])
                {
                    edge[1] = (edge[1] + 1) % Xs.Length;
                }

                adjacencyMatrix[numEdges * edge[0] + i] = true;
                adjacencyMatrix[numEdges * edge[1] + i] = true;
            }

            for (int i = 0; i < Xs.Length; i++)
            {
                Coordinates c = new ScottPlot.Coordinates(Xs[i], Ys[i] + VERTEX_LABEL_OFFSET_Y);
                Text t = GraphView.Plot.Add.Text("v" + i, c);
                t.LabelFontSize = 25;
                t.LabelFontColor = Generate.RandomColor(128);
                t.LabelBold = true;
                vertexIndexAndLabels[i] = t;
            }

            graphRenderer = new GraphRenderer(Xs, Ys, adjacencyMatrix);
            GraphView.Plot.Add.Plottable(graphRenderer);

            GraphView.MouseMove += FormsPlot1_MouseMove;
            GraphView.MouseLeftButtonDown += FormsPlot1_MouseLeftButtonDown;
            GraphView.MouseLeftButtonUp += FormsPlot1_MouseLeftButtonUp;
            GraphView.MouseRightButtonDown += FormsPlot1_MouseRightButtonDown;
        }

        private void FormsPlot1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);
            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = graphRenderer.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);

            // Show context menu if right-clicked on a vertex
            if (nearest.IsReal)
            {
                // clear existing menu items
                GraphView.Menu?.Clear();

                // add menu items with custom actions
                GraphView.Menu?.Add("Rename", (GraphView) =>
                {
                    Text t = vertexIndexAndLabels[nearest.Index];
                    t.LabelText = "changed"; // TODO
                    GraphView.Refresh();
                });

                GraphView.Menu?.Add("Change Color", (GraphView) =>
                {
                    GraphView.Refresh();
                });
                GraphView.Refresh();
            }
            else
            {
                // Set it back to normal right-click menu options:
                GraphView.Menu?.Reset();
            }
        }

        private void SetButtonSelected(Button selectedButton)
        {
            // Loop through all buttons and reset their tags
            foreach (var button in toolbarButtons)
            {
                button.Tag = null; // Deselect each button
            }

            // Set the selected button's tag
            selectedButton.Tag = "Selected";
        }

        private void btnAddVertex_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.AddVertex;
            SetButtonSelected(btnAddVertex);
        }

        private void btnAddEdge_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.AddEdge;
            SetButtonSelected(btnAddEdge);
        }

        private void btnErase_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.Erase;
            SetButtonSelected(btnErase);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.View;
            SetButtonSelected(btnView);
        }

        private void FormsPlot1_MouseLeftButtonDown(object? sender, MouseEventArgs e)
        {
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = graphRenderer.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
            IndexBeingDragged = nearest.IsReal ? nearest.Index : null;

            // Handle vertex creation
            if (currentMode == ToolMode.AddVertex && !IndexBeingDragged.HasValue)
            {
                // Add a new vertex at the mouse location
                AddVertex(mouseLocation.X, mouseLocation.Y);
            }

            if (IndexBeingDragged.HasValue)
                GraphView.Interaction.Disable();
        }

        private void FormsPlot1_MouseLeftButtonUp(object? sender, MouseEventArgs e)
        {
            IndexBeingDragged = null;
            GraphView.Interaction.Enable();
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = graphRenderer.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);
            GraphView.Cursor = nearest.IsReal ? Cursors.Hand : Cursors.Arrow;

            if (IndexBeingDragged.HasValue)
            {
                // Update the position of the vertex
                Xs[IndexBeingDragged.Value] = mouseLocation.X;
                Ys[IndexBeingDragged.Value] = mouseLocation.Y;

                // Update the position of the associated label
                var labelToUpdate = vertexIndexAndLabels[IndexBeingDragged.Value];
                if (labelToUpdate != null)
                {
                    labelToUpdate.Location = new Coordinates(mouseLocation.X, mouseLocation.Y + VERTEX_LABEL_OFFSET_Y);
                }

                GraphView.Refresh();
            }
        }


        private void AddVertex(double x, double y)
        {
            /*
            // Add the new vertex to the Xs and Ys arrays
            Array.Resize(ref Xs, Xs.Length + 1);
            Array.Resize(ref Ys, Ys.Length + 1);
            Xs[^1] = x; // Last index
            Ys[^1] = y; // Last index

            // Create a new DataPoint for the vertex
            ScottPlot.Coordinates coords = new ScottPlot.Coordinates(x, y);
            var newVertex = new DataPoint();
            newVertex.Coordinates = coords;

            // Create a new label and tie it to the vertex
            var label = GraphView.Plot.Add.Text($"v{Xs.Length - 1}", new Coordinates(x, y));
            label.LabelFontSize = 10;
            label.LabelFontColor = Generate.RandomColor(128);
            label.LabelBold = true;

            // Associate the vertex with its label
            vertexIndexAndLabels[newVertex] = label;

            // Refresh the graph renderer
            GraphView.Refresh();
            */
        }


        // Helper method to get DPI scaling factor
        private double GetDpiScale()
        {
            PresentationSource source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }

        ////////////////////////// TOOLBAR STUFF ///////////////////////////////////////////////////
        private void MenuItem_New_Click(object sender, RoutedEventArgs e)
        {
            // Logic to create a new document
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            // Logic to open an existing document
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            // Logic to save the current document
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Close the application
        }

        private void MenuItem_About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("About this application...", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ////////////////////////// END OF TOOLBAR STUFF ///////////////////////////////////////////////////
        ///

    }
}
