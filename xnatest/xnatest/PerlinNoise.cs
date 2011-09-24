using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            float[,] work2 = new float[cellsize, cellsize];
            float maxwork2 = 0;
            int octave;
            for (octave = 0; (1 << octave) < cellsize; octave += 1)
            {
                int cell = 1 << octave;
                int xi, yi;
                int k;
                for (xi = 0; xi < cellsize / cell; xi++) for (yi = 0; yi < cellsize / cell; yi++)
                    {
                        Random rs = new Random((int)(seed ^ (xi + xofs) + ((yi + yofs) << 16)));
                        int bas = rs.Next(255);
                        if (octave == 0)
                            callback.call(bas, xi, yi);
                        else
                            work[xi, yi] = bas;
                    }
                if (octave == 0)
                {
                    for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                            work2[xi, yi] = work[xi, yi];
                }
                else for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                        {
                            int xj, yj;
                            double[] sum = new double[4] { 0, 0, 0, 0 };
                            for (k = 0; k < 1; k++)
                            {
                                for (xj = 0; xj <= 1; xj++) for (yj = 0; yj <= 1; yj++)
                                    {
                                        sum[k] += (double)work[xi / cell + xj, yi / cell + yj]
                                            * (xj != 0 ? xi % cell : (cell - xi % cell - 1)) / (double)cell
                                            * (yj != 0 ? yi % cell : (cell - yi % cell - 1)) / (double)cell;
                                    }
                                work2[xi, yi] = work2[xi, yi] + (float)sum[k];
                                if (maxwork2 < work2[xi, yi])
                                    maxwork2 = work2[xi, yi];
                            }
                        }
            }
            for (int xi = 0; xi < cellsize; xi++) for (int yi = 0; yi < cellsize; yi++)
                {
                    callback.call(work2[xi, yi] / maxwork2, xi, yi);
                }
        }
    }
}
