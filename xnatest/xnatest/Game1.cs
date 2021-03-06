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
    /// The type to index World.volume. Be sure to keep it a struct.
    using CellIndex = Vec3i;

    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public partial class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;

        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        VertexPositionNormalTexture[] cubeVertices;
        VertexDeclaration vertexDeclaration;
        BasicEffect basicEffect;

        public const int CELLSIZE = 16;

        public int maxViewDistance = CELLSIZE * 2;


        Player player;

        /// <summary>
        /// A type dedicated real2ind() to return a indices vector and a fraction vector together.
        /// </summary>
        /// <remarks>Implicitly converts to Vec3i for compatibility.</remarks>
        /// <seealso cref="real2ind"/>
        public struct IndFrac
        {
            public Vec3i index;
            public Vector3 frac;
            public static implicit operator Vec3i(IndFrac inf)
            {
                return inf.index;
            }
        }
 
        /// <summary>
        /// Convert from real world coords to massvolume index vector
        /// </summary>
        /// <param name="pos">world vector</param>
        /// <returns>indices</returns>
        public static IndFrac real2ind(Vector3 pos)
        {
	        Vector3 tpos = pos;
	        Vec3i vi = new Vec3i((int)Math.Floor(tpos.X), (int)Math.Floor(tpos.Y), (int)Math.Floor(tpos.Z));
	        return new IndFrac(){index = vi + new Vec3i(CELLSIZE, CELLSIZE, CELLSIZE) / 2, frac = pos - vi.cast()};
        }

        /// <summary>
        /// Convert from massvolume index vector to real world coordinates
        /// </summary>
        /// <param name="ipos">indices</param>
        /// <returns>world vector</returns>
        public static Vector3 ind2real(Vec3i ipos)
        {
	        Vec3i tpos = ipos - new Vec3i(CELLSIZE, CELLSIZE, CELLSIZE) / 2;
	        return tpos.cast();
        }


        /// <summary>
        /// Returns remainder of divison of integers that never be negative.
        /// </summary>
        /// <remarks>
        /// Normally, dividing two integers can result in positive or negative, depending the signs of
        /// dividend and divisor.
        /// In our case, this is not desirable.
        /// </remarks>
        /// <param name="v">Dividend</param>
        /// <param name="divisor">Divisor</param>
        /// <returns>Remainder</returns>
        static public int SignModulo(int v, int divisor)
        {
            return (v - v / divisor * divisor + divisor) % divisor;
        }

        /// <summary>
        /// Returns quotient of division of integers that is always greatest integer equal or less than the quotient, regardless of sign.
        /// </summary>
        /// <remarks>
        /// Normally, dividing two integers truncates remainder of absolute value, but we want the result to be consistent regardless of
        /// zero position.
        /// </remarks>
        /// <param name="v">Dividend</param>
        /// <param name="divisor">Divisor</param>
        /// <returns>Quotient</returns>
        /// <seealso cref="SignModulo"/>
        static public int SignDiv(int v, int divisor)
        {
            return (v - SignModulo(v, divisor)) / divisor;
        }


        World world;


        public Game1()
        {
            world = new World(this);
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

        VertexBuffer vb;

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
//            world.volume.Add(new CellIndex(0, 0, 0), new CellVolume(world));
//            world.volume[new CellIndex(0, 0, 0)].initialize(new Vec3i(0,0,0));
            player = new Player(this, world);

            float tilt = MathHelper.ToRadians(0);  // 0 degree angle
            // Use the world matrix to tilt the cube along x and y axes.
            worldMatrix = Matrix.CreateRotationX(tilt) * Matrix.CreateRotationY(tilt);
            viewMatrix = Matrix.CreateLookAt(new Vector3(5, 5, 5), Vector3.Zero, Vector3.Up);

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
                MathHelper.ToRadians(45),  // 45 degree angle
                (float)GraphicsDevice.Viewport.Width /
                (float)GraphicsDevice.Viewport.Height,
                0.3f, 200.0f);

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

                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(0, 0, 1), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(0, 0, 1), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 0, 1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(0, 0, 1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(0, 0, 1), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(0, 0, 1), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, 0, -1), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(0, 0, -1), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(0, 0, -1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(0, 0, -1), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(0, 0, -1), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(0, 0, -1), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(1, 0, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 0], V3(1, 0, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 1, 1], V3(1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 1], V3(1, 0, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[1, 0, 0], V3(1, 0, 0), V2(0, 0)),

                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(-1, 0, 0), V2(0, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 1], V3(-1, 0, 0), V2(0, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(-1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 1], V3(-1, 0, 0), V2(1, 1)),
                new VertexPositionNormalTexture(tt3[0, 1, 0], V3(-1, 0, 0), V2(1, 0)),
                new VertexPositionNormalTexture(tt3[0, 0, 0], V3(-1, 0, 0), V2(0, 0)),
            };

            vb = new VertexBuffer(graphics.GraphicsDevice, vertexDeclaration, cubeVertices.Length, BufferUsage.None);
            vb.SetData(cubeVertices);
            graphics.GraphicsDevice.SetVertexBuffer(vb);

            base.Initialize();
        }

        Texture2D myTexture;
        Texture2D dirtTexture;
        Texture2D gravelTexture;

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
            dirtTexture = Content.Load<Texture2D>("dirt");
            gravelTexture = Content.Load<Texture2D>("gravel");
            spriteFont = Content.Load<SpriteFont>("SpriteFont1");

            basicEffect.TextureEnabled = true;
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

        public static System.IO.StreamWriter logwriter;

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            logwriter = new System.IO.StreamWriter("xnatest.log", true);
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
            player.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            world.Update(gameTime.ElapsedGameTime.TotalSeconds);

            logwriter.WriteLine("Update: dt = {0}, total = {1}, CellVolumes = {2}", gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f, gameTime.TotalGameTime.TotalMilliseconds / 1000.0f, world.volume.Count);
            logwriter.Close();
            logwriter = null;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            CellVolume.cellInvokes = 0;
            CellVolume.cellForeignInvokes = 0;
            CellVolume.cellForeignExists = 0;
            logwriter = new System.IO.StreamWriter("xnatest.log", true);
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
            // The stencil buffer could be disabled because of text rendering.
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Make sure that the vertex buffer for the cube is loaded.
            graphics.GraphicsDevice.SetVertexBuffer(vb);

            BoundingFrustum bf = new BoundingFrustum(basicEffect.View * basicEffect.Projection);

            IndFrac inf = real2ind(player.getPos());
            RasterizerState rasterizerState1 = new RasterizerState();
            rasterizerState1.CullMode = CullMode.CullClockwiseFace;
            graphics.GraphicsDevice.RasterizerState = rasterizerState1;
            foreach (System.Collections.Generic.KeyValuePair<CellIndex, CellVolume> kv in world.volume)
            {
                if (kv.Value.solidcount == 0)
                    continue;

                Vec3i keyindex = kv.Key * CELLSIZE;
                
                // Cull by bounding box and viewing frustum
                BoundingBox bb = new BoundingBox(ind2real(keyindex), ind2real((kv.Key + new Vec3i(1, 1, 1)) * CELLSIZE));
                if (bf.Contains(bb) == ContainmentType.Disjoint)
                    continue;

                // Cull too far CellVolumes
                if ((kv.Key.X + 1) * CELLSIZE + maxViewDistance < inf.index.X)
                    continue;
                if (inf.index.X < kv.Key.X * CELLSIZE - maxViewDistance)
                    continue;
                if ((kv.Key.Y + 1) * CELLSIZE + maxViewDistance < inf.index.Y)
                    continue;
                if (inf.index.Y < kv.Key.Y * CELLSIZE - maxViewDistance)
                    continue;
                if ((kv.Key.Z + 1) * CELLSIZE + maxViewDistance < inf.index.Z)
                    continue;
                if (inf.index.Z < kv.Key.Z * CELLSIZE - maxViewDistance)
                    continue;
                for (int ix = 0; ix < CELLSIZE; ix++) for (int iz = 0; iz < CELLSIZE; iz++)
                    {
                        // This detail culling is not much effective.
                        //if (bf.Contains(new BoundingBox(ind2real(keyindex + new Vec3i(ix, kv.Value.scanLines[ix, iz, 0], iz)), ind2real(keyindex + new Vec3i(ix + 1, kv.Value.scanLines[ix, iz, 1] + 1, iz + 1)))) != ContainmentType.Disjoint)
                        for (int iy = kv.Value.scanLines[ix, iz, 0]; iy <= kv.Value.scanLines[ix, iz, 1]; iy++)
                            DrawInternal(kv, ix, iy, iz, inf);
                    }
            }
#endif

            // Drawing debug texts
            int y = 0;
            spriteBatch.Begin();
            spriteBatch.DrawString(spriteFont, string.Format("Draw time = {0}", gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f), new Vector2(0, y), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("pos = {0}", player.getPos()), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("CellVolumes = {0}", world.volume.Count), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("Bricks = {0}, {1}, {2}, selected = {3}",
                player.bricks[Cell.Type.Grass],
                player.bricks[Cell.Type.Dirt],
                player.bricks[Cell.Type.Gravel],
                player.curtype),
                new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("WorldSolids = {0}", world.solidcount), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("cellInvokes = {0}", CellVolume.cellInvokes), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("cellForeignInvokes = {0}", CellVolume.cellForeignInvokes), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.DrawString(spriteFont, string.Format("cellForeignExists = {0}", CellVolume.cellForeignExists), new Vector2(0, y += spriteFont.LineSpacing), Color.Aqua);
            spriteBatch.End();

            logwriter.WriteLine("draw time = {0}", gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f);
            logwriter.WriteLine("cellInvokes = {0}", CellVolume.cellInvokes);
            logwriter.WriteLine("cellForeignInvokes = {0}", CellVolume.cellForeignInvokes);
            logwriter.WriteLine("cellForeignExists = {0}", CellVolume.cellForeignExists);
            logwriter.Close();
            logwriter = null;

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draw a single CellVolume.
        /// </summary>
        /// <param name="kv"></param>
        /// <param name="ix"></param>
        /// <param name="iy"></param>
        /// <param name="iz"></param>
        /// <param name="inf">Index of</param>
        protected void DrawInternal(System.Collections.Generic.KeyValuePair<CellIndex, CellVolume> kv, int ix, int iy, int iz, IndFrac inf)
        {
            if (kv.Value.cell(ix, iy, iz).type == Cell.Type.Air)
                return;
//            if (6 <= kv.Value.cell(ix, iy, iz).adjacents)
//                return;
            if (maxViewDistance < Math.Abs(ix + kv.Key.X * CELLSIZE - inf.index.X))
                return;
            if (maxViewDistance < Math.Abs(iy + kv.Key.Y * CELLSIZE - inf.index.Y))
                return;
            if (maxViewDistance < Math.Abs(iz + kv.Key.Z * CELLSIZE - inf.index.Z))
                return;
            bool x0 = ix + kv.Key.X * CELLSIZE < inf.index.X || kv.Value.cell(ix - 1, iy, iz).type != Cell.Type.Air;
            bool x1 = inf.index.X < ix + kv.Key.X * CELLSIZE || kv.Value.cell(ix + 1, iy, iz).type != Cell.Type.Air;
            bool y0 = iy + kv.Key.Y * CELLSIZE < inf.index.Y || kv.Value.cell(ix, iy - 1, iz).type != Cell.Type.Air;
            bool y1 = inf.index.Y < iy + kv.Key.Y * CELLSIZE || kv.Value.cell(ix, iy + 1, iz).type != Cell.Type.Air;
            bool z0 = iz + kv.Key.Z * CELLSIZE < inf.index.Z || kv.Value.cell(ix, iy, iz - 1).type != Cell.Type.Air;
            bool z1 = inf.index.Z < iz + kv.Key.Z * CELLSIZE || kv.Value.cell(ix, iy, iz + 1).type != Cell.Type.Air;

            // It's very unreasonable, but adding one to ix and iy seems to fix the problem #4.
            basicEffect.World = Matrix.CreateWorld(new Vector3(
                kv.Key.X * CELLSIZE + ix - CELLSIZE / 2 + 1,
                kv.Key.Y * CELLSIZE + iy - CELLSIZE / 2,
                kv.Key.Z * CELLSIZE + iz - CELLSIZE / 2 + 1),
                V3(0, 0, 1), Vector3.Up);
            switch (kv.Value.cell(ix, iy, iz).type)
            {
                case Cell.Type.Grass: basicEffect.Texture = myTexture; break;
                case Cell.Type.Dirt: basicEffect.Texture = dirtTexture; break;
                case Cell.Type.Gravel: basicEffect.Texture = gravelTexture; break;
            }
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                if (!x0 && !x1 && !y0 && !y1)
                    graphics.GraphicsDevice.DrawPrimitives(
                        PrimitiveType.TriangleList,
                        0,
                        12
                    );
                else
                {
                    if (!x0)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 8 * 3, 2);
                    if (!x1)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 10 * 3, 2);
                    if (!y0)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0 * 3, 2);
                    if (!y1)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 2 * 3, 2);
                    if (!z0)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 4 * 3, 2);
                    if (!z1)
                        graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 6 * 3, 2);
                }
            }
        }

        /// <summary>
        /// The magic number sequence to identify this file as a binary save file.
        /// </summary>
        /// <remarks>Taken from Subversion repository's UUID.</remarks>
        static byte[] saveFileSignature = { 0x83, 0x1f, 0x50, 0xec, 0x5b, 0xf7, 0x40, 0x3a };

        /// <summary>
        /// The current version of this program's save file.
        /// </summary>
        static int saveFileVersion = 1;

        public void serialize(System.IO.BinaryWriter bw)
        {
            bw.Write(saveFileSignature);
            bw.Write(saveFileVersion);
            player.serialize(bw);
            world.serialize(bw);
        }

        public void unserialize(System.IO.BinaryReader br)
        {
            byte[] signature = br.ReadBytes(saveFileSignature.Length);
            if (!signature.SequenceEqual(saveFileSignature))
                throw new Exception("File signature mismatch");

            int version = br.ReadInt32();
            if(version != saveFileVersion)
                throw new Exception(string.Format("File version mismatch, file = {0}, program = {1}", version, saveFileVersion));

            player.unserialize(br);
            world.unserialize(br);
        }
    }
}
