using System;
using UnityEngine;

namespace ProjectTerra.Planet
{
    public enum CubeFaceDirection
    {
        Up,      // +Y (Polo Norte)
        Down,    // -Y (Polo Sul)
        Left,    // -X (Oceano Pacífico Leste / Oceania)
        Right,   // +X (Oceano Índico / África)
        Forward, // +Z (Europa / África / Atlântico)
        Back     // -Z (Américas / Pacífico)
    }

    public class CubeSphereFace
    {
        private Mesh mesh;
        private int resolution;
        private Vector3 localUp;
        private Vector3 axisA;
        private Vector3 axisB;
        private double radius;

        public CubeSphereFace(Mesh mesh, int resolution, Vector3 localUp, double radius)
        {
            this.mesh = mesh;
            this.resolution = resolution;
            this.localUp = localUp;
            this.radius = radius;

            axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            axisB = Vector3.Cross(localUp, axisA);
        }

        public void ConstructMesh()
        {
            int numVertices = (resolution + 1) * (resolution + 1);
            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            Vector4[] tangents = new Vector4[numVertices];
            int[] triangles = new int[resolution * resolution * 6];

            int triIndex = 0;

            for (int y = 0; y <= resolution; y++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    int i = x + y * (resolution + 1);
                    Vector2 percent = new Vector2((float)x / resolution, (float)y / resolution);

                    // Ponto no cubo [-1, 1]
                    Vector3 pointOnUnitCube = localUp + (percent.x - 0.5f) * 2f * axisA + (percent.y - 0.5f) * 2f * axisB;

                    // Mapeamento esférico normalizado com correção de Nowell para distribuição uniforme
                    Vector3 sphereNormal = CubeToSphericalNormalized(pointOnUnitCube);
                    vertices[i] = sphereNormal * (float)radius;
                    normals[i] = sphereNormal;

                    // Mapeamento UV Equirretangular (Latitude / Longitude padrão NASA)
                    uvs[i] = CalculateEquirectangularUV(sphereNormal);

                    // Tangente perpendicular ao normal
                    Vector3 tangent3D = Vector3.Cross(Vector3.up, sphereNormal);
                    if (tangent3D.sqrMagnitude < 0.001f)
                        tangent3D = Vector3.Cross(Vector3.forward, sphereNormal);
                    tangent3D.Normalize();
                    tangents[i] = new Vector4(tangent3D.x, tangent3D.y, tangent3D.z, 1.0f);

                    // Gerar os dois triângulos do quad
                    if (x < resolution && y < resolution)
                    {
                        triangles[triIndex] = i;
                        triangles[triIndex + 1] = i + resolution + 2;
                        triangles[triIndex + 2] = i + resolution + 1;

                        triangles[triIndex + 3] = i;
                        triangles[triIndex + 4] = i + 1;
                        triangles[triIndex + 5] = i + resolution + 2;

                        triIndex += 6;
                    }
                }
            }

            // Prevenção de quebra de UV no antimeridiano (180° de longitude):
            // Quando a face cruza a linha de 180°, adiciona 1.0f aos vértices com u < 0.5f
            // para que a GPU faça a interpolação contínua e sem esticar a textura pelo mapa inteiro.
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

            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.tangents = tangents;
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Projeção esferificada de Philip Nowell para minimizar distorções de área nos cantos do cubo.
        /// </summary>
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

        /// <summary>
        /// Converte vetor normal na esfera para coordenadas de textura UV equirretangular [0, 1].
        /// </summary>
        private Vector2 CalculateEquirectangularUV(Vector3 normal)
        {
            float longitude = Mathf.Atan2(normal.x, -normal.z); // [-PI, PI]
            float latitude = Mathf.Asin(Mathf.Clamp(normal.y, -1f, 1f)); // [-PI/2, PI/2]

            float u = (longitude / (2f * Mathf.PI)) + 0.5f;
            float v = (latitude / Mathf.PI) + 0.5f;

            return new Vector2(u, v);
        }
    }
}
