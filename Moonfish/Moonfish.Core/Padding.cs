using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{
    public static class Padding
    {
        public static void Pad(this Stream stream, int alignment = 4)
        {
            stream.Seek(GetCount(stream.Position, alignment), SeekOrigin.Current);
        }

        public static int Pad(long address, int alignment = 4)
        {
            address += (int)GetCount(address, alignment);
            return (int)address;
        }

        static long GetCount(long address, long alignment = 4)
        {
            return (-address) & (alignment - 1);
        }
    }
}
