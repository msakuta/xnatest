﻿using System;

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
        /// The hand-made pseudo-random number sequence generator.
        /// </summary>
        /// <remarks>
        /// It's not really advanced technique, just a pair of multiply-with-carry random sequence generators.
        /// It's statically unsafe, but fast to initialize and prone to the seed's bias.
        /// 
        /// The interface resembles System.Random's one.
        /// </remarks>
        public struct Random
        {
            private uint w, z;
            public Random(uint seed1, uint seed2 = 0)
            {
                w = ((z = seed1) ^ 123459876) * 123459871;
                z += ((w += seed2) ^ 1534241562) * 123459876;
            }

            /// <summary>
            /// Retrieve a number and advance the sequence.
            /// </summary>
            /// <param name="max">Ought to be maximum range of returned number, but it's ignored here.</param>
            /// <returns>Always in range [0, 256).</returns>
            public uint Next(int max)
            {
                uint u = (((z = 36969 * (z & 65535) + (z >> 16)) << 16) + (w = 18000 * (w & 65535) + (w >> 16)));
                return (u >> 8) & 0xf0 + (u & 0xf);
            }
        }

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
            public long seed = 0; ///< Random seed
            public long cellsize = 16; ///< Size of the one edge of the square area.
            public int octaves = 4; ///< Number of octaves to accumulate the noise in.
            public double persistence = 0.5; ///< Persistence of the argument.
            public int xofs = 0, yofs = 0; ///< Offsets for each axes.
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

            double factor = 1.0;
            double sumfactor = 0.0;

            // Accumulate signal over octaves to produce Perlin noise.
            for (octave = 0; octave < param.octaves; octave += 1)
            {
                int cell = 1 << octave;
                if (octave == 0)
                {
                    for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                        {
                            Random rs = new Random((uint)(seed ^ (xi + param.xofs) + ((yi + param.yofs) << 16)));
                            work2[xi, yi] = (int)(rs.Next(baseMax) * factor);
                        }
                }
                else for (xi = 0; xi < cellsize; xi++) for (yi = 0; yi < cellsize; yi++)
                        {
                            int xj, yj;
                            int xsm = Game1.SignModulo(xi + param.xofs, cell);
                            int ysm = Game1.SignModulo(yi + param.yofs, cell);
                            int xsd = Game1.SignDiv(xi + param.xofs, cell);
                            int ysd = Game1.SignDiv(yi + param.yofs, cell);
                            double sum = 0;
                            for (xj = 0; xj <= 1; xj++) for (yj = 0; yj <= 1; yj++)
                                {
                                    Random rs = new Random((uint)(seed ^ (xsd + xj) + ((ysd + yj) << 16)));
                                    sum += (double)rs.Next(baseMax)
                                        * (xj != 0 ? xsm : (cell - xsm - 1)) / (double)cell
                                        * (yj != 0 ? ysm : (cell - ysm - 1)) / (double)cell;
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
