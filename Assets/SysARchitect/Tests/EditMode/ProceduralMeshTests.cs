using NUnit.Framework;
using UnityEngine;
using SysARchitect.Core;
using SysARchitect.Visualization;

namespace SysARchitect.Tests
{
    public class ProceduralMeshTests
    {
        [TestCase(NodeType.Frontend)]
        [TestCase(NodeType.Backend)]
        [TestCase(NodeType.Database)]
        [TestCase(NodeType.LoadBalancer)]
        [TestCase(NodeType.Cache)]
        [TestCase(NodeType.Container)]
        [TestCase(NodeType.KubernetesNode)]
        [TestCase(NodeType.KubernetesPod)]
        public void ConvexInfrastructureMeshesHaveOutwardFacingTriangles(NodeType type)
        {
            var mesh = ProceduralMeshGenerator.GenerateMesh(type);
            try
            {
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Assert.That(Vector3.Dot(normal, (a + b + c) / 3), Is.GreaterThan(0),
                        $"{type} triangle {i / 3} is degenerate or faces inward and will be culled.");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
