﻿using ScottPlot;
using System.Collections;
using System.Data;
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

        private Vertex? GetOtherVertex(CoordinateLine edge, Vertex vertex)
        {
            // Assuming CoordinateLine.Start and CoordinateLine.End give the coordinates of the edge's endpoints
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

        public bool IsBridge(CoordinateLine edge)
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

    }
}
