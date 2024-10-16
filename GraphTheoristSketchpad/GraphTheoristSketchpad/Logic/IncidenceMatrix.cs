using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using SkiaSharp;
using MathNet.Numerics.LinearAlgebra;

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
            Matrix<double> newMatrix = CreateMatrix.Sparse<double>(vertices.Count, matrix.ColumnCount);
            newMatrix.SetSubMatrix(0, 0, matrix);
            newMatrix[startIndex, newMatrix.ColumnCount - 1] = weight;
            newMatrix[endIndex, newMatrix.ColumnCount - 1] = weight;
            this.matrix = newMatrix;
        }

        public void AddEdge(Vertex start, Vertex end)
        {
            this.AddEdge(start, end, 1d);
        }
    }
}
