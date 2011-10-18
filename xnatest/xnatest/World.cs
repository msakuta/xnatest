using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
/// \file
/// \brief This file contains part of Game1 class to define internal classes.

namespace xnatest
{
    /// The type to index World.volume. Be sure to keep it a struct.
    using CellIndex = Vec3i;

    public partial class Game1
    {
        /// <summary>
        /// The internal atomic type that the world is made of.
        /// </summary>
        /// <remarks>
        /// To keep memory space efficiency fair, be sure to make it a struct and not to add much data members.
        /// </remarks>
        public struct Cell
        {
            public enum Type { Air, Grass };
            public Cell(Type t) { mtype = t; adjacents = 0; }
            private Type mtype;
            public Type type { get { return mtype; } }
            public int adjacents;
        }

        /// <summary>
        /// A unit of the world that contains certain number of Cells.
        /// </summary>
        public class CellVolume
        {
            Cell[, ,] v = new Cell[CELLSIZE, CELLSIZE, CELLSIZE];

            public Cell cell(int ix, int iy, int iz)
            {
                return v[ix, iy, iz];
            }

            public void initialize(Vec3i ci)
            {
                float[,] field = new float[CELLSIZE, CELLSIZE];
                PerlinNoise.perlin_noise(new PerlinNoise.PerlinNoiseParams() { seed = 12321, cellsize = CELLSIZE }, new PerlinNoise.FieldAssign(field));
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                        {
                            v[ix, iy, iz] = new Cell(field[ix, iz] * CELLSIZE / 2 < iy + ci.Y * CELLSIZE ? Cell.Type.Air : Cell.Type.Grass);
                        }
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                        {
                            updateAdj(ix, iy, iz);
                        }
            }

            void updateAdj(int ix, int iy, int iz)
            {
                v[ix, iy, iz].adjacents =
                    (0 < ix && v[ix - 1, iy, iz].type != Cell.Type.Air ? 1 : 0) +
                    (ix < CELLSIZE - 1 && v[ix + 1, iy, iz].type != Cell.Type.Air ? 1 : 0) +
                    (0 < iy && v[ix, iy - 1, iz].type != Cell.Type.Air ? 1 : 0) +
                    (iy < CELLSIZE - 1 && v[ix, iy + 1, iz].type != Cell.Type.Air ? 1 : 0) +
                    (0 < iz && v[ix, iy, iz - 1].type != Cell.Type.Air ? 1 : 0) +
                    (iz < CELLSIZE - 1 && v[ix, iy, iz + 1].type != Cell.Type.Air ? 1 : 0);
            }

            public bool isSolid(Vec3i ipos)
            {
                return
                    0 <= ipos.X && ipos.X < CELLSIZE &&
                    0 <= ipos.Y && ipos.Y < CELLSIZE &&
                    0 <= ipos.Z && ipos.Z < CELLSIZE &&
                    v[ipos.X, ipos.Y, ipos.Z].type != Cell.Type.Air;
            }
        }

        /// <summary>
        /// Type to represent a physical world. Merely a collection of CellVolumes.
        /// </summary>
        public class World
        {
            /// <summary>
            /// Reference to global world object
            /// </summary>
            Game1 game;

            public World(Game1 game)
            {
                this.game = game;
            }

            //            public CellVolume volume = new CellVolume();
            System.Collections.Generic.Dictionary<CellIndex, CellVolume> _volume = new System.Collections.Generic.Dictionary<CellIndex, CellVolume>();
            public System.Collections.Generic.Dictionary<CellIndex, CellVolume> volume { get { return _volume; } }

            public Cell cell(int ix, int iy, int iz)
            {
                CellVolume cv = _volume[new CellIndex(ix / CELLSIZE, iy / CELLSIZE, iz / CELLSIZE)];
                if (cv != null)
                    return cv.cell(ix - ix / CELLSIZE * CELLSIZE, iy - iy / CELLSIZE * CELLSIZE, iz - iz / CELLSIZE * CELLSIZE);
                return new Cell(Cell.Type.Air);
            }

            public bool isSolid(Vec3i v)
            {
                return isSolid(v.X, v.Y, v.Z);
            }

            public bool isSolid(int ix, int iy, int iz)
            {
                CellIndex ci = new CellIndex(
                    SignDiv(ix, CELLSIZE),
                    SignDiv(iy, CELLSIZE),
                    SignDiv(iz, CELLSIZE));
                if (_volume.ContainsKey(ci))
                {
                    CellVolume cv = _volume[ci];
                    return cv.isSolid(new Vec3i(
                        SignModulo(ix, CELLSIZE),
                        SignModulo(iy, CELLSIZE),
                        SignModulo(iz, CELLSIZE)));
                }
                else
                    return false;
            }

            /// <summary>
            /// Called every frame.
            /// </summary>
            /// <param name="dt">Delta-time</param>
            public void Update(double dt)
            {
                IndFrac i = real2ind(game.player.getPos());
                for (int ix = 0; ix < 2; ix++) for (int iy = 0; iy < 2; iy++) for (int iz = 0; iz < 2; iz++)
                        {
                            CellIndex ci = new CellIndex(
                                SignDiv((i.index.X + (2 * ix - 1) * CELLSIZE / 2), CELLSIZE),
                                SignDiv((i.index.Y + (2 * iy - 1) * CELLSIZE / 2), CELLSIZE),
                                SignDiv((i.index.Z + (2 * iz - 1) * CELLSIZE / 2), CELLSIZE));
                            if (!_volume.ContainsKey(ci))
                            {
                                CellVolume cv = new CellVolume();
                                cv.initialize(ci);
                                _volume.Add(ci, cv);
                            }
                        }
            }
        }

    }
}
