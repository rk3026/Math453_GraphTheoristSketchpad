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
        public void AddEdge(Vertex start, Vertex end, double weight)
        {
            int startIndex;
            int endIndex;

            if((startIndex = vertices.IndexOf(start)) == -1)
            {
                startIndex = vertices.Count;
                vertices.Add(start);
            }

            if ((endIndex = vertices.IndexOf(end)) == -1)
            {
                endIndex = vertices.Count;
                vertices.Add(end);
            }

            // new matrix with new edge.
            Matrix<double> newMatrix = CreateMatrix.Sparse<double>(vertices.Count, matrix.ColumnCount+1);
            newMatrix.SetSubMatrix(0, 0, matrix);
            newMatrix[startIndex, newMatrix.ColumnCount - 1] = weight;
            newMatrix[endIndex, newMatrix.ColumnCount - 1] = weight;
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
            for (int i = 0; i<startRow.Count; i++)
            {
                if (startRow[i] > 0 && endRow[i] > 0)
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
                if (matrix[vertexIndex, col] > 0)
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

                if (column[row] > 0)
                {
                    // row of the other endpoint for this edge
                    int endRow = row;
                    for(int j = 0; j < column.Count; ++j)
                    {
                        if (column[j] > 0 && j != row)
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

        public CoordinateLine[] getEdges()
        {
            CoordinateLine[] edges = new CoordinateLine[this.matrix.ColumnCount];

            for (int i = 0; i < this.matrix.ColumnCount; ++i)
            {
                Vector<double> column = this.matrix.Column(i);

                // line start
                Coordinates end1 = new Coordinates(0, 0);

                // line end
                Coordinates end2 = new Coordinates(1, 1);

                // get first line endpoint
                for (int j = 0; j < column.Count; ++j)
                {
                    // check if vertex is incident on edge
                    if (column[j] > 0d)
                    {
                        end1 = this.vertices[j].Location;
                    }
                }

                // get second line endpoint
                for (int j = column.Count - 1; j >= 0; --j)
                {
                    // check if vertex is incident on edge
                    if (column[j] > 0d)
                    {
                        end2 = this.vertices[j].Location;
                    }
                }

                edges[i] = new CoordinateLine(end1, end2);
            }

            return edges;
        }

        public int getEdgeCount()
        {
            return this.matrix.ColumnCount;
        }
    }
}
