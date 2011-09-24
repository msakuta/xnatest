using System;

namespace xnatest
{
    class PerlinNoise
    {
        public abstract class PerlinNoiseCallback
        {
            public abstract void call(float value, int ix, int iy);
        }

        public class FieldAssign : PerlinNoiseCallback
        {
            public float[,] field;
            public FieldAssign(float[,] afield)
            {
                field = afield;
            }
            public override void call(float value, int ix, int iy)
            {
                field[ix, iy] = value;
            }
        }

        public static void perlin_noise(long seed, PerlinNoiseCallback callback, long cellsize, int xofs = 0, int yofs = 0)
        {
            int[,] work = new int[cellsize, cellsize];
            int[,] work2 = new int[cellsize, cellsize];
            int maxwork2 = 0;
            int octave;
            int xi, yi;

            // Temporarily save noise patturn for use as the source signal of fractal noise.
            for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                {
                    Random rs = new Random((int)(seed ^ (xi + xofs) + ((yi + yofs) << 16)));
                    int bas = rs.Next(255);
                    work[xi, yi] = bas;
                }

            // Accumulate signal over octaves to produce Perlin noise.
            for (octave = 0; (1 << octave) < cellsize; octave += 1)
            {
                int cell = 1 << octave;
                int k;
                if (octave == 0)
                {
                    for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                            work2[xi, yi] = work[xi, yi];
                }
                else for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                        {
                            int xj, yj;
                            double sum = 0;
                            for (k = 0; k < 1; k++)
                            {
                                for (xj = 0; xj <= 1; xj++) for (yj = 0; yj <= 1; yj++)
                                    {
                                        sum += (double)work[xi / cell + xj, yi / cell + yj]
                                            * (xj != 0 ? xi % cell : (cell - xi % cell - 1)) / (double)cell
                                            * (yj != 0 ? yi % cell : (cell - yi % cell - 1)) / (double)cell;
                                    }
                                work2[xi, yi] += (int)sum;
                                if (maxwork2 < work2[xi, yi])
                                    maxwork2 = work2[xi, yi];
                            }
                        }
            }

            // Return result
            for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                {
                    callback.call((float)work2[xi, yi] / maxwork2, xi, yi);
                }
        }
    }
}
