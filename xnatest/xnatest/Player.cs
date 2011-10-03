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
    class Player{
        Game1.World world;
        public Player(Game1.World theworld)
        {
            world = theworld;
        }

        public Vector3 getPos() { return pos; }
	    public Quaternion getRot(){return rot;}
	    void setPos(Vector3 apos){pos = apos;}
	    void setRot(Quaternion arot){rot = arot;}
	    void updateRot(){
            desiredRot =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)yaw) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)pitch);
	    }
	    Vector3 pos = new Vector3(0,10,0);
	    Vector3 velo;
	    Quaternion rot;
	    Quaternion desiredRot;
	    double pitch = 0; ///< Pitch and Yaw
        double yaw = 0;
        public void think(float dt){
	        velo += new Vector3(0,-9.8f,0) * dt;

#if true
	        Vec3i ipos = Game1.real2ind(pos);
	        if(world.volume.isSolid(ipos)/* || world.volume.isSolid(ipos - Vec3i(0,1,0))*/){
		        velo = Vector3.Zero;
		        pos.Y = (float)(ipos.Y - Game1.CELLSIZE / 2 + 2.7);
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
                trymove(new Vector3(0, 0, -dt));
            if (ks.IsKeyDown(Keys.S))
                trymove(new Vector3(0, 0, dt));
            if (ks.IsKeyDown(Keys.A))
                trymove(new Vector3(-1, 0, 0) * dt);
            if (ks.IsKeyDown(Keys.D))
                trymove(new Vector3(1, 0, 0) * dt);
            if (ks.IsKeyDown(Keys.Q))
                trymove(new Vector3(0, 20 * dt, 0), true);
            if (ks.IsKeyDown(Keys.Z))
                trymove(new Vector3(0, -10 * dt, 0), true);

            updateRot();

	        pos += velo * dt;
	        rot = Quaternion.Slerp(desiredRot, rot, (float)Math.Exp(-dt * 5.0));
        }

        public bool trymove(Vector3 delta, bool setvelo = false)
        {
	        if(setvelo){
		        velo += delta;
		        return true;
	        }
	        Vector3 dest = pos + Vector3.Transform(delta, Matrix.CreateFromAxisAngle(Vector3.UnitY, (float)yaw));
	        if(!world.volume.isSolid(Game1.real2ind(dest + new Vector3(0,0.5f,0)))){
		        pos = dest;
		        return true;
	        }
	        return false;
        }
    }
}
