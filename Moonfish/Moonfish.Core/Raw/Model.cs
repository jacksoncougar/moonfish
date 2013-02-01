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
        public short[] Indices;
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
                        Indices = new short[count];
                        for (int i = 0; i < count; i++)
                        {
                            if (resource.data_size__or__first_index != sizeof(short)) throw new Exception(":D");
                            Indices[i] = binary_reader.ReadInt16();
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
        public bool ImportFromWavefront(string filename)
        {
            byte[] buffer = null;
            using (var file = File.OpenRead(filename))
            {
                buffer = new byte[file.Length];
                file.Read(buffer, 0, buffer.Length);
            } if (buffer == null) return false;
            MemoryStream stream = new MemoryStream(buffer);
            StreamReader reader = new StreamReader(stream);
            List<WavefrontObject> objects = new List<WavefrontObject>();
            List<WavefrontFace> faces = new List<WavefrontFace>();
            List<ushort> facestream = new List<ushort>();
            List<WavefrontFaceIndex> face_indices = new List<WavefrontFaceIndex>();
            List<Vector3> vertex_coords = new List<Vector3>();
            List<Vector2> texture_coords = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            bool default_object = true;
            int current_object_index = 0;
            objects.Add(new WavefrontObject());
            while(!reader.EndOfStream)
            {
                string line = reader.ReadLine().Trim();
                if (line.StartsWith("# ")) continue;
                if (line.StartsWith("o "))
                {
                    // if this is the first object token, use the default object
                    if (default_object) default_object = false;
                    else
                    {
                        Log.Warn(@"Support for multiple wavefront objects per mesh not implemented.
                                   Meshes can only accept a single wavefront object. Continuing, but only using first object!");
                        objects.Add(new WavefrontObject() { faces_start_index = face_indices.Count });
                        var copy = objects[current_object_index];
                        copy.faces_count = face_indices.Count - objects[current_object_index].faces_start_index;
                        objects[current_object_index] = copy;
                        current_object_index++;
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
                    string[] items = line.Split(' ');
                    if (items.Length != 4)
                    {
                        Log.Error("Support for polygonal wavefront faces not implemented. Faces must be triangles");
                        return false;
                    }
                    faces.Add(new WavefrontFace() { face_indices = new int[] { face_indices.Count, face_indices.Count + 1, face_indices.Count + 2 } });
                    for (var i = 1; i < items.Length; ++i)
                    {
                        if (false)
                        {
                            facestream.Add((ushort)(ushort.Parse(items[i]) - 1));
                            face_indices.Add(new WavefrontFaceIndex()
                            {
                                vertex_coord_index = (short)(short.Parse(items[i + 0]) - 1),
                                texture_coord_index = (short)(short.Parse(items[i + 1]) - 1),
                                normal_coord_index = (short)(short.Parse(items[i + 2]) - 1)
                            });
                        }
                        else
                        {
                            string[] indices = items[i].Split('/');
                            facestream.Add((ushort)(ushort.Parse(items[i]) - 1));
                            //face_indices.Add(new WavefrontFaceIndex()
                            //{
                            //    vertex_coord_index = (short)(short.Parse(indices[0]) - 1),
                            //    texture_coord_index = (short)(short.Parse(indices[1]) - 1),
                            //    normal_coord_index = (short)(short.Parse(indices[2]) - 1)
                            //});
                        }
                    }
                }
            }
            //TODO: generate tri-strips
            //generate sahder-group structs
            //compress vertex data
            //create bone map with default bone
            this.Vertices = new DefaultVertex[vertex_coords.Count];
            for(int i = 0; i < vertex_coords.Count;++i)
            {
                this.Vertices[i] = new DefaultVertex() { Position = vertex_coords[i] };
            }
            Adjacencies stripper = new Adjacencies(facestream.ToArray());
            Random r = new Random(DateTime.Now.Millisecond);
            TriangleStrip[] strips = stripper.GenerateStripArray(r.Next(facestream.Count / 3));
            QuickModelView quick_view = new QuickModelView(this, strips);
            while (!quick_view.ShowDialog())
            {
                strips = stripper.GenerateStripArray(r.Next(facestream.Count / 3)); 
                quick_view = new QuickModelView(this, strips);
            }
            return true;
        }
        struct WavefrontObject
        {
           public int faces_start_index;
           public int faces_count;

           public override string ToString()
           {
               return string.Format("{0} : {1}", faces_start_index, faces_count);
           }
        }
        struct WavefrontFace
        {
            public int[] face_indices;
        }
        struct WavefrontFaceIndex
        {
            public short vertex_coord_index;
            public short texture_coord_index;
            public short normal_coord_index;

            public override string ToString()
            {
                return string.Format("v: {0} / vt: {1} / vn: {2}", vertex_coord_index, texture_coord_index, normal_coord_index);
            }
        }

        private DefaultVertex[] ExtractVertices(CompressionRanges compression_ranges, byte[] coord_raw, int coord_size, byte[] texcoord_raw, int texcoord_size, byte[] vector_raw, int vector_size)
        {
            int vertex_count = coord_raw.Length / coord_size;
            DefaultVertex[] vertices = new DefaultVertex[vertex_count];
            for (int i = 0; i < vertex_count; ++i)
            {
                Vector3 position = new Vector3(BitConverter.ToInt16(coord_raw, i * coord_size), BitConverter.ToInt16(coord_raw, (i * coord_size) + 2), BitConverter.ToInt16(coord_raw, (i * coord_size) + 4));
                Range int16_range = new Range(short.MinValue, short.MaxValue);
                position.X = Project(position.X, compression_ranges.x);
                position.Y = Project(position.Y,  compression_ranges.y);
                position.Z = Project(position.Z,  compression_ranges.z);
                Vector2 texcoord = new Vector2(BitConverter.ToInt16(coord_raw, i * coord_size), BitConverter.ToInt16(coord_raw, i + 1 * coord_size));
                texcoord.X = Project(position.X,  compression_ranges.u1);
                texcoord.Y = Project(position.Y,  compression_ranges.v1);
                vertices[i] = new DefaultVertex() { Position = position, TextureCoordinates = texcoord, Normal = ExpandVector(BitConverter.ToInt32(vector_raw, i * vector_size)) };
            }
            return vertices;
        }

        Vector3 ExpandVector(int compressed_vector_data)
        {
            int CompressedData = compressed_vector_data;
            
            int x11 = (CompressedData & 0x000007FF);
            if ((x11 & 0x00000400) == 0x00000400)
            {
                x11 = -((~x11) & 0x000007FF);
                if (x11 == 0) x11 = -1;
            }
            
            int y11 = (CompressedData >> 11) & 0x000007FF;
            if ((y11 & 0x00000400) == 0x00000400)
            {
                y11 = -((~y11) & 0x000007FF);
                if (y11 == 0) y11 = -1;
            }
            
            int z10 = (CompressedData >> 22) & 0x000003FF;//last 10 bits
            if ((z10 & 0x00000200) == 0x00000200)
            {
                z10 = -((~z10) & 0x000003FF);
                if (z10 == 0) z10 = -1;
            }
            float x, y, z;
            x = (x11 / (float)0x000003ff);//10
            y = (y11 / (float)0x000003FF);//10
            z = (z10 / (float)0x000001FF);//9
            return new Vector3(x, y, x);
        }

        float Project(float value,  Range range)
        {
            const float Max = 1.0f / ushort.MaxValue;
            const float Half = short.MaxValue;
            return (((value + Half) * Max) * (range.max - range.min)) + range.min;
        }

        public struct DefaultVertex
        {
            public Vector3 Position;
            public Vector2 TextureCoordinates;
            public Vector3 Normal;
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
