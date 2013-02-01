using Moonfish.Core.Model;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model.Adjacency
{
    /// <summary>
    /// Wrapper class of a index array and colour. 
    /// TODO: not use this.
    /// </summary>
    public class TriangleStrip
    {
        public Color4 colour;
        public ushort[] indices;
    }
    public class Adjacencies
    {
        /// <summary>
        /// Stores adjacency information about a triangle
        /// </summary>
        struct AdjacentTriangle
        {
            internal ushort[] Vertices;
            internal ushort[] AdjacentTriangles;

            /// <summary>
            /// Given two valid vertices of this triangle, returns the third
            /// </summary>
            /// <param name="vert0">vertex index</param>
            /// <param name="vert1">vertex index</param>
            /// <returns>third vertex index, or 0xFFFF if invalid</returns>
            internal ushort OppositeVertex(ushort vert0, ushort vert1)
            {
                if (vert0 == Vertices[0] && vert1 == Vertices[1]) return Vertices[2];
                else if (vert1 == Vertices[0] && vert0 == Vertices[1]) return Vertices[2];
                else if (vert0 == Vertices[0] && vert1 == Vertices[2]) return Vertices[1];
                else if (vert1 == Vertices[0] && vert0 == Vertices[2]) return Vertices[1];
                else if (vert0 == Vertices[1] && vert1 == Vertices[2]) return Vertices[0];
                else if (vert1 == Vertices[1] && vert0 == Vertices[2]) return Vertices[0];
                else return 0xFFFF;
            }
            /// <summary>
            /// Returns the index of the edge defined by vert1 - vert0 (order does not matter)
            /// edges are defined as: v1 - v0 => e0, v2 - v0 => e2, v2 - v1 => e1
            /// </summary>
            /// <param name="vert0">edge end vertex</param>
            /// <param name="vert1">edge end vertex</param>
            /// <returns>edge index [0,1,2] or 0xFF if invalid</returns>
            internal byte FindEdge(ushort vert0, ushort vert1)
            {
                if (vert0 == Vertices[0] && vert1 == Vertices[1]) return 0;
                else if (vert1 == Vertices[0] && vert0 == Vertices[1]) return 0;
                else if (vert0 == Vertices[0] && vert1 == Vertices[2]) return 2;
                else if (vert1 == Vertices[0] && vert0 == Vertices[2]) return 2;
                else if (vert0 == Vertices[1] && vert1 == Vertices[2]) return 1;
                else if (vert1 == Vertices[1] && vert0 == Vertices[2]) return 1;
                else return 0xFF;
            }
        }
        struct AdjacentEdge : IComparable<AdjacentEdge>
        {
            public ushort ref0;
            public ushort ref1;
            public ushort owner_triangle;
            public AdjacentEdge(ushort vert0, ushort vert1, ushort face)
            {
                if (vert0 < vert1)
                {
                    ref0 = vert0;
                    ref1 = vert1;
                }
                else
                {
                    ref1 = vert0;
                    ref0 = vert1;
                }
                owner_triangle = face;
            }

            int IComparable<AdjacentEdge>.CompareTo(AdjacentEdge other)
            {
                var key0 = ref0 << 16 | ref1;
                var other_key0 = other.ref0 << 16 | other.ref1;
                return key0.CompareTo(other_key0);}

            public override string ToString()
            {
                return string.Format("{0} : {1}->{2}", owner_triangle, ref0, ref1);
            }
        }

        AdjacentTriangle[] triangle_buffer;
        AdjacentEdge[] edge_buffer;
        bool[] triangle_flags;

        /// <summary>
        /// Creates a new adjacency object, creates internal triangle buffers, edge buffers, and adjacency information
        /// </summary>
        /// <param name="face_array">array of vertex indices in the form of a triangle list</param>
        public Adjacencies(ushort[] face_array)
        {
            triangle_flags = new bool[face_array.Length / 3];
            triangle_buffer = new AdjacentTriangle[face_array.Length / 3];
            edge_buffer = new AdjacentEdge[face_array.Length];

            // initialize buffer data
            for (ushort index = 0; index < triangle_buffer.Length; ++index)
            {
                triangle_buffer[index] = new AdjacentTriangle() { Vertices = new ushort[3], AdjacentTriangles = new ushort[3] { 0xFFFF, 0xFFFF, 0xFFFF } };
                triangle_buffer[index].Vertices[0] = face_array[index * 3 + 0];
                triangle_buffer[index].Vertices[1] = face_array[index * 3 + 1];
                triangle_buffer[index].Vertices[2] = face_array[index * 3 + 2];
                edge_buffer[index * 3 + 0] = new AdjacentEdge(triangle_buffer[index].Vertices[0], triangle_buffer[index].Vertices[1], index);
                edge_buffer[index * 3 + 1] = new AdjacentEdge(triangle_buffer[index].Vertices[1], triangle_buffer[index].Vertices[2], index);
                edge_buffer[index * 3 + 2] = new AdjacentEdge(triangle_buffer[index].Vertices[2], triangle_buffer[index].Vertices[0], index);
            }

            // TODO optimize to use a better sort and remove the memory copy here
            List<AdjacentEdge> EdgeList = new List<AdjacentEdge>(edge_buffer);
            EdgeList.Sort();

            // initialize edge buffer and link adjacent triangles
            ushort last_ref0 = EdgeList[0].ref0;
            ushort last_ref1 = EdgeList[0].ref1;
            ushort count = 0;
            ushort[] temp_buffer = new ushort[3];
            for (int i = 0; i < EdgeList.Count; ++i)
            {
                ushort current_face = EdgeList[i].owner_triangle;
                ushort ref0 = EdgeList[i].ref0;
                ushort ref1 = EdgeList[i].ref1;
                if (ref0 == last_ref0 && ref1 == last_ref1)
                {
                    temp_buffer[count++] = current_face;
                    if (count == 3)
                    {
                        throw new Exception(":D");
                    }
                }
                else
                {
                    if (count == 2)
                    {
                        bool status = UpdateLink(temp_buffer[0], temp_buffer[1], last_ref0, last_ref1);
                        if (!status) throw new Exception(":D");
                    }
                    count = 0;
                    temp_buffer[count++] = current_face;
                    last_ref0 = ref0;
                    last_ref1 = ref1;
                }
            }
            if (count == 2) UpdateLink(temp_buffer[0], temp_buffer[1], last_ref0, last_ref1);
        }

        bool UpdateLink(ushort adj_triangle0, ushort adj_triangle1, ushort ref0, ushort ref1)
        {

            byte edge0 = triangle_buffer[adj_triangle0].FindEdge(ref0, ref1); if (edge0 == 0xff)
                return false;
            byte edge1 = triangle_buffer[adj_triangle1].FindEdge(ref0, ref1); if (edge1 == 0xff)
                return false;
            triangle_buffer[adj_triangle0].AdjacentTriangles[edge0] = adj_triangle1;
            triangle_buffer[adj_triangle1].AdjacentTriangles[edge1] = adj_triangle0;

            return true;
        }
        ushort[] FindBestStrip(int face_index, ref bool[] in_processed_faces, out ushort strip_length, int max_strip_length = 32)
        {
            ushort[][] strips = new ushort[3][];
            ushort[] strip_lengths = new ushort[3];
            bool[][] out_processed_faces = new bool[3][];
            ushort[] ref0 = new ushort[3];
            ushort[] ref1 = new ushort[3];

            ref0[0] = triangle_buffer[face_index].Vertices[0];
            ref1[0] = triangle_buffer[face_index].Vertices[1];

            ref0[1] = triangle_buffer[face_index].Vertices[2];
            ref1[1] = triangle_buffer[face_index].Vertices[0];

            ref0[2] = triangle_buffer[face_index].Vertices[1];
            ref1[2] = triangle_buffer[face_index].Vertices[2];


            for (int i = 0; i < 3; ++i)
            {
                out_processed_faces[i] = new bool[in_processed_faces.Length];
                Array.Copy(in_processed_faces, out_processed_faces[i], in_processed_faces.Length);

                strips[i] = GenerateStrip(face_index, ref0[i], ref1[i], out_processed_faces[i], out strip_lengths[i]);

                //TODO: reverse tri_strip trace
            }
            var longest = strip_lengths[0];
            int best_strip = 0;
            if (strip_lengths[1] > longest) best_strip = 1;
            if (strip_lengths[2] > longest) best_strip = 2;
            Array.Copy(out_processed_faces[best_strip], in_processed_faces, out_processed_faces[best_strip].Length);
            strip_length = strip_lengths[best_strip];
            return strips[best_strip];
        }
        ushort[] GenerateStrip(int face_index, ushort oldest, ushort middle, bool[] processed_faces, out ushort strip_length, int max_strip_length = 2048)
        {
            // create a buffer large enough to hold strip indices;
            ushort[] strip_indices = new ushort[max_strip_length];
            // add first two indices and set initial length of strip
            strip_indices[0] = oldest;
            strip_indices[1] = middle;
            ushort current_strip_length = 2;

            //ushort oldest = Faces[start_triangle_index].Vertices[0];
            //ushort middle = Faces[start_triangle_index].Vertices[1];
            ushort newest = default(ushort);

            bool processing = true;
            while (processing)
            {
                // exit if this strip is as long as we want
                if (current_strip_length + 1 == max_strip_length) processing = false;
                // find the third vertex of the triangle, given two known
                newest = triangle_buffer[face_index].OppositeVertex(oldest, middle);
                // add this index
                strip_indices[current_strip_length++] = newest;
                // mark this face as 'processed' => true
                processed_faces[face_index] = true;
                // get edge defined by the two vertices;
                byte current_edge = triangle_buffer[face_index].FindEdge(middle, newest);
                ushort link = triangle_buffer[face_index].AdjacentTriangles[current_edge];
                if (link == 0xFFFF)
                {
                    processing = false;
                    var curface = triangle_buffer[face_index];
                    if (curface.AdjacentTriangles[0] == 0xFFFF &&
                        curface.AdjacentTriangles[1] == 0xFFFF &&
                        curface.AdjacentTriangles[2] == 0xFFFF)
                    {
                        int i = 0;
                    }
                }
                else
                {
                    face_index = link;
                    if (processed_faces[face_index])
                        processing = false;
                }
                oldest = middle;
                middle = newest;
            }
            strip_length = current_strip_length;
            return strip_indices;
        }

        public TriangleStrip[] GenerateStripArray(int seedface, int max_strip_length = 2048)
        {
            triangle_flags = new bool[triangle_buffer.Length];
            int unprocessed_faces = triangle_buffer.Length;
            List<TriangleStrip> strips = new List<TriangleStrip>();
            while (triangle_flags.Where(x => x == false).Count() > 0)
            {
                ushort strip_length;
                var strip = FindBestStrip(seedface, ref triangle_flags, out strip_length);
                var return_strip = new TriangleStrip() { indices = new ushort[strip_length] };
                Array.Copy(strip, return_strip.indices, strip_length);
                strips.Add(return_strip);
                int seed = 0;
                for (seed = 0; seed < triangle_flags.Length; ++seed)
                {
                    if (!triangle_flags[seed])
                    { seedface = seed; break; }
                }
            }
            return strips.ToArray();
        }
    }
}
