using ScottPlot;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTheoristSketchpad.Interface
{
    internal class GraphRenderer : IPlottable
    {
        // data and customization options
        double[] Xs { get; }
        double[] Ys { get; }
        public IScatterSource Data { get; }
        public float Radius { get; set; } = 10;
        IColormap Colormap { get; set; } = new ScottPlot.Colormaps.Turbo();

        // items required by IPlottable
        public bool IsVisible { get; set; } = true;
        public IAxes Axes { get; set; } = new Axes();
        public IEnumerable<LegendItem> LegendItems => LegendItem.None;
        public AxisLimits GetAxisLimits() => new(Xs.Min(), Xs.Max(), Ys.Min(), Ys.Max());

        public GraphRenderer(double[] xs, double[] ys)
        { 
            Xs = xs; Ys = ys;
            this.Data = new ScottPlot.DataSources.ScatterSourceDoubleArray(xs, ys);
        }

        public void Render(RenderPack rp)
        {
            FillStyle FillStyle = new();
            using SKPaint paint = new();
            for (int i = 0; i < Xs.Length; i++)
            {
                Coordinates centerCoordinates = new(Xs[i], Ys[i]);
                Pixel centerPixel = Axes.GetPixel(centerCoordinates);
                FillStyle.Color = Colormap.GetColor(i / (Xs.Length - 1.0));
                ScottPlot.Drawing.DrawCircle(rp.Canvas, centerPixel, Radius, FillStyle, paint);
            }
        }
    }
}
