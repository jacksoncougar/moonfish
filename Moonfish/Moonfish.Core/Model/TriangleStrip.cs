using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    internal class TriangleStrip
    {
        internal ushort[] Indices { get; set; }
        internal ushort MaterialID { get; set; }

        /// <summary>
        /// Writes the contents of both indices into a single TriangleStrip without checking or fixing winding order
        /// </summary>
        /// <param name="source_strip"></param>
        /// <param name="strip_to_add"></param>
        /// <returns></returns>
        internal static TriangleStrip Append(ref TriangleStrip source_strip, TriangleStrip strip_to_add)
        {
            if (source_strip.Indices.Length == 0)                               // Potential for early exit...
            {
                source_strip = new TriangleStrip() { Indices = strip_to_add.Indices, MaterialID = strip_to_add.MaterialID };
                return source_strip;
            }
            ushort[] buffer = new ushort[source_strip.Indices.Length + strip_to_add.Indices.Length];
            source_strip.Indices.CopyTo(buffer, 0);
            strip_to_add.Indices.CopyTo(buffer, source_strip.Indices.Length);
            var copy =  new TriangleStrip() { Indices = buffer, MaterialID = source_strip.MaterialID };
            source_strip = copy;
            return copy;
        }

        /// <summary>
        /// Adds two TriangleStrips together maintaining winding order. Material ids are inherited from source strip
        /// </summary>
        /// <param name="source_strip">Source strip</param>
        /// <param name="strip_to_add">Strip to add to end of Source</param>
        /// <returns>returns new Triangle strip which contains indices from both input strips linked by degenerative triangles</returns>
        internal static TriangleStrip Concatenate(ref TriangleStrip source_strip, TriangleStrip strip_to_add)
        {
            if (source_strip.Indices.Length == 0)                               // Potential for early exit...
            {
                source_strip = new TriangleStrip() { Indices = strip_to_add.Indices, MaterialID = strip_to_add.MaterialID };
                return source_strip;
            }

            bool even = source_strip.Indices.Length % 2 == 0;                   /* Determine if the strip being concatenated with is even or odd, 
                                                                                 * to maintain winding orders*/
            var degenerative_indices_to_add = even ? 2 : 3;                     /* If this is an even strip, we only need two degenerative indices 
                                                                                 * (to create 3 degenerative triangle) and keep the winding order intact*/
            var source_length = source_strip.Indices.Length;
            var addition_length = strip_to_add.Indices.Length;

            var new_indices = new ushort[source_length + degenerative_indices_to_add + addition_length]; // Create buffer large enough to hold new strip indices
            var offset = 0;                                                     // Keep track of where we write the indices in the strip with this

            source_strip.Indices.CopyTo(new_indices, offset);                   // Copy source_indices to buffer
            offset += source_strip.Indices.Length;
            new_indices[offset++] = source_strip.Indices[source_length - 1];    /* Add last index in source strip to buffer
                                                                                 * strip now looks like ...abcc <- where c is last index*/
            new_indices[offset++] = strip_to_add.Indices[0];                    /* Add first index in add_strip to buffer
                                                                                 * strip now looks like ...abccj*/
            if (!even) new_indices[offset++] = strip_to_add.Indices[0];          /* If this strip is even add another index to flip winding order for the 
                                                                                 * strip being added.
                                                                                 * strip now looks like ...abccjj*/
            strip_to_add.Indices.CopyTo(new_indices, offset);                   /* Copy over the add_strip indices to buffer
                                                                                 * strip now looks like ...abccjjkl... if source_strip was odd or 
                                                                                 * ...abccjjjkl... if source_strip was even
                                                                                 * if odd: abc- bcc+ ccj- cjj+ jjk- jkl+        [jkl+]  
                                                                                 * if even abc+ bcc- ccj+ cjj- jjj+ jjk- jkl+   [jkl+]
                                                                                 */
            var new_strip = new TriangleStrip() { Indices = new_indices, MaterialID = source_strip.MaterialID };
            source_strip = new_strip;
            return source_strip;
        }
    }

    internal struct Triangle : IEnumerable<ushort>
    {
        public ushort Vertex1;
        public ushort Vertex2;
        public ushort Vertex3;
        public int MaterialID;

        IEnumerator<ushort> IEnumerable<ushort>.GetEnumerator()
        {
            yield return Vertex1;
            yield return Vertex2;
            yield return Vertex3;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
