using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Raw
{
    public class RadixSorter
    {
        public RadixSorter()
        {
            int[] random = new int[16];
            Random ran = new Random();
            for (int i = 0; i < random.Length; i++)
            {
                random[i] = ran.Next();
            }
            var sorted_random = Sort(random);
        }

        int[] Sort(int[] input_values)
        {
            int[] Counters = new int[256 * 4];
            int[] Offsets = new int[256];
            int[] output_values = new int[input_values.Length];

            for (int i = 0; i < input_values.Length; ++i)
            {
                int value = input_values[i];
                Counters[256 * 0 + (value >> 0 & 0xFF)]++;
                Counters[256 * 1 + (value >> 8 & 0xFF)]++;
                Counters[256 * 2 + (value >> 16 & 0xFF)]++;
                Counters[256 * 3 + (value >> 24 & 0xFF)]++;
            }
            for (int i = 1; i < 256; ++i)
            {
                Offsets[i] = Offsets[i - 1] + Counters[i - 1];

            } 
            for (int i = 0; i < output_values.Length; ++i)
            {
               // byte b = input_values[i];
               // output_values[Offsets[b]++] = input_values[i];
            }
            return output_values;
        }
    }
}
