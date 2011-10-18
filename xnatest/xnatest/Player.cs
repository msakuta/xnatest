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

        public Player(Game1.World theworld)
        {
            world = theworld;
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
                trymove(new Vector3(0, 0, -walkSpeed * dt));
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
    }
}
