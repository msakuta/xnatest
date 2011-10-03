using Microsoft.Xna.Framework;


namespace xnatest
{

    /// <summary>
    /// Vector3 variant with int as element type.
    /// If only class template is available in C#, we wouldn't need to re-invent the wheels here.
    /// </summary>
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

        public Vector3 cast()
        {
            return new Vector3(X, Y, Z);
        }
    }
}
