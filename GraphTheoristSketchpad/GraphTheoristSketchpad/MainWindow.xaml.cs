using ScottPlot.Plottables;
using ScottPlot;
using ScottPlot.WPF;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Printing;

namespace GraphTheoristSketchpad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WpfPlot formsPlot1;
        public MainWindow()
        {
            InitializeComponent();
            formsPlot1 = Graph1;

            Scatter = Graph1.Plot.Add.Scatter(Xs, Ys);
            Scatter.LineWidth = 2;
            Scatter.MarkerSize = 10;
            Scatter.Smooth = true;

            formsPlot1.MouseMove += FormsPlot1_MouseMove;
            formsPlot1.MouseDown += FormsPlot1_MouseDown;
            formsPlot1.MouseUp += FormsPlot1_MouseUp;
        }

        readonly double[] Xs = Generate.RandomAscending(10);
        readonly double[] Ys = Generate.RandomSample(10);
        readonly ScottPlot.Plottables.Scatter Scatter;
        int? IndexBeingDragged = null;

        private void FormsPlot1_MouseDown(object? sender, MouseEventArgs e)
        {
            Pixel mousePixel = new Pixel(e.GetPosition(Graph1).X,e.GetPosition(Graph1).Y);
            Coordinates mouseLocation = Graph1.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, Graph1.Plot.LastRender);
            IndexBeingDragged = nearest.IsReal ? nearest.Index : null;

            if (IndexBeingDragged.HasValue)
                Graph1.Interaction.Disable();
        }

        private void FormsPlot1_MouseUp(object? sender, MouseEventArgs e)
        {
            IndexBeingDragged = null;
            formsPlot1.Interaction.Enable();
            formsPlot1.Refresh();
        }

        private void FormsPlot1_MouseMove(object? sender, MouseEventArgs e)
        {
            Pixel mousePixel = new Pixel(e.GetPosition(formsPlot1).X, e.GetPosition(formsPlot1).Y);
            Coordinates mouseLocation = formsPlot1.Plot.GetCoordinates(mousePixel);
            DataPoint nearest = Scatter.Data.GetNearest(mouseLocation, formsPlot1.Plot.LastRender);
            formsPlot1.Cursor = nearest.IsReal ? Cursors.Hand : Cursors.Arrow;

            if (IndexBeingDragged.HasValue)
            {
                Xs[IndexBeingDragged.Value] = mouseLocation.X;
                Ys[IndexBeingDragged.Value] = mouseLocation.Y;
                formsPlot1.Refresh();
            }
        }
    }
}