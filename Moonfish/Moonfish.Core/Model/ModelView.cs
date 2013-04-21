using Moonfish.Core.Definitions;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Moonfish.Core.Model
{
    class ModelView : GameWindow
    {
        private Model model;
        ViewCamera camera = new ViewCamera();
        Point last_mouse = new Point();

        private bool DragMode = false;
        bool reset_mouse = true;
        static float unit_length = 0.01f;


        public ModelView(Model model)
        {
            // TODO: Complete member initialization
            this.model = model;

            this.Mouse.ButtonDown += Mouse_ButtonDown;
            this.Mouse.ButtonUp += Mouse_ButtonUp;
            this.Mouse.Move += Mouse_Move;
            this.Mouse.WheelChanged += Mouse_WheelChanged;

            camera.LookAt(Vector3.Zero);
        }

        void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs e)
        {
            camera.Telescope(e.DeltaPrecise * 0.25f);
        }

        void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs e)
        {
            // if we are in 'drag-mode'
            if (DragMode)
            {
                //  store the mouse delta
                //var delta = Point.Subtract(e.Position, (Size)last_mouse);

                //  return the mouse to the previous position
                if (reset_mouse)
                {
                    Cursor.Position = last_mouse;
                    reset_mouse = false;
                    return;
                }
                RotateCamera(e.XDelta, e.YDelta);
                reset_mouse = true;
            }
            // endif 'drag-modoe'
        }

        private void RotateCamera(float xdelta, float ydelta)
        {
            camera.RotateY(xdelta);
            camera.RotateX(ydelta);
        }

        void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle)
            {
                DragMode = false;
                Cursor.Position = last_mouse;
                Cursor.Show();
            }
        }

        void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle)
            {
                DragMode = true;
                last_mouse = Cursor.Position;
                Cursor.Hide();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height);

            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, Width / (float)Height, 1.01f, 1000.0f);
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
            GL.VertexPointer(3, VertexPointerType.Float, 12, 0);
            //GL.EnableClientState(ArrayCap.NormalArray);
            //GL.NormalPointer(NormalPointerType.Float, 56, 8);
            //GL.TexCoordPointer(2, TexCoordPointerType.Float, 56, 0);
            //GL.EnableClientState(ArrayCap.TextureCoordArray);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);
            camera.Update(); 
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref camera.ViewMatrix);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.PushMatrix();
            GL.Rotate(30, Vector3.UnitZ);
            foreach (var region in model.Regions)
            {
                //RenderMesh(region.Permutations[0].HighLOD);
                RenderEdges(region.Permutations[0].HighLOD);
            }
            //RenderNodes();
            GL.PopMatrix();
            SwapBuffers();
        }

        private void RenderEdges(short p)
        {

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 3 * model.Mesh[p].Coordinates.Length), model.Mesh[p].Coordinates, BufferUsageHint.StaticDraw);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(sizeof(ushort) * model.Mesh[p].Indices.Length), model.Mesh[p].Indices, BufferUsageHint.StaticDraw);


            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Lighting);
            GL.Color4(Color4.GreenYellow);
            foreach (var group in model.Mesh[p].Primitives)
            {
                GL.DrawElements(BeginMode.TriangleStrip, group.strip_length, DrawElementsType.UnsignedShort, group.strip_start * 2);
            }
            GL.Color4(Color4.Blue);
            GL.PointSize(10f);
            GL.Begin(BeginMode.Points);
            GL.Vertex3(0, 0, 0);
            GL.End();
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
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(sizeof(float) * 3 * model.Mesh[p].Normals.Length), model.Mesh[p].Coordinates, BufferUsageHint.StaticDraw);
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

    class ViewCamera
    {
        ViewConstraintSettings contraints = new ViewConstraintSettings();
        Vector3 position = new Vector3(0, 0, 4);
        Vector3 origin = new Vector3(0, 0, 0);
        float x_rotation;
        float y_rotation;
        float z_rotation;
        bool view_matrix_is_dirty = true;

        public Matrix4 ViewMatrix = Matrix4.Identity;

        public bool Update()
        {
            if (view_matrix_is_dirty)
            {
                if (UpdateViewMatrix() == false) return false;      //  exit on failure
            }
            return true;
        }

        public bool UpdateViewMatrix()
        {
            ViewMatrix = Matrix4.Identity;
            ViewMatrix *= Matrix4.CreateFromAxisAngle(Vector3.UnitY, MathHelper.DegreesToRadians(-y_rotation));
            ViewMatrix *= Matrix4.CreateFromAxisAngle(Vector3.UnitX, MathHelper.DegreesToRadians(-x_rotation));
            ViewMatrix *= Matrix4.CreateTranslation(-position);
            //ViewMatrix *= Matrix4.LookAt(position, origin, Vector3.UnitY);

            //
            return true;
        }

        public void LookAt(Vector3 location)
        {
            origin = location; 
            view_matrix_is_dirty = true;
        }

        public void RotateX(float degrees)
        {
            var rotation = UpdateRotation(contraints.Wrap_X, contraints.X_Range, x_rotation, degrees);
            if (rotation == x_rotation) return; // if nothing has changed save ourselves from updating needlessly,
            view_matrix_is_dirty = true;        // otherwise the rotation value has changed and should be reculcuted
            x_rotation = rotation;
        }

        public void RotateY(float degrees)
        {
            var rotation = UpdateRotation(contraints.Wrap_Y, contraints.Y_Range, y_rotation, degrees);
            if (rotation == y_rotation) return; // if nothing has changed save ourselves from updating needlessly,
            view_matrix_is_dirty = true;        // otherwise the rotation value has changed and should be reculcuted
            y_rotation = rotation;
        }

        public void RotateZ(float degrees)
        {
            var rotation = UpdateRotation(contraints.Wrap_Z, contraints.Z_Range, z_rotation, degrees);
            if (rotation == z_rotation) return; // if nothing has changed save ourselves from updating needlessly,
            view_matrix_is_dirty = true;        // otherwise the rotation value has changed and should be reculcuted
        }

        private float UpdateRotation(bool allow_wrap, Range rotation_contraints, float rotation_field, float rotation_degrees)
        {
            var rotation_sum = rotation_field + rotation_degrees;
            if (allow_wrap)
                return Range.Wrap(rotation_contraints, rotation_sum);
            else
                return Range.Truncate(rotation_contraints, rotation_sum);
        }

        internal void Telescope(float translation)
        {
            var Forward = ViewMatrix.Column2.Xyz;
            this.position.Z += translation;
            view_matrix_is_dirty = true;
        }
    }

    class ViewConstraintSettings
    {
        /* * *
         * Axis alignment
         * 
         *         y z
         *      x__|/ 
         * 
         * Looking at the screen x is horizontal, y is vertical and z is depth.
         * * */
        bool _wrap_x_rotation = true;      //  allow excess values to wrap back into the range?
        bool _x_rotation_enabled = true;    //  allow rotation at all?
        float _x_rotation_min = 0.0f;     //  looking striaght down
        float _x_rotation_max = 360.0f;      //  looking staight up

        bool _wrap_y_rotation = true;      //  allow excess values to wrap back into the range?
        bool _y_rotation_enabled = true;    //  allow rotation at all?
        float _y_rotation_min = 0.0f;       //  looking straight forward
        float _y_rotation_max = 360.0f;     //  looking straight forward (1 circular rotation)

        bool _wrap_z_rotation = false;      //  allow excess values to wrap back into the range?
        bool _z_rotation_enabled = false;   //  allow rotation at all?
        float _z_rotation_min = 0.0f;       //  looking level
        float _z_rotation_max = 0.0f;       //  looking level

        public Range X_Range { get { return new Range(_x_rotation_min, _x_rotation_max); } }
        public Range Y_Range { get { return new Range(_y_rotation_min, _y_rotation_max); } }
        public Range Z_Range { get { return new Range(_z_rotation_min, _z_rotation_max); } }
        public bool Wrap_X { get { return _wrap_x_rotation; } }
        public bool Wrap_Y { get { return _wrap_y_rotation; } }
        public bool Wrap_Z { get { return _wrap_z_rotation; } }
    }
}
