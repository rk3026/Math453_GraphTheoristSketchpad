using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SkiaSharp;
using MathNet.Numerics.LinearAlgebra;
using ScottPlot;
using System.Windows.Controls;

namespace GraphTheoristSketchpad.Logic
{
    class IncidenceMatrix
    {
        public bool IsDirected
        {
            get
            {
                return isDirected;
            }

            set
            {
                if (isDirected != value)
                {
                    isDirected = value;
                    if (isDirected)
                    {
                        ToDirected();
                    }
                    else
                    {
                        ToUndirected();
                    }
                }
            }
        }

        private bool isDirected;

        // Vertex rows and edge columns for weighted graph.
        private Matrix<double> matrix;

        // endpoints for all edges at the index present in the matrix rows
        private List<Vertex> vertices;

        public IncidenceMatrix()
        {
            vertices = new List<Vertex>();
            matrix = CreateMatrix.Sparse<double>(0,0);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            // Calculate padding based on longest vertex label
            int maxLabelWidth = 0;
            if (vertices.Count > 0 )
            {
                maxLabelWidth = vertices.Max(v => v.Label.Length);
            }
            int padding = maxLabelWidth + 1; // Extra space for readability

            // Add column headers with padding
            sb.Append(' ', padding); // Space for row headers
            for (int j = 0; j < this.matrix.ColumnCount; j++)
            {
                sb.Append($"e{j + 1}".PadRight(4)); // e1, e2, etc., with padding
            }
            sb.AppendLine();

            // Add rows with row headers
            for (int i = 0; i < this.matrix.RowCount; i++)
            {
                sb.Append(vertices[i].Label.PadRight(padding)); // Row header with padding
                Vector<double> row = this.matrix.Row(i);

                for (int j = 0; j < row.Count; j++)
                {
                    sb.Append($"{row[j],4} "); // Align matrix values with padding
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }



        // Adds a column to the matrix for this new edge.
        // Also adds rows if endpoints are new vertices.
        public void AddEdge(Vertex tail, Vertex head, double weight)
        {
            int tailIndex;
            int headIndex;

            if((tailIndex = vertices.IndexOf(tail)) == -1)
            {
                tailIndex = vertices.Count;
                vertices.Add(tail);
            }

            if ((headIndex = vertices.IndexOf(head)) == -1)
            {
                headIndex = vertices.Count;
                vertices.Add(head);
            }

            // new matrix with new edge.
            Matrix<double> newMatrix = CreateMatrix.Sparse<double>(vertices.Count, matrix.ColumnCount+1);
            newMatrix.SetSubMatrix(0, 0, matrix);

            // set tail weight last to overwrite negative head weight in case of a loop
            newMatrix[headIndex, newMatrix.ColumnCount - 1] = this.isDirected ? -weight : weight;
            newMatrix[tailIndex, newMatrix.ColumnCount - 1] = weight;
            this.matrix = newMatrix;
        }

        public void AddEdge(Vertex start, Vertex end)
        {
            this.AddEdge(start, end, 1d);
        }

        public void RemoveEdge(Vertex start, Vertex end)
        {
            int startIndex = vertices.IndexOf(start);
            int endIndex = vertices.IndexOf(end);
            Vector<double> startRow = this.matrix.Row(startIndex);
            Vector<double> endRow = this.matrix.Row(endIndex);

            // remove columns where both rows corresponding to vertices have nonzero weights
            for (int i = startRow.Count-1; i >= 0; --i)
            {
                if (startRow[i] != 0 && endRow[i] != 0)
                {
                    this.matrix = this.matrix.RemoveColumn(i);
                    break;
                }
            }
        }

        public void RemoveVertex(Vertex vertex)
        {
            int vertexIndex = vertices.IndexOf(vertex);

            if (vertexIndex == -1)
                return;

            // Find all edges incident to this vertex and remove them
            List<int> edgesToRemove = new List<int>();

            // Iterate through the columns (edges) and mark the ones connected to this vertex
            for (int col = 0; col < matrix.ColumnCount; col++)
            {
                if (matrix[vertexIndex, col] != 0)
                {
                    edgesToRemove.Add(col);
                }
            }

            // Remove the edges from the matrix (start from last to avoid shifting)
            edgesToRemove.Reverse();
            foreach (int col in edgesToRemove)
            {
                matrix = matrix.RemoveColumn(col);
            }

            // Remove the vertex from the vertex list
            vertices.RemoveAt(vertexIndex);

            // Remove the vertex row from the matrix
            matrix = matrix.RemoveRow(vertexIndex);
        }

        // returns array of edges incident on v that start at v
        public CoordinateLine[] getEdgesOn(Vertex v)
        {
            // row of vertex v
            int row = vertices.IndexOf(v);

            // return no edges when vertex not in matrix
            if(row == -1)
            {
                return new CoordinateLine[0];
            }

            List<CoordinateLine> incidentEdges = new List<CoordinateLine>();

            // check if each edge (column) in matrix is incident on v
            for (int i = 0; i < this.matrix.ColumnCount; ++i)
            {
                Vector<double> column = this.matrix.Column(i);

                // found incident edge
                if (column[row] != 0)
                {
                    // row of the other endpoint for this edge
                    int endRow = row;

                    //search for other endpoint
                    for(int j = 0; j < column.Count; ++j)
                    {
                        if (column[j] != 0 && j != row)
                        {
                            endRow = j;
                            break;
                        }
                    }

                    // add found incident edge to list
                    incidentEdges.Add(new CoordinateLine(vertices[row].Location, vertices[endRow].Location));
                }
            }

            return incidentEdges.ToArray();
        }

        // get edges where start is tail and end is head
        public CoordinateLine[] getEdges()
        {
            CoordinateLine[] edges = new CoordinateLine[this.matrix.ColumnCount];

            for (int i = 0; i < this.matrix.ColumnCount; ++i)
            {
                Vector<double> column = this.matrix.Column(i);

                // line start
                Coordinates? tail = null;

                // line end
                Coordinates? head = null;

                // get head and tail of line.
                for (int j = 0; j < column.Count; ++j)
                {
                    // get first endpoint
                    if (column[j] != 0d && tail == null && head == null)
                    {
                        if (column[j] < 0d)
                            head = this.vertices[j].Location;
                        else
                            tail = this.vertices[j].Location;
                    }
                    // get second endpoint when first one is already set
                    else if (column[j] != 0d)
                        if(tail == null)
                            tail = this.vertices[j].Location;
                        else
                            head = this.vertices[j].Location;
                }

                // edge is loop
                if (head == null)
                {
                    edges[i] = new CoordinateLine((Coordinates)tail!, (Coordinates)tail!);
                }
                else
                {
                    edges[i] = new CoordinateLine((Coordinates)tail!, (Coordinates)head!);
                }
            }

            return edges;
        }

        // converts matrix to an incident matrix for a directed graph
        // edges become two parallel arcs directed oppositely.
        private void ToDirected()
        {
            List<Vector<double>> columns = new List<Vector<double>>();

            for (int c = 0; c < this.matrix.ColumnCount; ++c)
            {
                Vector<double> col = this.matrix.Column(c);

                int? tailIndex = null;
                int? headIndex = null;
                // get tail and head of edge
                for (int r = 0; r < col.Count; r++)
                {
                    if (col[r] != 0)
                    {
                        if(tailIndex == null)
                            tailIndex = r;
                        else
                            headIndex = r;
                    }
                }

                // create two arcs when edge isnt a loop
                if (headIndex != null)
                {
                    // create new arcs for both possible directions for edge.
                    Vector<double> newArc1 = col.Clone();
                    newArc1[(int)tailIndex!] *= -1;
                    columns.Add(newArc1);

                    Vector<double> newArc2 = col.Clone();
                    newArc2[(int)headIndex!] *= -1;
                    columns.Add(newArc2);
                }
                // create one loop to one loop
                else
                {
                    columns.Add(col);
                }
            }

            Matrix<double> newMatrix = CreateMatrix.Sparse<double>(vertices.Count(), columns.Count());
            for(int c = 0; c < columns.Count(); ++c)
            {
                newMatrix.SetColumn(c, columns[c]);
            }

            this.matrix = newMatrix;
        }

        private void ToUndirected()
        {
            // count of each arc on each vertex
            Dictionary<Vector<double>, int> arcCounts =
                new Dictionary<Vector<double>, int>();

            List<Vector<double>> columns = new List<Vector<double>>();

            for (int c = 0; c < this.matrix.ColumnCount; ++c)
            {
                Vector<double> col = this.matrix.Column(c);

                int? tailIndex = null;
                int? headIndex = null;

                // find tail and head indices of edge
                for (int r = 0; r < col.Count; r++)
                {
                    if (col[r] > 0)
                    {
                        tailIndex = r;
                    }
                    else if (col[r] < 0)
                    {
                        headIndex = r;
                    }
                }

                // edge isnt a loop
                if (headIndex != null)
                {
                    if(arcCounts.ContainsKey(col))
                    {
                        arcCounts[col] += 1;
                    }
                    else
                    {
                        arcCounts[col] = 1;
                    }
                }
                else
                {
                    columns.Add(col);
                }
            }


            // combine opposite arcs into bidirectional edge
            foreach(Vector<double> arc in arcCounts.Keys)
            {
                Vector<double> inverseArc = -arc;
                if(arcCounts.ContainsKey(inverseArc))
                {
                    // replace negative weight (head) in column with positive one
                    Vector<double> edge = arc.Clone();
                    for(int i = 0; i < edge.Count; ++i)
                    {
                        if (edge[i] < 0)
                        {
                            edge[i] = double.Abs(edge[i]);
                            break;
                        }
                    }

                    // add bidirectional edge for each arc inverseArc pair
                    for (int i = 0; i < int.Min(arcCounts[arc], arcCounts[inverseArc]); ++i)
                    {
                        columns.Add(edge);
                    }

                    arcCounts.Remove(arc);
                    arcCounts.Remove(inverseArc);
                }
            }

            Matrix<double> newMatrix = CreateMatrix.Sparse<double>(vertices.Count(), columns.Count());
            for (int c = 0; c < columns.Count(); ++c)
            {
                newMatrix.SetColumn(c, columns[c]);
            }

            this.matrix = newMatrix;
        }

        public int getEdgeCount()
        {
            return this.matrix.ColumnCount;
        }
    }
}
