using ScottPlot;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace GraphTheoristSketchpad.Interface
{
    internal class GraphRenderer : IPlottable
    {
        // data and customization options
        double[] Xs { get; }
        double[] Ys { get; }
        BitArray AdjacencyMatrix { get; }
        public IScatterSource Data { get; }
        public float Radius { get; set; } = 10;
        IColormap Colormap { get; set; } = new ScottPlot.Colormaps.Turbo();

        // items required by IPlottable
        public bool IsVisible { get; set; } = true;
        public IAxes Axes { get; set; } = new Axes();
        public IEnumerable<LegendItem> LegendItems => LegendItem.None;
        public AxisLimits GetAxisLimits() => new(Xs.Min(), Xs.Max(), Ys.Min(), Ys.Max());

        public GraphRenderer(double[] xs, double[] ys, BitArray adjacencyMatrix)
        { 
            Xs = xs; Ys = ys;
            this.Data = new ScottPlot.DataSources.ScatterSourceDoubleArray(xs, ys);
            this.AdjacencyMatrix = adjacencyMatrix;
        }

        public void Render(RenderPack rp)
        {
            FillStyle FillStyle = new();
            using SKPaint paint = new();

            CoordinateLine[] edges = getEdges();

            for (int i = 0; i < edges.Length; ++i)
            {
                PixelLine pixelEdge = Axes.GetPixelLine(edges[i]);
                Drawing.DrawLine(rp.Canvas, paint, pixelEdge);
            }

            for (int i = 0; i < Xs.Length; ++i)
            {
                Coordinates centerCoordinates = new(Xs[i], Ys[i]);
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                FillStyle.Color = Colormap.GetColor(i / (Xs.Length - 1.0));
                Drawing.DrawCircle(rp.Canvas, centerPixel, Radius, FillStyle, paint);
            }
        }


        private CoordinateLine[] getEdges()
        {
            int matrixRowWidth = this.AdjacencyMatrix.Length / this.Xs.Length;

            CoordinateLine[] edges = new CoordinateLine[matrixRowWidth];

            for (int i = 0; i < matrixRowWidth; ++i)
            {
                // line start
                Coordinates end1 = new Coordinates(0, 0);

                // line end
                Coordinates end2 = new Coordinates(1, 1);

                // get first line endpoint
                for (int j = 0; j < this.Xs.Length; ++j)
                {
                    // check if both vertices are incident on same edge
                    if (this.AdjacencyMatrix[j * matrixRowWidth + i])
                    {
                        end1 = new Coordinates(Xs[j], Ys[j]);
                    }
                }

                // get second line endpoint
                for (int j = this.Xs.Length-1; j >= 0; --j)
                {
                    // check if both vertices are incident on same edge
                    if (this.AdjacencyMatrix[j * matrixRowWidth + i])
                    {
                        end2 = new Coordinates(Xs[j], Ys[j]);
                    }
                }

                edges[i] = new CoordinateLine(end1, end2);
            }

            return edges;
        }

        // true when two vertices are adjacent.
        private bool checkAjacency(int indexA, int indexB)
        {
            int matrixRowWidth = this.AdjacencyMatrix.Length / this.Xs.Length;
            for (int i = 0; i < matrixRowWidth; ++i)
            {
                // check if both vertices are incident on same edge
                if (this.AdjacencyMatrix[indexA*matrixRowWidth+i] &&
                    this.AdjacencyMatrix[indexA * matrixRowWidth + i])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
