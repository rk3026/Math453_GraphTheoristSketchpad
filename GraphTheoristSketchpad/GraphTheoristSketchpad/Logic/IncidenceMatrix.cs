﻿using System;
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

        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.matrix.RowCount; ++i)
            {
                Vector<double> row = this.matrix.Row(i);
                for (int j = 0; j < row.Count; j++)
                {
                    sb.Append(row[j] + " ");
                }
                sb.Append('\n');
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
    }
}
