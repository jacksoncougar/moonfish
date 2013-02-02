// Released to the public domain. Use, modify and relicense at will.

using System;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using OpenTK.Input;
using Moonfish.Core.Raw;
using System.Threading;
using Moonfish.Core.Model.Adjacency;

namespace StarterKit
{
    class QuickModelView : GameWindow
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

        bool return_value = false;
        float yaw_ = default(float);
        float rotation_speed = (float)Math.PI / (90.0f);
        int selectedstrip_ = 0;
        float zoom_ = zoom_min;
        const float zoom_max = 1000.0f;
        const float zoom_min = 1.0f;
        float zoom_step = 0.015f;
        private TriangleStrip[] strips = new TriangleStrip[0];
        public ushort[] GetStrip() { return strips[0].indices; }
        private Adjacencies stripper;

        public QuickModelView(Moonfish.Core.Model.Mesh mesh, Adjacencies stripper)
            : base(400, 400)
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

        public QuickModelView(Moonfish.Core.Model.Mesh mesh)
        {
            // TODO: Complete member initialization
            this.mesh = mesh;
            strips = new TriangleStrip[] { new TriangleStrip() { colour = Color4.Red, indices = mesh.Indices } };
            SelectedStrip = 0;
            draw_strip = true;
        }
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            GL.ClearColor(0.1f, 0.2f, 0.5f, 0.0f); 
            GL.PointSize(2.0f);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);
            float[] lightPose1 = { -4f, 2.0f, 0.0f, 1.0f };
            float[] lightColor1 = { .5f, 0.5f, 0.5f, 0.0f };
            GL.Light(LightName.Light0, LightParameter.Diffuse, lightColor1);
            GL.Light(LightName.Light0, LightParameter.Position, lightPose1);

            GL.Disable(EnableCap.Lighting);
            GL.DisableClientState(ArrayCap.NormalArray);
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
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.NormalArray);
            //mesh.GenerateNormals();
            return true;
        }

        public override void Exit()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            base.Exit();
        }

        /// <summary>
        /// Called when it is time to setup the next frame. Add you game logic here.
        /// </summary>
        /// <param name="e">Contains timing information for framerate independent logic.</param>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

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

            Matrix4 modelview = Matrix4.LookAt(new Vector3(Zoom,0f,1f), Vector3.Zero, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);

            GL.Rotate(Yaw, Vector3.UnitZ);

            if (draw_strip)
            {
                // draw vertices as points
                GL.Begin(BeginMode.Points);
                GL.Color4(Color4.White);
                GL.Vertex3(new Vector3(0, 0, 0));
                for (uint i = 0; i < mesh.Vertices.Length; i++)
                {
                    GL.Color4(Color4.Yellow);
                    GL.Vertex3(mesh.Vertices[i].Position);
                }
                GL.End(); 
                //draw edges 
                GL.Begin(BeginMode.LineStrip);
                for (uint i = 0; i < strips[0].indices.Length - 1; i++)
                {
                    if (strips[0].indices[i] == strips[0].indices[i + 1]) continue;
                    GL.Color4(Color4.Black);
                    GL.Vertex3(mesh.Vertices[strips[0].indices[i]].Position);
                }
                GL.End();
                //draw shaded tris
                GL.CullFace(CullFaceMode.Back);
                for (uint i = 0; i < SelectedStrip; i++)
                {
                    GL.Begin(BeginMode.TriangleStrip);
                    for (uint j = 0; j < strips[i].indices.Length; j++)
                    {
                        GL.PointSize(4.0f);
                        GL.Color4(Color4.Gray);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Normal3((Vector3)mesh.Vertices[strips[i].indices[j]].Normal);
                    }
                    GL.End();
                }
                //draw shaded tris
                GL.CullFace(CullFaceMode.Front);
                for (uint i = 0; i < SelectedStrip; i++)
                {
                    GL.Begin(BeginMode.TriangleStrip);
                    for (uint j = 0; j < strips[i].indices.Length; j++)
                    {
                        GL.PointSize(4.0f);
                        GL.Color4(Color4.Red);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Normal3((Vector3)mesh.Vertices[strips[i].indices[j]].Normal);
                    }
                    GL.End();
                }
                //draw normals
                for (uint i = 0; i < SelectedStrip; i++)
                {
                    GL.Begin(BeginMode.Lines);
                    for (uint j = 0; j < strips[i].indices.Length; j++)
                    {
                        GL.Color4(Color4.Red);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position);
                        GL.Color4(Color4.Green);
                        GL.Vertex3(mesh.Vertices[strips[i].indices[j]].Position + (Vector3)mesh.Vertices[strips[i].indices[j]].Normal * 0.15f);
                    }
                    GL.End();
                }
            }
            SwapBuffers();
        }
    }
}