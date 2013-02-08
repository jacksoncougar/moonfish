using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    class ModelView : GameWindow
    {
        private Model model;

        public ModelView(Model model)
        {
            // TODO: Complete member initialization
            this.model = model;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e); 
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.0f, 64.0f);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 0.0f);
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
            GL.Light(LightName.Light0, LightParameter.Position, lightPose1);

            uint[] buffers = new uint[2];
            GL.GenBuffers(2, buffers);
            GL.BindBuffer(BufferTarget.ArrayBuffer, buffers[0]);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, buffers[1]);

            GL.EnableClientState(ArrayCap.VertexArray);
            GL.VertexPointer(3, VertexPointerType.Float, 56, 20);
            GL.EnableClientState(ArrayCap.NormalArray);
            GL.NormalPointer(NormalPointerType.Float, 56, 8);
            GL.TexCoordPointer(2, TexCoordPointerType.Float, 56, 0);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            Matrix4 modelview = Matrix4.LookAt(new Vector3(2, 0f, 1f), Vector3.Zero, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.PushMatrix();
            GL.Rotate(30, Vector3.UnitZ);
            foreach (var region in model.Regions)
            {
                RenderMesh(region.Permutations[0].HighLOD);
            }
            GL.PopMatrix();
            SwapBuffers();
        }

        private void RenderMesh(short p)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 14 * model.Mesh[p].Vertices.Length), model.Mesh[p].Vertices, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * model.Mesh[p].Indices.Length), model.Mesh[p].Indices, BufferUsageHint.StaticDraw);

            GL.Color4(Color4.LawnGreen);
            GL.DrawArrays(BeginMode.Points, 0, model.Mesh[p].Vertices.Length);
            foreach (var group in model.Mesh[p].ShaderGroups)
            {
                GL.Color4(Color4.Wheat);
                GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
            }
        }
    }
}
