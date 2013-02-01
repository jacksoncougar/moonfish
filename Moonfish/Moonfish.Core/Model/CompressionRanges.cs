using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public struct CompressionRanges
    {
        public readonly Range x;
        public readonly Range y;
        public readonly Range z;
        public readonly Range u1;
        public readonly Range v1;
        public readonly Range u2;
        public readonly Range v2;

        public CompressionRanges(Range x, Range y, Range z, Range u1, Range v1, Range u2, Range v2)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.u1 = u1;
            this.v1 = v1;
            this.u2 = u2;
            this.v2 = v2;
        }
    }
}