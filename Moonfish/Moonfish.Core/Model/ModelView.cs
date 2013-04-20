using Moonfish.Core.Definitions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Moonfish.Core.Model
{
    class ModelView : GameWindow
    {
        private Model model;
        private bool DragMode = false;
        private float Yaw { get { return yaw; } set { yaw = value > 360 ? value - 360 : value < 0 ? 360 - value : value; } }
        private float Pitch { get { return pitch; } set { pitch = value > 89.0f ? 89.0f : value < -89.0 ? -89.0f : value; } }
        private float Zoom { get { return zoom; } set { zoom = value > zoom_max ? zoom_max : value < zoom_min ? zoom_min : value; } }
        private Vector2 mouse_delta = Vector2.Zero;
        private Vector3 forward = Vector3.UnitY;
        float yaw = default(float);
        float pitch = default(float);
        float zoom = zoom_min;
        static float zoom_step = 0.125f;
        static float zoom_min = 0.01f;
        static float zoom_max = 65.00f;
        static float unit_length = 0.01f;

        public ModelView(Model model)
        {
            // TODO: Complete member initialization
            this.model = model;

            this.Mouse.ButtonDown += Mouse_ButtonDown;
            this.Mouse.ButtonUp += Mouse_ButtonUp;
            this.Mouse.Move += Mouse_Move;
            this.Mouse.WheelChanged += Mouse_WheelChanged;
        }

        void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs e)
        {
            Zoom -= e.DeltaPrecise * zoom_step;
        }

        void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs e)
        {
            if (DragMode)
            {
                Yaw += e.XDelta;
                Pitch += e.YDelta;
            }
        }

        void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle) DragMode = false;
        }

        void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle) DragMode = true;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e); 
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, zoom_min, zoom_max);
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
            float[] lightPose1 = { -4f, -4.0f, -4.0f, 1.0f };
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

            Matrix4 modelview = Matrix4.LookAt(new Vector3(forward * zoom), model.Center * 10f, Vector3.UnitZ);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref modelview);
            GL.Scale(Vector3.One * 10);
            GL.GetFloat(GetPName.ModelviewMatrix, out modelview);
            GL.Rotate(Pitch, modelview.Column0.Xyz);
            forward = modelview.Column2.Xyz;
            GL.Rotate(Yaw, Vector3.UnitZ);
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
                RenderEdges(region.Permutations[0].HighLOD);
            }
            RenderNodes();
            GL.PopMatrix();
            SwapBuffers();
        }

        private void RenderEdges(short p)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 14 * model.Mesh[p].Coordinates.Length), model.Mesh[p].Coordinates, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * model.Mesh[p].Indices.Length), model.Mesh[p].Indices, BufferUsageHint.StaticDraw);

            GL.Color4(Color4.GreenYellow);
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.DepthTest);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line); 
            foreach (var group in model.Mesh[p].Primitives)
            {
                GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.DepthTest);
        }

        private void RenderNodes()
        {
            GL.Disable(EnableCap.DepthTest);
            GL.Color4(Color4.Red);
            GL.PointSize(2.50f);
            GL.Begin(BeginMode.Lines);
            RenderNodes(new DNode(), model.Nodes[0]);
            GL.End();
            GL.Enable(EnableCap.DepthTest);
        }

        private void RenderNodes(DNode parent, DNode node)
        {
            /* Intent: render the current node (root) at its position,
             * then push all the nodes translations onto the stack and render the next child node
             * */
            var transformed = new DNode(parent);
            transformed.Position += node.Position;
            transformed.Rotation *= node.Rotation;
            var rotations = Matrix4.Rotate(transformed.Rotation);
            GL.Color4(Color4.Yellow);
            GL.Vertex3(transformed.Position);
            Vector3 pointer = new Vector3();

            pointer = Vector3.TransformVector(node.Up, rotations);
            pointer.Normalize();
            pointer *= 2 * unit_length;
            GL.Vertex3(transformed.Position + pointer);

            GL.Color4(Color4.Red);
            GL.Vertex3(transformed.Position);
            GL.Vertex3(transformed.Position + node.Up * unit_length);
            GL.Color4(Color4.Blue);
            GL.Vertex3(transformed.Position);
            GL.Vertex3(transformed.Position + node.Right * unit_length);
            GL.Color4(Color4.Green);
            GL.Vertex3(transformed.Position);
            GL.Vertex3(transformed.Position + node.Forward * unit_length);

            GL.Color4(Color4.DarkMagenta);
            GL.Vertex3(transformed.Position);
            GL.Vertex3(transformed.AbsolutePosition);

            if (node.FirstChild_NodeIndex != -1)
            {
                var next_node = new DNode(model.Nodes[node.FirstChild_NodeIndex]);
                var p_copy = new DNode(transformed);
                RenderNodes(p_copy, next_node);
            }
            if (node.NextSibling_NodeIndex != -1)
            {
                var sibling_node = new DNode(model.Nodes[node.NextSibling_NodeIndex]);
                RenderNodes(parent, sibling_node);
            }
        }

        private void RenderMesh(short p)
        {
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 14 * model.Mesh[p].Normals.Length), model.Mesh[p].Normals, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * model.Mesh[p].Indices.Length), model.Mesh[p].Indices, BufferUsageHint.StaticDraw);

            GL.Color4(Color4.LawnGreen);
            GL.DrawArrays(BeginMode.Points, 0, model.Mesh[p].Normals.Length);
            foreach (var group in model.Mesh[p].Primitives)
            {
                GL.Color4(Color4.RoyalBlue);
                GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
            }
        }
    }
}
