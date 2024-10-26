using ScottPlot;
using System.Collections;
using System.Windows.Input;

namespace GraphTheoristSketchpad.Logic
{
    public class Graph
    {
        // Event that is triggered when the graph changes
        public event EventHandler? GraphChanged;

        public ISet<Vertex> Vertices { get; } = new HashSet<Vertex>();

        private IncidenceMatrix matrix;

        public Graph()
        {
            matrix = new IncidenceMatrix();
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

            matrix.RemoveVertex(v);

            // Trigger the event when a vertex is removed
            OnGraphChanged();

            return true;
        }

        public string GetIncidenceMatrix()
        {
            return matrix.ToString();
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
            return matrix.getEdges();
        }

        public CoordinateLine[] getEdgesOn(Vertex v)
        {
            return matrix.getEdgesOn(v);
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
            matrix.AddEdge(start, end);

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

            matrix.RemoveEdge(start, end);

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
            throw new NotImplementedException();
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
    }
}
