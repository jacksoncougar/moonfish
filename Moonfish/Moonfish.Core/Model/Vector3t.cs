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
                ushort radix = (ushort)(bits >> 00 & 0x7FF);                // retrieve the bits of this componant (first 11 bits)
                if (radix == 0x7FF || radix == 0) return 0;                 // two special cases for zero: return zero on either
                if ((radix & 0x400) == 0x400)                               // if sign bit is set, output should be negetive
                    return -(float)(~(radix) & 0x3FF) * xy_max_inverse;     /* return the radix 'ones compliment' trimming the sign bit
                                                                             * return the negetive value*/
                else                                                        /* else just return the radix value multiplied by the pre-
                                                                             * calculated ratio */
                    return (float)(radix) * xy_max_inverse;
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
                ushort radix = (ushort)(bits >> 00 & 0x7FF);                // retrieve the bits of this componant (first 11 bits)
                if (radix == 0x7FF || radix == 0) return 0;                 // two special cases for zero: return zero on either
                if ((radix & 0x400) == 0x400)                               // if sign bit is set, output should be negetive
                    return -(float)(~(radix) & 0x3FF) * xy_max_inverse;     /* return the radix 'ones compliment' trimming the sign bit
                                                                             * return the negetive value*/
                else                                                        /* else just return the radix value multiplied by the pre-
                                                                             * calculated ratio */
                    return (float)(radix) * xy_max_inverse;
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
                ushort radix = (ushort)(bits >> 00 & 0x3FF);                // retrieve the bits of this componant (first 10 bits)
                if (radix == 0x7FF || radix == 0) return 0;                 // two special cases for zero: return zero on either
                if ((radix & 0x200) == 0x200)                               // if sign bit is set, output should be negetive
                    return -(float)(~(radix) & 0x1FF) * z_max_inverse;     /* return the radix 'ones compliment' trimming the sign bit
                                                                             * return the negetive value*/
                else                                                        /* else just return the radix value multiplied by the pre-
                                                                             * calculated ratio */
                    return (float)(radix) * z_max_inverse;
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
            //float g = Y;
            var t = new Vector3(X,Y,Z);
            if (System.Math.Round(t.Length, 4) != 1)
            {
                int ge = 0;
            }
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
