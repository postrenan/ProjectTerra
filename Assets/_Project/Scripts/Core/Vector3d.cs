using System;
using UnityEngine;

namespace ProjectTerra.Core
{
    [Serializable]
    public struct Vector3d : IEquatable<Vector3d>
    {
        public double x;
        public double y;
        public double z;

        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3d(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public static Vector3d zero => new Vector3d(0, 0, 0);
        public static Vector3d one => new Vector3d(1, 1, 1);
        public static Vector3d up => new Vector3d(0, 1, 0);
        public static Vector3d forward => new Vector3d(0, 0, 1);
        public static Vector3d right => new Vector3d(1, 0, 0);

        public double magnitude => Math.Sqrt(x * x + y * y + z * z);
        public double sqrMagnitude => x * x + y * y + z * z;

        public Vector3d normalized
        {
            get
            {
                double mag = magnitude;
                return mag > 1e-15 ? this / mag : zero;
            }
        }

        public Vector3 ToVector3() => new Vector3((float)x, (float)y, (float)z);

        public static Vector3d operator +(Vector3d a, Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3d operator -(Vector3d a) => new Vector3d(-a.x, -a.y, -a.z);
        public static Vector3d operator *(Vector3d a, double d) => new Vector3d(a.x * d, a.y * d, a.z * d);
        public static Vector3d operator *(double d, Vector3d a) => new Vector3d(a.x * d, a.y * d, a.z * d);
        public static Vector3d operator /(Vector3d a, double d) => new Vector3d(a.x / d, a.y / d, a.z / d);

        public static double Distance(Vector3d a, Vector3d b) => (a - b).magnitude;
        public static double Dot(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3d Cross(Vector3d a, Vector3d b)
        {
            return new Vector3d(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }

        public bool Equals(Vector3d other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is Vector3d other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(x, y, z);
        public override string ToString() => $"({x:F3}, {y:F3}, {z:F3})";
    }
}
