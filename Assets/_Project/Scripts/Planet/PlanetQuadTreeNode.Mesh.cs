using UnityEngine;

namespace ProjectTerra.Planet
{
    public partial class PlanetQuadTreeNode
    {
        public void ConstructMesh()
        {
            int res = Planet.ChunkResolution;
            int numVertices = (res + 1) * (res + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            Vector4[] tangents = new Vector4[numVertices];
            int[] triangles = new int[res * res * 6];

            int triIndex = 0;
            double radius = Planet.PlanetRadius;

            float minU = CubeCenter.x - CubeSize * 0.5f;
            float minV = CubeCenter.y - CubeSize * 0.5f;

            for (int y = 0; y <= res; y++)
            {
                for (int x = 0; x <= res; x++)
                {
                    int i = x + y * (res + 1);
                    float px = (float)x / res;
                    float py = (float)y / res;

                    float u = minU + px * CubeSize;
                    float v = minV + py * CubeSize;

                    Vector3 pointOnUnitCube = LocalUp + u * AxisA + v * AxisB;
                    Vector3 sphereNormal = CubeToSphericalNormalized(pointOnUnitCube);

                    vertices[i] = sphereNormal * (float)radius;
                    normals[i] = sphereNormal;
                    uvs[i] = CalculateEquirectangularUV(sphereNormal);

                    Vector3 tangent3D = Vector3.Cross(Vector3.up, sphereNormal);
                    if (tangent3D.sqrMagnitude < 0.001f)
                        tangent3D = Vector3.Cross(Vector3.forward, sphereNormal);
                    tangent3D.Normalize();
                    tangents[i] = new Vector4(tangent3D.x, tangent3D.y, tangent3D.z, 1.0f);

                    if (x < res && y < res)
                    {
                        triangles[triIndex] = i;
                        triangles[triIndex + 1] = i + res + 2;
                        triangles[triIndex + 2] = i + res + 1;

                        triangles[triIndex + 3] = i;
                        triangles[triIndex + 4] = i + 1;
                        triangles[triIndex + 5] = i + res + 2;

                        triIndex += 6;
                    }
                }
            }

            // Prevenção de quebra de UV no antimeridiano (180° de longitude):
            // Quando um nó da Quadtree cruza o meridiano de 180°, ajusta os vértices com u < 0.5f somando 1.0f
            // eliminando o estiramento diagonal pelo globo.
            bool hasLowU = false;
            bool hasHighU = false;
            for (int k = 0; k < numVertices; k++)
            {
                if (uvs[k].x < 0.25f) hasLowU = true;
                if (uvs[k].x > 0.75f) hasHighU = true;
            }

            if (hasLowU && hasHighU)
            {
                for (int k = 0; k < numVertices; k++)
                {
                    if (uvs[k].x < 0.5f)
                    {
                        uvs[k].x += 1.0f;
                    }
                }
            }

            Mesh.Clear();
            Mesh.vertices = vertices;
            Mesh.triangles = triangles;
            Mesh.normals = normals;
            Mesh.uv = uvs;
            Mesh.tangents = tangents;
            Mesh.RecalculateBounds();

            IsMeshConstructed = true;
        }

        private Vector3 CubeToSphericalNormalized(Vector3 p)
        {
            float x2 = p.x * p.x;
            float y2 = p.y * p.y;
            float z2 = p.z * p.z;

            float sx = p.x * Mathf.Sqrt(Mathf.Max(0f, 1f - (y2 * 0.5f) - (z2 * 0.5f) + (y2 * z2 / 3f)));
            float sy = p.y * Mathf.Sqrt(Mathf.Max(0f, 1f - (z2 * 0.5f) - (x2 * 0.5f) + (z2 * x2 / 3f)));
            float sz = p.z * Mathf.Sqrt(Mathf.Max(0f, 1f - (x2 * 0.5f) - (y2 * 0.5f) + (x2 * y2 / 3f)));

            return new Vector3(sx, sy, sz).normalized;
        }

        private Vector2 CalculateEquirectangularUV(Vector3 normal)
        {
            float longitude = Mathf.Atan2(normal.x, -normal.z);
            float latitude = Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f));

            float u = (longitude / (2f * Mathf.PI)) + 0.5f;
            float v = (latitude / Mathf.PI) + 0.5f;

            return new Vector2(u, v);
        }
    }
}
