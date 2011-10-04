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
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        VertexPositionNormalTexture[] cubeVertices;
        VertexDeclaration vertexDeclaration;
        BasicEffect basicEffect;

        public const int CELLSIZE = 32;


        Player player;

 
        /// Convert from real world coords to massvolume index vector
        public static Vec3i real2ind(Vector3 pos)
        {
	        Vector3 tpos = pos + new Vector3(0,-1.7f,0);
	        Vec3i vi = new Vec3i((int)Math.Floor(tpos.X), (int)Math.Floor(tpos.Y), (int)Math.Floor(tpos.Z));
	        return vi + new Vec3i(CELLSIZE, CELLSIZE, CELLSIZE) / 2;
        }

        public static Vector3 ind2real(Vec3i ipos)
        {
	        Vec3i tpos = ipos - new Vec3i(CELLSIZE, CELLSIZE, CELLSIZE) / 2;
	        return tpos.cast() - new Vector3(0,-1.7f,0);
        }

        public struct Cell
        {
            public enum Type { Air, Grass };
            public Cell(Type t) { mtype = t; }
            private Type mtype;
            public Type type { get { return mtype; } }
        }

        public class CellVolume
        {
            Cell[, ,] v = new Cell[CELLSIZE, CELLSIZE, CELLSIZE];
            public Cell cell(int ix, int iy, int iz)
            {
                return v[ix, iy, iz];
            }
            public void initialize()
            {
		        float[,] field = new float[CELLSIZE, CELLSIZE];
		        PerlinNoise.perlin_noise(new PerlinNoise.PerlinNoiseParams(){ seed = 12321, cellsize = CELLSIZE}, new PerlinNoise.FieldAssign(field));
		        for(int ix = 0; ix < CELLSIZE; ix++) for(int iy = 0; iy < CELLSIZE; iy++) for(int iz = 0; iz < CELLSIZE; iz++){
                    v[ix, iy, iz] = new Cell(field[ix, iz] * CELLSIZE / 2 < iy ? Cell.Type.Air : Cell.Type.Grass);
		        }
/*		        for(int ix = 0; ix < CELLSIZE; ix++) for(int iy = 0; iy < CELLSIZE; iy++) for(int iz = 0; iz < CELLSIZE; iz++){
			        updateAdj(ix, iy, iz);
		        }*/
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

        public class World{
            public CellVolume volume = new CellVolume();
        }

        World world = new World();


        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 1200;
            graphics.PreferredBackBufferHeight = 800;
        }

        static Vector2 V2(float x, float y)
        {
            return new Vector2(x, y);
        }

        static Vector3 V3(float x, float y, float z)
        {
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            world.volume.initialize();
            player = new Player(world);

            float tilt = MathHelper.ToRadians(0);  // 0 degree angle
            // Use the world matrix to tilt the cube along x and y axes.
            worldMatrix = Matrix.CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
            viewMatrix = Matrix.CreateLookAt(new Vector3(5, 5, 5), Vector3.Zero, Vector3.Up);

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),  // 45 degree angle
                (float)GraphicsDevice.Viewport.Width /
                (float)GraphicsDevice.Viewport.Height,
                1.0f, 200.0f);

            basicEffect = new BasicEffect(graphics.GraphicsDevice);

            basicEffect.World = worldMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.Projection = projectionMatrix;

            // primitive color
            basicEffect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
            basicEffect.DiffuseColor = new Vector3(1.0f, 1.0f, 1.0f);
            basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
            basicEffect.SpecularPower = 5.0f;
            basicEffect.Alpha = 1.0f;

            basicEffect.LightingEnabled = true;
            if (basicEffect.LightingEnabled)
            {
                basicEffect.AmbientLightColor = Vector3.One * 0.25f;
                basicEffect.DirectionalLight0.Enabled = true; // enable each light individually
                if (basicEffect.DirectionalLight0.Enabled)
                {
                    // sky light
                    basicEffect.DirectionalLight0.DiffuseColor = new Vector3(0.1f, 0.2f, 0.2f); // range is 0 to 1
                    basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(0, -1, 0));
                    // points from the light to the origin of the scene
                    basicEffect.DirectionalLight0.SpecularColor = Vector3.One;
                }

                basicEffect.DirectionalLight1.Enabled = true;
                if (basicEffect.DirectionalLight1.Enabled)
                {
                    // sun light
                    basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                    basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0.15f, -1, -0.25f));
                    basicEffect.DirectionalLight1.SpecularColor = Vector3.Zero;
                }
            }


            vertexDeclaration = new VertexDeclaration(new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            });

            Vector3[, ,] tt3 = new Vector3[2, 2, 2];

            for (int ix = 0; ix < 2; ix++) for (int iy = 0; iy < 2; iy++) for (int iz = 0; iz < 2; iz++)
                        tt3[ix, iy, iz] = new Vector3(ix, iy, iz);

            cubeVertices = new VertexPositionNormalTexture[]{
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, 0, -1), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(0, 0, -1), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(0, 0, -1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(0, 0, -1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(0, 0, -1), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, 0, -1), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(0, 0, 1), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(0, 0, 1), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 0, 1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 0, 1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(0, 0, 1), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(0, 0, 1), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, -1, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(0, -1, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(0, -1, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(0, -1, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(0, -1, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, -1, 0), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(0, 1, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(0, 1, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 1, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 1, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(0, 1, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(0, 1, 0), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(-1, 0, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(-1, 0, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(-1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(-1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(-1, 0, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(-1, 0, 0), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(1, 0, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(1, 0, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(1, 0, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(1, 0, 0), V2(0, 0)),
            };

            VertexBuffer vb = new VertexBuffer(graphics.GraphicsDevice, vertexDeclaration, cubeVertices.Length, BufferUsage.None);
            vb.SetData(cubeVertices);
            graphics.GraphicsDevice.SetVertexBuffer(vb);

            base.Initialize();
        }

        Texture2D myTexture;

        Vector2 spritePosition = Vector2.Zero;

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            myTexture = Content.Load<Texture2D>("grass");

            basicEffect.TextureEnabled = true;
            basicEffect.Texture = myTexture;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        Vector2 spriteSpeed = new Vector2(100, 0);

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            spritePosition += spriteSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            int MaxX = graphics.GraphicsDevice.Viewport.Width - myTexture.Width;
            int MinX = 0;

            // Check for the bounce.
            if (spritePosition.X > MaxX)
            {
                spriteSpeed.X *= -1;
                spritePosition.X = MaxX;
            }
            else if (spritePosition.X < MinX)
            {
                spriteSpeed.X *= -1;
                spritePosition.X = MinX;
            }

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            player.think((float)gameTime.ElapsedGameTime.TotalSeconds);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            /*
            const double dist = 16.0;
            double phase = gameTime.TotalGameTime.TotalMilliseconds / 1000.0;
            basicEffect.View = Matrix.CreateLookAt(new Vector3((float)(dist * Math.Cos(phase)), (float)(dist * (Math.Sin(phase / 10.0) + 1.0) / 2.0), (float)(dist * Math.Sin(phase))), Vector3.Zero, Vector3.Up);
             */

            // Obtaininig a conjugate requires a local varable declared, which is silly.
            Quaternion qrot = player.getRot();
            qrot.Conjugate();
            basicEffect.View = Matrix.CreateTranslation(-player.getPos()) * Matrix.CreateFromQuaternion(qrot);

            graphics.GraphicsDevice.Clear(Color.SteelBlue);

#if false
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
            spriteBatch.Draw(myTexture, spritePosition, Color.White);
            spriteBatch.End();
#else
            RasterizerState rasterizerState1 = new RasterizerState();
            rasterizerState1.CullMode = CullMode.CullClockwiseFace;
            graphics.GraphicsDevice.RasterizerState = rasterizerState1;
            for (int ix = 0; ix < CELLSIZE; ix++) for (int iy = 0; iy < CELLSIZE; iy++) for (int iz = 0; iz < CELLSIZE; iz++) if (world.volume.cell(ix, iy, iz).type != Cell.Type.Air)
                    {
                        // It's very unreasonable, but adding one to ix and iy seems to fix the problem #4.
                        basicEffect.World = Matrix.CreateWorld(new Vector3(ix - CELLSIZE / 2 + 1, iy - CELLSIZE / 2, iz - CELLSIZE / 2 + 1), V3(0, 0, 1), Vector3.Up);
                        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            graphics.GraphicsDevice.DrawPrimitives(
                                PrimitiveType.TriangleList,
                                0,
                                12
                            );
                        }
                    }
#endif
            base.Draw(gameTime);
        }
    }
}
