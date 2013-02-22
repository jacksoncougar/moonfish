using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Moonfish.Core.Raw;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using StarterKit;
using System.Threading;
using Moonfish.Core.Model.Adjacency;
using Moonfish.Core.Model.Wavefront;
using System.Runtime.InteropServices;
using Moonfish.Core.Definitions;
using System.Xml;

namespace Moonfish.Core.Model
{
    /// <summary>
    /// Contains data required to render a 3d model
    /// </summary>
    public class Mesh
    {
        public string Name = "default";
        public BeginMode PrimitiveType;

        public ushort[] Indices;

        public Vector3[] VertexCoordinates;
        public Vector2[] TextureCoordinates;
        public Vector3[] VertexNormals;

        public Vector3[] VertexTangents;
        public Vector3[] VertexBitangents;

        public VertexWeight[] VertexWeights;
        public byte[] Nodes;
        public MeshPrimitive[] Primitives;

        /// <summary>
        /// Generating from a list this way is not grande.
        /// </summary>
        /// <returns></returns>
        protected bool GenerateNormals()
        {
            if (Indices == null || VertexCoordinates == null) return false;

            VertexNormals = new Vector3[VertexCoordinates.Length];

            int ref0, ref1, ref2;
            bool winding = false;
            for (int i = 0; i < Indices.Length - 2; i++)
            {
                if (winding)
                {
                    ref0 = Indices[i + 0];
                    ref1 = Indices[i + 1];
                    ref2 = Indices[i + 2];
                }
                else
                {
                    ref0 = Indices[i + 0];
                    ref2 = Indices[i + 1];
                    ref1 = Indices[i + 2];
                }
                winding = !winding;
                if (ref0 == ref1 || ref1 == ref2 || ref0 == ref2) continue;
                Vector3 vec1 = VertexCoordinates[ref2] - VertexCoordinates[ref0];
                Vector3 vec2 = VertexCoordinates[ref1] - VertexCoordinates[ref0];
                Vector3 normal = Vector3.Cross(vec1, vec2);
                VertexNormals[ref0] += normal;
                VertexNormals[ref1] += normal;
                VertexNormals[ref2] += normal;
            }
            for (int i = 0; i < VertexNormals.Length; i++)
            {
                VertexNormals[i].Normalize();
            }
            return true;
        }
        protected bool GenerateTexCoords()
        {
            if (Indices == null || VertexCoordinates == null) return false;
            TextureCoordinates = new Vector2[VertexCoordinates.Length];
            int ref0, ref1, ref2;
            for (int i = 0; i < Indices.Length - 2; ++i)
            {
                ref0 = Indices[i + 0];
                ref1 = Indices[i + 1];
                ref2 = Indices[i + 2];
                if (ref0 == ref1 || ref1 == ref2 || ref0 == ref2) continue;
                if (TextureCoordinates[ref0] == Vector2.Zero)
                    TextureCoordinates[ref0] = VertexCoordinates[ref0].Xy;
                if (TextureCoordinates[ref1] == Vector2.Zero)
                    TextureCoordinates[ref1] = VertexCoordinates[ref1].Xy;
                if (TextureCoordinates[ref2] == Vector2.Zero)
                    TextureCoordinates[ref2] = VertexCoordinates[ref2].Xy;
            }
            for (int i = 0; i < TextureCoordinates.Length; i++)
            {
                TextureCoordinates[i].Normalize();
            }
            return true;
        }
        protected bool GenerateTangentSpaceVectors()
        {
            if (this.VertexCoordinates == null || this.Indices == null) return false;
            Vector3[] tangents = new Vector3[this.VertexCoordinates.Length * 2];
            int bitan = this.VertexCoordinates.Length;
            for (int i = 0; i < this.Indices.Length - 2; i++)
            {
                if (IsDegenerate(this.Indices[i], this.Indices[i + 1], this.Indices[i + 2]))
                {
                    //do loop code
                    continue;
                }
                ushort i1 = this.Indices[i + 0];
                ushort i2 = this.Indices[i + 1];
                ushort i3 = this.Indices[i + 2];
                Vector3 v1 = this.VertexCoordinates[i1];
                Vector3 v2 = this.VertexCoordinates[i2];
                Vector3 v3 = this.VertexCoordinates[i3];
                Vector2 t1 = this.TextureCoordinates[i1];
                Vector2 t2 = this.TextureCoordinates[i2];
                Vector2 t3 = this.TextureCoordinates[i3];

                float x1 = v2.X - v1.X;
                float x2 = v3.X - v1.X;
                float y1 = v2.Y - v1.Y;
                float y2 = v3.Y - v1.Y;
                float z1 = v2.Z - v1.Z;
                float z2 = v3.Z - v1.Z;

                float tu1 = t2.X - t1.X;
                float tu2 = t3.X - t1.X;
                float tv1 = t2.Y - t1.Y;
                float tv2 = t3.Y - t1.Y;

                float r = 1.0f / (tu1 * tv2 - tu2 * tv1);
                Vector3 sdir = new Vector3(
                    (tv2 * x1 - tv1 * x2) * r,
                    (tv2 * y1 - tv1 * y2) * r,
                    (tv2 * z1 - tv1 * z2) * r
                    );
                Vector3 tdir = new Vector3(
                    (tu1 * x2 - tu2 * x1) * r,
                    (tu1 * y2 - tu2 * y1) * r,
                    (tu1 * z2 - tu2 * z1) * r
                    );
                tangents[i1] += sdir;
                tangents[i2] += sdir;
                tangents[i3] += sdir;
                tangents[bitan + i1] += tdir;
                tangents[bitan + i2] += tdir;
                tangents[bitan + i3] += tdir;
            }
            for (int i = 0; i < this.VertexCoordinates.Length; i++)
            {
                Vector3 t = tangents[i];
                Vector3 n = this.VertexNormals[i];
                this.VertexTangents[i] = (t - n * Vector3.Dot(n, t));
                this.VertexTangents[i].Normalize();
                bool lefthanded = Vector3.Dot(Vector3.Cross(n, t), tangents[bitan + i]) < 0.0F ? true : false;
                this.VertexBitangents[i] = Vector3.Cross(n, this.VertexTangents[i]);
                if (lefthanded) this.VertexBitangents[i] *= -1;
            }
            return true;
        }
        
        internal ushort GetTriangleCount()
        {
            ushort triangle_count = 0;
            for (ushort i = 0; i < this.Indices.Length - 2; i++)
            {
                if (IsDegenerate(this.Indices[i], this.Indices[i + 1], this.Indices[i + 2])) continue;
                else ++triangle_count;
            }
            return triangle_count;
        }

        public bool Load(binary_seperation_plane_structure.water water)
        {
            PrimitiveType = BeginMode.TriangleStrip;
            return Load(water.Raw.ToArray(), water.Resources.Select(
                x => x.GetDefinition<DResource>()), new DCompressionRanges(), water.Raw.HeaderSize);
        }
        public bool Load(binary_seperation_plane_structure.detail_object detail_object)
        {
            PrimitiveType = BeginMode.Triangles;
            return Load(detail_object.Raw.ToArray(), detail_object.Resources.Select(
                x => x.GetDefinition<DResource>()), new DCompressionRanges(), detail_object.Raw.HeaderSize);
        }

        /// <summary>
        /// Deserializes a Halo 2 formatted raw-resource block and initializes the Mesh object from it
        /// </summary>
        /// <param name="raw_data"></param>
        /// <param name="raw_resources"></param>
        /// <param name="compression_ranges"></param>
        /// <returns></returns>
        protected bool Load(byte[] raw_data, IEnumerable<DResource> raw_resources, DCompressionRanges compression_ranges, int header_size)
        {
            int[] vertex_resource_sizes = new int[0];
            VertexResource[] vertex_resource_types = new VertexResource[0];

            int stream_length = BitConverter.ToInt32(raw_data, 4);
            int first_address = 4 + 4 + header_size;                                                /* 0:header_four_cc : "blkh"
                                                                                                     * 4:total_resource_data_length
                                                                                                     * 8:header_fields */

            MemoryStream resource_stream = new MemoryStream(raw_data, first_address, stream_length, false);  /* Create a stream containing the resource_data 
                                                                                                     * from the raw_data byte array
                                                                                                     * */
            BinaryReader binary_reader = new BinaryReader(resource_stream);

            /*  Intent: switch each raw resource and load the mesh-related data from the resource_stream
             * */
            foreach (var resource in raw_resources)
            {
                if (resource.first_ != 0) continue;                                         // skip the vertex resources and other wierd resources

                int count = BitConverter.ToInt32(raw_data, 8 + resource.header_address);    // get the header_value (which is 'count' of blocks for this resource)...
                resource_stream.Position = resource.resource_offset;                        // move stream to offset of resource data

                switch (resource.header_address)
                {
                    #region Shader Groups
                    // case: Shader Groups
                    case 0:
                        // initialize the shader_groups array;
                        Primitives = new MeshPrimitive[count];
                        // read each block
                        for (int i = 0; i < count; i++)
                        {
                            Primitives[i] = binary_reader.ReadDefinition<MeshPrimitive>();
                        }
                        break;
                    #endregion
                    #region Indices
                    case 32:
                        Indices = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            if (resource.data_size__or__first_index != sizeof(short)) throw new Exception(":D");
                            Indices[i] = binary_reader.ReadUInt16();
                        }
                        break;
                    #endregion
                    #region Vertex Resources pass-1
                    case 56:
                        /* Process the first resource at offset 56 which should be the resource describing all 
                         * other vertex-data resources after it. Load from this resource the size and type enum 
                         * of the vertex_data resources.
                         * */
                        switch (resource.first_)
                        {
                            case 0:
                                vertex_resource_sizes = new int[count];
                                vertex_resource_types = new VertexResource[count];
                                for (int i = 0; i < count; i++)
                                {
                                    byte[] buffer = binary_reader.ReadBytes(resource.data_size__or__first_index);
                                    vertex_resource_types[i] = (VertexResource)buffer[0];
                                    vertex_resource_sizes[i] = buffer[1];
                                }
                                break;
                        }
                        break;
                    #endregion
                    case 100:
                        Nodes = binary_reader.ReadBytes(count);
                        break;
                }
            }
            var vertex_resources = raw_resources.Where(x => x.first_ == 2).ToArray();

            /* Intent: process vertex resources by type and load additional bone information if present
             * */
            int vertex_count = vertex_resources[0].resource_length / vertex_resource_sizes[0];

            #region Vertex Data Field Initialization
            /* Intent: intialize all fields to empty arrays to prevent the unintialized value compiler error,
             * switch through all the vertex_resources we are going to process and create an array to hold all 
             * the members we will be reading. */
            this.VertexCoordinates = new Vector3[0];
            this.TextureCoordinates = new Vector2[0];
            this.VertexNormals = new Vector3[0];
            this.VertexTangents = new Vector3[0];
            this.VertexBitangents = new Vector3[0];
            this.VertexWeights = new VertexWeight[0];

            foreach (var vertex_resource in vertex_resources)                   /* Switch through all the loaded 
                                                                                 * vertex_resources */
            {
                switch (vertex_resource_types[vertex_resource.data_size__or__first_index])
                {
                    case VertexResource.coordinate_with_skinned_node:
                    case VertexResource.coordinate_with_rigid_node:             /* if the resource contains skeleton nodes:
                                                                                 * initialize the VertexWeights array*/
                        this.VertexWeights = new VertexWeight[vertex_count];
                        goto case VertexResource.coordinate_compressed;         /*  also load a vertex coordinate array */

                    case VertexResource.coordinate_float:
                    case VertexResource.coordinate_compressed:                  /* if the resource contains vertex coordinates: 
                                                                                 * initialize the VertexCoordinates array */
                        this.VertexCoordinates = new Vector3[vertex_count];
                        break;

                    case VertexResource.texture_coordinate_compressed:
                    case VertexResource.texture_coordinate_float:                /* if the resource contains texture coordinates: 
                                                                                  * initialize the TextureCoordinates array */
                        this.TextureCoordinates = new Vector2[vertex_count];
                        break;
                    case VertexResource.tangent_space_unit_vectors_compressed:   /* if the resource contains tangent-space data: 
                                                                                  * initialize the TBN vector arrays */
                        this.VertexNormals = new Vector3[vertex_count];
                        this.VertexTangents = new Vector3[vertex_count];
                        this.VertexBitangents = new Vector3[vertex_count];
                        break;
                }
            }
            #endregion

            #region Vertex Data Loading
            foreach (var vertex_resource in vertex_resources)
            {
                binary_reader.BaseStream.Position = vertex_resource.resource_offset;
                var buffer = binary_reader.ReadBytes(vertex_resource.resource_length);
                var stride = vertex_resource_sizes[vertex_resource.data_size__or__first_index];
                for (int i = 0; i < vertex_count; ++i)
                {
                    switch (vertex_resource_types[vertex_resource.data_size__or__first_index])
                    {
                        case VertexResource.coordinate_float:
                            this.VertexCoordinates[i] = new Vector3(
                                BitConverter.ToSingle(buffer, i * stride),
                                BitConverter.ToSingle(buffer, (i * stride) + 4),
                                BitConverter.ToSingle(buffer, (i * stride) + 8));
                            break;
                        case VertexResource.coordinate_compressed:
                            this.VertexCoordinates[i] = new Vector3(
                                BitConverter.ToInt16(buffer, i * stride),
                                BitConverter.ToInt16(buffer, (i * stride) + 2),
                                BitConverter.ToInt16(buffer, (i * stride) + 4));
                            this.VertexCoordinates[i].X = Inflate(this.VertexCoordinates[i].X, compression_ranges.X);
                            this.VertexCoordinates[i].Y = Inflate(this.VertexCoordinates[i].Y, compression_ranges.Y);
                            this.VertexCoordinates[i].Z = Inflate(this.VertexCoordinates[i].Z, compression_ranges.Z);
                            break;
                        case VertexResource.coordinate_with_rigid_node:
                            VertexWeights[i] = new VertexWeight(buffer[(i * stride) + 6]);
                            goto case VertexResource.coordinate_compressed;
                        case VertexResource.coordinate_with_skinned_node:
                            var bone0 = buffer[(i * stride) + 6];
                            var bone1 = buffer[(i * stride) + 7];
                            var weight0 = (float)buffer[(i * stride) + 9] / (float)byte.MaxValue;
                            var weight1 = (float)buffer[(i * stride) + 10] / (float)byte.MaxValue;
                            VertexWeights[i] = new VertexWeight()
                            {
                                Bone0 = bone0,
                                Bone1 = bone1,
                                Bone0_weight = weight0,
                                Bone1_weight = weight1
                            };
                            goto case VertexResource.coordinate_compressed;
                        case VertexResource.texture_coordinate_float:
                            this.TextureCoordinates[i] = new Vector2(
                            BitConverter.ToSingle(buffer, i * stride),
                            BitConverter.ToSingle(buffer, (i * stride) + 4));
                            break;
                        case VertexResource.texture_coordinate_compressed:
                            this.TextureCoordinates[i] = new Vector2(
                            BitConverter.ToInt16(buffer, i * stride),
                            BitConverter.ToInt16(buffer, (i * stride) + 2));
                            this.TextureCoordinates[i].X = Inflate(this.TextureCoordinates[i].X, compression_ranges.U);
                            this.TextureCoordinates[i].Y = Inflate(this.TextureCoordinates[i].Y, compression_ranges.V);
                            break;
                        case VertexResource.tangent_space_unit_vectors_compressed:
                            this.VertexNormals[i] = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, i * stride));
                            this.VertexTangents[i] = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, (i * stride) + 4));
                            this.VertexBitangents[i] = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, i * (stride) + 8));
                            break;
                    }
                }
            }
            #endregion

            return true;
        }

        internal Triangle[] GenerateTriangleFromStrip()
        {
            List<Triangle> triangles = new List<Triangle>();
            bool winding = false;
            for (int i = 0; i < this.Indices.Length - 2; i++)
            {
                winding = !winding;
                if (IsDegenerate(this.Indices[i + 0], this.Indices[i + 1], this.Indices[i + 2])) continue;
                triangles.Add(new Triangle()
                {
                    Vertex1 = this.Indices[i + 0],
                    Vertex2 = winding ? this.Indices[i + 1] : this.Indices[i + 2],
                    Vertex3 = winding ? this.Indices[i + 2] : this.Indices[i + 1],
                });
            }
            return triangles.ToArray();
        }

        /// <summary>
        /// Simple index comparison to check for degenerate triangles in a TriangleStrip
        /// </summary>
        /// <param name="ref0"></param>
        /// <param name="ref1"></param>
        /// <param name="ref2"></param>
        /// <returns>retuns false on the condition that any two indices are the same value</returns>
        protected static bool IsDegenerate(ushort ref0, ushort ref1, ushort ref2)
        {
            return (ref0 == ref1 || ref0 == ref2 || ref1 == ref2);
        }

        /// <summary>
        /// Takes a float-value and converts it into a range then stores the range ratio in a short
        /// </summary>
        /// <param name="input_range"></param>
        /// <param name="value"></param>
        /// <returns>returns the ratio of 'value' in 'input_range' represented as a short<returns>
        protected static short Deflate(Range input_range, float value)
        {
            value = Range.Truncate(input_range, value);                             // Ensure input value is within the expected range
            var delta = (value - input_range.min);                                  // This projects value into the range with 0 as the lowest value in the range
            var range_span = input_range.max - input_range.min;                     // The total width of values the range covers
            var ratio = delta / range_span;                                         // The location in the range which this value resides at
            var short_value = (short)(ratio * ushort.MaxValue - short.MaxValue);    /* ratio { 0.0 -> 1.0 } * 65535 - 32767
                                                                                     * the subtration at the end is to maintain a signed value */
#if DEBUG
            var compression_delta = Inflate(short_value, input_range) - value;      // Crappy unit testing
            if (compression_delta > 0.001) throw new Exception();
#endif
            return short_value;
        }
        protected static float Inflate(float value_in_range, Range range)
        {
            const float ushort_max_inverse = 1.0f / ushort.MaxValue;
            return (((value_in_range + short.MaxValue) * ushort_max_inverse) * (range.max - range.min)) + range.min;
        }

        /// <summary>
        /// Has data which describes the start of the primitive in the Indices array, 
        /// the length of that primitive's indices, and the material group of that primitive
        /// </summary>
        public class MeshPrimitive : IDefinition
        {
            short unknown0 = 2;                                     //flags or enum?
            short unknown1 = 3;                                     //flags or enum?
            public ushort shader_index;
            public ushort strip_start;
            public ushort strip_length;
            int unknown2;                                           //byte[4] 00s? not an int32.
            short s_unknown3 = 1;
            uint unknown4;                                          //byte[4] 00s?
            uint unknown5;                                          //byte[4] 00s?
            uint unknown6;                                          //byte[4] 00s?
            uint unknown7;                                          //byte[4] 00s?
            Quaternion unknown8 = new Quaternion(1, 0, 0, 1);       //16 bytes
            BoundingBox local_bounds;                               //24 bytes

            public MeshPrimitive(ushort start_offset, ushort length)
            {
                // TODO: Complete member initialization
                strip_start = start_offset;
                strip_length = length;
            }

            public MeshPrimitive() { }

            byte[] IDefinition.ToArray()
            {
                MemoryStream buffer = new MemoryStream(72);
                BinaryWriter bin = new BinaryWriter(buffer);
                bin.Write(unknown0);
                bin.Write(unknown1);
                bin.Write(shader_index);
                bin.Write(strip_start);
                bin.Write(strip_length);
                bin.Write(unknown2);
                bin.Write(s_unknown3);
                bin.Write(unknown4);
                bin.Write(unknown5);
                bin.Write(unknown6);
                bin.Write(unknown7);
                bin.Write(unknown8.X);
                bin.Write(unknown8.Y);
                bin.Write(unknown8.Z);
                bin.Write(unknown8.W);
                bin.Write(local_bounds.x.min);
                bin.Write(local_bounds.x.max);
                bin.Write(local_bounds.y.min);
                bin.Write(local_bounds.y.max);
                bin.Write(local_bounds.z.min);
                bin.Write(local_bounds.z.max);
                return buffer.ToArray();
            }

            void IDefinition.FromArray(byte[] buffer)
            {
                BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
                unknown0 = bin.ReadInt16();
                unknown1 = bin.ReadInt16();
                shader_index = bin.ReadUInt16();
                strip_start = bin.ReadUInt16();
                strip_length = bin.ReadUInt16();
                unknown2 = bin.ReadInt32();
                s_unknown3 = bin.ReadInt16();
                unknown4 = bin.ReadUInt32();
                unknown5 = bin.ReadUInt32();
                unknown6 = bin.ReadUInt32();
                unknown7 = bin.ReadUInt32();
                unknown8 = bin.ReadQuaternion();
                local_bounds = bin.ReadDefinition<BoundingBox>();
            }

            int IDefinition.Size
            {
                get { return 72; }
            }
        }

        public struct BoundingBox : IDefinition
        {
            public Range x;
            public Range y;
            public Range z;

            byte[] IDefinition.ToArray()
            {
                MemoryStream buffer = new MemoryStream(32);
                BinaryWriter bin = new BinaryWriter(buffer);
                bin.Write(x);
                bin.Write(y);
                bin.Write(z);
                return buffer.ToArray();
            }

            void IDefinition.FromArray(byte[] buffer)
            {
                BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
                x = bin.ReadRange();
                y = bin.ReadRange();
                z = bin.ReadRange();
            }

            int IDefinition.Size
            {
                get { return 24; }
            }
        }

        enum VertexResource : byte
        {
            none = 0x00,
            coordinate_float = 0x01,
            coordinate_compressed = 0x02,
            coordinate_with_rigid_node = 0x04,
            coordinate_with_skinned_node = 0x08,

            texture_coordinate_float = 0x18,
            texture_coordinate_compressed = 0x19,

            tangent_space_unit_vectors_compressed = 0x1B,

            lightmap_uv_coordinate_one = 0x1F,
            lightmap_uv_coordinate_two = 0x30,
        }
    }
}
