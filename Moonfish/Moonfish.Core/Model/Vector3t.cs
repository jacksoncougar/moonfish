using OpenTK;

namespace Moonfish.Core.Model
{
    /// <summary>
    /// A one/third vector where each componant is composed of 11 or 10 bits
    /// A single bit for sign, and the remainder used for magnitude (domain = {0.0 => 1.0} )
    /// </summary>
    public struct Vector3t
    {
        const float z_max_inverse = 1 / (float)0x1FF;
        const float xy_max_inverse = 1 / (float)0x3FF;
        private uint bits;

        public float X
        {
            get
            {
                short lsb_radix = (short)((bits >> 00 & 0x7FF));
                if (lsb_radix == 0x400) return -1;
                return ((lsb_radix & 0x400) == 0x400 ? (float)-((~lsb_radix & 0x3FF)) * -z_max_inverse : (float)(lsb_radix & 0x1FF) * z_max_inverse);
            }
            set
            {
                bits &= ~(uint)0x7FF;
                var x_bits = (uint)(value < 0 ? 0x400 | (uint)(-value * 0x3FF) & 0x3FF : (uint)(value * 0x3FF) & 0x3FF);
                bits |= x_bits;
            }
        }
        public float Y
        {
            get
            {
                short mid_radix = (short)((bits >> 11 & 0x7FF));
                if (mid_radix == 0x400) return -1;
                return ((mid_radix & 0x400) == 0x400 ? (float)-((~mid_radix & 0x3FF)) * -z_max_inverse : (float)(mid_radix & 0x1FF) * z_max_inverse);
            }
            set
            {
                bits &= ~(uint)0x7FF << 11 | 0x7FF;
                var y_bits = (uint)(value < 0 ? 0x400 | (uint)(-value * 0x3FF) & 0x3FF : (uint)(value * 0x3FF) & 0x3FF);
                bits |= y_bits << 11;
            }
        }
        public float Z
        {
            get
            {
                short msb_radix = (short)((bits >> 22 & 0x3FF));
                if (msb_radix == 0x200) return -1;
                return ((msb_radix & 0x200) == 0x200 ? (float)-((~msb_radix & 0x3FF)) * -z_max_inverse : (float)(msb_radix & 0x1FF) * z_max_inverse);
            }
            set
            {
                bits &= 0x003FFFFF;
                var z_bits = (uint)(value < 0 ? 0x200 | (uint)(-value * 0x1FF) & 0x1FF : (uint)(value * 0x1FF) & 0x1FF);
                bits |= z_bits << 22;
            }
        }

        public Vector3t(uint value)
        {
            if (value == 2097152)
            {
                int o = 0;
            }
            bits = value;
            float g = Y;
        }

        public static explicit operator Vector3(Vector3t tvector)
        {
            return new Vector3(x: tvector.X, y: tvector.Y, z: tvector.Z);
        }
        public static explicit operator Vector3t(Vector3 vector3)
        {
            return new Vector3t() { X = vector3.X, Y = vector3.Y, Z = vector3.Z };
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Z);
        }
    }
}
