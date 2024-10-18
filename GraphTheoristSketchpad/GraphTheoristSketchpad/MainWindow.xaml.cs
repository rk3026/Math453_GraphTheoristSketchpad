using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Windows;
using System.Windows.Input;
using System.Collections;
using GraphTheoristSketchpad.Interface;
using System.Windows.Controls;
using GraphTheoristSketchpad.Logic;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        enum ToolMode { None, AddVertex, AddEdge, Erase, View,
            Edit
        }
        ToolMode currentMode = ToolMode.View;
        private List<Button> toolbarButtons;
        public GraphRendererPlot graphRendererPlot = new GraphRendererPlot();
        Vertex CurrentlyLeftClickedVertex = null;
        Vertex CurrentlyRightClickedVertex = null;

        public MainWindow()
        {
            InitializeComponent();
            toolbarButtons = new List<Button>
            {
                btnAddVertex,
                btnAddEdge,
                btnErase,
                btnView,
                btnEdit
                // Include any other buttons you want to manage
            };
            SetButtonSelected(btnView); // Select the view button

            GraphView.Plot.Grid.IsVisible = false;

            AxisManager axis = GraphView.Plot.Axes;
            axis.Left.IsVisible = false;
            axis.Bottom.IsVisible = false;
            axis.Right.IsVisible = false;
            axis.Top.IsVisible = false;

            GraphView.Plot.Add.Plottable(graphRendererPlot);

            GraphView.MouseMove += FormsPlot1_MouseMove; // Separate so each mode has its own function.
            GraphView.MouseLeftButtonDown += FormsPlot1_MouseLeftButtonDown;
            GraphView.MouseLeftButtonUp += FormsPlot1_MouseLeftButtonUp;
            GraphView.MouseRightButtonDown += FormsPlot1_MouseRightButtonDown;
        }

        private void FormsPlot1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);
            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, 1);
            //DataPoint nearest = graphRendererPlot.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);

            // Show context menu if right-clicked on a vertex
            if (nearestVertex != null)
            {
                CurrentlyRightClickedVertex = nearestVertex;
                // clear existing menu items
                GraphView.Menu?.Clear();

                // add menu items with custom actions
                GraphView.Menu?.Add("Rename", (graphView) =>
                {
                    //GraphView.KeyDown += renameVertex;
                    GraphView.TextInput += renameVertex;
                    graphView.Refresh();
                });

                GraphView.Menu?.Add("Change Color", (graphView) =>
                {
                    graphView.Refresh();
                });
                GraphView.Refresh();
            }
            else
            {
                // Set it back to normal right-click menu options:
                GraphView.Menu?.Reset();
            }
        }

        private void renameVertex(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "\b")
            {
                if (CurrentlyRightClickedVertex.Label.Length == 0)
                {
                    return;
                }
                CurrentlyRightClickedVertex.Label = CurrentlyRightClickedVertex.Label.Substring(0, CurrentlyRightClickedVertex.Label.Length - 1);
            }
            else if (e.Text == "\r")
            {
                GraphView.TextInput -= renameVertex;
            }
            else
            {
                CurrentlyRightClickedVertex.Label += e.Text;
            }
            GraphView.Refresh();
        }
        /*
        private void renameVertex(object sender, KeyEventArgs e)
        {
            int keyValue = (int)e.Key;
            // Check if the backspace key was pressed
            if (e.Key == Key.Back && CurrentlyRightClickedVertex.Label.Length > 0)
            {
                // Remove the last character from the label
                CurrentlyRightClickedVertex.Label = CurrentlyRightClickedVertex.Label.Substring(0, CurrentlyRightClickedVertex.Label.Length - 1);
                e.Handled = true;
            }
            // Check if the enter key was pressed
            else if (e.Key == Key.Enter)
            {
                // Stop renaming by detaching the event handler
                GraphView.KeyDown -= renameVertex;
                GraphView.TextInput -= renameVertex;
                e.Handled = true;
            }
            GraphView.Refresh();
        }
        */


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
            // Load the custom cursor from the Resources folder
            var cursorStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/67732.cur")).Stream;
            GraphView.Cursor = new Cursor(cursorStream);
            currentMode = ToolMode.Erase;
            SetButtonSelected(btnErase);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.View;
            SetButtonSelected(btnView);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            currentMode = ToolMode.Edit;
            SetButtonSelected(btnEdit);
        }

        private void FormsPlot1_MouseLeftButtonDown(object? sender, MouseEventArgs e)
        {
            // Dragging Vertices:
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);

            // Based on current mode, modify the graph:
            switch (currentMode)
            {
                case ToolMode.AddVertex:
                    Console.WriteLine("Adding a vertex at: " + mouseLocation.X + ", " + mouseLocation.Y);
                    AddVertex(mouseLocation.X, mouseLocation.Y);
                    break;
                case ToolMode.AddEdge:
                    Vertex? firstVertexIncident = graphRendererPlot.graph.getNearestVertex(mouseLocation, 1);
                    CurrentlyLeftClickedVertex = firstVertexIncident;
                    break;
                case ToolMode.Edit:
                    Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, 1);
                    if (nearestVertex != null)
                    {
                        CurrentlyLeftClickedVertex = nearestVertex;
                    }
                    break;
                case ToolMode.Erase:
                    Console.WriteLine("Deleting items at: " + mouseLocation.X + ", " + mouseLocation.Y);
                    DeleteVertexOrEdge(mouseLocation.X, mouseLocation.Y);
                    break;
                case ToolMode.View:
                    break;
                default:
                    break;

            }
            GraphView.Interaction.Disable();
        }

        private void DeleteVertexOrEdge(double x, double y)
        {
            Coordinates location = new Coordinates(x, y);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(location, 1);
            if (nearestVertex != null)
            {
                graphRendererPlot.graph.RemoveVertex(nearestVertex);
            }

            CoordinateLine? nearestEdge = graphRendererPlot.graph.getNearestEdge(location,1);
            if (nearestEdge != null)
            {
                graphRendererPlot.graph.RemoveEdge((CoordinateLine)nearestEdge);
            }
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseLeftButtonUp(object? sender, MouseEventArgs e)
        {
            // Dragging Vertices:
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            // Based on current mode, modify the graph:
            switch (currentMode)
            {
                case ToolMode.AddVertex:
                    break;
                case ToolMode.AddEdge:
                    Vertex? secondVertexIncident = graphRendererPlot.graph.getNearestVertex(mouseLocation, 1);
                    if (secondVertexIncident != null && CurrentlyLeftClickedVertex != null)
                    {
                        graphRendererPlot.graph.AddEdge(CurrentlyLeftClickedVertex, secondVertexIncident);
                    }
                    CurrentlyLeftClickedVertex = null;

                    break;
                case ToolMode.Edit:
                    CurrentlyLeftClickedVertex = null;
                    break;
                case ToolMode.Erase:
                    break;
                case ToolMode.View:
                    break;
                default:
                    break;

            }

            GraphView.Interaction.Enable();
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, 1);

            if (currentMode != ToolMode.Erase)
            {
                GraphView.Cursor = nearestVertex != null ? Cursors.Hand : Cursors.Arrow;
            }

            if (CurrentlyLeftClickedVertex != null && currentMode == ToolMode.Edit)
            {
                // Update the position of the vertex
                CurrentlyLeftClickedVertex.Location = mouseLocation;

                GraphView.Refresh();
            }
        }


        private void AddVertex(double x, double y)
        {
            Graph g = graphRendererPlot.graph;
            Vertex newVertex = new Vertex(x, y);
            newVertex.Style.FillColor = new Color(5, 5, 50, 255);
            newVertex.Label = "v" + g.Vertices.Count.ToString();
            g.Add(newVertex);
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
