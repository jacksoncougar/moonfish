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
            {   //lets try twos compliment?
                ushort radix = (ushort)((bits >> 00 & 0x7FF));
                //if ((radix & 0x400) == 0x400) //negetive value
                //{
                    //what if there is not sign bit? would that make more sense?
                    //x7ff would be max_value -
                    //x400 would be... half value +//?????
                    //3ff would be less than half
                    //00 would be zero, then we offset by k, k? where k is half the range of values
                    //0x7FF should result in ;must be negetive max?
                    //0x400 should result in -1? 0?; positive max?
                    // what should result in 1? 0x3FF??
                    // what should result in 0? 0x000??
                //if (radix == 0x7FF) return -1;
                ////these values are negetive
                //if (radix == 0x400) return 1;
                ////these values are positve
                //if (radix == 0) return 0;
                if (radix == 0x7FF || radix == 0) return 0;//true? else return the value with a sign 
                return (float)(radix - 0x3FF) / (float)0x3FF;
                //}
                //else
                //{
                //    short rr = (short)(radix);
                //    return rr * xy_max_inverse;
                //}
                //if ((radix & 0x400) == 0x400) return -((~radix) & 0x000007FF) * xy_max_inverse;
                ////if ((radix & 0x400) == 0x400) return (float)(~(radix & 0x3FF) + 1) * xy_max_inverse;
                //else return (float)(radix & 0x3FF) * xy_max_inverse;
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
                short radix = (short)((bits >> 11 & 0x7FF));
                if (radix == 0x7FF || radix == 0) return 0;//true? else return the value with a sign 
                return (float)(radix - 0x3FF) / (float)0x3FF;
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
                short radix = (short)((bits >> 22 & 0x3FF));
                if (radix == 0x3FF || radix == 0) return 0;//true? else return the value with a sign 
                return (float)(radix - 0x1FF) / (float)0x1FF;
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
