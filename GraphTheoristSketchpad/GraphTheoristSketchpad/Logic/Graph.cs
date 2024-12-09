﻿using ScottPlot;
using System.CodeDom;
using System.Collections;
using System.Data;
using System.Net.Http.Headers;
using System.Windows.Input;

namespace GraphTheoristSketchpad.Logic
{
    public class Graph
    {
        // Event that is triggered when the graph changes
        public event EventHandler? GraphChanged;

        public ISet<Vertex> Vertices { get; } = new HashSet<Vertex>();

        public bool IsDirected
        {
            get
            {
                return this.incidenceMatrix.IsDirected;
            }

            set
            {
                this.incidenceMatrix.IsDirected = value;
            }
        }

        private IncidenceMatrix incidenceMatrix;

        public Graph()
        {
            incidenceMatrix = new IncidenceMatrix();
            this.IsDirected = false;
        }

        // Method to raise the GraphChanged event
        protected virtual void OnGraphChanged()
        {
            GraphChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool RemoveVertex(Vertex v)
        {
            if (!Vertices.Remove(v))
                return false;

            incidenceMatrix.RemoveVertex(v);

            // Trigger the event when a vertex is removed
            OnGraphChanged();

            return true;
        }

        public DataTable GetIncidenceMatrixTable()
        {
            return incidenceMatrix.ToDataTable();
        }

        // returns ordered list representing shortest path calculate with Dijkstra's algorithm
        public List<KeyValuePair<Vertex, Vertex>>? shortestPath(Vertex start, Vertex end)
        {
            return null;
        }

        // returns Dictionary of coloring if this graph can be colored by k colors
        public Dictionary<Vertex, int>? colorableBy(int k)
        {
            // colors of each vertex
            Dictionary<Vertex, int> coloring = new Dictionary<Vertex, int>();

            // neighbors of each vertex
            Dictionary<Vertex, ISet<Vertex>> neighbors = new Dictionary<Vertex, ISet<Vertex>>();

            // Get neighbors on each vertex
            foreach(Vertex v in this.Vertices)
            {       
                neighbors[v] = this.incidenceMatrix.getNeighborsOf(v);

                // if there's a loop, graph is not colorable
                if (neighbors[v].Contains(v))
                    return null;
            }

            if (validColoring(k, ref coloring, neighbors))
                return coloring;
            else
                return null;
        }

        // returns coloring of graph vertces with minimum colors
        public Dictionary<Vertex, int>? minimumColoring()
        {
            // no coloring for graph with loops
            if (this.incidenceMatrix.containsLoop())
                return null;

            // check every smallest k to see if graph is colorable by it
            int k = 0;
            while(true)
            {
                Dictionary<Vertex, int>? coloring = colorableBy(k);
                if (coloring != null)
                    return coloring;
                ++k;
            }
        }

        public int getChromaticNumber()
        {
            // no coloring for graph with loops
            if (this.incidenceMatrix.containsLoop())
                return 0;

            // check every smallest k to see if graph is colorable by it
            int k = 0;
            while (true)
            {
                Dictionary<Vertex, int>? coloring = colorableBy(k);
                if (coloring != null)
                    return k;
                ++k;
            }
        }

        // returns string representing chromatic polynomial
        public string getChromaticPolynomial()
        {
            // the number of ways to color a graph with no vertices is always 1
            if(this.Vertices.Count() == 0)
            {
                return "1";
            }
            // if there's loops then there's no way to color the graph
            else if(this.incidenceMatrix.containsLoop())
            {
                return "0";
            }

            List<double> polynomial = new List<double>();

            // stop adding polynomial terms when number of colors exceeds number of verticies in graph
            for(int k = 1; k <= this.Vertices.Count(); ++k)
            {
                // polynomial term for k colors
                List<double> cPolynomial = [1];

                // calculate k factorial
                double kFactorial = 1;
                for (int i = 2; i < k; ++i)
                    kFactorial *= k;

                // coefficient for entire cPolynomial
                double coefficient = getChromaticPolynomial(k)/kFactorial;

                // multiply out k roots for 0 to k-1
                for(int i = 0; i < k; ++i)
                {
                    List<double> multipledByK = new List<double>(cPolynomial);
                    multipledByK.Insert(0, 0);

                    // multiply each element by i then add to multipledByK
                    for (int j = 0; j < cPolynomial.Count(); ++j)
                    {
                        multipledByK[j] -= cPolynomial[j] * i;
                    }

                    cPolynomial = multipledByK;
                }

                // add polynomial term to full polynomial.
                for (int j = 0; j < cPolynomial.Count(); ++j)
                {
                    // increase polynomial size to accommodate more terms
                    if (polynomial.Count() <= j)
                        polynomial.Add(0);

                    polynomial[j] += cPolynomial[j]*coefficient;
                }
            }

            // convert polynomial to string
            string polynomialString = "";
            for(int i = 0; i < polynomial.Count; ++i)
            {
                if(polynomial[i] != 0)
                {
                    // add operator between terms
                    if (polynomial[i] < 0)
                    {
                        polynomialString += "-";
                    }
                    else if(polynomialString != "")
                    {
                        polynomialString += "+";
                    }

                    polynomialString += Math.Abs(polynomial[i]) + "k^" + i;
                }
            }

            return polynomialString;
        }

        // returns the number of ways to color this graph with k colors
        public int getChromaticPolynomial(int k)
        {
            // colors of each vertex
            Dictionary<Vertex, int> coloring = new Dictionary<Vertex, int>();

            // neighbors of each vertex
            Dictionary<Vertex, ISet<Vertex>> neighbors = new Dictionary<Vertex, ISet<Vertex>>();

            // Get neighbors on each vertex
            foreach (Vertex v in this.Vertices)
            {
                neighbors[v] = this.incidenceMatrix.getNeighborsOf(v);

                // if there's a loop, graph is not colorable
                if (neighbors[v].Contains(v))
                    return 0;
            }

            return getChromaticPolynomial(k, coloring, neighbors);
        }

        // returns number of ways to color this graph with k colors when colors in coloring are already decided
        private int getChromaticPolynomial(int k, Dictionary<Vertex, int> coloring, Dictionary<Vertex, ISet<Vertex>> neighbors)
        {
            // validate coloring so far
            foreach (Vertex v in coloring.Keys)
            {
                foreach (Vertex n in neighbors[v])
                {
                    if (coloring.ContainsKey(n) && coloring[v] == coloring[n])
                    {
                        return 0;
                    }
                }
            }

            // pick an uncolored vertex to color
            Vertex? cVertex = null;
            foreach (Vertex v in neighbors.Keys)
            {
                if (!coloring.ContainsKey(v))
                {
                    cVertex = v;
                    break;
                }
            }

            if (cVertex == null)
            {
                // coloring is complete and correct. Count it.
                return 1;
            }
            else
            {
                // total number of ways to choose remaining colors
                int sum = 0;

                // color cVertex
                for (int i = 0; i < k; ++i)
                {
                    Dictionary<Vertex, int> localColoring = new Dictionary<Vertex, int>(coloring);

                    localColoring[cVertex] = i;
                    sum += getChromaticPolynomial(k, localColoring, neighbors); 
                }

                return sum;
            }
        }

        // returns true of the coloring is valid and completes the coloring if any vertices are missing.
        private bool validColoring(int k, ref Dictionary<Vertex, int> coloring, Dictionary<Vertex, ISet<Vertex>> neighbors)
        {
            // validate coloring so far
            foreach (Vertex v in coloring.Keys)
            {
                foreach (Vertex n in neighbors[v])
                {
                    if (coloring.ContainsKey(n) && coloring[v] == coloring[n])
                    {
                        return false;
                    }
                }
            }

            // pick an uncolored vertex to color
            Vertex? cVertex = null;
            foreach (Vertex v in neighbors.Keys)
            {
                if (!coloring.ContainsKey(v))
                {
                    cVertex = v;
                    break;
                }
            }

            if(cVertex == null)
            {
                return true;
            }
            else
            {
                // color cVertex
                for(int i = 0; i < k; ++i)
                {
                    Dictionary<Vertex, int> localColoring = new Dictionary<Vertex, int>(coloring);
                    
                    // try to maximize the number of colors used
                    localColoring[cVertex] = (i+coloring.Count()) % k;
                    if(validColoring(k, ref localColoring, neighbors))
                    {
                        coloring = localColoring;
                        return true;
                    }
                }
            }

            return false;
        }

        public Vertex? getNearestVertex(Coordinates location, double maxDistance = 15)
        {
            double closestCoordinates = double.MaxValue;
            Vertex closestVertex = null;
            foreach (Vertex v in Vertices)
            {
                double currentVertexDistance = new CoordinateLine(v.Location, location).Length;
                if (currentVertexDistance < closestCoordinates)
                {
                    closestCoordinates = currentVertexDistance;
                    closestVertex = v;
                }
            }
            if (closestCoordinates > maxDistance)
            {
                return null;
            }
            else
            {
                return closestVertex;
            }
        }

        public CoordinateLine? getNearestEdge(Coordinates location, double maxDistance = 15)
        {
            CoordinateLine? closestEdge = null;
            CoordinateLine[] edges = getEdges();
            double closestDistance = double.MaxValue;

            foreach (CoordinateLine e in edges)
            {
                double distance = GetDistancePointToLineSegment(e.Start, e.End, location);

                if (distance < closestDistance && distance <= maxDistance)
                {
                    closestDistance = distance;
                    closestEdge = e;
                }
            }

            return closestEdge;
        }

        private double GetDistancePointToLineSegment(Coordinates lineStart, Coordinates lineEnd, Coordinates point)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            if (dx == 0 && dy == 0)
            {
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));
            }

            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Max(0, Math.Min(1, t));

            double closestX = lineStart.X + t * dx;
            double closestY = lineStart.Y + t * dy;

            return Math.Sqrt(Math.Pow(point.X - closestX, 2) + Math.Pow(point.Y - closestY, 2));
        }

        public CoordinateLine[] getEdges()
        {
            return incidenceMatrix.getEdges();
        }

        public CoordinateLine[] getEdgesOn(Vertex v)
        {
            return incidenceMatrix.getEdgesOn(v);
        }

        public bool Add(Vertex item)
        {
            Vertices.Add(item);

            // Trigger the event when a vertex is added
            OnGraphChanged();

            return true;
        }

        public bool AddEdge(Vertex start, Vertex end)
        {
            incidenceMatrix.AddEdge(start, end);

            // Trigger the event when an edge is added
            OnGraphChanged();

            return true;
        }

        public bool RemoveEdge(CoordinateLine edge)
        {
            Vertex? start = getNearestVertex(edge.Start, 1);
            Vertex? end = getNearestVertex(edge.End, 1);
            if (start == null || end == null)
            {
                return false;
            }

            incidenceMatrix.RemoveEdge(start, end);

            // Trigger the event when an edge is removed
            OnGraphChanged();

            return true;
        }

        public void ExceptWith(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public void IntersectWith(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSubsetOf(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public bool SetEquals(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public void UnionWith(IEnumerable<Vertex> other)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            this.Vertices.Clear();
            this.incidenceMatrix.Clear();
        }

        public bool Contains(Vertex item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Vertex[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Vertex item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int getEdgeCount()
        {
            return this.incidenceMatrix.getEdgeCount();
        }

        public int GetComponentCount()
        {
            HashSet<Vertex> visited = new HashSet<Vertex>();
            int componentCount = 0;

            foreach (var vertex in Vertices)
            {
                if (!visited.Contains(vertex))
                {
                    DFS(vertex, visited);
                    componentCount++;
                }
            }

            return componentCount;
        }

        private void DFS(Vertex vertex, HashSet<Vertex> visited)
        {
            visited.Add(vertex);

            foreach (var edge in getEdgesOn(vertex))
            {
                Vertex? neighbor = GetOtherVertex(edge, vertex);
                if (neighbor != null && !visited.Contains(neighbor))
                {
                    DFS(neighbor, visited);
                }
            }
        }

        // Gets the neighbor of a vertex from a specific edge and vertex. (helper function for DFS)
        private Vertex? GetOtherVertex(CoordinateLine edge, Vertex vertex)
        {
            if (vertex.Location.Equals(edge.Start))
            {
                return Vertices.FirstOrDefault(v => v.Location.Equals(edge.End));
            }
            else if (vertex.Location.Equals(edge.End))
            {
                return Vertices.FirstOrDefault(v => v.Location.Equals(edge.Start));
            }

            return null;
        }

        private bool IsBridge(CoordinateLine edge)
        {
            // Get the current count of connected components
            int originalComponentCount = GetComponentCount();

            // Temporarily remove the edge
            RemoveEdge(edge);

            // Recalculate the number of connected components
            int newComponentCount = GetComponentCount();

            // Restore the edge
            AddEdge(getNearestVertex(edge.Start), getNearestVertex(edge.End));

            // If the component count increased, the edge is a bridge
            return newComponentCount > originalComponentCount;
        }

        public List<CoordinateLine> GetBridges()
        {
            List<CoordinateLine> bridges = new List<CoordinateLine>();

            foreach (var edge in getEdges())
            {
                if (IsBridge(edge))
                {
                    bridges.Add(edge);
                }
            }

            return bridges;
        }

        public int GetVertexDegree(Vertex v)
        {
            if (!Vertices.Contains(v))
                throw new ArgumentException("The vertex does not exist in the graph.");

            // Get all edges connected to the vertex
            CoordinateLine[] edges = getEdgesOn(v);

            // Return the count of those edges as the degree of the vertex
            return edges.Length;
        }

        // Helper function for determining if bipartite
        private Dictionary<Vertex, int>? PerformBFSColoring()
        {
            // Dictionary to store the color of each vertex (-1: not colored, 0: color 0, 1: color 1)
            Dictionary<Vertex, int> color = new Dictionary<Vertex, int>();
            foreach (var vertex in Vertices)
            {
                color[vertex] = -1; // Initialize all vertices as not colored
            }

            // BFS to color the graph
            foreach (var startVertex in Vertices)
            {
                if (color[startVertex] == -1) // Start BFS only if the vertex is not colored
                {
                    Queue<Vertex> queue = new Queue<Vertex>();
                    queue.Enqueue(startVertex);
                    color[startVertex] = 0; // Assign the first color

                    while (queue.Count > 0)
                    {
                        Vertex current = queue.Dequeue();
                        int currentColor = color[current];

                        foreach (var edge in getEdgesOn(current))
                        {
                            Vertex? neighbor = GetOtherVertex(edge, current);
                            if (neighbor != null)
                            {
                                if (color[neighbor] == -1)
                                {
                                    // Assign the opposite color to the neighbor
                                    color[neighbor] = 1 - currentColor;
                                    queue.Enqueue(neighbor);
                                }
                                else if (color[neighbor] == currentColor)
                                {
                                    // If the neighbor has the same color, the graph is not bipartite
                                    return null; // indicates failure
                                }
                            }
                        }
                    }
                }
            }

            return color; // Return the color mapping
        }


        public bool IsBipartite()
        {
            // Check if the BFS coloring returns a valid color mapping
            return PerformBFSColoring() != null;
        }

        // Returns the two bipartite sets in a bipartite graph.
        public List<HashSet<Vertex>> GetPartiteSets()
        {
            var colorMapping = PerformBFSColoring();

            if (colorMapping == null)
                return new List<HashSet<Vertex>>(); // Return empty list if the graph is not bipartite

            // Separate vertices into two sets based on their colors
            HashSet<Vertex> setA = new HashSet<Vertex>();
            HashSet<Vertex> setB = new HashSet<Vertex>();

            foreach (var pair in colorMapping)
            {
                if (pair.Value == 0)
                    setA.Add(pair.Key);
                else
                    setB.Add(pair.Key);
            }

            return new List<HashSet<Vertex>> { setA, setB };
        }


    }
}
