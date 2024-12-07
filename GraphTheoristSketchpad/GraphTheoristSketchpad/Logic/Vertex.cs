using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphTheoristSketchpad.Logic
{
    // Styling information and placement of a graph vertex
    public class Vertex
    {
        // display name
        public string Label { get; set; }

        // location in space
        public Coordinates Location { get; set; }

        // vertex display shape and color
        public MarkerStyle Style { get; set; }

        public Vertex(double x, double y) : this(x, y, "", new MarkerStyle(MarkerShape.FilledCircle, 20))
        {
        }

        public Vertex(double x, double y, string label, MarkerStyle style)
        {
            this.Label = label;
            this.Location = new Coordinates(x, y);
            this.Style = style;
        }

        public override string ToString()
        {
            return Label;
        }
    }
}
