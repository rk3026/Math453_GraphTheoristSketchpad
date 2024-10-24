﻿using GraphTheoristSketchpad.Logic;
using MathNet.Numerics.LinearAlgebra.Factorization;
using ScottPlot;
using ScottPlot.Plottables;
using SkiaSharp;
using System.Windows.Input;

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

            // draw edges
            foreach (CoordinateLine edge in sameEdges.Keys)
            {
                PixelLine pixelEdge = Axes.GetPixelLine(edge);

                // edge is loop
                if (edge.Start.Equals(edge.End))
                {
                    for (int i = 1; i <= sameEdges[edge]; ++i)
                    {
                        int radius = i * 10 + 10;
                        rp.Canvas.DrawCircle((float)pixelEdge.X1+radius, (float)pixelEdge.Y1, radius, edgePaint);
                    }
                }
                // edge is not loop
                else
                {
                    for (int i = 1; i <= sameEdges[edge]; ++i)
                    {
                        // Calculate the middle point between the two vertices
                        Pixel start = pixelEdge.Pixel1;
                        Pixel end = pixelEdge.Pixel2;

                        // Calculate control point for the quadratic Bezier curve
                        // Offset for each parallel edge to spread the arcs apart
                        float offset = (i + ((sameEdges[edge] + 1) % 2)) / 2 * (-2 * (i % 2) + 1) * 40 - 20* (-2 * (i % 2) + 1)*((sameEdges[edge] + 1) % 2); // Adjust offset size for parallel edges
                        Pixel controlPoint = GetControlPointForArc(start, end, offset);

                        // Draw quadratic Bézier curve as an arc
                        SKPath path = new SKPath();
                        path.MoveTo(start.X, start.Y);
                        path.QuadTo(controlPoint.X, controlPoint.Y, end.X, end.Y);

                        // Draw the arc on the canvas
                        rp.Canvas.DrawPath(path, edgePaint);
                    }
                }
            }
            

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
    }
}
