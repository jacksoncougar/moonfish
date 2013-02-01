using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public struct Range
    {
        public readonly float min;
        public readonly float max;

        public Range(float min1, float max1)
        {
            // TODO: Complete member initialization
            this.min = min1;
            this.max = max1;
        }

        internal static Range Include(Range x, float p)
        {
            float min = x.min;
            float max = x.min;
            if (x.min > p) min = p;
            if (x.max < p) max = p;
            return new Range(min, max);
        }
    }
}
