using GraphTheoristSketchpad.Logic;
using ScottPlot;
using ScottPlot.Plottables;
using SkiaSharp;

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
            paint.StrokeWidth = 5;
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = SKColors.Red;
            foreach (CoordinateLine edge in sameEdges.Keys)
            {
                for(int i = 1; i <= sameEdges[edge]; ++i)
                {
                    PixelLine pixelEdge = Axes.GetPixelLine(edge);
                    //Pixel offset = new Pixel(i * 10, i * 10);
                    //pixelEdge = new PixelLine(pixelEdge.Pixel1+offset, pixelEdge.Pixel2+offset);
                    //Drawing.DrawLine(rp.Canvas, paint, pixelEdge);
                    SKPath path = new SKPath();
                    path.MoveTo(pixelEdge.X1, pixelEdge.Y1);
                    //path.MoveTo(0, 0);
                    //path.LineTo(pixelEdge.X2, pixelEdge.Y2);

                    //path.ArcTo(pixelEdge.X1, pixelEdge.Y1, pixelEdge.X2, pixelEdge.Y2, i*10);
                    path.AddArc(new SKRect(pixelEdge.X1, pixelEdge.Y1, pixelEdge.X2, pixelEdge.Y2), 0, 180);

                    //path.AddPoly([new SKPoint(pixelEdge.X2, pixelEdge.Y2)], false)
                    rp.Canvas.DrawPath(path, paint);
                    
                }
            }

            /*
            // Draw edges
            for (int i = 0; i < edges.Length; ++i)
            {
                PixelLine pixelEdge = Axes.GetPixelLine(edges[i]);
                Drawing.DrawLine(rp.Canvas, paint, pixelEdge);
            }*/

            // Draw vertices and their labels
            foreach (Vertex v in graph.Vertices)
            {
                // Draw the actual vertex:
                Coordinates centerCoordinates = v.Location;
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                //Drawing.DrawCircle(rp.Canvas, centerPixel, VertexRadius, FillStyle, paint);
                Drawing.DrawMarker(rp.Canvas, paint, centerPixel, v.Style);

                // Draw the label for the vertex:
                // Set up SKPaint for text (label)
                using SKPaint textPaint = new SKPaint
                {
                    Color = SKColors.Black, // You can change this to any color you prefer
                    TextSize = 20,          // Size of the text
                    IsAntialias = true,     // Smoothens the text rendering
                    Typeface = SKTypeface.FromFamilyName("Arial"), // You can change the font if needed
                };

                // Draw the label for the vertex
                string vertexLabel = v.Label;
                if (!string.IsNullOrEmpty(vertexLabel))
                {
                    // Draw the text slightly offset from the center of the vertex
                    float textOffsetX = 10;  // Horizontal offset for label position
                    float textOffsetY = -10; // Vertical offset for label position

                    rp.Canvas.DrawText(vertexLabel, centerPixel.X + textOffsetX, centerPixel.Y + textOffsetY, textPaint);
                }

            }
        }
    }
}
