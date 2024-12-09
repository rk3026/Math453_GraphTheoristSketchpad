using ScottPlot;
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
            // Get the DataTable from the incidence matrix
            DataTable dt = incidenceMatrix.ToDataTable();

            // Loop through all the vertices to ensure they are present in the table
            foreach (Vertex v in Vertices)
            {
                // Check if the vertex is already in the first column (Vertex column)
                bool vertexExists = false;

                foreach (DataRow row in dt.Rows)
                {
                    if (row["Vertex"] == v)
                    {
                        vertexExists = true;
                        break;
                    }
                }

                // If the vertex isn't found, add it
                if (!vertexExists)
                {
                    DataRow newRow = dt.NewRow();
                    newRow["Vertex"] = v;

                    // Add columns for edges, initialize them to 0 or any default value
                    for (int col = 1; col < dt.Columns.Count; col++)
                    {
                        newRow[col] = 0;  // Set default value for the edges (0 or another appropriate default)
                    }

                    // Add the new row to the DataTable
                    dt.Rows.Add(newRow);
                }
            }

            return dt;
        }

        //DEBUG TEST FUNCTION
        public DataTable GetIncidenceMatrixTable(Vertex v)
        {
            // Get the DataTable from the incidence matrix
            DataTable dt = incidenceMatrix.getComponentMatrixOf(v).ToDataTable();

            return dt;
        }

        // returns ordered list representing shortest path calculate with Dijkstra's algorithm
        public List<KeyValuePair<Vertex, Vertex>>? getShortestPath(Vertex start, Vertex end)
        {
            // Dictionary to store the shortest distance from the start vertex to each vertex
            Dictionary<Vertex, double> distances = new Dictionary<Vertex, double>();

            // Dictionary to store the previous vertex on the shortest path to each vertex
            Dictionary<Vertex, Vertex?> previous = new Dictionary<Vertex, Vertex?>();

            // Priority queue to explore vertices with the smallest known distance
            PriorityQueue<Vertex, double> priorityQueue = new PriorityQueue<Vertex, double>();

            // Initialize distances and priority queue
            foreach (var vertex in Vertices)
            {
                distances[vertex] = double.MaxValue;
                previous[vertex] = null;
            }
            distances[start] = 0;
            priorityQueue.Enqueue(start, 0);

            while (priorityQueue.Count > 0)
            {
                // Get the vertex with the smallest distance
                var currentVertex = priorityQueue.Dequeue();

                // If we reached the destination, reconstruct the path
                if (currentVertex.Equals(end))
                {
                    var path = new List<KeyValuePair<Vertex, Vertex>>();
                    for (var at = end; at != null && previous[at] != null; at = previous[at])
                    {
                        path.Add(new KeyValuePair<Vertex, Vertex>(previous[at]!, at));
                    }
                    path.Reverse();
                    return path;
                }

                // Skip if the current distance is greater than the known shortest distance
                if (distances[currentVertex] == double.MaxValue)
                    continue;

                // Explore neighbors
                var neighbors = incidenceMatrix.getNeighborsOf(currentVertex);
                foreach (var neighbor in neighbors)
                {
                    List<double> weights = incidenceMatrix.getWeight(currentVertex, neighbor);
                    double minWeight = weights.Min();
                    double distanceThroughCurrent = distances[currentVertex] + minWeight;

                    if (distanceThroughCurrent < distances[neighbor])
                    {
                        // Update the shortest distance and the path
                        distances[neighbor] = distanceThroughCurrent;
                        previous[neighbor] = currentVertex;
                        priorityQueue.Enqueue(neighbor, distanceThroughCurrent);
                    }
                }
            }

            // If we reach here, there's no path from start to end
            return null;
        }

        // Returns the maximum flow from start to end using the Ford-Fulkerson algorithm
        public double GetMaxFlow(Vertex start, Vertex end)
        {
            // Initialize the residual graph with the same vertices and edges as the original graph
            Dictionary<CoordinateLine, double> residualGraph = new Dictionary<CoordinateLine, double>();

            foreach (CoordinateLine edge in incidenceMatrix.getEdges())
            {
                var startVertex = getNearestVertex(edge.Start);
                var endVertex = getNearestVertex(edge.End);

                // Sum up the weights for this edge
                residualGraph[edge] = incidenceMatrix.getWeight(startVertex, endVertex).Sum();

                // Add reverse edge with initial capacity 0 if not already present
                var reverseEdge = new CoordinateLine(edge.End, edge.Start);
                if (!residualGraph.ContainsKey(reverseEdge))
                {
                    residualGraph[reverseEdge] = 0;
                }
            }

            double maxFlow = 0;

            while (true)
            {
                // Find an augmenting path using BFS
                var augmentingPath = FindAugmentingPath(start, end, residualGraph);
                if (augmentingPath == null) break; // No more augmenting paths

                // Find the minimum capacity (bottleneck) in the augmenting path
                double pathFlow = double.MaxValue;
                for (int i = 0; i < augmentingPath.Count - 1; i++)
                {
                    var u = augmentingPath[i];
                    var v = augmentingPath[i + 1];
                    var edge = new CoordinateLine(u.Location, v.Location);
                    pathFlow = Math.Min(pathFlow, residualGraph[edge]);
                }

                // Update the residual capacities in the graph
                for (int i = 0; i < augmentingPath.Count - 1; i++)
                {
                    var u = augmentingPath[i];
                    var v = augmentingPath[i + 1];
                    var edge = new CoordinateLine(u.Location, v.Location);
                    var reverseEdge = new CoordinateLine(v.Location, u.Location);

                    residualGraph[edge] -= pathFlow;
                    residualGraph[reverseEdge] += pathFlow; // Update reverse edge for the flow
                }

                // Add path flow to the total max flow
                maxFlow += pathFlow;
            }

            return maxFlow;
        }

        // Helper function: Find an augmenting path using BFS
        private List<Vertex>? FindAugmentingPath(Vertex start, Vertex end, Dictionary<CoordinateLine, double> residualGraph)
        {
            Queue<Vertex> queue = new Queue<Vertex>();
            Dictionary<Vertex, Vertex?> parent = new Dictionary<Vertex, Vertex?>();

            queue.Enqueue(start);
            parent[start] = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current.Equals(end))
                {
                    // Reconstruct the augmenting path
                    var path = new List<Vertex>();
                    for (var v = end; v != null; v = parent[v])
                    {
                        path.Add(v);
                    }
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in incidenceMatrix.getNeighborsOf(current))
                {
                    var edge = new CoordinateLine(current.Location, neighbor.Location);
                    if (!parent.ContainsKey(neighbor) && residualGraph[edge] > 0) // Valid path with capacity > 0
                    {
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return null; // No augmenting path found
        }

        public List<KeyValuePair<Vertex, Vertex>> GetSpanningTreeWithRoot(Vertex root)
        {
            List<KeyValuePair<Vertex, Vertex>> spanningTreeEdges = new List<KeyValuePair<Vertex, Vertex>>();
            HashSet<Vertex> visited = new HashSet<Vertex>();

            void DFS(Vertex current)
            {
                visited.Add(current);

                var neighbors = incidenceMatrix.getNeighborsOf(current);
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        spanningTreeEdges.Add(new KeyValuePair<Vertex, Vertex>(current, neighbor));
                        DFS(neighbor);
                    }
                }
            }

            DFS(root);
            return spanningTreeEdges;
        }

        public List<KeyValuePair<Vertex, Vertex>> GetSpanningTree()
        {
            List<KeyValuePair<Vertex, Vertex>> spanningTreeEdges = new List<KeyValuePair<Vertex, Vertex>>();
            HashSet<Vertex> visited = new HashSet<Vertex>();
            PriorityQueue<(Vertex, Vertex, double), double> edgeQueue = new PriorityQueue<(Vertex, Vertex, double), double>();

            // Start from an arbitrary vertex (e.g., the first one in the graph)
            Vertex start = Vertices.First();
            visited.Add(start);

            // Add all edges from the starting vertex to the priority queue
            var neighbors = incidenceMatrix.getNeighborsOf(start);
            foreach (var neighbor in neighbors)
            {
                double weight = incidenceMatrix.getWeight(start, neighbor).Min();
                edgeQueue.Enqueue((start, neighbor, weight), weight);
            }

            while (edgeQueue.Count > 0)
            {
                // Get the edge with the smallest weight
                var (u, v, w) = edgeQueue.Dequeue();

                // Skip if both vertices are already in the spanning tree
                if (visited.Contains(u) && visited.Contains(v))
                    continue;

                // Add the edge to the spanning tree
                spanningTreeEdges.Add(new KeyValuePair<Vertex, Vertex>(u, v));

                // Add the new vertex to the visited set
                Vertex newVertex = visited.Contains(u) ? v : u;
                visited.Add(newVertex);

                // Add all edges from the new vertex to the priority queue
                neighbors = incidenceMatrix.getNeighborsOf(newVertex);
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        double weight = incidenceMatrix.getWeight(newVertex, neighbor).Min();
                        edgeQueue.Enqueue((newVertex, neighbor, weight), weight);
                    }
                }
            }

            return spanningTreeEdges;
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

            // Temporarily remove the edge (DO NOT USE REMOVE EDGE FUNCTION OR GRAPHCHANGED WILL BE CALLED)
            Vertex? start = getNearestVertex(edge.Start, 1);
            Vertex? end = getNearestVertex(edge.End, 1);
            if (start == null || end == null)
            {
                return false;
            }

            incidenceMatrix.RemoveEdge(start, end);

            // Recalculate the number of connected components
            int newComponentCount = GetComponentCount();

            // Add the edge back
            incidenceMatrix.AddEdge(start, end);

            // Restore the edge

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
