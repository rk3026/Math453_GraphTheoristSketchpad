using ScottPlot;
using System.Collections;
using System.Windows.Input;

namespace GraphTheoristSketchpad.Logic
{
    public class Graph
    {
        public ISet<Vertex> Vertices { get; } = new HashSet<Vertex>();


        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        private IncidenceMatrix matrix;

        public Graph()
        {
            matrix = new IncidenceMatrix();
        }

        public bool RemoveVertex(Vertex v)
        {
            this.Vertices.Remove(v);
            return true;
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

            // Iterate over all edges
            foreach (CoordinateLine e in edges)
            {
                // Calculate the perpendicular distance from location to the edge
                double distance = GetDistancePointToLineSegment(e.Start, e.End, location);

                // If the edge is within maxDistance and is the closest so far, update closestEdge
                if (distance < closestDistance && distance <= maxDistance)
                {
                    closestDistance = distance;
                    closestEdge = e;
                }
            }

            // Return the closest edge (or null if none is within maxDistance)
            return closestEdge;
        }

        private double GetDistancePointToLineSegment(Coordinates lineStart, Coordinates lineEnd, Coordinates point)
        {
            double dx = lineEnd.X - lineStart.X;
            double dy = lineEnd.Y - lineStart.Y;

            if (dx == 0 && dy == 0)
            {
                // The line segment is a point, return the distance to that point
                return Math.Sqrt(Math.Pow(point.X - lineStart.X, 2) + Math.Pow(point.Y - lineStart.Y, 2));
            }

            // Calculate t (parameter that gives the projection of the point on the line)
            double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / (dx * dx + dy * dy);

            // Clamp t to the range [0, 1] to ensure it falls within the segment
            t = Math.Max(0, Math.Min(1, t));

            // Find the closest point on the segment
            double closestX = lineStart.X + t * dx;
            double closestY = lineStart.Y + t * dy;

            // Return the Euclidean distance from the point to the closest point on the segment
            return Math.Sqrt(Math.Pow(point.X - closestX, 2) + Math.Pow(point.Y - closestY, 2));
        }



        public CoordinateLine[] getEdges()
        {
            return matrix.getEdges();
        }

        public bool Add(Vertex item)
        {
            Vertices.Add(item);
            return true;
        }

        public bool AddEdge(Vertex start, Vertex end)
        {
            this.matrix.AddEdge(start, end);
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

            this.matrix.RemoveEdge(start, end);
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
