﻿using System;
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
            float max = x.max;
            if (x.min > p) min = p;
            if (x.max < p) max = p;
            return new Range(min, max);
        }

        internal static Range Expand(Range x, float p)
        {
            float min = x.min;
            float max = x.max;
            min -= p;
            max += p;
            return new Range(min, max);
        }

        /// <summary>
        /// Truncates the passed value to the closest value in range if value is outside of the range (range.min or range.max)
        /// Else returns the value unchanged.
        /// </summary>
        /// <param name="range">The range of values to check against</param>
        /// <param name="value">The value to truncate</param>
        /// <returns>The truncated value</returns>
        internal static float Truncate(Range range, float value)
        {
            if (value < range.min) return range.min;
            if (value > range.max) return range.max;
            return value;
        }

        internal static float Median(Range range)
        {
            return (range.min + range.max) * 0.5f;
        }
    }
}
