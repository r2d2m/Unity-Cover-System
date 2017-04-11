﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class CoverGenerator : MonoBehaviour {
    struct Edge
    {
        public readonly Vector3 v, w;
        public readonly float Length;

        public Edge(Vector3 v, Vector3 w)
        {
            this.v = v;
            this.w = w;
            Length = Vector3.Distance(v, w);
        }

        public override bool Equals(object obj)
            => obj is Edge e && ((e.v, e.w).Equals((v, w)) || (e.v, e.w).Equals((w, v)));
    }

    struct Triangle
    {
        public readonly Vector3[] vertices;
        public readonly Edge[] edges;

        public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            vertices = new Vector3[] { v1, v2, v3 };
            edges = new[] { new Edge(v1, v2), new Edge(v2, v3), new Edge(v3, v1) };
        }

        public bool Contains(Edge edge) =>
            edges.Any(e => e.Equals(edge));

        public bool Contains(Vector3 vertex) =>
            vertices.Any(v => v.Equals(vertex));
    }

    public GameObject CoverPoint;
    public Transform CoverPointParent;
    public float CoverPointsDistance = 3;

    private List<GameObject> coverPoints = new List<GameObject>();

    private void Start() => Generate();

    public void Generate()
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();

        var triangles = GetNavMeshTriangles(NavMesh.CalculateTriangulation());
        var edges = GetMeshEdges(triangles);
        var inTrianglesCount = InTrianglesCount(edges, triangles);

        var filtered = edges
            .Select((e, i) => new { edge = e, count = inTrianglesCount[i]})
            .Where(p => p.count == 1)
            .Select(p => p.edge)
            .Where(e => e.Length < 20) //Assume that outer edges are longest.
            .ToList();

        PlacePoints(filtered);

        sw.Stop();
        UnityEngine.Debug.Log("Total: " + sw.ElapsedMilliseconds);

        //filtered.ForEach(e => Debug.DrawLine(e.v, e.w, Color.red)); //Draw edgeloops
    }
    
    public void Remove() =>
        coverPoints.ForEach(Destroy);
    
    private IEnumerable<Triangle> GetNavMeshTriangles(NavMeshTriangulation nmt) =>
        nmt.indices
            .Select((e, i) => new { vertexIndex = e, index = i })
            .GroupBy(x => x.index / 3, x => x.vertexIndex)
            .Select(grp => grp.Select(vi => nmt.vertices[vi]).ToList())
            .Select(l => new Triangle(l[0], l[1], l[2]));

    private IEnumerable<Edge> GetMeshEdges(IEnumerable<Triangle> triangles) =>
        triangles
             .SelectMany(t => t.edges)
             .Distinct();

    private List<int> InTrianglesCount(IEnumerable<Edge> edges, IEnumerable<Triangle> triangles) =>
        edges
            .Select(e => triangles.Count(t => t.Contains(e)))
            .ToList();

    private void PlacePoints(List<Edge> edges) =>
        edges.ForEach(e  =>
        {
            if (e.Length >= 1)
            {
                var pointCount = (int) Math.Ceiling(e.Length / CoverPointsDistance);
                var offset = e.Length / (pointCount + 1);
                Enumerable.Range(1, pointCount + 1).ToList().ForEach(i =>
                {
                    var d = (e.w - e.v).normalized;
                    var g = Instantiate(CoverPoint, e.v + i * offset * d, Quaternion.LookRotation(Quaternion.Euler(0, -90, 0) * d)); //.normalized
                    g.transform.SetParent(CoverPointParent);
                    coverPoints.Add(g);
                });
            }
        });

    /*private void PlaceP(List<Edge> edges, int index)
    {
        if (index >= edges.Count()) return;

        var e = edges[index];
        if (e.Length >= 1)
        {
            var pointCount = (int) Math.Ceiling(e.Length / CoverPointsDistance);
            var offset = e.Length / (pointCount + 1);
            void L(int i)
            {
                if (i >= pointCount + 1) return;

                var d = (e.w - e.v).normalized;
                var g = Instantiate(CoverPoint, e.v + i * offset * d, Quaternion.LookRotation(Quaternion.Euler(0, -90, 0) * d)); //.normalized
                g.transform.SetParent(CoverPointParent);
                coverPoints.Add(g);

                L(i + 1);
            }
            L(0);
        }

        PlaceP(edges, index+1);
    }*/

}

/*private void PlacePoints(Edge[] edges) //LINQ
{
    foreach (var edge in edges)
    {
        if (edge.Length < 1)
            continue;

        var pointCount = (int) Math.Ceiling(edge.Length / CoverPointsDistance);
        var offset = edge.Length / (pointCount + 1);

        for (int i = 1; i <= pointCount; i++)
        {
            var g = Instantiate(CoverPoint, edge.v + i * offset * (edge.w - edge.v).normalized, Quaternion.LookRotation(Quaternion.Euler(0, -90, 0) * (edge.w - edge.v))); //.normalized
            g.transform.SetParent(CoverPointParent);
        }
    }
}*/