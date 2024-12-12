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
using System.Data;
using Xceed.Wpf.Toolkit.Core.Converters;

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

        // endpoints for all edges at the index present in the incidenceMatrix rows
        private List<Vertex> vertices;

        public IncidenceMatrix()
        {
            vertices = new List<Vertex>();
            matrix = CreateMatrix.Sparse<double>(0,0);
        }

        private IncidenceMatrix(Matrix<double> matrix, List<Vertex> vertices)
        {
            this.vertices = vertices;
            this.matrix = matrix;
        }

        // replace underlying vertex list with one with equal size
        public void replaceVertexList(List<Vertex> vertices)
        {
            if(this.vertices.Count() == vertices.Count())
            {
                this.vertices = vertices;
            }
            else
            {
                throw new ArgumentException("vertex lists don't match in size");
            }
            
        }

        public List<Vertex> getVertexList()
        {
            return new List<Vertex>(this.vertices);
        }

        public void addGraph(IncidenceMatrix matrix)
        {
            int originalRow = this.matrix.RowCount;
            int originalCol = this.matrix.ColumnCount;
            this.matrix = this.matrix.Resize(this.matrix.RowCount + matrix.matrix.RowCount, this.matrix.ColumnCount + matrix.matrix.ColumnCount);

            this.matrix.SetSubMatrix(originalRow, originalCol, matrix.matrix);
            
            for(int i = 0; i < matrix.vertices.Count; ++i)
            {
                this.vertices.Add(matrix.vertices[i]);
            }
        }

        public DataTable ToDataTable()
        {
            DataTable table = new DataTable();

            // Add a column for vertex names as the first column
            table.Columns.Add("Vertex", typeof(Vertex));

            // Add columns for each edge, named "Edge 1", "Edge 2", etc.
            for (int col = 0; col < matrix.ColumnCount; col++)
            {
                table.Columns.Add($"Edge {col + 1}", typeof(double));
            }

            // Add rows for each vertex, named by the vertex label
            for (int row = 0; row < matrix.RowCount; row++)
            {
                DataRow dataRow = table.NewRow();

                // Set the vertex label as the first column
                dataRow["Vertex"] = vertices[row];

                // Populate each cell in the row with the incidenceMatrix value
                for (int col = 0; col < matrix.ColumnCount; col++)
                {
                    dataRow[col + 1] = matrix[row, col]; // Adjust index to skip the vertex column
                }

                // Add the populated row to the DataTable
                table.Rows.Add(dataRow);
            }

            return table;
        }

        // returns true if this matrix contains a looping edge
        public bool containsLoop()
        {
            // check each edge for a column that contains only one weight
            for (int i = 0; i < this.matrix.ColumnCount; ++i)
            {
                Vector<double> column = this.matrix.Column(i);

                int nonZeroEntries = 0;
                foreach (double w in column)
                {
                    if (w != 0)
                        ++nonZeroEntries;
                }

                // if there's exactly 1 weight in column, then it's a looping edge
                if (nonZeroEntries == 1)
                    return true;
            }

            return false;
        }

        // Adds a column to the incidenceMatrix for this new edge.
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

            // new incidenceMatrix with new edge.
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
            bool isLoop = start == end;
            int startIndex = vertices.IndexOf(start);
            int endIndex = vertices.IndexOf(end);
            Vector<double> startRow = this.matrix.Row(startIndex);
            Vector<double> endRow = this.matrix.Row(endIndex);

            // remove columns where both rows corresponding to vertices have nonzero weights
            for (int i = startRow.Count-1; i >= 0; --i)
            {
                if (startRow[i] != 0 && endRow[i] != 0)
                {
                    if (isLoop)
                    {
                        // remove only if edge at column i is a loop
                        if(this.matrix.Column(i).Sum() == startRow[i])
                        {
                            this.matrix = this.matrix.RemoveColumn(i);
                            break;
                        }
                    }
                    else
                    {
                        this.matrix = this.matrix.RemoveColumn(i);
                        break;
                    }
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

            // Remove the edges from the incidenceMatrix (start from last to avoid shifting)
            edgesToRemove.Reverse();
            foreach (int col in edgesToRemove)
            {
                matrix = matrix.RemoveColumn(col);
            }

            // Remove the vertex from the vertex list
            vertices.RemoveAt(vertexIndex);

            // Remove the vertex row from the incidenceMatrix
            matrix = matrix.RemoveRow(vertexIndex);
        }

        // returns array of edges incident on v that start at v
        public CoordinateLine[] getEdgesOn(Vertex v)
        {
            // row of vertex v
            int row = vertices.IndexOf(v);

            // return no edges when vertex not in incidenceMatrix
            if(row == -1)
            {
                return new CoordinateLine[0];
            }

            List<CoordinateLine> incidentEdges = new List<CoordinateLine>();

            // check if each edge (column) in incidenceMatrix is incident on v
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

        // returns the incidence matrix for component containing v
        public IncidenceMatrix getComponentMatrixOf(Vertex v)
        {
            ISet<Vertex> vComponent = getComponentOf(v);

            // vertices to be removed from incidince matrix to form one for component
            IEnumerable<Vertex> excludedVertices = vertices.Except(vComponent);

            Matrix<double> componentMatrix = this.matrix.Clone();

            List<int> rowsToRemove = new List<int>();

            foreach(Vertex excludedVertex in excludedVertices)
            {
                // excluded vertex index
                int eVIndex = vertices.IndexOf(excludedVertex);
                rowsToRemove.Add(eVIndex);

                Vector<double> eVRow = componentMatrix.Row(eVIndex);

                for(int c = eVRow.Count()-1; c >= 0; --c)
                {
                    // remove all edges on excluded vertex
                    if (eVRow[c] != 0)
                    {
                        componentMatrix = componentMatrix.RemoveColumn(c);
                    }
                }
            }

            //remove rows from bottom up to prevent changing index of rows to remove
            rowsToRemove.Sort();
            for(int i = rowsToRemove.Count()-1; i >=0; --i)
            {
                componentMatrix = componentMatrix.RemoveRow(rowsToRemove[i]);
            }

            // order component vertex set according to its order in this incidence matrix
            SortedDictionary<int, Vertex> orderedVertices = new SortedDictionary<int, Vertex>();
            foreach(Vertex vCompVert in vComponent)
            {
                orderedVertices.Add(this.vertices.IndexOf(vCompVert), vCompVert);
            }

            List<Vertex> componentIMatrixVerts = new List<Vertex>();
            foreach (int index in orderedVertices.Keys)
            {
                componentIMatrixVerts.Add(this.vertices[index]);
            }

            IncidenceMatrix componentIMatrix =
                new IncidenceMatrix(componentMatrix, componentIMatrixVerts);

            return componentIMatrix;
        }

        // returns set of vertices representing the component
        public ISet<Vertex> getComponentOf(Vertex v)
        {
            ISet<Vertex> component = new HashSet<Vertex>();

            ISet<Vertex> visited = new HashSet<Vertex>();

            component.Add(v);

            // stop when all vertices in component have been visited
            while(!component.Equals(visited))
            {
                component = new HashSet<Vertex>(component.Union(getNeighborsOf(v)));
                visited.Add(v);

                // get next vertex in component that has not been visited
                ISet<Vertex> leftovers = new HashSet<Vertex>(component);
                leftovers.ExceptWith(visited);
                if (leftovers.Count() != 0)
                    v = leftovers.First();
                else
                    break;
            }

            return component;
        }

        // returns set of neighbors of v
        public ISet<Vertex> getNeighborsOf(Vertex v)
        {
            // row of vertex v
            int row = vertices.IndexOf(v);

            // return no vertices when vertex not in incidenceMatrix
            if (row == -1)
            {
                return new HashSet<Vertex>();
            }

            List<Vertex> neighbors = new List<Vertex>();

            // check if each edge (column) in incidenceMatrix is incident on v
            for (int i = 0; i < this.matrix.ColumnCount; ++i)
            {
                Vector<double> column = this.matrix.Column(i);

                // found incident edge
                if (column[row] != 0)
                {
                    // row of the other endpoint for this edge
                    int endRow = row;

                    //search for other endpoint
                    for (int j = 0; j < column.Count; ++j)
                    {
                        if (column[j] != 0 && j != row)
                        {
                            endRow = j;
                            break;
                        }
                    }

                    // add opposite endpoint of edge to list
                    neighbors.Add(vertices[endRow]);
                }
            }

            return new HashSet<Vertex>(neighbors);
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

        // converts incidenceMatrix to an incident incidenceMatrix for a directed graph
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

        public List<double> getWeight(Vertex start, Vertex end)
        {
            List<double> weights = new List<double>();

            Vector<double> startRow = this.matrix.Row(this.vertices.IndexOf(start));

            Vector<double> endRow = this.matrix.Row(this.vertices.IndexOf(end));

            for(int i = 0; i < startRow.Count(); ++i)
            {
                if (startRow[i] != 0 && endRow[i] != 0)
                {
                    // check if start row is head of edge/arc
                    if(startRow[i] > 0)
                    {
                        weights.Add(startRow[i]);
                    }
                }
            }

            return weights;
        }

        public void Clear()
        {
            this.vertices.Clear();
            this.matrix = CreateMatrix.Sparse<double>(0, 0);
        }
    }
}
