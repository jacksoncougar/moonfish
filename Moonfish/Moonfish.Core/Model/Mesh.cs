﻿using System;
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
    public class Mesh
    {
        public BeginMode PrimitiveType;
        public string Name = "default";
        public ushort[] Indices;
        public StandardVertex[] Vertices;
        public VertexWeight[] VertexWeights;
        public byte[] Nodes;
        public MaterialGroup[] Groups;
        public CompressionInformation Compression;

        private CompressionInformation GenerateCompressionData()
        {
            CompressionInformation compression = new CompressionInformation();
            foreach (var vertex in Vertices)
            {
                compression.X = Range.Include(compression.X, vertex.Position.X);
                compression.Y = Range.Include(compression.Y, vertex.Position.Y);
                compression.Z = Range.Include(compression.Z, vertex.Position.Z);
                compression.U = Range.Include(compression.U, vertex.TextureCoordinates.X);
                compression.V = Range.Include(compression.V, vertex.TextureCoordinates.Y);
            }
            compression.Expand(0.0001f);
            Compression = compression;
            return compression;
        }
        private DSection GenerateSectionData(uint raw_size)
        {
            DSection section = new DSection();
            section.RawSize = raw_size;
            section.RawDataSize = raw_size - (124);//arbitrary for now
            section.RawOffset = 0;
            section.TriangleCount = this.GetTriangleCount();
            section.VertexCount = (ushort)this.Vertices.Length;
            return section;
        }

        /// <summary>
        /// Generating from a list this way is not grande.
        /// </summary>
        /// <returns></returns>
        private bool GenerateNormals()
        {
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Normal = Vector3.Zero;
            }
            int ref0, ref1, ref2;
            bool winding = false;
            if (Indices == null || Vertices == null) return false;
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
                Vector3 vec1 = Vertices[ref2].Position - Vertices[ref0].Position;
                Vector3 vec2 = Vertices[ref1].Position - Vertices[ref0].Position;
                Vector3 normal = Vector3.Cross(vec1, vec2);
                Vertices[ref0].Normal += normal;
                Vertices[ref1].Normal += normal;
                Vertices[ref2].Normal += normal;
            }
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Normal.Normalize();
            }
            return true;
        }
        private bool GenerateTexCoords()
        {
            int ref0, ref1, ref2;
            if (Indices == null || Vertices == null) return false;
            for (int i = 0; i < Indices.Length - 2; ++i)
            {
                ref0 = Indices[i + 0];
                ref1 = Indices[i + 1];
                ref2 = Indices[i + 2];
                if (ref0 == ref1 || ref1 == ref2 || ref0 == ref2) continue;
                if (Vertices[ref0].TextureCoordinates == Vector2.Zero)
                    Vertices[ref0].TextureCoordinates = Vertices[ref0].Position.Xy;
                if (Vertices[ref1].TextureCoordinates == Vector2.Zero)
                    Vertices[ref1].TextureCoordinates = Vertices[ref1].Position.Xy;
                if (Vertices[ref2].TextureCoordinates == Vector2.Zero)
                    Vertices[ref2].TextureCoordinates = Vertices[ref2].Position.Xy;
            }
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].TextureCoordinates.NormalizeFast();
            }
            return true;
        }
        private bool GenerateTangentSpaceVectors()
        {
            if (this.Vertices == null || this.Indices == null) return false;
            Vector3[] tangents = new Vector3[this.Vertices.Length * 2];
            int bitan = this.Vertices.Length;
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
                Vector3 v1 = this.Vertices[i1].Position;
                Vector3 v2 = this.Vertices[i2].Position;
                Vector3 v3 = this.Vertices[i3].Position;
                Vector2 t1 = this.Vertices[i1].TextureCoordinates;
                Vector2 t2 = this.Vertices[i2].TextureCoordinates;
                Vector2 t3 = this.Vertices[i3].TextureCoordinates;

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
            for (int i = 0; i < this.Vertices.Length; i++)
            {
                Vector3 t = tangents[i];
                Vector3 n = this.Vertices[i].Normal;
                this.Vertices[i].Tangent = (t - n * Vector3.Dot(n, t));
                this.Vertices[i].Tangent.Normalize();
                bool lefthanded = Vector3.Dot(Vector3.Cross(n, t), tangents[bitan + i]) < 0.0F ? true : false;
                this.Vertices[i].Bitangent = Vector3.Cross(n, this.Vertices[i].Tangent);
                if (lefthanded) this.Vertices[i].Bitangent *= -1;
            }
            return true;
        }
        private ushort GetTriangleCount()
        {
            ushort triangle_count = 0;
            for (ushort i = 0; i < this.Indices.Length - 2; i++)
            {
                if (IsDegenerate(this.Indices[i], this.Indices[i + 1], this.Indices[i + 2])) continue;
                else ++triangle_count;
            }
            return triangle_count;
        }
        /// <summary>
        /// Deserializes a Halo 2 formatted raw-resource block and initializes the Mesh object from it
        /// </summary>
        /// <param name="raw_data"></param>
        /// <param name="raw_resources"></param>
        /// <param name="compression_ranges"></param>
        /// <returns></returns>
        public bool Load(ModelRaw raw_data, IEnumerable<Moonfish.Core.Structures.Resource> resources, model.CompressionRanges compression_ranges)
        {
            PrimitiveType = BeginMode.TriangleStrip;
            return Load(raw_data.ToArray(), resources.Select(x => x.GetDefinition<DResource>()), compression_ranges.GetDefinition<DCompressionRanges>(), raw_data.HeaderSize);
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
        bool Load(byte[] raw_data, IEnumerable<DResource> raw_resources, DCompressionRanges compression_ranges, int header_size)
        {
            int first_address = 4 + 4 + header_size;
            int coord_size = 0;
            int texcoord_size = 0;
            int vector_size = 0;

            int[] sizes = new int[0];
            VertexResource[] types = new VertexResource[0];

            VertexResource coordinate_type = VertexResource.none;
            VertexResource texture_coordinate_type = VertexResource.none;

            int stream_length = BitConverter.ToInt32(raw_data, 4);
            MemoryStream stream = new MemoryStream(raw_data, first_address, stream_length, false);
            BinaryReader binary_reader = new BinaryReader(stream);
            foreach (var resource in raw_resources)
            {
                if (resource.first_ != 0) continue;//skip the vertex resources
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
                        Groups = new MaterialGroup[count];
                        // read each block
                        for (int i = 0; i < count; i++)
                        {
                            Groups[i] = binary_reader.ReadDefinition<MaterialGroup>();
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
                                sizes = new int[count];
                                types = new VertexResource[count];
                                for (int i = 0; i < count; i++)
                                {
                                    byte[] buffer = binary_reader.ReadBytes(resource.data_size__or__first_index);
                                    types[i] = (VertexResource)buffer[0];
                                    sizes[i] = buffer[1];
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

            /*
             * Intent: process vertices by type and load additional bone information if present
             */

            int vertex_count = vertex_resources[0].resource_length / sizes[0];
            this.Vertices = new StandardVertex[vertex_count];
            this.VertexWeights = new VertexWeight[vertex_count];
            foreach (var vertex_resource in vertex_resources)
            {
                binary_reader.BaseStream.Position = vertex_resource.resource_offset;
                var buffer = binary_reader.ReadBytes(vertex_resource.resource_length);
                var stride = sizes[vertex_resource.data_size__or__first_index];
                for (int i = 0; i < vertex_count; ++i)
                {
                    switch (types[vertex_resource.data_size__or__first_index])
                    {
                        case VertexResource.coordinate_float:
                            this.Vertices[i].Position = new Vector3(
                                BitConverter.ToSingle(buffer, i * stride),
                                BitConverter.ToSingle(buffer, (i * stride) + 4),
                                BitConverter.ToSingle(buffer, (i * stride) + 8));
                            break;
                        case VertexResource.coordinate_compressed:
                            this.Vertices[i].Position = new Vector3(
                                BitConverter.ToInt16(buffer, i * stride),
                                BitConverter.ToInt16(buffer, (i * stride) + 2),
                                BitConverter.ToInt16(buffer, (i * stride) + 4));
                            this.Vertices[i].Position.X = Inflate(this.Vertices[i].Position.X, compression_ranges.X);
                            this.Vertices[i].Position.Y = Inflate(this.Vertices[i].Position.Y, compression_ranges.Y);
                            this.Vertices[i].Position.Z = Inflate(this.Vertices[i].Position.Z, compression_ranges.Z);
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
                            this.Vertices[i].TextureCoordinates = new Vector2(
                            BitConverter.ToSingle(buffer, i * stride),
                            BitConverter.ToSingle(buffer, (i * stride) + 4));
                            break;
                        case VertexResource.texture_coordinate_compressed:
                            this.Vertices[i].TextureCoordinates = new Vector2(
                            BitConverter.ToInt16(buffer, i * stride),
                            BitConverter.ToInt16(buffer, (i * stride) + 2));
                            this.Vertices[i].TextureCoordinates.X = Inflate(this.Vertices[i].TextureCoordinates.X, compression_ranges.U);
                            this.Vertices[i].TextureCoordinates.Y = Inflate(this.Vertices[i].TextureCoordinates.Y, compression_ranges.V);
                            break;
                        case VertexResource.tangent_space_unit_vectors_compressed:
                            this.Vertices[i].Normal = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, i * stride));
                            this.Vertices[i].Tangent = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, (i * stride) + 4));
                            this.Vertices[i].Bitangent = (Vector3)new Vector3t(BitConverter.ToUInt32(buffer, i * (stride) + 8));
                            break;
                    }
                }
            }
            //stuff
            {
                Range x = new Range(Vertices[0].Position.X, Vertices[0].Position.X);
                Range y = new Range(Vertices[0].Position.Y, Vertices[0].Position.Y);
                Range z = new Range(Vertices[0].Position.Z, Vertices[0].Position.Z);
                foreach (var vertex in this.Vertices)
                {
                    x = Range.Include(x, vertex.Position.X);
                    y = Range.Include(y, vertex.Position.Y);
                    z = Range.Include(z, vertex.Position.Z);
                }
                this.Center = new Vector3(Range.Median(x), Range.Median(y), Range.Median(z));
            }

            return true;
        }

        #region Import Methods

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

            WavefrontOBJ.Object[] objects = new WavefrontOBJ.Object[1] { new WavefrontOBJ.Object() { faces_start_index = 0 } };
            List<WavefrontOBJ.Face> faces = new List<WavefrontOBJ.Face>();
            List<Vector3> vertex_coords = new List<Vector3>();
            List<Vector2> texture_coords = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            Dictionary<string, int> material_names = new Dictionary<string, int>();
            material_names.Add("default", 0);
            int selected_material = 0;
            bool default_object = true;
            bool default_object_processed = false;
            bool has_normals = false;
            bool has_texcoords = false;

            Log.Info("Begin parsing Wavefront Object data from buffer");
            while (!reader.EndOfStream)
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
                        if (!default_object_processed)
                        {
                            objects[0].faces_count = faces.Count;
                            default_object_processed = true;
                        }
                    }
                }
                else if (line.StartsWith("v "))
                {
                    Vector3 vertex;
                    if (!WavefrontExtensions.TryParseVector3(out vertex, line)) return false;
                    vertex_coords.Add(vertex);
                }
                else if (line.StartsWith("vt "))
                {
                    has_texcoords = true;
                    Vector2 texcoord;
                    if (!WavefrontExtensions.TryParseVector2(out texcoord, line)) return false;
                    texture_coords.Add(texcoord);
                }
                else if (line.StartsWith("vn "))
                {
                    has_normals = true;
                    Vector3 normal;
                    if (!WavefrontExtensions.TryParseVector3(out normal, line)) return false;
                    normals.Add(normal);
                }
                else if (line.StartsWith("usemtl "))
                {
                    string name = line.Replace("usemtl ", "").Trim();
                    if (!material_names.ContainsKey(name)) material_names.Add(name, material_names.Count);
                    selected_material = material_names[name];
                }
                else if (line.StartsWith("f "))
                {
                    WavefrontOBJ.Face face;
                    if (!WavefrontOBJ.Face.TryParse(line, out face))
                    {
                        Log.Error(string.Format("Error parsing line: {0}", line));
                        return false;
                    }
                    else
                    {
                        face.material_id = selected_material;
                        faces.Add(face);
                    }
                }
                else Log.Warn(string.Format("Unsupported format found while parsing line: {0}", line));
            }
            if (!default_object_processed)
            {
                objects[0].faces_count = faces.Count;
                default_object_processed = true;
            }
            Log.Info("Partial success... finished parsing Wavefront Object data from buffer");

            List<Triangle> triangle_list = new List<Triangle>(faces.Count);
            List<StandardVertex> vertices = new List<StandardVertex>(vertex_coords.Count);
            List<string> tokens = new List<string>();

            for (int i = 0; i < faces.Count; i++)
            {
                Triangle triangle = new Triangle() { MaterialID = faces[i].material_id };
                if (!tokens.Contains(faces[i].GetToken(0)))
                {
                    triangle.Vertex1 = (ushort)vertices.Count;
                    tokens.Add(faces[i].GetToken(0));
                    vertices.Add(new StandardVertex()
                    {
                        Position = vertex_coords[faces[i].vertex_indices[0] - 1],
                        TextureCoordinates = faces[i].has_texcoord ? texture_coords[faces[i].texcoord_indices[0] - 1] : Vector2.Zero,
                        Normal = faces[i].has_normals ? normals[faces[i].normal_indices[0] - 1] : Vector3.Zero
                    });
                }
                else
                {
                    triangle.Vertex1 = (ushort)tokens.IndexOf(faces[i].GetToken(0));
                }
                if (!tokens.Contains(faces[i].GetToken(1)))
                {
                    triangle.Vertex2 = (ushort)vertices.Count;
                    tokens.Add(faces[i].GetToken(1));
                    vertices.Add(new StandardVertex()
                    {
                        Position = vertex_coords[faces[i].vertex_indices[1] - 1],
                        TextureCoordinates = faces[i].has_texcoord ? texture_coords[faces[i].texcoord_indices[1] - 1] : Vector2.Zero,
                        Normal = faces[i].has_normals ? normals[faces[i].normal_indices[1] - 1] : Vector3.Zero
                    });
                }
                else
                {
                    triangle.Vertex2 = (ushort)tokens.IndexOf(faces[i].GetToken(1));
                }
                if (!tokens.Contains(faces[i].GetToken(2)))
                {
                    triangle.Vertex3 = (ushort)vertices.Count;
                    tokens.Add(faces[i].GetToken(2));
                    vertices.Add(new StandardVertex()
                    {
                        Position = vertex_coords[faces[i].vertex_indices[2] - 1],
                        TextureCoordinates = faces[i].has_texcoord ? texture_coords[faces[i].texcoord_indices[2] - 1] : Vector2.Zero,
                        Normal = faces[i].has_normals ? normals[faces[i].normal_indices[2] - 1] : Vector3.Zero
                    });
                }
                else
                {
                    triangle.Vertex3 = (ushort)tokens.IndexOf(faces[i].GetToken(2));
                }
                triangle_list.Add(triangle);
            }


            List<TriangleStrip> strips = new List<TriangleStrip>(material_names.Count);
            foreach (var material in material_names)
            {
                var material_faces = triangle_list.Where(x => x.MaterialID == material.Value).Select(x => x.AsEnumerable<ushort>());
                List<ushort> tris = new List<ushort>(material_faces.Count() * 3);
                foreach (var item in material_faces)
                {
                    tris.AddRange(item.ToArray());
                }
                if (tris.Count > 0)
                {
                    Adjacencies stripper = new Adjacencies(tris.ToArray());
                    strips.Add(new TriangleStrip() { MaterialID = (ushort)material.Value, Indices = stripper.GenerateTriangleStrip() });
                }
            }
            this.Vertices = vertices.ToArray();
            this.Groups = new MaterialGroup[strips.Count];

            Log.Info("Parsing shader groups from triangle strips...");
            TriangleStrip combined_strip = new TriangleStrip() { Indices = new ushort[0] };
            ushort offset = 0;
            for (int i = 0; i < strips.Count; ++i)
            {
                TriangleStrip.Append(ref combined_strip, strips[i]);
                this.Groups[i] = new MaterialGroup(offset, (ushort)(combined_strip.Indices.Length - offset)) { shader_index = (ushort)i };
                Log.Info(string.Format(@"ShaderGroup[ {0} ] {{ Start = {1}, Length = {2} }}", i, offset, (combined_strip.Indices.Length - offset).ToString()));
                offset = (ushort)combined_strip.Indices.Length;

            }
            this.Indices = combined_strip.Indices;
            if (!has_texcoords)
            {
                Log.Warn("No texture coordinates found while parsing Wavefront file..." +
                        "\nGenerating uv coordinates from vertex positions (flat mapping)");
                this.GenerateTexCoords();
            }
            if (!has_normals)
            {
                Log.Warn("No normals found while parsing Wavefront file..." +
                        "\nGenerating normals...");
                this.GenerateNormals();
            }
            Log.Info("Generating tangent space vectors...");
            this.GenerateTangentSpaceVectors();
            Log.Info("Success, import finished! Returning true from ImportFromWavefront();");
            return true;
        }
        public void ImportFromCollada(Collada141.COLLADA collada)
        {
            var geometries = collada.Items.SingleOrDefault(x => x is Collada141.library_geometries) as Collada141.library_geometries;
            if (geometries == null) return;
            ImportGeometry(geometries.geometry[0]);
        }
        void ImportGeometry(Collada141.geometry geometry)
        {
            var mesh = geometry.Item as Collada141.mesh;
            if (mesh == null) return;
            var float_array = mesh.source[0].Item as Collada141.float_array;
            this.Vertices = new StandardVertex[float_array.Values.Length / 3];
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i] = new StandardVertex()
                {
                    Position = new Vector3(
                        (float)float_array.Values[i * 3 + 0],
                        (float)float_array.Values[i * 3 + 1],
                        (float)float_array.Values[i * 3 + 2]
                        )
                };
            }

            var poly_list = mesh.Items[0] as Collada141.polylist;
            if (poly_list == null) return;
            Triangle[] triangles = new Triangle[poly_list.count];
            var raw_indices = Collada141.COLLADA.ConvertIntArray(poly_list.p);
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = new Triangle()
                {
                    MaterialID = 0,
                    Vertex1 = (ushort)raw_indices[i * (3 * 2) + (0 * 2)],
                    Vertex2 = (ushort)raw_indices[i * (3 * 2) + (1 * 2)],
                    Vertex3 = (ushort)raw_indices[i * (3 * 2) + (2 * 2)],
                };
            }
            List<ushort> triangle_indices = new List<ushort>(triangles.Length * 3);
            foreach (var triangle in triangles)
            {
                triangle_indices.AddRange(triangle.ToArray());
            }
            Adjacencies adj = new Adjacencies(triangle_indices.ToArray());
            this.Indices = adj.GenerateTriangleStrip();
            this.Groups = new MaterialGroup[] { new MaterialGroup(0, (ushort)Indices.Length) };
        }

        #endregion

        #region Export Methods

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
        public Collada141.geometry ExportAsCOLLADAGeometry()
        {
            string base_id = string.Format("{0}-{1}", this.Name, "mesh");
            var source = new Collada141.source[]
            {
                CreateVector3Source(this.Vertices.Select(x=>x.Position), base_id, "positions"),
                //CreateVector3Source(this.Vertices.Select(x=>x.Normal), base_id, "normals"),
            };
            var vertices = new Collada141.vertices()
            {
                id = string.Format("{0}-{1}", base_id, "vertices"),
                input = new Collada141.InputLocal[]
                {
                    new Collada141.InputLocal() 
                    {                        
                        semantic = "POSITION", 
                        source = string.Format("#{0}", source[0].id)
                    }
                }
            };
            var triangles = GenerateTriangleFromStrip();
            var value = string.Join(" ", triangles.Select(x => string.Format("{0} {1} {2}", x.Vertex1, x.Vertex2, x.Vertex3)));
            StringBuilder vcount = new StringBuilder();
            foreach (var triangle in triangles)
                vcount.Append("3 ");
            var triangle_list = new Collada141.polylist()
            {
                p = value,
                vcount = vcount.ToString(),
                count = (ulong)triangles.Length,
                input = new Collada141.InputLocalOffset[] 
                { 
                    new Collada141.InputLocalOffset() 
                    {
                        semantic = "VERTEX",
                        offset = 0,
                        source = string.Format("#{0}", vertices.id)
                    }
                },
            };
            var mesh = new Collada141.mesh()
            {
                source = source,
                vertices = vertices,
                Items = new object[] { triangle_list },
            };
            return new Collada141.geometry() 
            { 
                id = base_id,
                Item = mesh 
            };
        }

        private Triangle[] GenerateTriangleFromStrip()
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

        private Collada141.source CreateVector3Source(IEnumerable<Vector3> source_vectors, string source_id, string source_type)
        {
            double[] float_array_data = new double[source_vectors.Count() * 3];
            {
                int i = 0;
                foreach (var vector in source_vectors)
                {
                    float_array_data[(i * 3) + 0] = vector.X;
                    float_array_data[(i * 3) + 1] = vector.Y;
                    float_array_data[(i * 3) + 2] = vector.Z;
                    ++i;
                }
            }

            Collada141.source source = new Collada141.source()
            {
                id = string.Format("{0}-{1}", source_id, source_type),
                Item = new Collada141.float_array()
                {
                    count = (ulong)float_array_data.Length,
                    id = string.Format("{0}-{1}-{2}", source_id, source_type, "array"),
                    Values = float_array_data,
                },
                technique_common = new Collada141.sourceTechnique_common()
                {
                    accessor = new Collada141.accessor()
                    {
                        count = (ulong)float_array_data.Length / 3,
                        stride = 3,
                        source = string.Format("#{0}-{1}-{2}", source_id, source_type, "array"),
                        param = new Collada141.param[]
                        {
                            new Collada141.param()
                            {
                                name = "X",
                                type = "float",
                            },
                            new Collada141.param()
                            {
                                name = "Y",
                                type = "float",
                            },
                            new Collada141.param()
                            {
                                name = "Z",
                                type = "float",
                            },
                        },
                    },
                },
            };
            return source;
        }

        public bool ExportForEntity(string desination_folder, string tagname)
        {
            model model = new model();                                                  // Make TagStructure object to hold our model definition data

            model.Compression.Add(new model.CompressionRanges(GenerateCompressionData()));    // Add a new Compression TagBlock, filling it from this mesh's data

            DResource[] resource = null;                                                 // Convert the model data into the halo 2 format and write it to a file
            int raw_size = -1;

            string output_filename = Path.Combine(desination_folder, tagname);
            string output_name = output_filename.Substring(output_filename.LastIndexOf('\\') + 1);
            string meta_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".mode");
            string meta_xml_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".mode.xml");
            string raw_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".moderaw");
            string raw_xml_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".moderaw.xml");
            string info_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".info");

            if (!Directory.Exists(desination_folder)) Directory.CreateDirectory(desination_folder);

            using (BinaryWriter bin = new BinaryWriter(File.Create(raw_filepath)))
            {
                var buffer = this.Serialize(out resource);
                raw_size = buffer.Length;
                bin.Write(buffer);
            } if (resource == null) return false;                                       // If we didn't get any resources back then the method failed.

            model.Regions.Add(new model.Region(new DRegion()));                         // Add a default region + default definition
            model.Regions[0].Permutations.Add(new model.Region.Permutation());          // Add a default permutation to that region
            model.Sections.Add(new model.Section(GenerateSectionData((uint)raw_size))); // Add a new Section tagBlock to hold our model information
            model.Sections[0].Resources.AddRange(new Moonfish.Core.Structures.Resource[]{
            new Moonfish.Core.Structures.Resource(resource[0]), 
            new Moonfish.Core.Structures.Resource(resource[1]), 
            new Moonfish.Core.Structures.Resource(resource[2]), 
            new Moonfish.Core.Structures.Resource(resource[3]), 
            new Moonfish.Core.Structures.Resource(resource[4]), 
            new Moonfish.Core.Structures.Resource(resource[5]), 
            new Moonfish.Core.Structures.Resource(resource[6]), 
            });
            model.Groups.Add(new model.Group(new DGroup()));                            // Add a default model_group + a default definition
            model.Nodes.Add(new model.Node(new DNode()));                               // Add a default node + default definition
            for (int i = 0; i < Groups.Length; i++)
            {
                model.Shaders.Add(new model.Shader(new DShader()));                     // Add a default shader + default definition
            }
            int meta_size = 0;
            using (var file = File.Create(meta_filepath))
            {
                Memory.Map(model, file);
                meta_size = (int)file.Length;
            }
            using (XmlWriter xml = XmlWriter.Create(File.Create(meta_xml_filepath),
                new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
            {
                xml.WriteStartElement("Meta");
                xml.WriteAttributeString("TagName", tagname);
                xml.WriteAttributeString("Offset", "0");
                xml.WriteAttributeString("Size", meta_size.ToString());
                xml.WriteAttributeString("TagType", "mode");
                xml.WriteAttributeString("Magic", "0");
                xml.WriteAttributeString("Parsed", "True");
                xml.WriteAttributeString("Date", DateTime.Now.ToShortDateString());
                xml.WriteAttributeString("Time", DateTime.Now.ToShortTimeString());
                xml.WriteAttributeString("EntityVersion", "0.1");
                xml.WriteAttributeString("Padding", "0");

                WriteEntityXmlNodes(xml, model, 0, tagname);

                xml.WriteEndElement();
            }
            int raw_pointer_address = (model.Sections as IPointable).Address + 56;//bug lol
            using (XmlWriter xml = XmlWriter.Create(raw_xml_filepath,
                new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
            {
                xml.WriteStartElement("RawData");
                xml.WriteAttributeString("TagType", "mode");
                xml.WriteAttributeString("TagName", tagname);
                xml.WriteAttributeString("RawType", "Model");
                xml.WriteAttributeString("RawChunkCount", "1");
                xml.WriteAttributeString("Date", DateTime.Now.ToShortDateString());
                xml.WriteAttributeString("Time", DateTime.Now.ToShortTimeString());
                xml.WriteAttributeString("EntityVersion", "0.1");
                {
                    xml.WriteStartElement("RawChunk");
                    xml.WriteAttributeString("RawDataType", "mode1");
                    xml.WriteAttributeString("PointerMetaOffset", raw_pointer_address.ToString());
                    xml.WriteAttributeString("RawType", "Model");
                    xml.WriteAttributeString("ChunkSize", raw_size.ToString());
                    xml.WriteAttributeString("PointsToOffset", "0");
                    xml.WriteAttributeString("RawLocation", "Internal");
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
            using (StreamWriter txt = new StreamWriter(info_filepath))
            {
                txt.WriteLine(meta_filepath);
            }
            return false;
        }

        #endregion

        private void WriteEntityXmlNodes(XmlWriter xml, IEnumerable<TagBlockField> tagblock, int current_offset, string tagname)
        {
            foreach (var field in tagblock)
            {
                if (field.Object.GetType() == typeof(StringID))
                {
                    xml.WriteStartElement("String");
                    xml.WriteAttributeString("Description", "Waffle");
                    xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
                    xml.WriteAttributeString("StringName", "default");
                    xml.WriteAttributeString("TagType", "mode");
                    xml.WriteAttributeString("TagName", tagname);
                    xml.WriteEndElement();
                }
                else if (field.Object.GetType() == typeof(TagIdentifier))
                {
                    xml.WriteStartElement("Ident");
                    xml.WriteAttributeString("Description", "Waffle");
                    xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
                    xml.WriteAttributeString("PointsToTagType", "mode");
                    xml.WriteAttributeString("PointsToTagName", tagname);
                    xml.WriteAttributeString("TagType", "mode");
                    xml.WriteAttributeString("TagName", tagname);
                    xml.WriteEndElement();
                }
                else if (field.Object.GetType() == typeof(TagPointer))
                {
                    xml.WriteStartElement("Ident");
                    xml.WriteAttributeString("Description", "Waffle");
                    xml.WriteAttributeString("Offset", (current_offset + (field.FieldOffset + 4)).ToString());
                    xml.WriteAttributeString("PointsToTagType", "");
                    xml.WriteAttributeString("PointsToTagName", "Null");
                    xml.WriteAttributeString("TagType", "mode");
                    xml.WriteAttributeString("TagName", tagname);
                    xml.WriteEndElement();
                }
                else
                {
                    IEnumerable<TagBlock> taglist_interface = (field.Object as IEnumerable<TagBlock>);
                    if (taglist_interface != null)
                    {
                        if (taglist_interface.Count<TagBlock>() == 0) continue;
                        xml.WriteStartElement("Reflexive");
                        xml.WriteAttributeString("Description", "Waffle");
                        xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
                        xml.WriteAttributeString("ChunkCount", taglist_interface.Count<TagBlock>().ToString());
                        xml.WriteAttributeString("ChunkSize", (field.Object as IPointable).SizeOf.ToString());
                        xml.WriteAttributeString("Translation", (field.Object as IPointable).Address.ToString());
                        xml.WriteAttributeString("PointsToTagType", "mode");
                        xml.WriteAttributeString("PointsToTagName", tagname);
                        xml.WriteAttributeString("TagType", "mode");
                        xml.WriteAttributeString("TagName", tagname);
                        xml.WriteEndElement();
                        foreach (var item in taglist_interface)
                        {
                            WriteEntityXmlNodes(xml, item, (field.Object as IPointable).Address, tagname);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Converts internal Mesh fields into Halo 2 compatible resource format
        /// </summary>
        /// <param name="resource_out">returns with array of Resource-meta structs represnting the blocks in the resource</param>
        /// <returns>array of bytes which holds the serialized Halo 2 resource</returns>
        public byte[] Serialize(out DResource[] resource_out)
        {
            /* Intent: Write out the this Model instance data into a format that 
             * the halo 2 version of blam! engine can use.
             * The resources that we will be focusing on are ShaderGroups, Indices,
             * Vertex position, texcoord, tangent space vectors, and a simple bonemap.*/

            Log.Info(@"Entering Model.Serialize()");

            if (Compression == null) { GenerateCompressionData(); }     // Check that we have compression data available before continuing
            DResource[] resource = new DResource[7];                    // Create resource defintion array
            MemoryStream buffer = new MemoryStream();
            BinaryWriter bin = new BinaryWriter(buffer);                // BinaryWriter

            Log.Info(string.Format(@"Writing header_tag @{0}", bin.BaseStream.Position));
            bin.WriteFourCC("blkh");                // Write the header_tag value
            bin.Write(0);                           // [uint] resource_data_size  (reserve)

            // * Begin resource_header // size: 112

            Log.Info(string.Format(@"Writing shader_groups_count = {0} @{1}", Groups.Length, bin.BaseStream.Position));
            bin.Write(Groups.Length);         // * 0x00: shader_groups_count;
            bin.Write(new byte[28]);                // * 0x08: some unused thing... count.
            Log.Info(string.Format(@"Writing indices_count = {0} @{1}", Indices.Length, bin.BaseStream.Position));
            bin.Write(Indices.Length);              // * 0x20: indices_count;
            bin.Write(new byte[20]);
            bin.Write(3);                           // * 0x38: vertex_resource_count; //special
            bin.Write(new byte[40]);
            bin.Write(1);                           // * 0x64: bone_map_count;          
            bin.Write(new byte[8]);

            Log.Info(string.Format(@"Resource data_start_offset = {0}", bin.BaseStream.Position));
            var resource_data_start_offset = bin.BaseStream.Position;   // This is the offset to which all DResource block_offsets are written from

            bin.WriteFourCC("rsrc");             // shader_group resource begin
            resource[0] = new DResource(0, 72, Groups.Length * 72, (int)(bin.BaseStream.Position - resource_data_start_offset));
            foreach (var group in Groups)
                bin.Write(group);                // shader_group data

            bin.WriteFourCC("rsrc");             // indices resource begin
            resource[1] = new DResource(32, sizeof(ushort), sizeof(ushort) * this.Indices.Length, (int)(bin.BaseStream.Position - resource_data_start_offset));
            foreach (ushort index in this.Indices)  // write each index ushort
                bin.Write(index);                // pad to word boundary
            bin.WritePadding(4);

            bin.WriteFourCC("rsrc");             // vertex_data_header?
            resource[2] = new DResource(56, 32, 32 * 3, (int)(bin.BaseStream.Position - resource_data_start_offset));
            bin.Write(Mesh.VERTEX_RESOURCE_HEADER_DATA); // laziness TODO: write out proper headers here to allow for uncompressed types

            bin.WriteFourCC("rsrc");
            resource[3] = new DResource(56, 0, this.Vertices.Length * sizeof(ushort) * 3, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            {
                foreach (var vertex in this.Vertices)
                {
                    bin.Write(Deflate(Compression.X, vertex.Position.X));
                    bin.Write(Deflate(Compression.Y, vertex.Position.Y));
                    bin.Write(Deflate(Compression.Z, vertex.Position.Z));
                }
                bin.WritePadding(4);
            }
            bin.WriteFourCC("rsrc");
            resource[4] = new DResource(56, 1, this.Vertices.Length * sizeof(ushort) * 2, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            {
                foreach (var vertex in this.Vertices)
                {
                    bin.Write(Deflate(Compression.U, vertex.TextureCoordinates.X));
                    bin.Write(Deflate(Compression.V, vertex.TextureCoordinates.Y));  // flip this still?
                }
            }
            bin.WriteFourCC("rsrc");
            resource[5] = new DResource(56, 2, this.Vertices.Length * sizeof(uint) * 3, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            foreach (var vertex in this.Vertices)
            {
                bin.Write((uint)(Vector3t)vertex.Normal);    //cast to vector3t is destructive...
                bin.Write((uint)(Vector3t)vertex.Tangent);
                bin.Write((uint)(Vector3t)vertex.Bitangent);
            }
            bin.WriteFourCC("rsrc");
            resource[6] = new DResource(100, 1, 1, (int)(bin.BaseStream.Position - resource_data_start_offset));
            bin.Write(0);                // default bone-map (no bones)
            bin.WriteFourCC("blkf");
            int resource_size = (int)bin.BaseStream.Position;
            bin.Seek(4, SeekOrigin.Begin);
            bin.Write(resource_size - 124);

            // debug dump
#if DEBUG
            try
            {
                using (var file = File.OpenWrite(@"D:\halo_2\model_raw.bin"))
                {
                    file.Write(buffer.ToArray(), 0, (int)buffer.Length);
                }
            }
            catch { }
#endif
            // end debug dump

            // 2. create a sections meta file for this, a bounding box, heck a whole mesh, why not.
            resource_out = resource;
            return buffer.ToArray();
        }        

        /// <summary>
        /// Simple index comparison to check for degenerate triangles in a TriangleStrip
        /// </summary>
        /// <param name="ref0"></param>
        /// <param name="ref1"></param>
        /// <param name="ref2"></param>
        /// <returns>retuns false on the condition that any two indices are the same value</returns>
        static bool IsDegenerate(ushort ref0, ushort ref1, ushort ref2)
        {
            return (ref0 == ref1 || ref0 == ref2 || ref1 == ref2);
        }

        /// <summary>
        /// Takes a float-value and converts it into a range then stores the range ratio in a short
        /// </summary>
        /// <param name="input_range"></param>
        /// <param name="value"></param>
        /// <returns>returns the ratio of 'value' in 'input_range' represented as a short<returns>
        static short Deflate(Range input_range, float value)
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
        static float Inflate(float value_in_range, Range range)
        {
            const float ushort_max_inverse = 1.0f / ushort.MaxValue;
            return (((value_in_range + short.MaxValue) * ushort_max_inverse) * (range.max - range.min)) + range.min;
        }

        #region Laziness
        /// <summary>
        /// This is the resource 'header' block for the vertex data blocks... 
        /// </summary>
        static readonly byte[] VERTEX_RESOURCE_HEADER_DATA = new byte[]{
            0x02, 0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x19, 0x04, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 
            0x1B, 0x0C, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        #endregion

        public class MaterialGroup : IDefinition
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

            public MaterialGroup(ushort p1, ushort p2)
            {
                // TODO: Complete member initialization
                strip_start = p1;
                strip_length = p2;
            }

            public MaterialGroup() { }

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

        public void Show()
        {
            QuickMeshView render_window = new QuickMeshView(this);
            render_window.Run(60);
        }

        public Vector3 Center { get; set; }
    }
}
