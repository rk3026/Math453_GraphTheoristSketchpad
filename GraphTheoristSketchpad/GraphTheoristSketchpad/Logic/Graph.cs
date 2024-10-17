using ScottPlot;
using System.Collections;

namespace GraphTheoristSketchpad.Logic
{
    public class Graph
    {
        public ISet<Vertex> Vertices { get; } = new HashSet<Vertex>();
        public ISet<Edge> Edges { get; } = new HashSet<Edge>();


        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        private IncidenceMatrix matrix;

        public Graph()
        {
            matrix = new IncidenceMatrix();
        }

        public Vertex? getNearestVertex(Coordinates location, double maxDistance = 15)
        {
            double closestCoordinates = double.MaxValue;
            Vertex closestVertex = null;
            foreach (Vertex v in Vertices)
            {
                double currentDistance = new CoordinateLine(v.Location, location).Length;
                if (currentDistance < closestCoordinates)
                {
                    closestCoordinates = currentDistance;
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
