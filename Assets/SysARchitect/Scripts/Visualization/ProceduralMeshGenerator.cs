namespace SysARchitect.Visualization
{
    using UnityEngine;
    using SysARchitect.Core;

    /// <summary>
    /// Generates procedural meshes for infrastructure node types.
    /// Each shape is a stylised low-poly representation sized for tabletop AR
    /// (roughly 0.3–0.5 units). All returned meshes include normals and UVs.
    /// </summary>
    public static class ProceduralMeshGenerator
    {
        // ────────────────────────────────────────────────────────────────
        // Public API
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a procedural mesh matching <paramref name="nodeType"/>.
        /// </summary>
        public static Mesh GenerateMesh(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.Frontend:       return GenerateFrontend();
                case NodeType.Backend:        return GenerateBackend();
                case NodeType.Database:       return GenerateDatabase();
                case NodeType.LoadBalancer:   return GenerateLoadBalancer();
                case NodeType.Cache:          return GenerateCache();
                case NodeType.Container:      return GenerateContainer();
                case NodeType.KubernetesNode: return GenerateKubernetesNode();
                case NodeType.KubernetesPod:  return GenerateKubernetesPod();
                default:                      return GenerateBackend();
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Individual generators
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// A flat monitor/screen shape — a rectangular prism (0.4w × 0.3h × 0.05d)
        /// with a slight top-edge bevel.
        /// </summary>
        static Mesh GenerateFrontend()
        {
            float w = 0.4f;
            float h = 0.3f;
            float d = 0.05f;
            float bevel = 0.015f; // bevel inset on top edge

            float hw = w * 0.5f;
            float hh = h * 0.5f;
            float hd = d * 0.5f;

            // 10 unique positions: 8 base box + 4 beveled top-front/top-back
            // We'll build face-by-face for correct normals.
            var mesh = new Mesh { name = "Frontend" };

            // Bottom-left-front = 0, clockwise around the base box
            // Front face, back face, left, right, bottom, top (flat), bevel strip
            Vector3[] verts = new Vector3[]
            {
                // Front face (0-3)  — normal (0,0,-1) but we want outward; our front is -Z
                new Vector3(-hw, -hh, -hd),
                new Vector3( hw, -hh, -hd),
                new Vector3( hw,  hh - bevel, -hd),
                new Vector3(-hw,  hh - bevel, -hd),

                // Back face (4-7)
                new Vector3( hw, -hh,  hd),
                new Vector3(-hw, -hh,  hd),
                new Vector3(-hw,  hh - bevel,  hd),
                new Vector3( hw,  hh - bevel,  hd),

                // Bottom face (8-11)
                new Vector3(-hw, -hh,  hd),
                new Vector3( hw, -hh,  hd),
                new Vector3( hw, -hh, -hd),
                new Vector3(-hw, -hh, -hd),

                // Left face (12-15)
                new Vector3(-hw, -hh,  hd),
                new Vector3(-hw, -hh, -hd),
                new Vector3(-hw,  hh - bevel, -hd),
                new Vector3(-hw,  hh - bevel,  hd),

                // Right face (16-19)
                new Vector3( hw, -hh, -hd),
                new Vector3( hw, -hh,  hd),
                new Vector3( hw,  hh - bevel,  hd),
                new Vector3( hw,  hh - bevel, -hd),

                // Bevel front strip (20-23)
                new Vector3(-hw,  hh - bevel, -hd),
                new Vector3( hw,  hh - bevel, -hd),
                new Vector3( hw - bevel,  hh, -hd + bevel),
                new Vector3(-hw + bevel,  hh, -hd + bevel),

                // Bevel back strip (24-27)
                new Vector3( hw,  hh - bevel,  hd),
                new Vector3(-hw,  hh - bevel,  hd),
                new Vector3(-hw + bevel,  hh,  hd - bevel),
                new Vector3( hw - bevel,  hh,  hd - bevel),

                // Top face (28-31)
                new Vector3(-hw + bevel,  hh, -hd + bevel),
                new Vector3( hw - bevel,  hh, -hd + bevel),
                new Vector3( hw - bevel,  hh,  hd - bevel),
                new Vector3(-hw + bevel,  hh,  hd - bevel),

                // Bevel left strip (32-35)
                new Vector3(-hw,  hh - bevel,  hd),
                new Vector3(-hw,  hh - bevel, -hd),
                new Vector3(-hw + bevel,  hh, -hd + bevel),
                new Vector3(-hw + bevel,  hh,  hd - bevel),

                // Bevel right strip (36-39)
                new Vector3( hw,  hh - bevel, -hd),
                new Vector3( hw,  hh - bevel,  hd),
                new Vector3( hw - bevel,  hh,  hd - bevel),
                new Vector3( hw - bevel,  hh, -hd + bevel),
            };

            int[] tris = new int[]
            {
                // Front
                0,2,1, 0,3,2,
                // Back
                4,6,5, 4,7,6,
                // Bottom
                8,10,9, 8,11,10,
                // Left
                12,14,13, 12,15,14,
                // Right
                16,18,17, 16,19,18,
                // Bevel front
                20,22,21, 20,23,22,
                // Bevel back
                24,26,25, 24,27,26,
                // Top
                28,30,29, 28,31,30,
                // Bevel left
                32,34,33, 32,35,34,
                // Bevel right
                36,38,37, 36,39,38,
            };

            Vector2[] uvs = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                uvs[i] = new Vector2(
                    Mathf.InverseLerp(-hw, hw, verts[i].x),
                    Mathf.InverseLerp(-hh, hh, verts[i].y));
            }

            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// A server tower — taller rectangular box (0.15w × 0.4h × 0.15d).
        /// UV layout enables shader-driven horizontal detail lines.
        /// </summary>
        static Mesh GenerateBackend()
        {
            return GenerateBox(0.15f, 0.4f, 0.15f, "Backend");
        }

        /// <summary>
        /// Classic database drum — cylinder with 16 sides, radius 0.15, height 0.25.
        /// </summary>
        static Mesh GenerateDatabase()
        {
            return GenerateCylinder(16, 0.15f, 0.25f, "Database");
        }

        /// <summary>
        /// Hexagonal prism — 6 sides, radius 0.2, height 0.1.
        /// Suggests a router or switch device.
        /// </summary>
        static Mesh GenerateLoadBalancer()
        {
            return GenerateCylinder(6, 0.2f, 0.1f, "LoadBalancer");
        }

        /// <summary>
        /// Small squarish prism (0.2w × 0.12h × 0.2d), like a memory chip.
        /// </summary>
        static Mesh GenerateCache()
        {
            return GenerateBox(0.2f, 0.12f, 0.2f, "Cache");
        }

        /// <summary>
        /// Rectangular box (0.25w × 0.15h × 0.15d), like a shipping container.
        /// </summary>
        static Mesh GenerateContainer()
        {
            return GenerateBox(0.25f, 0.15f, 0.15f, "Container");
        }

        /// <summary>
        /// Octagonal prism — 8 sides, radius 0.2, height 0.15.
        /// Distinct from the hexagonal load balancer.
        /// </summary>
        static Mesh GenerateKubernetesNode()
        {
            return GenerateCylinder(8, 0.2f, 0.15f, "KubernetesNode");
        }

        /// <summary>
        /// Small box (0.1 × 0.1 × 0.1), a compact cube for pods.
        /// </summary>
        static Mesh GenerateKubernetesPod()
        {
            return GenerateBox(0.1f, 0.1f, 0.1f, "KubernetesPod");
        }

        // ────────────────────────────────────────────────────────────────
        // Shared primitive builders
        // ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a box mesh with the given dimensions, centred at origin.
        /// Each face uses separate vertices for hard-edge normals.
        /// </summary>
        static Mesh GenerateBox(float width, float height, float depth, string meshName)
        {
            var mesh = new Mesh { name = meshName };
            float hw = width * 0.5f;
            float hh = height * 0.5f;
            float hd = depth * 0.5f;

            Vector3[] verts = new Vector3[24];
            Vector3[] normals = new Vector3[24];
            Vector2[] uvs = new Vector2[24];
            int[] tris = new int[36];

            int v = 0;
            int t = 0;

            // Helper to add a quad (two triangles).
            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n,
                         Vector2 uva, Vector2 uvb, Vector2 uvc, Vector2 uvd)
            {
                int start = v;
                verts[v] = a; normals[v] = n; uvs[v] = uva; v++;
                verts[v] = b; normals[v] = n; uvs[v] = uvb; v++;
                verts[v] = c; normals[v] = n; uvs[v] = uvc; v++;
                verts[v] = d; normals[v] = n; uvs[v] = uvd; v++;

                tris[t++] = start;
                tris[t++] = start + 1;
                tris[t++] = start + 2;
                tris[t++] = start;
                tris[t++] = start + 2;
                tris[t++] = start + 3;
            }

            // Front  (–Z)
            AddQuad(
                new Vector3(-hw, -hh, -hd), new Vector3(-hw,  hh, -hd),
                new Vector3( hw,  hh, -hd), new Vector3( hw, -hh, -hd),
                Vector3.back,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            // Back   (+Z)
            AddQuad(
                new Vector3( hw, -hh,  hd), new Vector3( hw,  hh,  hd),
                new Vector3(-hw,  hh,  hd), new Vector3(-hw, -hh,  hd),
                Vector3.forward,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            // Left   (–X)
            AddQuad(
                new Vector3(-hw, -hh,  hd), new Vector3(-hw,  hh,  hd),
                new Vector3(-hw,  hh, -hd), new Vector3(-hw, -hh, -hd),
                Vector3.left,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            // Right  (+X)
            AddQuad(
                new Vector3( hw, -hh, -hd), new Vector3( hw,  hh, -hd),
                new Vector3( hw,  hh,  hd), new Vector3( hw, -hh,  hd),
                Vector3.right,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            // Top    (+Y)
            AddQuad(
                new Vector3(-hw,  hh, -hd), new Vector3(-hw,  hh,  hd),
                new Vector3( hw,  hh,  hd), new Vector3( hw,  hh, -hd),
                Vector3.up,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            // Bottom (–Y)
            AddQuad(
                new Vector3(-hw, -hh,  hd), new Vector3(-hw, -hh, -hd),
                new Vector3( hw, -hh, -hd), new Vector3( hw, -hh,  hd),
                Vector3.down,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0));

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds an N-sided prism (cylinder) centred at origin with the given
        /// radius and height. Top and bottom caps included.
        /// </summary>
        static Mesh GenerateCylinder(int sides, float radius, float height, string meshName)
        {
            var mesh = new Mesh { name = meshName };
            float hh = height * 0.5f;

            // Side verts: sides * 4 (quad per side, unshared for flat normals).
            // Cap verts:  sides + 1 each (fan from centre).
            int sideVertCount = sides * 4;
            int capVertCount = sides + 1;
            int totalVerts = sideVertCount + capVertCount * 2;
            int totalTris = sides * 2 * 3 + sides * 3 * 2; // side tris + cap tris

            var verts   = new Vector3[totalVerts];
            var normals = new Vector3[totalVerts];
            var uvArray = new Vector2[totalVerts];
            var tris    = new int[totalTris];

            int vi = 0;
            int ti = 0;

            float angleStep = Mathf.PI * 2f / sides;

            // ── Side faces ─────────────────────────────────────────────
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * angleStep;
                float a1 = (i + 1) * angleStep;
                float cos0 = Mathf.Cos(a0), sin0 = Mathf.Sin(a0);
                float cos1 = Mathf.Cos(a1), sin1 = Mathf.Sin(a1);

                Vector3 bl = new Vector3(cos0 * radius, -hh, sin0 * radius);
                Vector3 tl = new Vector3(cos0 * radius,  hh, sin0 * radius);
                Vector3 tr = new Vector3(cos1 * radius,  hh, sin1 * radius);
                Vector3 br = new Vector3(cos1 * radius, -hh, sin1 * radius);

                // Flat normal = average of the two edge directions
                float midAngle = (a0 + a1) * 0.5f;
                Vector3 n = new Vector3(Mathf.Cos(midAngle), 0f, Mathf.Sin(midAngle)).normalized;

                float u0 = (float)i / sides;
                float u1 = (float)(i + 1) / sides;

                int start = vi;
                verts[vi] = bl; normals[vi] = n; uvArray[vi] = new Vector2(u0, 0); vi++;
                verts[vi] = tl; normals[vi] = n; uvArray[vi] = new Vector2(u0, 1); vi++;
                verts[vi] = tr; normals[vi] = n; uvArray[vi] = new Vector2(u1, 1); vi++;
                verts[vi] = br; normals[vi] = n; uvArray[vi] = new Vector2(u1, 0); vi++;

                tris[ti++] = start;
                tris[ti++] = start + 1;
                tris[ti++] = start + 2;
                tris[ti++] = start;
                tris[ti++] = start + 2;
                tris[ti++] = start + 3;
            }

            // ── Top cap ────────────────────────────────────────────────
            {
                int centreIndex = vi;
                verts[vi] = new Vector3(0f, hh, 0f);
                normals[vi] = Vector3.up;
                uvArray[vi] = new Vector2(0.5f, 0.5f);
                vi++;

                int firstRim = vi;
                for (int i = 0; i < sides; i++)
                {
                    float a = i * angleStep;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    verts[vi] = new Vector3(c * radius, hh, s * radius);
                    normals[vi] = Vector3.up;
                    uvArray[vi] = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f);
                    vi++;
                }

                for (int i = 0; i < sides; i++)
                {
                    tris[ti++] = centreIndex;
                    tris[ti++] = firstRim + (i + 1) % sides;
                    tris[ti++] = firstRim + i;
                }
            }

            // ── Bottom cap ─────────────────────────────────────────────
            {
                int centreIndex = vi;
                verts[vi] = new Vector3(0f, -hh, 0f);
                normals[vi] = Vector3.down;
                uvArray[vi] = new Vector2(0.5f, 0.5f);
                vi++;

                int firstRim = vi;
                for (int i = 0; i < sides; i++)
                {
                    float a = i * angleStep;
                    float c = Mathf.Cos(a), s = Mathf.Sin(a);
                    verts[vi] = new Vector3(c * radius, -hh, s * radius);
                    normals[vi] = Vector3.down;
                    uvArray[vi] = new Vector2(c * 0.5f + 0.5f, s * 0.5f + 0.5f);
                    vi++;
                }

                for (int i = 0; i < sides; i++)
                {
                    tris[ti++] = centreIndex;
                    tris[ti++] = firstRim + i;
                    tris[ti++] = firstRim + (i + 1) % sides;
                }
            }

            mesh.vertices = verts;
            mesh.normals = normals;
            mesh.uv = uvArray;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
