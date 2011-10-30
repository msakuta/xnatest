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

            public void serialize(System.IO.BinaryWriter bw)
            {
                bw.Write((byte)mtype);
//                bw.Write((byte)adjacents);
            }

            public void unserialize(System.IO.BinaryReader br)
            {
                mtype = (Type)br.ReadByte();
//                adjacents = br.ReadByte();
            }
        }

        /// <summary>
        /// A unit of the world that contains certain number of Cells.
        /// </summary>
        public class CellVolume
        {
            /// <summary>
            /// Indicating where this CellVolume resides in coordinates.
            /// </summary>
            CellIndex _index;
            public CellIndex index { get { return _index; } }

            /// <summary>
            /// The global World pointer.
            /// </summary>
            World world;
            Cell[, ,] v = new Cell[CELLSIZE, CELLSIZE, CELLSIZE];
            
            /// <summary>
            /// Indices are in order of [X, Z, beginning and end]
            /// </summary>
            int[,,] _scanLines = new int[CELLSIZE, CELLSIZE, 2];

            public int[, ,] scanLines { get { return _scanLines; } }

            int _solidcount = 0;

            /// <summary>
            /// Pointer to adjacent CellVolumes for caching.
            /// </summary>
//            CellVolume[] neighbors = new CellVolume[6];

            
            public CellVolume(World world) { this.world = world; }

            /// <summary>
            /// Count of solid Cells in this CellVolume.
            /// </summary>
            public int solidcount { get { return _solidcount; } }

            public static int cellInvokes = 0;
            public static int cellForeignInvokes = 0;
            public static int cellForeignExists = 0;

            /// <summary>
            /// Retrieves specific Cell in this CellVolume
            /// </summary>
            /// <remarks>
            /// If the indices reach border of the CellVolume, it will recursively retrieve foreign Cells.
            ///
            /// Note that even if two or more indices are out of range, this function will find the correct Cell
            /// by recursively calling itself in turn with each axes.
            /// But what's the difference between this and World.cell(), you may ask. That's the key for the next
            /// step of optimization.
            /// 
            /// I've felt it's significantly sensitive to algorithm's complexity compared to C++ equivalent code.
            /// It should be code optimization issue.
            /// </remarks>
            /// <param name="ix">Index along X axis in Cells. If in range [0, CELLSIZE), this object's member is returned.</param>
            /// <param name="iy">Index along Y axis in Cells. If in range [0, CELLSIZE), this object's member is returned.</param>
            /// <param name="iz">Index along Z axis in Cells. If in range [0, CELLSIZE), this object's member is returned.</param>
            /// <returns>Cell object</returns>
            public Cell cell(int ix, int iy, int iz)
            {
                cellInvokes += 1;
                if (ix < 0 || CELLSIZE <= ix)
                {
                    cellForeignInvokes += 1;
/*                    int nx = index.X + SignDiv(ix, CELLSIZE);
                    if (nx == index.X - 1 && neighbors[0] != null)
                        return neighbors[0].cell(SignModulo(ix, CELLSIZE), iy, iz);
                    else if (nx == index.X + 1 && neighbors[1] != null)
                        return neighbors[1].cell(SignModulo(ix, CELLSIZE), iy, iz);*/
                    CellIndex ci = new CellIndex(index.X + SignDiv(ix, CELLSIZE), index.Y, index.Z);
                    if (world.volume.ContainsKey(ci))
                    {
                        cellForeignExists += 1;
                        CellVolume cv = world.volume[ci];
                        return cv.cell(SignModulo(ix, CELLSIZE), iy, iz);
                    }
                    else
                        return cell(ix < 0 ? 0 : CELLSIZE - 1, iy, iz);
                }
                if (iy < 0 || CELLSIZE <= iy)
                {
                    cellForeignInvokes += 1;
/*                    int ny = index.Y + SignDiv(iy, CELLSIZE);
                    if (ny == index.Y - 1 && neighbors[2] != null)
                        return neighbors[2].cell(ix, SignModulo(iy, CELLSIZE), iz);
                    else if (ny == index.Y + 1 && neighbors[3] != null)
                        return neighbors[3].cell(ix, SignModulo(iy, CELLSIZE), iz);*/
                    CellIndex ci = new CellIndex(index.X, index.Y + SignDiv(iy, CELLSIZE), index.Z);
                    if (world.volume.ContainsKey(ci))
                    {
                        cellForeignExists += 1;
                        CellVolume cv = world.volume[ci];
                        return cv.cell(ix, SignModulo(iy, CELLSIZE), iz);
                    }
                    else
                        return cell(ix, iy < 0 ? 0 : CELLSIZE - 1, iz);
                }
                if (iz < 0 || CELLSIZE <= iz)
                {
                    cellForeignInvokes += 1;
                    CellIndex ci = new CellIndex(index.X, index.Y, index.Z + SignDiv(iz, CELLSIZE));
                    if (world.volume.ContainsKey(ci))
                    {
                        cellForeignExists += 1;
                        CellVolume cv = world.volume[ci];
                        return cv.cell(ix, iy, SignModulo(iz, CELLSIZE));
                    }
                    else
                        return cell(ix, iy, iz < 0 ? 0 : CELLSIZE - 1);
                }
                return ix < 0 || CELLSIZE <= ix || iy < 0 || CELLSIZE <= iy || iz < 0 || CELLSIZE <= iz ? new Cell(Cell.Type.Air) : v[ix, iy, iz];
            }

            /// <summary>
            /// Replace a Cell in a CellVolume with new one.
            /// </summary>
            /// <remarks>
            /// Adjacent cells will get cache data updated.
            /// </remarks>
            /// <param name="ix"></param>
            /// <param name="iy"></param>
            /// <param name="iz"></param>
            /// <param name="newCell">The new Cell to replace with.</param>
            /// <returns>Modified</returns>
            public bool setCell(int ix, int iy, int iz, Cell newCell)
            {
                if (ix < 0 || CELLSIZE <= ix || iy < 0 || CELLSIZE <= iy || iz < 0 || CELLSIZE <= iz)
                    return false;
                else
                {
                    // Update solidcount by difference of solidity before and after cell assignment.
                    int before = v[ix, iy, iz].type == Cell.Type.Air ? 0 : 1;
                    int after = newCell.type == Cell.Type.Air ? 0 : 1;
                    _solidcount += after - before;

                    v[ix, iy, iz] = newCell;
                    updateCache();
                    if (ix <= 0)
                        world.volume[new Vec3i(index.X - 1, index.Y, index.Z)].updateCache();
                    else if(CELLSIZE - 1 <= ix)
                        world.volume[new Vec3i(index.X + 1, index.Y, index.Z)].updateCache();
                    if (iy <= 0)
                        world.volume[new Vec3i(index.X, index.Y - 1, index.Z)].updateCache();
                    else if (CELLSIZE - 1 <= iy)
                        world.volume[new Vec3i(index.X, index.Y + 1, index.Z)].updateCache();
                    if (iz <= 0)
                        world.volume[new Vec3i(index.X, index.Y, index.Z - 1)].updateCache();
                    else if (CELLSIZE - 1 <= iz)
                        world.volume[new Vec3i(index.X, index.Y, index.Z + 1)].updateCache();
                    return true;
                }
            }

            /// <summary>
            /// Initialize this CellVolume with Perlin Noise with given position index.
            /// </summary>
            /// <param name="ci">The position of new CellVolume</param>
            public void initialize(Vec3i ci)
            {
                _index = ci;
                float[,] field = new float[CELLSIZE, CELLSIZE];
                PerlinNoise.perlin_noise(new PerlinNoise.PerlinNoiseParams() { seed = 12321, cellsize = CELLSIZE, octaves = 7, xofs = ci.X * CELLSIZE, yofs = ci.Z * CELLSIZE }, new PerlinNoise.FieldAssign(field));
                _solidcount = 0;
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                        {
                            v[ix, iy, iz] = new Cell(field[ix, iz] * CELLSIZE * 2 < iy + ci.Y * CELLSIZE ? Cell.Type.Air : Cell.Type.Grass);
                            if (v[ix, iy, iz].type != Cell.Type.Air)
                                _solidcount++;
                        }
            }

            public void updateCache()
            {
/*                CellIndex ci = new CellIndex(index.X - 1, index.Y, index.Z);
                neighbors[0] = world.volume.ContainsKey(ci) ? world.volume[ci] : null;
                ci = new CellIndex(index.X + 1, index.Y, index.Z);
                neighbors[1] = world.volume.ContainsKey(ci) ? world.volume[ci] : null;
                ci = new CellIndex(index.X, index.Y - 1, index.Z);
                neighbors[2] = world.volume.ContainsKey(ci) ? world.volume[ci] : null;
                ci = new CellIndex(index.X, index.Y + 1, index.Z);
                neighbors[3] = world.volume.ContainsKey(ci) ? world.volume[ci] : null;*/

                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                        {
                            updateAdj(ix, iy, iz);
                        }
                
                // Build up scanline map
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iz = 0; iz < CELLSIZE; iz++)
                    {
                        // Find start and end points for this scan line
                        bool begun = false;
                        for (int iy = 0; iy < CELLSIZE; iy++)
                        {
                            Cell c = v[ix, iy, iz];
                            if (c.adjacents != 0 && c.type == Cell.Type.Air
                                || c.adjacents != 6 && c.type != Cell.Type.Air
                                || c.adjacents != 0 && c.adjacents != 6)
                            {
                                if (!begun)
                                {
                                    begun = true;
                                    scanLines[ix, iz, 0] = iy;
                                }
                                scanLines[ix, iz, 1] = iy;
                            }
                        }
                    }
            }

            void updateAdj(int ix, int iy, int iz)
            {
                v[ix, iy, iz].adjacents =
                    (cell(ix - 1, iy, iz).type != Cell.Type.Air ? 1 : 0) +
                    (cell(ix + 1, iy, iz).type != Cell.Type.Air ? 1 : 0) +
                    (cell(ix, iy - 1, iz).type != Cell.Type.Air ? 1 : 0) +
                    (cell(ix, iy + 1, iz).type != Cell.Type.Air ? 1 : 0) +
                    (cell(ix, iy, iz - 1).type != Cell.Type.Air ? 1 : 0) +
                    (cell(ix, iy, iz + 1).type != Cell.Type.Air ? 1 : 0);
            }

            public bool isSolid(Vec3i ipos)
            {
                return
                    0 <= ipos.X && ipos.X < CELLSIZE &&
                    0 <= ipos.Y && ipos.Y < CELLSIZE &&
                    0 <= ipos.Z && ipos.Z < CELLSIZE &&
                    v[ipos.X, ipos.Y, ipos.Z].type != Cell.Type.Air;
            }

            public void serialize(System.IO.BinaryWriter bw)
            {
                bw.Write(index.X);
                bw.Write(index.Y);
                bw.Write(index.Z);
                bw.Write(_solidcount);
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                            v[ix, iy, iz].serialize(bw);
            }

            public void unserialize(System.IO.BinaryReader br)
            {
                _index.X = br.ReadInt32();
                _index.Y = br.ReadInt32();
                _index.Z = br.ReadInt32();
                _solidcount = br.ReadInt32();
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++)
                    v[ix, iy, iz].unserialize(br);
            }
        }

        /// <summary>
        /// Type to represent a physical world. Merely a collection of CellVolumes.
        /// </summary>
        [Serializable]
        public class World : System.Runtime.Serialization.ISerializable
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

            /// <summary>
            /// Replace a Cell in a CellVolume in a World.
            /// </summary>
            /// <remarks>
            /// Internally calls CellVolume.setCell().
            /// </remarks>
            /// <param name="ix"></param>
            /// <param name="iy"></param>
            /// <param name="iz"></param>
            /// <param name="newCell">The new Cell to replace with.</param>
            /// <returns>True if successful</returns>
            public bool setCell(int ix, int iy, int iz, Cell newCell)
            {
                CellVolume cv = _volume[new CellIndex(SignDiv(ix, CELLSIZE), SignDiv(iy, CELLSIZE), SignDiv(iz, CELLSIZE))];
                if (cv != null)
                    return cv.setCell(SignModulo(ix, CELLSIZE), SignModulo(iy, CELLSIZE), SignModulo(iz, CELLSIZE), newCell);
                return false;
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
            /// Returns sum of all solid cell counts in CellVolumes in this World.
            /// </summary>
            public int solidcount{
                get{
                    int count = 0;
                    foreach(System.Collections.Generic.KeyValuePair<CellIndex, CellVolume> kv in volume)
                    {
                        count += kv.Value.solidcount;
                    }
                    return count;
                }
            }


            void tryadd(System.Collections.Generic.HashSet<CellVolume> set, CellIndex ci)
            {
                if (_volume.ContainsKey(ci))
                    set.Add(_volume[ci]);
            }

            class CellVolumeComp : System.Collections.Generic.IEqualityComparer<CellVolume>
            {
                public bool Equals(CellVolume a, CellVolume b)
                {
                    return a.index == b.index;
                }
                public int GetHashCode(CellVolume cv)
                {
                    return cv.index.GetHashCode();
                }
            }

            /// <summary>
            /// Called every frame.
            /// </summary>
            /// <param name="dt">Delta-time</param>
            public void Update(double dt)
            {
                System.Collections.Generic.HashSet<CellVolume> changed = new HashSet<CellVolume>(new CellVolumeComp());
                IndFrac i = real2ind(game.player.getPos());
                int radius = game.maxViewDistance / CELLSIZE;
                for (int ix = -radius; ix <= radius; ix++) for (int iy = -radius; iy < radius; iy++) for (int iz = -radius; iz <= radius; iz++)
                        {
                            CellIndex ci = new CellIndex(
                                SignDiv((i.index.X + ix * CELLSIZE), CELLSIZE),
                                SignDiv((i.index.Y + (2 * iy - 1) * CELLSIZE / 2), CELLSIZE),
                                SignDiv((i.index.Z + iz * CELLSIZE), CELLSIZE));
                            if (!_volume.ContainsKey(ci))
                            {
                                CellVolume cv = new CellVolume(this);
                                cv.initialize(ci);
                                _volume.Add(ci, cv);
                                changed.Add(cv);
/*                                tryadd(changed, new CellIndex(ix - 1, iy, iz));
                                tryadd(changed, new CellIndex(ix + 1, iy, iz));
                                tryadd(changed, new CellIndex(ix, iy - 1, iz));
                                tryadd(changed, new CellIndex(ix, iy + 1, iz));
                                tryadd(changed, new CellIndex(ix, iy, iz - 1));
                                tryadd(changed, new CellIndex(ix, iy, iz + 1));*/
                            }
                        }

                foreach (CellVolume v in changed)
                {
                    v.updateCache();
                    logwriter.WriteLine("hash {1}: {0}", (string)v.index, v.index.GetHashCode());
                }
            }

            public void serialize(System.IO.BinaryWriter bw)
            {
                bw.Write(_volume.Count);
                foreach (System.Collections.Generic.KeyValuePair<CellIndex, CellVolume> kv in _volume)
                    kv.Value.serialize(bw);
            }

            public void unserialize(System.IO.BinaryReader br)
            {
                try
                {
                    _volume.Clear();
                    int count = br.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        CellVolume cv = new CellVolume(this);
                        cv.unserialize(br);
                        _volume.Add(cv.index, cv);
                    }
                    foreach(KeyValuePair<CellIndex, CellVolume> kv in _volume)
                        kv.Value.updateCache();
                }
                catch(Exception e)
                    {
                        logwriter.WriteLine(e.ToString());
                        return;
                    }
/*                foreach (System.Collections.Generic.KeyValuePair<CellIndex, CellVolume> kv in _volume)
                    kv.Value.unserialize(br);**/
            }

            public void GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext sc)
            {
                si.AddValue("_volume", _volume);
            }
        }

    }
}
