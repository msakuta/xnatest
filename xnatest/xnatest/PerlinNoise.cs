using System;

namespace xnatest
{
    /// <summary>
    /// Type and function collection about generating Perlin Noise.
    /// </summary>
    /// <remarks>
    /// Really should be a namespace, but C# doesn't allow a function that is not a class or struct member.
    /// </remarks>
    class PerlinNoise
    {
        /// <summary>
        /// Callback object that receives result of perlin_noise
        /// </summary>
        /// We do this because not all application needs temporary memry to store the result.
        public abstract class PerlinNoiseCallback
        {
            public abstract void call(float value, int ix, int iy);
        }

        /// <summary>
        /// Callback object that will assign noise values to float 2-d array field.
        /// </summary>
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

        /// <summary>
        /// Parameters given to perlin_noise
        /// </summary>
        public class PerlinNoiseParams
        {
            public long seed = 0; /// Random seed
            public long cellsize = 16; /// Size of the one edge of the square area.
            public double persistence = 0.5; /// Persistence of the argument.
            public int xofs = 0, yofs = 0; /// Offsets for each axes.
        }

        public static void perlin_noise(long seed, PerlinNoiseCallback callback, long cellsize, int xofs = 0, int yofs = 0)
        {
            perlin_noise(new PerlinNoiseParams() { seed = seed, cellsize = cellsize, persistence = 0.5 }, callback);
        }

        /// <summary>
        /// Generate Perlin Noise and return it through callback.
        /// </summary>
        /// <param name="param">Parameters to generate the noise.</param>
        /// <param name="callback">Callback object to receive the result.</param>
        public static void perlin_noise(PerlinNoiseParams param, PerlinNoiseCallback callback)
        {
            long seed = param.seed;
            long cellsize = param.cellsize;
            const int baseMax = 255;
            int[,] work = new int[cellsize, cellsize];
            int[,] work2 = new int[cellsize, cellsize];
            int maxwork2 = 0;
            int octave;
            int xi, yi;

            // Temporarily save noise patturn for use as the source signal of fractal noise.
            for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                {
                    Random rs = new Random((int)(seed ^ (xi + param.xofs) + ((yi + param.yofs) << 16)));
                    int bas = rs.Next(baseMax);
                    work[xi, yi] = bas;
                }

            double factor = 1.0;
            double sumfactor = 0.0;

            // Accumulate signal over octaves to produce Perlin noise.
            for (octave = 0; (1 << octave) < cellsize; octave += 1)
            {
                int cell = 1 << octave;
                if (octave == 0)
                {
                    for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                            work2[xi, yi] = (int)(work[xi, yi] * factor);
                }
                else for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                        {
                            int xj, yj;
                            double sum = 0;
                            for (xj = 0; xj <= 1; xj++) for (yj = 0; yj <= 1; yj++)
                                {
                                    sum += (double)work[xi / cell + xj, yi / cell + yj]
                                        * (xj != 0 ? xi % cell : (cell - xi % cell - 1)) / (double)cell
                                        * (yj != 0 ? yi % cell : (cell - yi % cell - 1)) / (double)cell;
                                }
                            work2[xi, yi] += (int)(sum * factor);
                            if (maxwork2 < work2[xi, yi])
                                maxwork2 = work2[xi, yi];
                        }
                sumfactor += factor;
                factor /= param.persistence;
            }

            // Return result
            for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                {
                    callback.call((float)(work2[xi, yi] / baseMax / sumfactor), xi, yi);
                }
        }
    }
}
