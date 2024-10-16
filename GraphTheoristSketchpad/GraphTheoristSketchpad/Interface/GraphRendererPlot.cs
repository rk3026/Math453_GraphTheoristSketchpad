﻿using GraphTheoristSketchpad.Logic;
using ScottPlot;
using ScottPlot.Plottables;
using SkiaSharp;
using System.Windows.Input;

namespace GraphTheoristSketchpad.Interface
{
    // This class can be assigned to a wpf plot, which allows for creation of graphs.
    public class GraphRendererPlot : IPlottable
    {
        public Graph graph = new Graph();
        IColormap Colormap { get; set; } = new ScottPlot.Colormaps.Turbo();
        // items required by IPlottable
        public bool IsVisible { get; set; } = true;
        public IAxes Axes { get; set; } = new Axes();
        public IEnumerable<LegendItem> LegendItems => LegendItem.None;
        public AxisLimits GetAxisLimits() => AxisLimits.Default;

        public GraphRendererPlot()
        {
        }

        public void Render(RenderPack rp)
        {
            FillStyle FillStyle = new();
            using SKPaint paint = new();

            // Get all edges
            CoordinateLine[] edges = graph.getEdges();

            Dictionary<CoordinateLine, int> sameEdges =
                new Dictionary<CoordinateLine, int>();

            foreach(CoordinateLine edge in edges)
            {
                int edgeCount;
                if(sameEdges.TryGetValue(edge, out edgeCount))
                {
                    sameEdges[edge] = edgeCount+1;
                }
                else
                {
                    sameEdges.Add(edge, 1);
                }
            }

            // edge style
            paint.StrokeWidth = 5;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.Red;

            // draw edges
            foreach (CoordinateLine edge in sameEdges.Keys)
            {
                for(int i = 1; i <= sameEdges[edge]; ++i)
                {
                    PixelLine pixelEdge = Axes.GetPixelLine(edge);

                    // Calculate the middle point between the two vertices
                    Pixel start = pixelEdge.Pixel1;
                    Pixel end = pixelEdge.Pixel2;

                    // Calculate control point for the quadratic Bezier curve
                    // Offset for each parallel edge to spread the arcs apart
                    float offset = (i+((sameEdges[edge]+1)%2))/2*(-2*(i%2)+1) * 40; // Adjust offset size for parallel edges
                    Pixel controlPoint = GetControlPointForArc(start, end, offset);

                    // Draw quadratic Bézier curve as an arc
                    SKPath path = new SKPath();
                    path.MoveTo(start.X, start.Y);
                    path.QuadTo(controlPoint.X, controlPoint.Y, end.X, end.Y);

                    paint.IsAntialias = true;
                    paint.StrokeWidth = 2;
                    paint.Style = SKPaintStyle.Stroke;
                    // Draw the arc on the canvas
                    rp.Canvas.DrawPath(path, paint);
                }
            }
            

            // Draw vertices after edges (as in your original code)
            // Draw vertices and their labels
            foreach (Vertex v in graph.Vertices)
            {
                Coordinates centerCoordinates = v.Location;
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                Drawing.DrawMarker(rp.Canvas, paint, centerPixel, v.Style);

                using SKPaint textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 20,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial"),
                };

                string vertexLabel = v.Label;
                if (!string.IsNullOrEmpty(vertexLabel))
                {
                    float textOffsetX = 10;
                    float textOffsetY = -10;
                    rp.Canvas.DrawText(vertexLabel, centerPixel.X + textOffsetX, centerPixel.Y + textOffsetY, textPaint);
                }
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

    }
}
