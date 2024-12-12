using ScottPlot;
using System.Windows;
using System.Windows.Input;
using GraphTheoristSketchpad.Interface;
using System.Windows.Controls;
using GraphTheoristSketchpad.Logic;
using SkiaSharp.Views.WPF;

namespace GraphTheoristSketchpad
{
    public partial class MainWindow : Window
    {
        private bool isGraphFramed = false;
        private bool isDraggingInfoPanel = false;
        private Point clickPositionInfoPanel;
        enum ToolMode { 
            AddVertex, AddEdge, Erase, View, Edit
        }
        ToolMode currentMode = ToolMode.View;
        private List<Button> toolbarButtons;
        private GraphRendererPlot graphRendererPlot = new GraphRendererPlot();
        private Vertex CurrentlyLeftClickedVertex = null;
        private Vertex CurrentlyRightClickedVertex = null;
        private List<Vertex> selectedVertices = new List<Vertex>();

        // For the selection rectangle //
        Coordinates MouseDownCoordinates;
        Coordinates MouseNowCoordinates;
        Coordinates LastMouseLocation;
        CoordinateRect MouseSelectionRect => new(MouseDownCoordinates, MouseNowCoordinates);
        bool MouseIsDown = false;
        readonly ScottPlot.Plottables.Rectangle RectanglePlot;
        // End for the selection rect //

        public MainWindow()
        {
            InitializeComponent();

            InitializeColorPickers();

            // Toolbar setup:
            toolbarButtons = new List<Button>
            {
                btnAddVertex,
                btnAddEdge,
                btnErase,
                btnView,
                btnEdit
            };
            SetButtonSelected(btnView); // Select the view button as default

            // Axis and visual setup:
            GraphView.Plot.Axes.Title.Label.Text = "Graph";
            GraphView.Plot.Grid.IsVisible = false;
            AxisManager axis = GraphView.Plot.Axes;
            axis.Left.TickLabelStyle.IsVisible = false;
            axis.Bottom.TickLabelStyle.IsVisible = false;
            axis.Right.TickLabelStyle.IsVisible = false;
            axis.Top.TickLabelStyle.IsVisible = false;
            axis.Bottom.MinorTickStyle.Length = 0;
            axis.Bottom.MajorTickStyle.Length = 0;
            axis.Left.MinorTickStyle.Length = 0;
            axis.Left.MajorTickStyle.Length = 0;
            GraphView.Plot.Add.Plottable(graphRendererPlot);
            GraphView.Plot.Axes.SquareUnits();
            //GraphView.Plot.Axes.Frameless(false);
            this.GraphView.Plot.Axes.Title.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Left.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Bottom.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Top.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Right.IsVisible = this.isGraphFramed;

            // Subscribe to events:
            GraphView.MouseMove += FormsPlot1_MouseMove; // Separate so each mode has its own function.
            GraphView.MouseDown += FormsPlot1_MouseDown;
            GraphView.MouseUp += FormsPlot1_MouseUp;
            GraphView.MouseLeftButtonDown += FormsPlot1_MouseLeftButtonDown;
            GraphView.MouseLeftButtonUp += FormsPlot1_MouseLeftButtonUp;
            GraphView.MouseRightButtonDown += FormsPlot1_MouseRightButtonDown;
            graphRendererPlot.graph.GraphChanged += UpdateGraphInfoUI;

            RectanglePlot = GraphView.Plot.Add.Rectangle(0, 0, 0, 0);
            UpdateGraphInfoUI(null, null);
        }

        private void InitializeColorPickers()
        {
            NewVertexColorPicker.SelectedColor = graphRendererPlot.vertexPaint.Color.ToColor();
            EdgeColorPicker.SelectedColor = graphRendererPlot.edgePaint.Color.ToColor();
            BridgeColorPicker.SelectedColor = graphRendererPlot.bridgePaint.Color.ToColor();
            LinkColorPicker.SelectedColor = graphRendererPlot.linkPaint.Color.ToColor();
        }

        private void Separator_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                isDraggingInfoPanel = true;
                clickPositionInfoPanel = e.GetPosition(this);
                Mouse.Capture((Border)sender);
            }
        }

        private void Separator_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDraggingInfoPanel)
            {
                Point currentPosition = e.GetPosition(this);
                double offset = clickPositionInfoPanel.X-currentPosition.X;

                    // Resize the info panel
                    double newWidth = InfoPanel.Width + offset;
                    if (newWidth > 50) // Minimum width for the InfoPanel
                    {
                        InfoPanel.Width = newWidth;
                    }

                clickPositionInfoPanel = currentPosition;
            }
        }

        private void Separator_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isDraggingInfoPanel)
            {
                isDraggingInfoPanel = false;
                Mouse.Capture(null);
            }
        }


        private void UpdateGraphInfoUI(object? sender, EventArgs e)
        {
            graphRendererPlot.PerformKColoring();
            VertexCountLabel.Content = "Number of Vertices: " + this.graphRendererPlot.graph.Vertices.Count.ToString();
            EdgeCountLabel.Content = "Number of Edges: " + this.graphRendererPlot.graph.getEdgeCount().ToString();
            ComponentCountLabel.Content = "Number of Components: " + graphRendererPlot.graph.GetComponentCount().ToString();
            BipartiteLabel.Content = "Is Bipartite?: " + graphRendererPlot.graph.IsBipartite().ToString();
            IncidenceMatrixDataGrid.ItemsSource = this.graphRendererPlot.graph.GetIncidenceMatrixTable().DefaultView;
            MinimumColorLabel.Content = "Chromatic Number: " + this.graphRendererPlot.graph.getChromaticNumber().ToString();
            //ChromaticPolynomialLabel.Content = "Chromatic Polynomial: " + this.graphRendererPlot.GetCurrentChromaticPolynomial().ToString();

            // Handle Chromatic Coloring Error visibility
            if (graphRendererPlot.KColoringSuccessful)
            {
                ChromaticColoringError.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChromaticColoringError.Visibility = Visibility.Visible;
            }

            // Update the algorithm selectors
            UpdateAlgorithmSelectors();
            RunAlgorithmButton.IsChecked = false;

            // Refresh the graph view
            GraphView.Refresh();
        }

        private void UpdateAlgorithmSelectors()
        {
            // Clear the current items in all selectors
            DijkstraStartNodeSelector.Items.Clear();
            DijkstraEndNodeSelector.Items.Clear();
            FordSourceNodeSelector.Items.Clear();
            FordSinkNodeSelector.Items.Clear();
            SpanningTreeRootNodeSelector.Items.Clear();
            CartesianProduct1stComponentSelector.Items.Clear();
            CartesianProduct2ndComponentSelector.Items.Clear();

            // Get the list of vertices from the graph
            List<Vertex> vertices = graphRendererPlot.graph.Vertices.ToList();

            SpanningTreeRootNodeSelector.Items.Add(new ComboBoxItem { Content = "None" });
            // Populate the selectors with the updated vertices
            foreach (var vertex in vertices)
            {
                // Create ComboBoxItems with the vertex as the underlying data
                var startItem = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                DijkstraStartNodeSelector.Items.Add(startItem);

                var endItem = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                DijkstraEndNodeSelector.Items.Add(endItem);

                var sourceItem = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                FordSourceNodeSelector.Items.Add(sourceItem);

                var sinkItem = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                FordSinkNodeSelector.Items.Add(sinkItem);

                var rootItem = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                SpanningTreeRootNodeSelector.Items.Add(rootItem);

                var component1Item = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                CartesianProduct1stComponentSelector.Items.Add(component1Item);

                var component2Item = new ComboBoxItem { Content = vertex.Label, Tag = vertex };
                CartesianProduct2ndComponentSelector.Items.Add(component2Item);
            }
        }

        // Event handler for ComboBox selection change
        private void OnComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RunAlgorithmButton.IsChecked = false;
            var comboBox = sender as ComboBox;
            if (comboBox == null || comboBox.SelectedItem == null) return;

            // Retrieve the selected ComboBoxItem and its associated Vertex
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            var vertex = selectedItem?.Tag as Vertex;
            if (vertex == null) return;

            // Highlight the selected vertex
            HighlightVertex(vertex);
        }

        private void HighlightVertex(Vertex vertex)
        {
            // Clear previous markers
            GraphView.Plot.Remove<ScottPlot.Plottables.Marker>();

            var newMarker = GraphView.Plot.Add.Marker(vertex.Location);
            newMarker.MarkerStyle.Shape = MarkerShape.FilledCircle;
            newMarker.MarkerStyle.Size = 30;
            newMarker.MarkerStyle.FillColor = ScottPlot.Colors.Blue.WithOpacity(0.4); // Selected color
            newMarker.MarkerStyle.LineColor = ScottPlot.Colors.Blue; // Outline color
            newMarker.MarkerStyle.LineWidth = 1;

            GraphView.Refresh(); // Refresh to display the updated visuals
        }


        private void AlgorithmList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Collapse all inputs by default
            DijkstraInputs.Visibility = Visibility.Collapsed;
            FordFulkersonInputs.Visibility = Visibility.Collapsed;
            SpanningTreeInputs.Visibility = Visibility.Collapsed;
            CartesianProductInputs.Visibility = Visibility.Collapsed;
            RunAlgorithmButton.IsChecked = false;

            // Get the selected algorithm from the ComboBox
            var selectedAlgorithm = (AlgorithmSelector.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (selectedAlgorithm == null) return;

            // Populate selectors based on the selected algorithm
            switch (selectedAlgorithm)
            {
                case "Dijkstra's Algorithm":
                    DijkstraInputs.Visibility = Visibility.Visible;
                    break;
                case "Ford-Fulkerson (Max Flow)":
                    FordFulkersonInputs.Visibility = Visibility.Visible;
                    break;
                case "Minimum Spanning Tree":
                    SpanningTreeInputs.Visibility = Visibility.Visible;
                    break;
                case "Cartesian Product":
                    CartesianProductInputs.Visibility = Visibility.Visible;
                    break;
            }
        }


        private void OnRunAlgorithmChecked(object sender, RoutedEventArgs e)
        {
            var selectedAlgorithm = (AlgorithmSelector.SelectedItem as ListBoxItem)?.Content.ToString();
            switch (selectedAlgorithm)
            {
                case "Dijkstra's Algorithm":

                    var dijkstraStartItem = DijkstraStartNodeSelector.SelectedItem as ComboBoxItem;
                    var dijkstraEndItem = DijkstraEndNodeSelector.SelectedItem as ComboBoxItem;

                    // Get the actual Vertex objects
                    var dijkstraStartVertex = dijkstraStartItem?.Tag as Vertex;
                    var dijkstraEndVertex = dijkstraEndItem?.Tag as Vertex;

                    if (dijkstraStartVertex != null && dijkstraEndVertex != null)
                    {
                        List<KeyValuePair<Vertex,Vertex>>? dijkstraPath = graphRendererPlot.graph.getShortestPath(dijkstraStartVertex, dijkstraEndVertex);
                        graphRendererPlot.DijkstraPath.Clear();
                        foreach(KeyValuePair<Vertex,Vertex> vertexPair in dijkstraPath)
                        {
                            graphRendererPlot.DijkstraPath.Add(vertexPair);
                        }
                    }
                    break;

                case "Ford-Fulkerson (Max Flow)":
                    var fordSourceItem = FordSourceNodeSelector.SelectedItem as ComboBoxItem;
                    var fordSinkItem = FordSinkNodeSelector.SelectedItem as ComboBoxItem;

                    // Get the actual Vertex objects
                    var fordSourceVertex = fordSourceItem?.Tag as Vertex;
                    var fordSinkVertex = fordSinkItem?.Tag as Vertex;

                    if (fordSourceVertex != null && fordSinkVertex != null)
                    {
                        FordFulkersonMaxFlowLabel.Content = "Max Flow from " + fordSourceVertex.Label + " to " + fordSinkVertex.Label + ": " + graphRendererPlot.graph.GetMaxFlow(fordSourceVertex, fordSinkVertex);
                    }
                    break;

                case "Minimum Spanning Tree":
                    var spanningTreeRootItem = SpanningTreeRootNodeSelector.SelectedItem as ComboBoxItem;
                    var spanningTreeRootVertex = spanningTreeRootItem?.Tag as Vertex;

                    if (spanningTreeRootVertex != null)
                    {
                        List<KeyValuePair<Vertex, Vertex>>? spanningTree = graphRendererPlot.graph.GetSpanningTreeWithRoot(spanningTreeRootVertex);
                        graphRendererPlot.DijkstraPath.Clear();
                        foreach (KeyValuePair<Vertex, Vertex> vertexPair in spanningTree)
                        {
                            graphRendererPlot.DijkstraPath.Add(vertexPair);
                        }
                    }
                    else
                    {
                        List<KeyValuePair<Vertex, Vertex>>? spanningTree = graphRendererPlot.graph.GetSpanningTree();
                        graphRendererPlot.DijkstraPath.Clear();
                        foreach (KeyValuePair<Vertex, Vertex> vertexPair in spanningTree)
                        {
                            graphRendererPlot.DijkstraPath.Add(vertexPair);
                        }
                    }
                    break;
                case "Cartesian Product":
                    var cartesianStartItem = CartesianProduct1stComponentSelector.SelectedItem as ComboBoxItem;
                    var cartesianEndItem = CartesianProduct2ndComponentSelector.SelectedItem as ComboBoxItem;

                    // Get the actual Vertex objects
                    var cartesian1stComponentVertex = cartesianStartItem?.Tag as Vertex;
                    var cartesian2ndComponentVertex = cartesianEndItem?.Tag as Vertex;

                    graphRendererPlot.graph.CartesianProduct(cartesian1stComponentVertex, cartesian2ndComponentVertex);

                    break;
                default:
                    break;
            }
            GraphView.Refresh();

        }

        private void OnRunAlgorithmUnchecked(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.DijkstraPath.Clear();
            GraphView.Refresh();
        }




        private void UpdateSelectionMarkers()
        {
            // Clear previous markers
            GraphView.Plot.Remove<ScottPlot.Plottables.Marker>();

            // Add markers for selected vertices
            foreach (Vertex vertex in selectedVertices)
            {
                var newMarker = GraphView.Plot.Add.Marker(vertex.Location);
                newMarker.MarkerStyle.Shape = MarkerShape.FilledCircle;
                newMarker.MarkerStyle.Size = 30;
                newMarker.MarkerStyle.FillColor = ScottPlot.Colors.Blue.WithOpacity(0.4); // Selected color
                newMarker.MarkerStyle.LineColor = ScottPlot.Colors.Blue; // Outline color
                newMarker.MarkerStyle.LineWidth = 1;
            }
            GraphView.Refresh(); // Refresh to display the updated visuals
        }

        // ONLY for the edit mode:
        private void FormsPlot1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton  == MouseButtonState.Pressed) { return; } // Don't care about right clicking, shouldn't remove selection.

            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            if (currentMode != ToolMode.Edit) {
                selectedVertices.Clear();
                UpdateSelectionMarkers();
                return;
            }
            else if (isShiftPressed)
            {
                return;
            }
            MouseIsDown = true;
            if (CurrentlyLeftClickedVertex != null)
            {
                return;
            }
            else
            {
                selectedVertices.Clear();
            }
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);
            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            MouseDownCoordinates = mouseLocation;
            //GraphView.UserInputProcessor.IsEnabled = false; // re-enable the default click-drag-pan behavior
        }

        // ONLY for the edit mode:
        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            if (currentMode != ToolMode.Edit) return;
            
            MouseIsDown = false;
            RectanglePlot.IsVisible = false;

            // identify selectedPoints
            List<Vertex> selectedPoints = graphRendererPlot.getVerticesInRect(MouseSelectionRect);
            foreach (Vertex vertex in selectedPoints)
            {
                selectedVertices.Add(vertex);
            }

            UpdateSelectionMarkers();

            // reset the mouse positions
            MouseDownCoordinates = Coordinates.NaN;
            MouseNowCoordinates = Coordinates.NaN;

            // update the plot
            GraphView.Refresh();
            //GraphView.UserInputProcessor.IsEnabled = true; // re-enable the default click-drag-pan behavior
        }

        private void FormsPlot1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // length of 15 pixels in coordinate units
            double vectorSelectDist = Math.Abs(graphRendererPlot.Axes.GetCoordinateX(0) - graphRendererPlot.Axes.GetCoordinateX(15));
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);
            Coordinates mouseLocation = graphRendererPlot.Axes.GetCoordinates(mousePixel);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, vectorSelectDist);
            //DataPoint nearest = graphRendererPlot.Data.GetNearest(mouseLocation, GraphView.Plot.LastRender);

            // Show context menu if right-clicked on a vertex
            if (nearestVertex != null)
            {
                CurrentlyRightClickedVertex = nearestVertex;
                // clear existing menu items
                GraphView.Menu?.Clear();

                // Add menu items with custom actions
                GraphView.Menu?.Add("Rename", (graphView) =>
                {
                    // Unsubscribe from any existing renameVertex handlers
                    GraphView.TextInput -= renameVertex;

                    // Subscribe to the renameVertex event
                    GraphView.TextInput += renameVertex;

                    // Refresh the graph view
                    graphView.Refresh();
                });

                GraphView.Menu?.Add("Change Color", (graphView) =>
                {
                    var vertexPosition = CurrentlyRightClickedVertex.Location;
                    ColorPickerPopup.IsOpen = true;
                    ScottPlot.Color c = CurrentlyRightClickedVertex.Style.FillColor;
                    System.Windows.Media.Color color = new System.Windows.Media.Color();
                    color.R = c.R;
                    color.G = c.G;
                    color.B = c.B;
                    color.A = c.A;
                    VertexColorPicker.SelectedColor = color;
                    // Handle the color selection event
                    VertexColorPicker.SelectedColorChanged += (sender, args) =>
                    {
                        // Get the selected color
                        var selectedColor = VertexColorPicker.SelectedColor;

                        // Apply the selected color to the vertex
                        if (selectedColor.HasValue)
                        {
                            foreach(Vertex v in selectedVertices)
                            {
                                changeVertexColor(selectedColor.Value, v);
                            }
                            if (selectedVertices.Count == 0)
                            {
                                changeVertexColor(selectedColor.Value, CurrentlyRightClickedVertex);
                            }
                        }
                        graphView.Refresh();
                    };
                });
                GraphView.Refresh();
            }
            else
            {
                // Set it back to normal right-click menu options:
                GraphView.Menu?.Reset();
            }
        }

        private void changeVertexColor(System.Windows.Media.Color c, Vertex v)
        {
            ScottPlot.Color newC = new ScottPlot.Color(c.R, c.G, c.B, c.A);
            v.Style.FillColor = newC;
        }

        private void renameVertex(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == "\b")
            {
                foreach (Vertex v in selectedVertices)
                {
                    if (v.Label.Length > 0)
                    {
                        v.Label = v.Label.Substring(0, v.Label.Length - 1);
                    }
                }
                if (selectedVertices.Count == 0 && CurrentlyRightClickedVertex.Label.Length > 0)
                {
                    CurrentlyRightClickedVertex.Label = CurrentlyRightClickedVertex.Label.Substring(0, CurrentlyRightClickedVertex.Label.Length - 1);
                }
            }
            else if (e.Text == "\r")
            {
                GraphView.TextInput -= renameVertex;
            }
            else
            {
                foreach (Vertex v in selectedVertices)
                {
                    v.Label += e.Text;
                }
                if (selectedVertices.Count == 0)
                {
                    CurrentlyRightClickedVertex.Label += e.Text;
                }
            }
            UpdateGraphInfoUI(this, new EventArgs());
            GraphView.Refresh();
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
            // Load the custom cursor from the Resources folder
            var cursorStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/eraser.cur")).Stream;
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

        private void ToggleInfoPanelButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the button is toggled on or off and set the visibility accordingly.
            if (ToggleInfoPanelButton.IsChecked == true)
            {
                // Set the panel visibility to visible
                InfoPanel.Visibility = Visibility.Visible;
            }
            else
            {
                // Set the panel visibility to collapsed
                InfoPanel.Visibility = Visibility.Collapsed;
            }
        }


        private void btnToggleDirected_Checked(object sender, RoutedEventArgs e)
        {
            // Convert to a directed graph
            graphRendererPlot.graph.IsDirected = true;
            UpdateGraphInfoUI(null, null!);
            GraphView.Refresh();
        }
        private void btnToggleDirected_Unchecked(object sender, RoutedEventArgs e)
        {
            // Convert from directed to undirected
            graphRendererPlot.graph.IsDirected = false;
            UpdateGraphInfoUI(null, null!);
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseLeftButtonDown(object? sender, MouseEventArgs e)
        {
            // length of 15 pixels in coordinate units
            double vectorSelectDist = Math.Abs(graphRendererPlot.Axes.GetCoordinateX(0) - graphRendererPlot.Axes.GetCoordinateX(15));
            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);
            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);

            // Check if Shift is pressed
            bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

            // Based on current mode, modify the graph:
            switch (currentMode)
            {
                case ToolMode.AddVertex:
                    Console.WriteLine("Adding a vertex at: " + mouseLocation.X + ", " + mouseLocation.Y);
                    AddVertex(mouseLocation.X, mouseLocation.Y);
                    break;

                case ToolMode.AddEdge:
                    Vertex? firstVertexIncident = graphRendererPlot.graph.getNearestVertex(mouseLocation, vectorSelectDist);
                    CurrentlyLeftClickedVertex = firstVertexIncident;
                    break;

                case ToolMode.Edit:
                    Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, vectorSelectDist);
                    if (nearestVertex != null)
                    {
                        if (isShiftPressed)
                        {
                            // Shift clicked, toggle selection
                            if (selectedVertices.Contains(nearestVertex))
                            {
                                selectedVertices.Remove(nearestVertex); // Remove from selection
                            }
                            else
                            {
                                selectedVertices.Add(nearestVertex); // Add to selection
                            }
                            GraphView.Refresh(); // Refresh to update vertex appearances
                            UpdateSelectionMarkers();
                        }
                        else
                        {
                            CurrentlyLeftClickedVertex = nearestVertex; // Normal click behavior
                        }
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
            // length of 15 pixels in coordinate units
            double vectorSelectDist = Math.Abs(graphRendererPlot.Axes.GetCoordinateX(0) - graphRendererPlot.Axes.GetCoordinateX(15));
            Coordinates location = new Coordinates(x, y);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(location, vectorSelectDist);
            if (nearestVertex != null)
            {
                graphRendererPlot.graph.RemoveVertex(nearestVertex);
            }

            CoordinateLine? nearestEdge = graphRendererPlot.graph.getNearestEdge(location, vectorSelectDist);
            if (nearestEdge != null)
            {
                graphRendererPlot.graph.RemoveEdge((CoordinateLine)nearestEdge);
            }
            GraphView.Refresh();
        }

        private void FormsPlot1_MouseLeftButtonUp(object? sender, MouseEventArgs e)
        {
            // length of 15 pixels in coordinate units
            double vectorSelectDist = Math.Abs(graphRendererPlot.Axes.GetCoordinateX(0) - graphRendererPlot.Axes.GetCoordinateX(15));

            graphRendererPlot.temporaryLine = null;

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
                    Vertex? secondVertexIncident = graphRendererPlot.graph.getNearestVertex(mouseLocation, vectorSelectDist);
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
            // length of 15 pixels in coordinate units
            double vectorSelectDist = Math.Abs(graphRendererPlot.Axes.GetCoordinateX(0) - graphRendererPlot.Axes.GetCoordinateX(15));

            double dpiScale = GetDpiScale();
            Pixel mousePixel = new Pixel(e.GetPosition(GraphView).X * dpiScale, e.GetPosition(GraphView).Y * dpiScale);

            Coordinates mouseLocation = GraphView.Plot.GetCoordinates(mousePixel);
            Vertex? nearestVertex = graphRendererPlot.graph.getNearestVertex(mouseLocation, vectorSelectDist);

            // Change cursor based on mode and nearest vertex
            if (currentMode != ToolMode.Erase)
            {
                GraphView.Cursor = nearestVertex != null ? Cursors.Hand : Cursors.Arrow;
            }

            // If in Erase mode and the left mouse button is pressed, erase items
            if (currentMode == ToolMode.Erase && e.LeftButton == MouseButtonState.Pressed)
            {
                DeleteVertexOrEdge(mouseLocation.X, mouseLocation.Y);
            }

            // OLD CODE FOR MOVING A SINGLE VERTEX
            // If in Edit mode and a vertex is being dragged, update its position
            if (CurrentlyLeftClickedVertex != null && currentMode == ToolMode.Edit)
            {
                CurrentlyLeftClickedVertex.Location = mouseLocation;
                GraphView.Refresh();
            }

            if (CurrentlyLeftClickedVertex != null && currentMode == ToolMode.AddEdge)
            {
                Coordinates mouseCoords = new Coordinates(mouseLocation.X, mouseLocation.Y);
                CoordinateLine line = new CoordinateLine(CurrentlyLeftClickedVertex.Location, mouseCoords);
                graphRendererPlot.temporaryLine = line;
                GraphView.Refresh();
            }

            if (currentMode == ToolMode.Edit && MouseIsDown)
            {
                if (CurrentlyLeftClickedVertex != null && !selectedVertices.Contains(CurrentlyLeftClickedVertex))
                {
                    selectedVertices.Clear();
                    return;
                }
                RectanglePlot.IsVisible = true;
                Coordinates delta = new Coordinates(mouseLocation.X - LastMouseLocation.X, mouseLocation.Y - LastMouseLocation.Y);
                foreach (Vertex v in selectedVertices)
                {
                    v.Location = new Coordinates(delta.X + v.Location.X, delta.Y+v.Location.Y);

                }
                MouseNowCoordinates = mouseLocation;
                RectanglePlot.CoordinateRect = MouseSelectionRect;
                GraphView.Refresh();
                UpdateSelectionMarkers();
            }
            LastMouseLocation = mouseLocation;
        }



        private void AddVertex(double x, double y)
        {
            Graph g = graphRendererPlot.graph;
            Vertex newVertex = new Vertex(x, y);
            newVertex.Style.FillColor = new ScottPlot.Color(graphRendererPlot.vertexPaint.Color.Red, graphRendererPlot.vertexPaint.Color.Green, graphRendererPlot.vertexPaint.Color.Blue, graphRendererPlot.vertexPaint.Color.Alpha);
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
            System.Windows.MessageBox.Show("About this application...", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DisplayBridgesLinksCheckbox_Click(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.IsDisplayingBridgesAndLinks = !graphRendererPlot.IsDisplayingBridgesAndLinks;
            GraphView.Refresh();
        }

        private void DisplayVertexDegreeCheckbox_Click(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.IsDisplayingVertexDegree = !graphRendererPlot.IsDisplayingVertexDegree;
            GraphView.Refresh();
        }

        public void DisplayBipartiteSetsCheckbox_Click(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.IsDisplayingBipartiteSets = !graphRendererPlot.IsDisplayingBipartiteSets;
            GraphView.Refresh();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.Clear();
            UpdateSelectionMarkers();
            UpdateGraphInfoUI(null, null);
            GraphView.Refresh();
        }

        private void ToggleFramedGraphButton_Click(Object sender, RoutedEventArgs e)
        {
            this.isGraphFramed = !this.isGraphFramed;
            // this.GraphView.Plot.Axes.Frameless(this.isGraphFramed);
            this.GraphView.Plot.Axes.Title.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Left.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Bottom.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Top.IsVisible = this.isGraphFramed;
            this.GraphView.Plot.Axes.Right.IsVisible = this.isGraphFramed;
            this.GraphView.Refresh();
        }

        private void ColorGraphCheckbox_Click(object sender, RoutedEventArgs e)
        {
            graphRendererPlot.IsKColoring = !graphRendererPlot.IsKColoring;
            UpdateGraphInfoUI(this, null);
            GraphView.Refresh();
        }

        private void KColoringTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            graphRendererPlot.PerformKColoring();
            if (string.IsNullOrWhiteSpace(KColoringTextbox.Text))
            {
                // Handle empty input by resetting or setting to a default value
                graphRendererPlot.KColoringNumber = 0; // Default value, adjust as needed
                GraphView.Refresh();
                return;
            }

            // Attempt to parse the input
            if (int.TryParse(KColoringTextbox.Text, out int newValue))
            {
                // Update the graph's K-coloring number
                graphRendererPlot.KColoringNumber = newValue;
            }
            else
            {
                // Revert to the last valid value
                KColoringTextbox.Text = graphRendererPlot.KColoringNumber.ToString();
                KColoringTextbox.CaretIndex = KColoringTextbox.Text.Length; // Ensure cursor stays at the end
            }

            UpdateGraphInfoUI(this, null);
            GraphView.Refresh();
        }

        private void RefitButton_Click(Object sender, RoutedEventArgs e)
        {
            graphRendererPlot.Refit();
            this.GraphView.Refresh();
        }


        private void NumericOnlyInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); // Allow only numeric input
        }

        private void NewVertexColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            graphRendererPlot.vertexPaint.Color = new SkiaSharp.SKColor(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B, e.NewValue.Value.A);

            // Update each vertex's color on the graph (possibly remove this?)
            foreach (Vertex v in graphRendererPlot.graph.Vertices)
            {
                v.Style.FillColor = new ScottPlot.Color(graphRendererPlot.vertexPaint.Color.Red, graphRendererPlot.vertexPaint.Color.Green, graphRendererPlot.vertexPaint.Color.Blue, graphRendererPlot.vertexPaint.Color.Alpha);
            }

            GraphView.Refresh();
        }

        private void EdgeColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            graphRendererPlot.edgePaint.Color = new SkiaSharp.SKColor(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B, e.NewValue.Value.A);
            GraphView.Refresh();
        }

        private void BridgeColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            graphRendererPlot.bridgePaint.Color = new SkiaSharp.SKColor(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B, e.NewValue.Value.A);
            GraphView.Refresh();
        }

        private void LinkColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            graphRendererPlot.linkPaint.Color = new SkiaSharp.SKColor(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B, e.NewValue.Value.A);
            GraphView.Refresh();
        }

        private void GraphTitleTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            GraphView.Plot.Axes.Title.Label.Text = GraphTitleTextbox.Text;
            GraphView.Refresh();
        }

        ////////////////////////// END OF TOOLBAR STUFF ///////////////////////////////////////////////////
        ///

    }
}
