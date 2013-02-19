using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public class Camera
    {
        private bool DragMode = false;
        public float Yaw { get { return yaw; } set { yaw = value > 360 ? value - 360 : value < 0 ? 360 - value : value; } }
        public float Pitch { get { return pitch; } set { pitch = value > 89.0f ? 89.0f : value < -89.0 ? -89.0f : value; } }
        public float Zoom { get { return zoom; } set { zoom = value > zoom_max ? zoom_max : value < zoom_min ? zoom_min : value; } }
        private Vector2 mouse_delta = Vector2.Zero;
        public Vector3 Forward = Vector3.UnitY;
        float yaw = default(float);
        float pitch = default(float);
        float zoom = zoom_min;
        static float zoom_step = 0.125f;
        static float zoom_min = 0.01f;
        static float zoom_max = 65.00f;
        static float unit_length = 0.01f;

        public Camera()
        {
        }

        internal void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs e)
        {
            Zoom -= e.DeltaPrecise * zoom_step;
        }
        internal void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs e)
        {
            if (DragMode)
            {
                Yaw += e.XDelta;
                Pitch += e.YDelta;
            }
        }
        internal void Mouse_ButtonUp(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle) DragMode = false;
        }
        internal void Mouse_ButtonDown(object sender, OpenTK.Input.MouseButtonEventArgs e)
        {
            if (e.Button == OpenTK.Input.MouseButton.Middle) DragMode = true;
        }
    }
}
