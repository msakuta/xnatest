using Microsoft.Xna.Framework;


namespace xnatest
{

    /// <summary>
    /// Vector3 variant with int as element type.
    /// </summary>
    /// <remarks>
    /// If only class template is available in C#, we wouldn't need to re-invent the wheels here.
    /// </remarks>
    public struct Vec3i
    {
        public int X;
        public int Y;
        public int Z;

        public Vec3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3i(Vector3 v)
        {
            X = (int)v.X;
            Y = (int)v.Y;
            Z = (int)v.Z;
        }

        public static Vec3i operator /(Vec3i v, int d)
        {
            return new Vec3i(v.X / d, v.Y / d, v.Z / d);
        }

        public static Vec3i operator +(Vec3i a, Vec3i v)
        {
            return new Vec3i(a.X + v.X, a.Y + v.Y, a.Z + v.Z);
        }

        public static Vec3i operator -(Vec3i a, Vec3i b)
        {
            return a + new Vec3i(-b.X, -b.Y, -b.Z);
        }

        /// <summary>
        /// Scale
        /// </summary>
        public static Vec3i operator *(Vec3i a, int v)
        {
            return new Vec3i(a.X * v, a.Y * v, a.Z * v);
        }

        public Vector3 cast()
        {
            return new Vector3(X, Y, Z);
        }

        public bool Equals(Vec3i o)
        {
            return X == o.X && Y == o.Y && Z == o.Z;
        }

        public static bool operator ==(Vec3i a, Vec3i b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(Vec3i a, Vec3i b)
        {
            return !a.Equals(b);
        }

        public override int GetHashCode()
        {
            return (112116 ^ X) * (56549791 ^ Y) * (45890174 ^ Z);
        }

        public static explicit operator string(Vec3i v)
        {
            return string.Format("({0},{1},{2})", v.X, v.Y, v.Z);
        }
    }
}
