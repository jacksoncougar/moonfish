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
    public class RenderMesh : Mesh
    {
        internal DCompressionRanges GenerateCompressionData()
        {
            DCompressionRanges compression = new DCompressionRanges();
            compression.X = new Range(Coordinates[0].X, Coordinates[0].X);
            compression.Y = new Range(Coordinates[0].Y, Coordinates[0].Y);
            compression.Z = new Range(Coordinates[0].Z, Coordinates[0].Z);
            compression.U = new Range(TextureCoordinates[0].X, TextureCoordinates[0].X);
            compression.V = new Range(TextureCoordinates[0].Y, TextureCoordinates[0].Y);
            for (int i = 0; i < Coordinates.Length; ++i)
            {
                compression.X = Range.Include(compression.X, Coordinates[i].X);
                compression.Y = Range.Include(compression.Y, Coordinates[i].Y);
                compression.Z = Range.Include(compression.Z, Coordinates[i].Z);
                compression.U = Range.Include(compression.U, TextureCoordinates[i].X);
                compression.V = Range.Include(compression.V, TextureCoordinates[i].Y);
            }
            compression.Expand(0.0001f);
            return compression;
        }
        private DSection GenerateSectionData()
        {
            DSection section = new DSection();
            section.TriangleCount = this.GetTriangleCount();
            section.VertexCount = (ushort)this.Coordinates.Length;
            return section;
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
            return base.Load(raw_data.ToArray(), resources.Select(x => x.GetDefinition<DResource>()), compression_ranges.GetDefinition<DCompressionRanges>(), raw_data.HeaderSize);
        }

        public model Save()
        {
            model model = new model();                                                  // Make TagStructure object to hold our model definition data

            var compression_ranges = GenerateCompressionData();
            model.Compression.Add(new model.CompressionRanges(compression_ranges));    // Add a new Compression TagBlock, filling it from this mesh's data

            DResource[] resource = null;        

            model.Regions.Add(new model.Region(new DRegion()));                         // Add a default region + default definition
            model.Regions[0].Permutations.Add(new model.Region.Permutation());          // Add a default permutation to that region
            model.Sections.Add(new model.Section(GenerateSectionData())); // Add a new Section tagBlock to hold our model information
            model.Sections[0].Raw.AddRange(Serialize(out resource, out compression_ranges));
            model.Sections[0].Resources.AddRange(resource.Select(x => new Moonfish.Core.Structures.Resource(x)));
            model.Groups.Add(new model.Group(new DGroup()));                            // Add a default model_group + a default definition
            model.Nodes.Add(new model.Node(new DNode()));                               // Add a default node + default definition
            for (int i = 0; i < Primitives.Length; i++)
            {
                model.Shaders.Add(new model.Shader(new DShader()));                     // Add a default shader + default definition
            }
            return model;
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
            List<Vector3> vertex_coordiantes = new List<Vector3>(vertex_coords.Count);
            List<Vector2> texture_coordinates = new List<Vector2>(vertex_coords.Count);
            List<Vector3> vertex_normals = new List<Vector3>(vertex_coords.Count);
            List<string> tokens = new List<string>();

            for (int i = 0; i < faces.Count; i++)
            {
                Triangle triangle = new Triangle() { MaterialID = faces[i].material_id };
                for (int token = 0; token < 3; ++token)
                {
                    if (!tokens.Contains(faces[i].GetToken(token)))
                    {
                        triangle.Vertex1 = (ushort)vertex_coordiantes.Count;
                        tokens.Add(faces[i].GetToken(0));
                        vertex_coordiantes.Add(vertex_coords[faces[i].vertex_indices[token] - 1]);
                        texture_coordinates.Add(faces[i].has_texcoord ? texture_coords[faces[i].texcoord_indices[token] - 1] : Vector2.Zero);
                        vertex_normals.Add(faces[i].has_normals ? normals[faces[i].normal_indices[token] - 1] : Vector3.Zero);
                    }
                    else
                    {
                        triangle.Vertex1 = (ushort)tokens.IndexOf(faces[i].GetToken(token));
                    }
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
            this.Coordinates = vertex_coordiantes.ToArray();
            this.TextureCoordinates = texture_coordinates.ToArray();
            this.Normals = vertex_normals.ToArray();

            this.Primitives = new MeshPrimitive[strips.Count];

            Log.Info("Parsing shader groups from triangle strips...");
            TriangleStrip combined_strip = new TriangleStrip() { Indices = new ushort[0] };
            ushort offset = 0;
            for (int i = 0; i < strips.Count; ++i)
            {
                TriangleStrip.Append(ref combined_strip, strips[i]);
                this.Primitives[i] = new MeshPrimitive(offset, (ushort)(combined_strip.Indices.Length - offset)) { shader_index = (ushort)i };
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
            this.Coordinates = new Vector3[float_array.Values.Length / 3];
            for (int i = 0; i < Coordinates.Length; i++)
            {
                Coordinates[i] = new Vector3(
                        (float)float_array.Values[i * 3 + 0],
                        (float)float_array.Values[i * 3 + 1],
                        (float)float_array.Values[i * 3 + 2]);
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
            this.Primitives = new MeshPrimitive[] { new MeshPrimitive(0, (ushort)Indices.Length) };
        }

        #endregion

        #region Export Methods

        public bool ExportAsWavefront(string filename)
        {
            using (StreamWriter writer = File.CreateText(filename))
            {
                writer.WriteLine("# moonfish 2013 : Wavefront OBJ");
                foreach (var vertex_coordinate in Coordinates)
                {
                    writer.WriteLine("v {0} {1} {2}", vertex_coordinate.X,
                        vertex_coordinate.Y, vertex_coordinate.Z);
                }
                foreach (var texture_coordinate in TextureCoordinates)
                {
                    writer.WriteLine("vt {0} {1}", texture_coordinate.X.ToString("#0.00000"),
                        texture_coordinate.Y.ToString("#0.00000"));
                }
                foreach (var normal in Normals)
                {
                    writer.WriteLine("vn {0} {1} {2}", normal.X.ToString("#0.00000"),
                        normal.Y.ToString("#0.00000"), normal.Z.ToString("#0.00000"));
                }
                bool winding_flag = true;
                for (var i = 0; i < Indices.Length - 2; ++i)
                {
                    if (IsDegenerate(Indices[i + 0], Indices[i + 1], Indices[i + 2]))
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
                CreateVector3Source(this.Coordinates, base_id, "positions"),
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

        #endregion
        
        /// <summary>
        /// Converts internal Mesh fields into Halo 2 compatible resource format
        /// </summary>
        /// <param name="resource_out">returns with array of Resource-meta structs represnting the blocks in the resource</param>
        /// <returns>array of bytes which holds the serialized Halo 2 resource</returns>
        public byte[] Serialize(out DResource[] resource_out, out DCompressionRanges compression_ranges, DCompressionRanges input_compression = null)
        {
            /* Intent: Write out the this Model instance data into a format that 
             * the halo 2 version of blam! engine can use.
             * The resources that we will be focusing on are ShaderGroups, Indices,
             * Vertex position, texcoord, tangent space vectors, and a simple bonemap.*/

            Log.Info(@"Entering Model.Serialize()");

            if (input_compression == null) { input_compression = GenerateCompressionData(); }     // Check that we have compression data available before continuing
            compression_ranges = input_compression;

            DResource[] resource = new DResource[7];                    // Create resource defintion array
            MemoryStream buffer = new MemoryStream();
            BinaryWriter bin = new BinaryWriter(buffer);                // BinaryWriter

            Log.Info(string.Format(@"Writing header_tag @{0}", bin.BaseStream.Position));
            bin.WriteFourCC("blkh");                // Write the header_tag value
            bin.Write(0);                           // [uint] resource_data_size  (reserve)

            // * Begin resource_header // size: 112

            Log.Info(string.Format(@"Writing shader_groups_count = {0} @{1}", Primitives.Length, bin.BaseStream.Position));
            bin.Write(Primitives.Length);           // * 0x00: shader_groups_count;
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
            resource[0] = new DResource(0, 72, Primitives.Length * 72, (int)(bin.BaseStream.Position - resource_data_start_offset));
            foreach (var group in Primitives)
                bin.Write(group);                // shader_group data

            bin.WriteFourCC("rsrc");             // indices resource begin
            resource[1] = new DResource(32, sizeof(ushort), sizeof(ushort) * this.Indices.Length, (int)(bin.BaseStream.Position - resource_data_start_offset));
            foreach (ushort index in this.Indices)  // write each index ushort
                bin.Write(index);                // pad to word boundary
            bin.WritePadding(4);

            bin.WriteFourCC("rsrc");             // vertex_data_header?
            resource[2] = new DResource(56, 32, 32 * 3, (int)(bin.BaseStream.Position - resource_data_start_offset));
            bin.Write(VERTEX_RESOURCE_HEADER_DATA); // laziness TODO: write out proper headers here to allow for other types

            bin.WriteFourCC("rsrc");
            resource[3] = new DResource(56, 0, this.Coordinates.Length * sizeof(ushort) * 3, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            {
                foreach (var vertex in this.Coordinates)
                {
                    bin.Write(Deflate(input_compression.X, vertex.X));
                    bin.Write(Deflate(input_compression.Y, vertex.Y));
                    bin.Write(Deflate(input_compression.Z, vertex.Z));
                }
                bin.WritePadding(4);
            }
            bin.WriteFourCC("rsrc");
            resource[4] = new DResource(56, 1, this.TextureCoordinates.Length * sizeof(ushort) * 2, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            {
                foreach (var vertex in this.TextureCoordinates)
                {
                    bin.Write(Deflate(input_compression.U, vertex.X));
                    bin.Write(Deflate(input_compression.V, vertex.Y));  // flip this still?
                }
            }
            bin.WriteFourCC("rsrc");
            resource[5] = new DResource(56, 2, this.Normals.Length * sizeof(uint) * 3, (int)(bin.BaseStream.Position - resource_data_start_offset), true);
            for(int i = 0; i < this.Normals.Length;++i)
            {
                bin.Write((uint)(Vector3t)Normals[i]);    //cast to vector3t is destructive...
                bin.Write((uint)(Vector3t)Tangents[i]);
                bin.Write((uint)(Vector3t)Bitangents[i]);
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

        public void Show()
        {
            QuickMeshView render_window = new QuickMeshView(this);
            render_window.Run(60);
        }

        public Vector3 Center { get; set; }
    }
}
