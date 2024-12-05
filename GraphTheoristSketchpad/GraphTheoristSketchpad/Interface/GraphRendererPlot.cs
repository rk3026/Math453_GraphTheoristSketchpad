using GraphTheoristSketchpad.Logic;
using ScottPlot;
using SkiaSharp;

namespace GraphTheoristSketchpad.Interface
{
    // This class can be assigned to a scottplot plot, which allows for creation of graphs.
    public class GraphRendererPlot : IPlottable
    {
        public Graph graph = new Graph();
        public CoordinateLine? temporaryLine = null;
        IColormap Colormap { get; set; } = new ScottPlot.Colormaps.Turbo();
        // items required by IPlottable
        public bool IsVisible { get; set; } = true;
        public bool IsDisplayingBridgesAndLinks { get; set; } = false;
        public bool IsDisplayingVertexDegree { get; set; } = false;
        public bool IsDisplayingBipartiteSets { get; set; } = false;
        public bool IsKColoring { get; set; } = false;
        public int KColoringNumber { get; set; } = 0;
        public IAxes Axes { get; set; } = new Axes();
        public IEnumerable<LegendItem> LegendItems => LegendItem.None;
        public AxisLimits GetAxisLimits() => AxisLimits.Default;

        public SKPaint textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 20,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            BlendMode = SKBlendMode.Multiply
        };

        public SKPaint vertexPaint = new SKPaint
        {
            Color = new SKColor(0,0,0),
            Style = SKPaintStyle.Stroke,
        };

        public SKPaint edgePaint = new SKPaint
        {
            Color = SKColors.Red.WithAlpha(130),
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
        };

        public SKPaint bridgePaint = new SKPaint
        {
            Color = SKColors.Blue.WithAlpha(130),
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
        };

        public SKPaint linkPaint = new SKPaint
        {
            Color = SKColors.Red.WithAlpha(130),
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
        };

        public SKPaint degreeLabelPaint = new SKPaint
        {
            Color = SKColors.DarkGreen,
            StrokeWidth = 1.5f,
            TextSize = 17,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Square,
            Typeface = SKTypeface.FromFamilyName("Arial"),
            Style = SKPaintStyle.StrokeAndFill,
        };

        public GraphRendererPlot()
        {
        }

        public void Render(RenderPack rp)
        {
            DrawEdges(rp);
            PerformKColoring();
            DrawVerticesAndLabels(rp);
            DrawTemporaryLine(rp);
        }

        private void PerformKColoring()
        {
            if (!IsKColoring)
            {
                foreach (Vertex v in graph.Vertices)
                {
                    v.Style.FillColor = new ScottPlot.Color(vertexPaint.Color.Red, vertexPaint.Color.Green, vertexPaint.Color.Blue, vertexPaint.Color.Alpha);
                }
                return;
            }

            Dictionary<Vertex, int> colorings = graph.colorableBy(KColoringNumber);
            if (colorings == null) return;

            foreach (Vertex v in colorings.Keys)
            {
                int colorIndex = colorings[v];
                ScottPlot.Color vertexColor = GenerateColor(colorIndex, KColoringNumber); // Generate a unique color based on the index
                v.Style.FillColor = vertexColor;
            }
        }

        private ScottPlot.Color GenerateColor(int index, int numColors)
        {
            float hue = (float)index / (float)numColors;
            return ScottPlot.Color.FromHSL(hue, 1f, 0.5f);
        }

        private void DrawEdges(RenderPack rp)
        {
            FillStyle FillStyle = new();
            SKPaint edgeP = edgePaint;
            SKPaint linkP = linkPaint;
            SKPaint bridgeP = bridgePaint;

            // Get all edges
            CoordinateLine[] edges = graph.getEdges();

            // Dictionary to store edges and their counts
            Dictionary<CoordinateLine, int> sameEdges = new Dictionary<CoordinateLine, int>();
            foreach (CoordinateLine edge in edges)
            {
                if (sameEdges.TryGetValue(edge, out int edgeCount))
                {
                    sameEdges[edge] = edgeCount + 1;
                }
                else
                {
                    sameEdges.Add(edge, 1);
                }
            }

            // Check if bridges should be displayed
            HashSet<CoordinateLine> bridges = IsDisplayingBridgesAndLinks
                ? new HashSet<CoordinateLine>(graph.GetBridges())
                : new HashSet<CoordinateLine>();

            // Draw edges
            foreach (CoordinateLine edge in sameEdges.Keys)
            {
                PixelLine pixelEdge = Axes.GetPixelLine(edge);

                SKPaint currentPaint;
                if (IsDisplayingBridgesAndLinks)
                {
                    currentPaint = bridges.Contains(edge) ? bridgeP : linkP;
                }
                else
                {
                    currentPaint = edgeP;
                }

                // Edge is a loop
                if (edge.Start.Equals(edge.End))
                {
                    // Same logic as original for loops
                    CoordinateLine[] incidentEdges = graph.getEdgesOn(graph.getNearestVertex(edge.Start)!);

                    List<KeyValuePair<CoordinateLine, double>> edgeAngles = new();
                    foreach (CoordinateLine incidentEdge in incidentEdges)
                    {
                        if (!incidentEdge.Equals(edge))
                        {
                            double deltaY = incidentEdge.End.Y - incidentEdge.Start.Y;
                            double deltaX = incidentEdge.End.X - incidentEdge.Start.X;
                            double edgeAngle = Math.Atan2(deltaY, deltaX);
                            edgeAngles.Add(new KeyValuePair<CoordinateLine, double>(incidentEdge, edgeAngle));
                        }
                    }

                    Pixel direction = new Pixel(1, 0);
                    if (edgeAngles.Count != 0)
                    {
                        edgeAngles.Sort((x, y) => x.Value < y.Value ? -1 : 1);

                        double largestEdgeAngle = edgeAngles.First().Value;
                        double largestAngle = 0;
                        double lastAngle = edgeAngles.Last().Value - 2 * Math.PI;

                        foreach (KeyValuePair<CoordinateLine, double> edgeAngle in edgeAngles)
                        {
                            double angle = edgeAngle.Value - lastAngle;
                            if (angle > largestAngle)
                            {
                                largestEdgeAngle = edgeAngle.Value;
                                largestAngle = angle;
                            }
                            lastAngle = edgeAngle.Value;
                        }

                        double directionAngle = largestEdgeAngle - largestAngle / 2;
                        direction = new Pixel(Math.Cos(directionAngle), -Math.Sin(directionAngle));
                    }

                    for (int i = 1; i <= sameEdges[edge]; ++i)
                    {
                        int radius = i * 10 + 10;
                        rp.Canvas.DrawCircle(pixelEdge.X1 + direction.X * radius, pixelEdge.Y1 + direction.Y * radius, radius, currentPaint);
                    }
                }
                else // Edge is not a loop
                {
                    CoordinateLine inverseEdge = new CoordinateLine(edge.End, edge.Start);
                    int bendOffset = 0;
                    // total number of edges between two vertices
                    int totalEdgeCount = sameEdges[edge];
                    int inverseEdgeCount;
                    if (sameEdges.TryGetValue(inverseEdge, out inverseEdgeCount))
                    {
                        totalEdgeCount += inverseEdgeCount;

                        // offset by number of inverse edges when inverse edges are drawn inbetween these edges.
                        if (inverseEdge.Start.X < edge.Start.X)
                        {
                            bendOffset = inverseEdgeCount;
                        }
                    }

                    for (int i = 1 + bendOffset; i <= sameEdges[edge] + bendOffset; ++i)
                    {
                        Pixel start = pixelEdge.Pixel1;
                        Pixel end = pixelEdge.Pixel2;

                        // Calculate control point for the quadratic Bezier curve
                        // Offset for each parallel edge to spread the arcs apart
                        float offset = (i + ((totalEdgeCount + 1) % 2)) / 2 * (-2 * (i % 2) + 1) * 40 - 20 * (-2 * (i % 2) + 1) * ((totalEdgeCount + 1) % 2);
                        
                        // flip offset to prevent inverted arc from flipping it.
                        if(bendOffset != 0)
                        {
                            offset *= -1;
                        }

                        Pixel controlPoint = GetControlPointForArc(start, end, offset);

                        SKPath path = new SKPath();
                        path.MoveTo(start.X, start.Y);
                        path.QuadTo(controlPoint.X, controlPoint.Y, end.X, end.Y);

                        rp.Canvas.DrawPath(path, currentPaint);

                        if (this.graph.IsDirected)
                        {
                            DrawArrowOnLine(rp, pixelEdge, offset / 2);
                        }
                    }
                }
            }
        }

        private void DrawArrowOnLine(RenderPack rp, PixelLine pixelEdge, float offset)
        {
            // Calculate the midpoint between the two vertices
            Pixel start = pixelEdge.Pixel1;
            Pixel end = pixelEdge.Pixel2;
            float midX = (start.X + end.X) / 2;
            float midY = (start.Y + end.Y) / 2;

            // Calculate the direction of the line
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;

            // Normalize the direction vector to get the unit vector for the line
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            float ux = dx / length;
            float uy = dy / length;

            // Calculate the perpendicular unit vector for the offset
            float perpUx = -uy;
            float perpUy = ux;

            // Apply the offset along the perpendicular direction
            midX += perpUx * offset;
            midY += perpUy * offset;

            // Calculate the angle of the line
            float angle = (float)Math.Atan2(dy, dx);

            // Length of the arrowhead lines
            float arrowLength = 10f;
            float arrowAngle = 30f * (float)(Math.PI / 180);

            // Calculate the points for the two sides of the arrowhead
            float x1 = midX - arrowLength * (float)Math.Cos(angle - arrowAngle);
            float y1 = midY - arrowLength * (float)Math.Sin(angle - arrowAngle);
            float x2 = midX - arrowLength * (float)Math.Cos(angle + arrowAngle);
            float y2 = midY - arrowLength * (float)Math.Sin(angle + arrowAngle);

            // Draw the arrowhead lines on the canvas
            rp.Canvas.DrawLine(midX, midY, x1, y1, edgePaint);
            rp.Canvas.DrawLine(midX, midY, x2, y2, edgePaint);
        }


        private void DrawVerticesAndLabels(RenderPack rp)
        {
            // Draw vertices and their labels
            foreach (Vertex v in graph.Vertices)
            {
                // Draw the actual vertex:
                Coordinates centerCoordinates = v.Location;
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                Drawing.DrawMarker(rp.Canvas, vertexPaint, centerPixel, v.Style);

                // Draw the vertex label:
                string vertexLabel = v.Label;
                if (!string.IsNullOrEmpty(vertexLabel))
                {
                    float textOffsetX = 10;
                    float textOffsetY = -10;

                    rp.Canvas.DrawText(vertexLabel, centerPixel.X + textOffsetX, centerPixel.Y + textOffsetY, textPaint);
                }

                // Draw the degree label:
                if (IsDisplayingVertexDegree)
                {
                    int degree = this.graph.GetVertexDegree(v);
                    float textOffsetX = -10;
                    float textOffsetY = -10;

                    rp.Canvas.DrawText(degree.ToString(), centerPixel.X + textOffsetX, centerPixel.Y + textOffsetY, degreeLabelPaint);
                }
            }
        }

        private void DrawTemporaryLine(RenderPack rp)
        {
            // Draw temporary line (from selected vertex to current mouse position)
            if (temporaryLine != null)
            {
                PixelLine pixelEdge = Axes.GetPixelLine((CoordinateLine)temporaryLine);
                // Calculate the middle point between the two vertices
                Pixel start = pixelEdge.Pixel1;
                Pixel end = pixelEdge.Pixel2;
                rp.Canvas.DrawLine(start.X, start.Y, end.X, end.Y, edgePaint);
            }
        }

        // Function to calculate the control point for the arc
        private Pixel GetControlPointForArc(Pixel start, Pixel end, float offset)
        {
            // Calculate the midpoint between the two vertices
            float midX = (start.X + end.X) / 2;
            float midY = (start.Y + end.Y) / 2;

            // Find the perpendicular direction for the control point
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;

            // Normalize the direction vector and apply the offset for curvature
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            float offsetX = -dy / length * offset;
            float offsetY = dx / length * offset;

            // Create the control point at the offset
            return new Pixel(midX + offsetX, midY + offsetY);
        }

        // returns vertices contained in rectangle
        public List<Vertex> getVerticesInRect(CoordinateRect rect)
        {
            List<Vertex> vertices = new List<Vertex>();
            foreach(Vertex v in graph.Vertices)
            {
                if(rect.Contains(v.Location))
                {
                    vertices.Add(v);
                }
            }

            return vertices;
        }

        // Custom Autosfit function, finds the size to fit all the vertices in.
        public void Refit()
        {
            // Calculate graph boundaries based on vertices
            if (graph.Vertices.Count == 0)
                return; // No vertices to scale

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (Vertex vertex in graph.Vertices)
            {
                minX = Math.Min(minX, vertex.Location.X);
                minY = Math.Min(minY, vertex.Location.Y);
                maxX = Math.Max(maxX, vertex.Location.X);
                maxY = Math.Max(maxY, vertex.Location.Y);
            }

            // Add some padding to make the plot look better
            double padding = 0.1 * Math.Max(maxX - minX, maxY - minY);
            minX -= padding;
            minY -= padding;
            maxX += padding;
            maxY += padding;

            // Set the axes spans
            Axes.XAxis.Range.Set(minX, maxX);
            Axes.YAxis.Range.Set(minY, maxY);
            //Axes.SetSpanX(maxX - minX);
            //Axes.SetSpanY(maxY - minY);
        }

        public void Clear()
        {
            // Remove any temporary line
            temporaryLine = null;
            graph.Clear();
        }
    }
}
