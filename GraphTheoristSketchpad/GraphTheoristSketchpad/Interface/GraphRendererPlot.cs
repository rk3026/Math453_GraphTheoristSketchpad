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
        public IAxes Axes { get; set; } = new Axes();
        public IEnumerable<LegendItem> LegendItems => LegendItem.None;
        public AxisLimits GetAxisLimits() => AxisLimits.Default;

        public SKPaint textPaint;

        public SKPaint edgePaint;

        public GraphRendererPlot()
        {
            textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 20,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                BlendMode = SKBlendMode.Multiply
            };

            edgePaint = new SKPaint
            {
                Color = SKColors.Red.WithAlpha(130),
                IsAntialias = true,
                StrokeWidth = 2,
                Style = SKPaintStyle.Stroke,
            };
        }

        public void Render(RenderPack rp)
        {
            DrawEdges(rp);
            DrawVerticesAndLabels(rp);
            DrawTemporaryLine(rp);
        }

        private void DrawEdges(RenderPack rp)
        {
            FillStyle FillStyle = new();
            using SKPaint paint = new();

            // Get all edges
            CoordinateLine[] edges = graph.getEdges();

            Dictionary<CoordinateLine, int> sameEdges =
                new Dictionary<CoordinateLine, int>();

            foreach (CoordinateLine edge in edges)
            {
                int edgeCount;
                if (sameEdges.TryGetValue(edge, out edgeCount))
                {
                    sameEdges[edge] = edgeCount + 1;
                }
                else
                {
                    sameEdges.Add(edge, 1);
                }
            }

            // draw edges
            foreach (CoordinateLine edge in sameEdges.Keys)
            {
                PixelLine pixelEdge = Axes.GetPixelLine(edge);

                // edge is loop
                if (edge.Start.Equals(edge.End))
                {
                    CoordinateLine[] incidentEdges =
                        graph.getEdgesOn(graph.getNearestVertex(edge.Start)!);

                    // get counter clockwise angle for each edge
                    List<KeyValuePair<CoordinateLine, double>> edgeAngles =
                        new List<KeyValuePair<CoordinateLine, double>>();
                    foreach (CoordinateLine incidentEdge in incidentEdges)
                    {
                        // add if not loop
                        if (!incidentEdge.Equals(edge))
                        {
                            double deltaY = incidentEdge.End.Y - incidentEdge.Start.Y;
                            double deltaX = incidentEdge.End.X - incidentEdge.Start.X;
                            double edgeAngle = Math.Atan2(deltaY, deltaX);
                            edgeAngles.Add(new KeyValuePair<CoordinateLine, double>(incidentEdge, edgeAngle));
                        }
                    }

                    // unit vector pointing to middle of largest angle
                    Pixel direction = new Pixel(1, 0);

                    // find largest angle between edges
                    if (edgeAngles.Count != 0)
                    {
                        // sort by angle
                        edgeAngles.Sort((KeyValuePair<CoordinateLine, double> x, KeyValuePair<CoordinateLine, double> y) => { return x.Value < y.Value ? -1 : 1; });

                        // counter clockwise angle of edge with largest angle
                        double largestEdgeAngle = edgeAngles.First().Value;

                        // largest counter clockwise angle angle between two edges
                        double largestAngle = 0;
                        double lastAngle = edgeAngles.Last().Value - 2 * Math.PI;

                        // find largest counter clockwise angle
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

                    // draw loops
                    for (int i = 1; i <= sameEdges[edge]; ++i)
                    {
                        int radius = i * 10 + 10;
                        rp.Canvas.DrawCircle(pixelEdge.X1 + direction.X * radius, pixelEdge.Y1 + direction.Y * radius, radius, edgePaint);
                    }
                }
                // edge is not loop
                else
                {
                    CoordinateLine inverseEdge = new CoordinateLine(edge.End, edge.Start);

                    // offset arcs going in one direction so they don't overlap one's going the other direction
                    int bendOffset = 0;
                    int inverseEdgeCount;
                    if (sameEdges.TryGetValue(inverseEdge, out inverseEdgeCount) && inverseEdge.Start.X < edge.Start.X)
                    {
                        bendOffset = inverseEdgeCount;
                    }

                    for (int i = 1 + bendOffset; i <= sameEdges[edge] + bendOffset; ++i)
                    {
                        // Calculate the middle point between the two vertices
                        Pixel start = pixelEdge.Pixel1;
                        Pixel end = pixelEdge.Pixel2;

                        // Calculate control point for the quadratic Bezier curve
                        // Offset for each parallel edge to spread the arcs apart
                        float offset = (i + ((sameEdges[edge] + 1) % 2)) / 2 * (-2 * (i % 2) + 1) * 40 - 20 * (-2 * (i % 2) + 1) * ((sameEdges[edge] + 1) % 2);
                        Pixel controlPoint = GetControlPointForArc(start, end, offset);

                        // Draw quadratic Bézier curve as an arc
                        SKPath path = new SKPath();
                        path.MoveTo(start.X, start.Y);
                        path.QuadTo(controlPoint.X, controlPoint.Y, end.X, end.Y);

                        // Draw the arc on the canvas
                        rp.Canvas.DrawPath(path, edgePaint);

                        if (this.graph.IsDirected)
                        {
                            //rp.Canvas.DrawCircle(end.X, end.Y, 30, edgePaint);
                            DrawArrowOnLine(rp, pixelEdge, offset/2);
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
            using SKPaint paint = new();
            // Draw vertices and their labels
            foreach (Vertex v in graph.Vertices)
            {
                Coordinates centerCoordinates = v.Location;
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                Drawing.DrawMarker(rp.Canvas, paint, centerPixel, v.Style);

                string vertexLabel = v.Label;
                if (!string.IsNullOrEmpty(vertexLabel))
                {
                    float textOffsetX = 10;
                    float textOffsetY = -10;

                    rp.Canvas.DrawText(vertexLabel, centerPixel.X + textOffsetX, centerPixel.Y + textOffsetY, textPaint);
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
