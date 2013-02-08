// Released to the public domain. Use, modify and relicense at will.

using Moonfish.Core.Model.Adjacency;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace StarterKit
{
    class QuickMeshView : GameWindow
    {
        int SelectedStrip
        {
            get { return selectedstrip_; }
            set { if (value >= 0 && value <= strips.Length) selectedstrip_ = value; }
        }
        float Yaw
        {
            get
            {
                yaw_ = yaw_ > 360 ? yaw_ + (float)(rotation_speed - 360) : yaw_ + rotation_speed;
                return yaw_;
            }
        }
        float Zoom
        {
            get { return zoom_; }
            set { if (value > zoom_min && value < zoom_max) zoom_ = value; }
        }

        private Moonfish.Core.Model.Mesh mesh;
        bool draw_strip = false;

        float yaw_ = default(float);
        float rotation_speed = (float)Math.PI / (40.0f);
        int selectedstrip_ = 0;
        float zoom_ = zoom_min + 1.0f;
        const float zoom_max = 100.0f;
        const float zoom_min = 1.0f;
        float zoom_step = 0.15f;
        private TriangleStrip[] strips = new TriangleStrip[0];
        public ushort[] GetStrip() { return strips[0].indices; }
        private Adjacencies stripper;

        public QuickMeshView(Moonfish.Core.Model.Mesh mesh, Adjacencies stripper)
            : base(400, 400, GraphicsMode.Default, "", GameWindowFlags.Default)
        {
            // TODO: Complete member initialization
            this.mesh = mesh;
            this.stripper = stripper;
            Random random = new Random();
            draw_strip = false;
            strips = new TriangleStrip[] { new TriangleStrip() { indices = stripper.GenerateTriangleStrip() } };
            byte[] rgb = new byte[3];
            for (int i = 0; i < strips.Length; i++)
            {
                random.NextBytes(rgb);
                strips[i].colour = new Color4(rgb[0], rgb[1], rgb[2], 255);
            }
            SelectedStrip = SelectedStrip > strips.Length ? strips.Length : SelectedStrip;
            draw_strip = true;
        }

        public QuickMeshView(Moonfish.Core.Model.Mesh mesh)
            : base(400, 400, GraphicsMode.Default, "", GameWindowFlags.Default)
        {
            // TODO: Complete member initialization
            this.mesh = mesh;
            strips = new TriangleStrip[] { new TriangleStrip() { colour = Color4.Red, indices = mesh.Indices } };
            SelectedStrip = 0;
            draw_strip = true;
        }

        Vector4 light0_position = new Vector4(-2, -2, 2, 1);
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.1f, 0.1f, 0.1f, 0.0f);
            GL.PointSize(2.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.AmbientAndDiffuse);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            float[] lightPose1 = { -4f, 7.0f, 6.0f, 1.0f };
            float[] lightColor1 = { 0.4f, 0.32f, 1f, 0.0f };
            GL.Light(LightName.Light0, LightParameter.Diffuse, lightColor1);
            GL.Light(LightName.Light0, LightParameter.Position, light0_position);

            uint[] buffers = new uint[2];
            GL.GenBuffers(2, buffers);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffers[0]);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffers[1]);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 14 * mesh.Vertices.Length), mesh.Vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort)* mesh.Indices.Length), mesh.Indices, BufferUsageHint.StaticDraw);
            
            string path = string.Empty;
            if (File.Exists(path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "gameball.BMP")))
            {
                Bitmap bitmap = new Bitmap(path);
                System.Drawing.Imaging.BitmapData bitmap_data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

                int id = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, id);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bitmap.Width, bitmap.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, bitmap_data.Scan0);
                GL.Enable(EnableCap.Texture2D);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                GL.TexEnv(TextureEnvTarget.TextureEnv,
                 TextureEnvParameter.TextureEnvMode,
                 (float)TextureEnvMode.Modulate);
                bitmap.UnlockBits(bitmap_data);
            }
            
            
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, 56, 20);
            GL.EnableClientState(ArrayCap.NormalArray);
            GL.NormalPointer(NormalPointerType.Float, 56, 8);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, 56, 0);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
        }

        /// <summary>
        /// Called when your window is resized. Set your viewport here. It is also
        /// a good place to set up your projection matrix (which probably changes
        /// along when the aspect ratio of your window).
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        bool EnableLighting()
        {
            GL.Enable(EnableCap.NormalArray);
            return true;
        }

        public override void Exit()
        {
            base.Exit();
        }

        /// <summary>
        /// Called when it is time to setup the next frame. Add you game logic here.
        /// </summary>
        /// <param name="e">Contains timing information for framerate independent logic.</param>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            Matrix4 modelview = Matrix4.LookAt(new Vector3(Zoom, 0f, 1f), Vector3.Zero, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            if (Keyboard[Key.Escape])
            {
                Exit();
            }
            if (Keyboard[Key.End])
            {
                EnableLighting();
            }
            if (Keyboard[Key.W] || Keyboard[Key.Up])
                Zoom -= zoom_step;
            if (Keyboard[Key.S] || Keyboard[Key.Back])
                Zoom += zoom_step;
            if (Keyboard[Key.Plus])
            {
                SelectedStrip++;
                Thread.Sleep(10);
            }
            if (Keyboard[Key.Minus] || Keyboard[Key.Back])
            {
                SelectedStrip--;
                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Called when it is time to render the next frame. Add your rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.PushMatrix();

            GL.Scale(10, 10, 10);
            var rot = Yaw;
            GL.Rotate(rot, Vector3.UnitZ);
            GL.Rotate(-rot, Vector3.UnitY);

            if (draw_strip)
            {
                GL.Color4(Color4.White);
                GL.Begin(BeginMode.Points);
                GL.Vertex3(light0_position.Xyz);
                GL.End();
                GL.Enable(EnableCap.Lighting);
                GL.Color4(Color4.LawnGreen);
                GL.DrawArrays(BeginMode.Points, 0, mesh.Vertices.Length);
                foreach (var group in mesh.ShaderGroups)
                {
                    GL.Color4(Color4.Wheat);
                    GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
                }
                GL.Color4(new Color4(0x11, 0x11, 0x11, 0xFF));
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                foreach (var group in mesh.ShaderGroups)
                {
                    GL.Color4(0x33,0x33,0x33,0xFF);
                    GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
                }
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            
                GL.Disable(EnableCap.Lighting);
                for (uint i = 0; i < SelectedStrip; i++)
                {
                    GL.Begin(BeginMode.Lines);
                    for (uint j = 0; j < strips[i].indices.Length; j++)
                    {
                        GL.Color4(Color4.Green);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position + (Vector3)mesh.Vertices[strips[i].indices[j]].Normal * 0.001f); 

                        GL.Color4(Color4.Red);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position + (Vector3)mesh.Vertices[strips[i].indices[j]].Tangent * 0.001f); 
                       
                        GL.Color4(Color4.Blue); 
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position + (Vector3)mesh.Vertices[strips[i].indices[j]].Bitangent * 0.001f);
                        
                    }
                    GL.End();
                }
            }
            GL.PopMatrix();
            SwapBuffers();
        }
    }
}