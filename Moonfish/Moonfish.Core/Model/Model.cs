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

namespace Moonfish.Core.Model
{
    public class Mesh
    {
        ShaderGroup[] ShaderGroups;
        public ushort[] Indices;
        public DefaultVertex[] Vertices;

        public bool Load(byte[] raw_data, Resource[] raw_resources, CompressionRanges compression_ranges)
        {
            const int first_address = 4 + 116;
                        int coord_size = 0;
                        int texcoord_size = 0;
                        int vector_size = 0;
            int stream_length = BitConverter.ToInt32(raw_data,4);
            MemoryStream stream = new MemoryStream(raw_data, first_address, stream_length, false);
            BinaryReader binary_reader = new BinaryReader(stream);
            foreach (var resource in raw_resources)
            {
                if(resource.first_ != 0) continue;//skip the vertex resources
                //get the header_value (which is  count  of blocks for this resource)...
                int count = BitConverter.ToInt32(raw_data, 8 + resource.header_address);
                // move stream to start of resource data
                binary_reader.BaseStream.Position = resource.resource_offset;
                switch (resource.header_address)
                {
                    #region Shader Groups
                    // case: Shader Groups
                    case 0:
                        // initialize the shader_groups array;
                        ShaderGroups = new ShaderGroup[count];
                        // read each block
                        for (int i = 0; i < count; i++)
                        {
                            ShaderGroups[i] = new ShaderGroup() { data = binary_reader.ReadBytes(resource.data_size__or__first_index) };
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
                        switch (resource.first_)
                        {
                            case 0:
                                for (int i = 0; i < count; i++)
                                {
                                    byte[] buffer = binary_reader.ReadBytes(resource.data_size__or__first_index);
                                    if(i ==0) coord_size = buffer[1];
                                    else if(i==1) texcoord_size  = buffer[1];
                                    else if (i == 2) vector_size = buffer[1];
                                    else throw new Exception("D:");
                                }
                                break;
                        }
                        break;
                    #endregion
                }
            }
            var vertex_resources = raw_resources.Where(x => x.first_ == 2).ToArray();
            binary_reader.BaseStream.Position = vertex_resources[0].resource_offset;
            byte[] coord_raw = binary_reader.ReadBytes(vertex_resources[0].resource_length);
            binary_reader.BaseStream.Position = vertex_resources[1].resource_offset;
            byte[] texcoord_raw = binary_reader.ReadBytes(vertex_resources[1].resource_length);
            binary_reader.BaseStream.Position = vertex_resources[2].resource_offset;
            byte[] vector_raw = binary_reader.ReadBytes(vertex_resources[2].resource_length);
            Vertices = ExtractVertices(compression_ranges, coord_raw, coord_size, texcoord_raw, texcoord_size, vector_raw, vector_size);
            return true;
        }
        public bool ExportAsWavefront(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                writer.WriteLine("# moonfish 2013 : Wavefront OBJ");
                foreach (var vertex in Vertices)
                {
                    writer.WriteLine("v {0} {1} {2}", vertex.Position.X, 
                        vertex.Position.Y, vertex.Position.Z);
                } 
                foreach (var vertex in Vertices)
                {
                    writer.WriteLine("vt {0} {1}", vertex.TextureCoordinates.X.ToString("#0.00000"),
                        vertex.TextureCoordinates.Y.ToString("#0.00000"));
                } 
                foreach (var vertex in Vertices)
                {
                    writer.WriteLine("vn {0} {1} {2}", vertex.Normal.X.ToString("#0.00000"),
                        vertex.Normal.Y.ToString("#0.00000"), vertex.Normal.Z.ToString("#0.00000"));
                }
                bool winding_flag = true;
                for (var i = 0; i < Indices.Length - 2; ++i)
                {
                    if (Indices[i] == Indices[i + 1] || Indices[i] == Indices[i + 2] || Indices[i + 1] == Indices[i + 2])
                    {
                        winding_flag = !winding_flag;
                        continue;
                    }
                    if (winding_flag)
                    {//0,1,2
                        writer.WriteLine("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}", Indices[i] + 1, Indices[i + 1] + 1, Indices[i + 2] + 1);
                    }
                    else
                    {//0,2,1
                        writer.WriteLine("f {0} {1} {2}", Indices[i] + 1, Indices[i + 2] + 1, Indices[i + 1] + 1);
                    }
                    winding_flag = !winding_flag;
                }
            }
            return true;
        }
        public void Serialize()
        {
            // 1.create halo2 formatted resource blocks
            MemoryStream buffer = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(buffer);
            writer.WriteFourCC("blkh");
            writer.Write(new byte[4]);              // int: resource size  (reserve)
            writer.Write(new byte[116]);            // header (reserve)
            writer.WriteFourCC("rsrc");             // shader_group resource begin
            writer.Write(new byte[72]);             // shader_group data (reserve)
            writer.WriteFourCC("rsrc");             // indices resource begin
            foreach (ushort index in this.Indices)  // write each index ushort
                writer.Write(index);                // pad to word boundary
            writer.WritePadding(4);
            writer.WriteFourCC("rsrc");             // vertex_data_header?
            writer.Write(new byte[32]);             // vertex resource
            writer.Write(new byte[32]);             // texcoord resource
            writer.Write(new byte[32]);             // vectors resource (normal, tangent, binormal)
            writer.WriteFourCC("rsrc");
            {
                Range x = new Range();
                Range y = new Range();
                Range z = new Range();
                foreach (var vertex in this.Vertices)
                {
                    x = Range.Include(x, vertex.Position.X);
                    y = Range.Include(y, vertex.Position.Y);
                    z = Range.Include(z, vertex.Position.Z);
                }
                foreach (var vertex in this.Vertices)
                {
                    writer.Write(Deflate(x, vertex.Position.X));
                    writer.Write(Deflate(y, vertex.Position.Y));
                    writer.Write(Deflate(z, vertex.Position.Z));
                }
                writer.WritePadding(4);
            }
            writer.WriteFourCC("rsrc");
            {
                Range u = new Range();
                Range v = new Range();
                foreach (var vertex in this.Vertices)
                {
                    u = Range.Include(u, vertex.TextureCoordinates.X);
                    v = Range.Include(v, vertex.TextureCoordinates.Y);
                }
                foreach (var vertex in this.Vertices)
                {
                    writer.Write(Deflate(u, vertex.TextureCoordinates.X));
                    writer.Write(Deflate(v, vertex.TextureCoordinates.Y));  // flip this still?
                }              
            }
            writer.WriteFourCC("rsrc");
            foreach (var vertex in this.Vertices)
            {
                writer.Write((uint)vertex.Normal);
                writer.Write(0);
                writer.Write(0);
            }
            writer.WriteFourCC("rsrc");
            writer.Write(0);                // default bone-map (no bones)
            writer.WriteFourCC("blkf");

            // debug dump
            #if DEBUG
            using (var file = File.OpenWrite(@"D:\halo_2\model_raw.bin"))
            {
                file.Write(buffer.ToArray(), 0, (int)buffer.Length);
            }
            #endif
            // end debug dump

            // 2. create a sections meta file for this, a bounding box, heck a whole mesh, why not.
        }
        public bool ImportFromWavefront(string filename)
        {
            Log.Info(string.Format("Loading file {0} into memory buffer", filename));
            byte[] buffer = null;
            using (var file = File.OpenRead(filename))
            {
                buffer = new byte[file.Length];
                file.Read(buffer, 0, buffer.Length);
            } if (buffer == null)
            {
                Log.Error("Failed to create memory buffer");
                return false;
            }
            MemoryStream stream = new MemoryStream(buffer);
            StreamReader reader = new StreamReader(stream);

            List<Moonfish.Core.Model.WavefrontObject.Object> objects = new List<Moonfish.Core.Model.WavefrontObject.Object>();
            List<Moonfish.Core.Model.WavefrontObject.Face> faces = new List<Moonfish.Core.Model.WavefrontObject.Face>();
            
            List<ushort> facestream = new List<ushort>();

            List<Vector3> vertex_coords = new List<Vector3>();
            List<Vector2> texture_coords = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            bool default_object = true;
            int current_object_index = 0;

            objects.Add(new Moonfish.Core.Model.WavefrontObject.Object());

            Log.Info("Begin parsing Wavefront Object data from buffer");
            while(!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.StartsWith("# "))
                {
                    Log.Info(line);
                    continue;
                }
                if (line.StartsWith("o "))
                {
                    // if this is the first object token, use the default object
                    if (default_object) default_object = false;
                    else
                    {
                        Log.Warn(@"Support for multiple wavefront objects per mesh not implemented.
                                   Meshes can only accept a single wavefront object. Continuing, but only using first object!");
                        
                        throw new Exception("jk :D");
                    }
                }
                else if (line.StartsWith("v "))
                {
                    string[] items = line.Split(' ');
                    vertex_coords.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                }
                else if (line.StartsWith("vt "))
                {
                    string[] items = line.Split(' ');
                    texture_coords.Add(new Vector2(float.Parse(items[1]), float.Parse(items[2])));
                }
                else if (line.StartsWith("vn "))
                {
                    string[] items = line.Split(' ');
                    normals.Add(new Vector3(float.Parse(items[1]), float.Parse(items[2]), float.Parse(items[3])));
                }
                else if (line.StartsWith("f "))
                {
                    Moonfish.Core.Model.WavefrontObject.Face face;
                    if (!Moonfish.Core.Model.WavefrontObject.Face.TryParse(line, out face))
                    {
                        Log.Error(string.Format("Error parsing line: {0}", line));
                        return false;
                    }
                    else faces.Add(face);
                }
            }
            Log.Info("Success! Finished parsing Wavefront Object data from buffer");
            //TODO: generate tri-strips
            //generate sahder-group structs
            //compress vertex data
            //create bone map with default bone
            this.Vertices = new DefaultVertex[vertex_coords.Count];
            for (int i = 0; i < vertex_coords.Count; ++i)
            {
                this.Vertices[i] = new DefaultVertex() { Position = vertex_coords[i] };
            }
            ushort[] vertex_indices = new ushort[faces.Count * 3];
            for (int i = 0; i < faces.Count; i++)
            {
                vertex_indices[i * 3 + 0] = (ushort)(faces[i].vertex_indices[0] - 1);
                vertex_indices[i * 3 + 1] = (ushort)(faces[i].vertex_indices[1] - 1);
                vertex_indices[i * 3 + 2] = (ushort)(faces[i].vertex_indices[2] - 1);
            }
            Adjacencies stripper = new Adjacencies(vertex_indices);
            Random r = new Random(DateTime.Now.Millisecond);
            QuickModelView quick_view = new QuickModelView(this, stripper);
            quick_view.Run();
            this.Indices = quick_view.GetStrip();
            return true;
        }

        private DefaultVertex[] ExtractVertices(CompressionRanges compression_ranges, byte[] coord_raw, int coord_size, byte[] texcoord_raw, int texcoord_size, byte[] vector_raw, int vector_size)
        {
            int vertex_count = coord_raw.Length / coord_size;
            DefaultVertex[] vertices = new DefaultVertex[vertex_count];
            for (int i = 0; i < vertex_count; ++i)
            {
                Vector3 position = new Vector3(BitConverter.ToInt16(coord_raw, i * coord_size), BitConverter.ToInt16(coord_raw, (i * coord_size) + 2), BitConverter.ToInt16(coord_raw, (i * coord_size) + 4));
                Range int16_range = new Range(short.MinValue, short.MaxValue);
                position.X = Inflate(position.X, compression_ranges.x);
                position.Y = Inflate(position.Y,  compression_ranges.y);
                position.Z = Inflate(position.Z,  compression_ranges.z);
                Vector2 texcoord = new Vector2(BitConverter.ToInt16(coord_raw, i * coord_size), BitConverter.ToInt16(coord_raw, i + 1 * coord_size));
                texcoord.X = Inflate(position.X,  compression_ranges.u1);
                texcoord.Y = Inflate(position.Y,  compression_ranges.v1);
                vertices[i] = new DefaultVertex() { Position = position, TextureCoordinates = texcoord, Normal = new Vector3t(BitConverter.ToUInt32(vector_raw, i * vector_size)) };
            }
            return vertices;
        }

        /// <summary>
        /// Takes a value and converts it into a range from 0.0 to 1.0 stored as a ushort
        /// </summary>
        /// <param name="input_range"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        ushort Deflate(Range input_range, float value)
        {
            const float max_ushort = ushort.MaxValue;
            if (value > input_range.max) value = input_range.max;
            if (value < input_range.min) value = input_range.min;
            var ratio = value / input_range.max;
            return (ushort)(max_ushort * ratio);
        }
        float Inflate(float value_in_range,  Range range)
        {
            const float Max = 1.0f / ushort.MaxValue;
            const float Half = short.MaxValue;
            return (((value_in_range + Half) * Max) * (range.max - range.min)) + range.min;
        }

        public struct DefaultVertex
        {
            public Vector3 Position;
            public Vector2 TextureCoordinates;
            public Vector3t Normal;

            public override string ToString()
            {
                return string.Format("{0}, {1}, {2}", Position, TextureCoordinates, Normal);
            }
        }
        struct ShaderGroup
        {
            public byte[] data;
        }
        public struct Resource
        {
            public byte first_;
            public byte second_;
            public short header_address;
            public short header_address_again;
            public short data_size__or__first_index;
            public int resource_length;
            public int resource_offset;

            public Resource(byte[] buffer)
            {
                first_ = buffer[0];
                second_ = buffer[1];
                header_address = BitConverter.ToInt16(buffer, 2);
                header_address_again = BitConverter.ToInt16(buffer, 4);
                data_size__or__first_index = BitConverter.ToInt16(buffer, 6);
                resource_length = BitConverter.ToInt32(buffer, 8);
                resource_offset = BitConverter.ToInt32(buffer, 12);
            }
        }

        public void Show()
        {
            QuickModelView render_window = new QuickModelView(this);
            render_window.Run();
        }
    }
}
