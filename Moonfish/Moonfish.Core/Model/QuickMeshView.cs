// Released to the public domain. Use, modify and relicense at will.

using Moonfish.Core.Model;
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
        public enum Mode
        {
            water,
            other,
        }
        Mode mode = Mode.other;
        Camera camera = new Camera();

        private Moonfish.Core.Model.RenderMesh mesh;
        Vector4 light0_position = new Vector4(-2, -2, 2, 1);
        bool draw_strip = false;

        public QuickMeshView(Moonfish.Core.Model.RenderMesh mesh)
            : base(400, 400, GraphicsMode.Default, "", GameWindowFlags.Default)
        {
            // TODO: Complete member initialization
            this.mesh = mesh;
            draw_strip = true;


            this.Mouse.ButtonDown += camera.Mouse_ButtonDown;
            this.Mouse.ButtonUp += camera.Mouse_ButtonUp;
            this.Mouse.Move += camera.Mouse_Move;
            this.Mouse.WheelChanged += camera.Mouse_WheelChanged;
        }

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
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 14 * mesh.Coordinates.Length), mesh.Coordinates, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * mesh.Indices.Length), mesh.Indices, BufferUsageHint.StaticDraw);

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

            //Matrix4 modelview = Matrix4.LookAt(new Vector3(0, 0, 0f), new Vector3(-10, 26, 4.5f), Vector3.UnitZ);

            //GL.MatrixMode(MatrixMode.Modelview);
            //GL.LoadMatrix(ref modelview);
            Matrix4 modelview = Matrix4.LookAt(new Vector3(camera.Zoom, 0, 10), Vector3.Zero, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);
            GL.GetFloat(GetPName.ModelviewMatrix, out modelview);
            GL.Rotate(camera.Pitch, modelview.Column0.Xyz);
            camera.Forward = modelview.Column2.Xyz;
            GL.Rotate(camera.Yaw, Vector3.UnitZ);
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
            //GL.Translate(-mesh.Center);

            //draw vertices
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.DepthTest);
            GL.Color4(Color4.DarkBlue);
            GL.DrawArrays(BeginMode.Points, 0, mesh.Coordinates.Length);

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Color4(Color4.LightCyan);
            if (mesh.Primitives == null)
            {
                GL.DrawElements(BeginMode.TriangleStrip, mesh.Indices.Length, DrawElementsType.UnsignedShort, 0);
            }
            else
            {
                foreach (var group in mesh.Primitives)
                {
                    GL.DrawElements(BeginMode.Triangles, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
                }
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Color4(Color4.DarkOrange);
            if (mesh.Primitives == null)
            {
                GL.DrawElements(BeginMode.TriangleStrip, mesh.Indices.Length, DrawElementsType.UnsignedShort, 0);
            }
            else
            {
                foreach (var group in mesh.Primitives)
                {
                    GL.DrawElements(mesh.PrimitiveType, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
                }
            }
            GL.PopMatrix();
            SwapBuffers();
        }
    }
}