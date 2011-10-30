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

namespace xnatest
{
    /// <summary>
    /// The Player's class, maintaining camera's position and velocity, etc.
    /// </summary>
    class Player{
        /// <summary>
        /// Reference to global world object
        /// </summary>
        Game1.World world;
        Game1 game;

        public Player(Game1 thegame, Game1.World theworld)
        {
            game = thegame;
            world = theworld;
            bricks = new Dictionary<Game1.Cell.Type,int>();
            bricks[Game1.Cell.Type.Grass] = 0;
            bricks[Game1.Cell.Type.Dirt] = 0;
            bricks[Game1.Cell.Type.Gravel] = 0;
        }

        public Vector3 getPos() { return pos; }
	    public Quaternion getRot(){return rot;}
	    void setPos(Vector3 apos){pos = apos;}
	    void setRot(Quaternion arot){rot = arot;}

        /// <summary>
        /// Update internal variable that represents rotation by Pitch and Yaw values.
        /// </summary>
	    void updateRot(){
            desiredRot =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)yaw) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)pitch);
	    }

        /// <summary>
        /// Position of this Entity
        /// </summary>
        Vector3 pos = new Vector3(0,10,0);

        /// <summary>
        /// Velocity of this Entity
        /// </summary>
	    Vector3 velo;

        /// <summary>
        /// Quaternion representing rotation of this Entity
        /// </summary>
	    Quaternion rot;

        /// <summary>
        /// Quaternion representing rotation that this Entity going to face.
        /// </summary>
	    Quaternion desiredRot;

        double pitch = 0; ///< Pitch and Yaw
        double yaw = 0;

        enum MoveMode { Walk, Fly, Ghost };
        MoveMode moveMode;
        public bool flying {get { return moveMode == MoveMode.Fly || moveMode == MoveMode.Ghost;}}
        bool floorTouched = false;

        KeyboardState oldKeys;

        /// <summary>
        /// The current type of Cell being placed.
        /// </summary>
        public Game1.Cell.Type curtype = Game1.Cell.Type.Grass;

        /// <summary>
        /// The brick materials the Player has.
        /// </summary>
        public System.Collections.Generic.Dictionary<Game1.Cell.Type, int> bricks{get; set;}

        /// <summary>
        /// Half-size of the Player along X axis.
        /// </summary>
        const float boundWidth = 0.4f;

        /// <summary>
        /// Half-size of the Player along Z axis.
        /// </summary>
        const float boundLength = 0.4f;

        /// <summary>
        /// Half-size of the Player along Y axis.
        /// </summary>
        public const float boundHeight = 1.7f;

        /// <summary>
        /// Height of eyes measured from feet.
        /// </summary>
        public const float eyeHeight = 1.5f;

        /// <summary>
        /// Walking speed, [meters per second]
        /// </summary>
        public const float walkSpeed = 2.0f;

        /// <summary>
        /// Run speed, [meters per second]
        /// </summary>
        public const float runSpeed = 5.0f;

        static Vec3i[] directions = new Vec3i[]{
            new Vec3i(-1,  0,  0),
            new Vec3i( 1,  0,  0),
            new Vec3i( 0, -1,  0),
            new Vec3i( 0,  1,  0),
            new Vec3i( 0,  0, -1),
            new Vec3i( 0,  0,  1),
        };

        /// <summary>
        /// Called every frame
        /// </summary>
        /// <param name="dt">Delta-time</param>
        public void Update(float dt){
            // Ignore gravity if flying mode is active
            if (flying)
                velo = velo * (float)(Math.Exp(-10.0f * dt));
            else
    	        velo += new Vector3(0,-9.8f,0) * dt;

#if false
	        Vec3i ipos = Game1.real2ind(pos);
	        if(world.volume.isSolid(ipos)/* || world.volume.isSolid(ipos - Vec3i(0,1,0))*/){
		        velo = Vector3.Zero;
		        pos.Y = (float)(ipos.Y - Game1.CELLSIZE / 2 + eyeHeight + 1.0);
	        }
#endif
        /*	else if(world.volume.isSolid(ipos + Vec3i(0,1,0))){
		        player.velo.clear();
		        player.pos[1] = ipos[1] - CELLSIZE / 2 + 1.7 + 1;
	        }*/

            KeyboardState ks = Keyboard.GetState();
            if (ks.IsKeyDown(Keys.NumPad4))
                yaw += dt;
            if (ks.IsKeyDown(Keys.NumPad6))
                yaw -= dt;
            if (ks.IsKeyDown(Keys.NumPad8))
                pitch += dt;
            if (ks.IsKeyDown(Keys.NumPad2))
                pitch -= dt;
            if (ks.IsKeyDown(Keys.W))
                trymove(new Vector3(0, 0, (ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift) ? runSpeed : walkSpeed) * -dt));
            if (ks.IsKeyDown(Keys.S))
                trymove(new Vector3(0, 0, walkSpeed * dt));
            if (ks.IsKeyDown(Keys.A))
                trymove(new Vector3(-1, 0, 0) * walkSpeed  * dt);
            if (ks.IsKeyDown(Keys.D))
                trymove(new Vector3(1, 0, 0) * walkSpeed * dt);
            if (flying)
            {
                if (ks.IsKeyDown(Keys.Q))
                    trymove(new Vector3(0, dt * walkSpeed, 0));
                if (ks.IsKeyDown(Keys.Z))
                    trymove(new Vector3(0, -dt * walkSpeed, 0));
            }
            if (ks.IsKeyDown(Keys.Space) && floorTouched)
                trymove(new Vector3(0, 5, 0), true);

            // Toggle fly mode
            if (oldKeys != null && oldKeys.IsKeyDown(Keys.F) && ks.IsKeyUp(Keys.F))
                moveMode = moveMode == MoveMode.Fly ? MoveMode.Walk : MoveMode.Fly;

            // Toggle ghost mode
            if (oldKeys != null && oldKeys.IsKeyDown(Keys.C) && ks.IsKeyUp(Keys.C))
                moveMode = moveMode == MoveMode.Ghost ? MoveMode.Walk : MoveMode.Ghost;

            // Toggle curtype
            if (oldKeys != null && oldKeys.IsKeyDown(Keys.X) && ks.IsKeyUp(Keys.X))
                curtype = curtype == Game1.Cell.Type.Grass ? Game1.Cell.Type.Dirt : curtype == Game1.Cell.Type.Dirt ? Game1.Cell.Type.Gravel : Game1.Cell.Type.Grass;

            // Dig the cell forward
            if (oldKeys != null && oldKeys.IsKeyDown(Keys.T) && ks.IsKeyUp(Keys.T))
            {
                Vector3 dir = Vector3.Transform(Vector3.Forward, rot);
                for (int i = 0; i < 8; i++)
                {
                    Vec3i ci = Game1.real2ind(pos + dir * i / 2).index;
                    Game1.Cell c = world.cell(ci.X, ci.Y, ci.Z);
                    if (c.isSolid() && world.setCell(ci.X, ci.Y, ci.Z, new Game1.Cell(Game1.Cell.Type.Air)))
                    {
                        bricks[c.type] += 1;
                        break;
                    }
                }
            }

            // Place a solid cell next to another solid cell.
            // Feasible only if the player has a brick.
            if (oldKeys != null && oldKeys.IsKeyDown(Keys.G) && ks.IsKeyUp(Keys.G) && 0 < bricks[curtype])
            {
                Vector3 dir = Vector3.Transform(Vector3.Forward, rot);
                for (int i = 0; i < 8; i++)
                {
                    Vec3i ci = Game1.real2ind(pos + dir * i / 2).index;

                    if(world.isSolid(ci.X, ci.Y, ci.Z))
                        continue;

                    bool buried = false;
                    for (int ix = 0; ix < 2 && !buried; ix++) for (int iz = 0; iz < 2 && !buried; iz++) for (int iy = 0; iy < 2 && !buried; iy++)
                            {
                                // Position to check collision with the walls
                                Vector3 hitcheck = new Vector3(pos.X + (ix * 2 - 1) * boundWidth, pos.Y - eyeHeight + iy * boundHeight, pos.Z + (iz * 2 - 1) * boundLength);

                                if(ci == Game1.real2ind(hitcheck))
                                    buried = true;
                            }
                    if (buried)
                        continue;

                    bool supported = false;
                    for(int j = 0; j < directions.Length; j++)
                        if (world.isSolid(ci + directions[j]))
                        {
                            supported = true;
                            break;
                        }
                    if (!supported)
                        continue;

                    if (world.setCell(ci.X, ci.Y, ci.Z, new Game1.Cell(curtype)))
                    {
                        bricks[curtype] -= 1;
                        break;
                    }
                }
            }

            if (oldKeys != null && oldKeys.IsKeyDown(Keys.K) && ks.IsKeyUp(Keys.K))
            {
                try
                {
                    System.IO.FileStream fs = new System.IO.FileStream("save.sav", System.IO.FileMode.Create);
                    System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    game.serialize(bw);
                    fs.Close();
                }
                catch (Exception e)
                {
                    Game1.logwriter.Write(e.ToString());
                }
            }

            if (oldKeys != null && oldKeys.IsKeyDown(Keys.L) && ks.IsKeyUp(Keys.L))
            {
                try
                {
                    System.IO.FileStream fs = new System.IO.FileStream("save.sav", System.IO.FileMode.Open);
                    System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
                    System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                    game.unserialize(br);
                    fs.Close();
                }
                catch (Exception e)
                {
                    Game1.logwriter.Write(e.ToString());
                }
            }

            oldKeys = ks;

            updateRot();

            floorTouched = false;
            trymove(velo * dt); //pos += velo * dt;
	        rot = Quaternion.Slerp(desiredRot, rot, (float)Math.Exp(-dt * 5.0));
        }

        /// <summary>
        /// Try moving to a position designated by the coordinates pos + delta.
        /// </summary>
        /// <param name="delta">Vector to add to current position to designate destination.</param>
        /// <param name="setvelo">Flag that tells delta really means velocity vector.</param>
        /// <returns>true if the movement is feasible, otherwise false and no movement is performed.</returns>
        public bool trymove(Vector3 delta, bool setvelo = false)
        {
	        if(setvelo){
		        velo += delta;
		        return true;
	        }
            
            // Retrieve vector in world coordinates
            Vector3 worldDelta = Vector3.Transform(delta, Matrix.CreateFromAxisAngle(Vector3.UnitY, (float)yaw));

            // Destination position
            Vector3 dest = pos + worldDelta;

            Vector3 worldDeltaDir = worldDelta;
            worldDeltaDir.Normalize();

            if (moveMode != MoveMode.Ghost)
            {
                for (int ix = 0; ix < 2; ix++) for (int iz = 0; iz < 2; iz++) for (int iy = 0; iy < 2; iy++)
                        {
                            // Position to check collision with the walls
                            Vector3 hitcheck = new Vector3(dest.X + (ix * 2 - 1) * boundWidth, dest.Y - eyeHeight + iy * boundHeight, dest.Z + (iz * 2 - 1) * boundLength);

                            Game1.IndFrac inf = Game1.real2ind(hitcheck);

                            if (world.isSolid(inf))
                            {
                                // Clear velocity component along delta direction
                                float vad = Vector3.Dot(velo, worldDeltaDir);
                                if (0 < vad)
                                {
                                    if (worldDeltaDir.Y < 0)
                                        floorTouched = true;
                                    velo -= vad * worldDeltaDir;
                                }
                                return false;
                            }
                        }
            }

            pos = dest;
            return false;
        }

        static void serializeVector3(System.IO.BinaryWriter bw, Vector3 v)
        {
            bw.Write(v.X);
            bw.Write(v.Y);
            bw.Write(v.Z);
        }

        static void unserializeVector3(System.IO.BinaryReader br, ref Vector3 v)
        {
            v.X = br.ReadSingle();
            v.Y = br.ReadSingle();
            v.Z = br.ReadSingle();
        }

        static void serializeQuat(System.IO.BinaryWriter bw, Quaternion q)
        {
            bw.Write(q.W);
            bw.Write(q.X);
            bw.Write(q.Y);
            bw.Write(q.Z);
        }

        static void unserializeQuat(System.IO.BinaryReader br, ref Quaternion q)
        {
            q.W = br.ReadSingle();
            q.X = br.ReadSingle();
            q.Y = br.ReadSingle();
            q.Z = br.ReadSingle();
        }

        public void serialize(System.IO.BinaryWriter bw)
        {
            serializeVector3(bw, pos);
            serializeVector3(bw, velo);
            bw.Write(pitch);
            bw.Write(yaw);
            serializeQuat(bw, rot);
            serializeQuat(bw, desiredRot);
            bw.Write(bricks[Game1.Cell.Type.Grass]);
            bw.Write(bricks[Game1.Cell.Type.Dirt]);
            bw.Write(bricks[Game1.Cell.Type.Gravel]);
            bw.Write((byte)moveMode);
        }

        public void unserialize(System.IO.BinaryReader br)
        {
            unserializeVector3(br, ref pos);
            unserializeVector3(br, ref velo);
            pitch = br.ReadDouble();
            yaw = br.ReadDouble();
            unserializeQuat(br, ref rot);
            unserializeQuat(br, ref desiredRot);
            bricks[Game1.Cell.Type.Grass] = br.ReadInt32();
            bricks[Game1.Cell.Type.Dirt] = br.ReadInt32();
            bricks[Game1.Cell.Type.Gravel] = br.ReadInt32();
            moveMode = (MoveMode)br.ReadByte();
        }
    }
}
