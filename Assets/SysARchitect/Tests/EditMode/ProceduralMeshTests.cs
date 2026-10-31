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
        [TestCase(NodeType.DockerHost)]
        [TestCase(NodeType.KubernetesCluster)]
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

        [TestCase(NodeType.Frontend)]
        [TestCase(NodeType.Backend)]
        [TestCase(NodeType.Database)]
        [TestCase(NodeType.LoadBalancer)]
        [TestCase(NodeType.Cache)]
        [TestCase(NodeType.Container)]
        [TestCase(NodeType.KubernetesNode)]
        [TestCase(NodeType.KubernetesPod)]
        [TestCase(NodeType.DockerHost)]
        [TestCase(NodeType.KubernetesCluster)]
        public void DetailedHardwareHasBoundedGeometryAndCorrectFaceNormals(NodeType type)
        {
            var mesh = ProceduralMeshGenerator.GenerateHolographicMesh(type);
            try
            {
                Assert.That(mesh.vertexCount, Is.LessThan(1500));
                Assert.That(mesh.subMeshCount, Is.EqualTo(1));
                Assert.That(mesh.colors.Length, Is.EqualTo(mesh.vertexCount));
                Assert.That(mesh.colors, Has.Some.EqualTo(Color.white), "Emissive hardware regions must exist.");
                var v = mesh.vertices; var n = mesh.normals; var t = mesh.triangles;
                for (int i = 0; i < t.Length; i += 3)
                {
                    var normal = Vector3.Cross(v[t[i + 1]] - v[t[i]], v[t[i + 2]] - v[t[i]]);
                    Assert.That(Vector3.Dot(normal, n[t[i]]), Is.GreaterThan(0), $"{type}: face {i / 3}");
                }
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
