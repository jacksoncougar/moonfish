using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
//FUCK THIS RIGHT HERE.
namespace Moonfish.Core.Model.Wavefront
{
    internal static class WavefrontExtensions
    {
        public static bool TryParseVector3(out Vector3 vector3, string line)
        {
            /*  Target format string:
             *  "token " + " " + "#0.0000#" + " " + "#0.0000#" + " " + "#0.0000#"
             *  and the componants are stored as a float parseable string delimited by whitespace             * 
             * */
            string[] items = line.Split(' ');                               
            float x = 0, y = 0, z = 0;                                      // initialize
            var status = true;                                              // setup begin simple error handling         
            if (items.Length == 4)
            {
                if (!float.TryParse(items[1], out x)) status = false;
                if (!float.TryParse(items[2], out y)) status = false;
                if (!float.TryParse(items[3], out z)) status = false;
            }
            else status = false;
            if (!status)
            {
                Log.Error(string.Format("Error parsing line: {0}", line));  // we made a logger, use it
                vector3 = Vector3.Zero;
                return false;
            }
            vector3 = new Vector3(x, y, z);
            return true;
        }
        public static bool TryParseVector2(out Vector2 vector2, string line)
        {
            /*  Target format string:
             *  "token " + " " + "#0.0000#" + " " + "#0.0000#"
             *  and the componants are stored as a float parseable string delimited by whitespace             * 
             * */
            string[] items = line.Split(' ');
            float x = 0, y = 0;                                             // initialize
            var status = true;                                              // setup begin simple error handling         
            if (items.Length == 3)
            {
                if (!float.TryParse(items[1], out x)) status = false;
                if (!float.TryParse(items[2], out y)) status = false;
            }
            else status = false;
            if (!status)
            {
                Log.Error(string.Format("Error parsing line: {0}", line));  // we made a logger, use it
                vector2 = Vector2.Zero;
                return false;
            }
            vector2 = new Vector2(x, y);
            return true;
        }
    }

    internal class WavefrontOBJ
    {
        int TextureCoordCount;
        int VertexCount;
        int NormalCount;

        float[] Vertices;
        float[] TextureCoords;
        float[] Normals;

        public bool Load(string filename)
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

            Object[] objects = new Object[1] { new Object() { faces_start_index = 0 } };
            List<Face> faces = new List<Face>();
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
            return false;
        }

        internal struct Object
        {
            public int faces_start_index;
            public int faces_count;

            public override string ToString()
            {
                return string.Format("o default #{0} : {1}", faces_start_index, faces_count);
            }
        }
        internal struct Face
        {
            public int[] vertex_indices;
            public int[] texcoord_indices;
            public int[] normal_indices;
            public bool has_texcoord;
            public bool has_normals;
            public int length;
            public int material_id;

            public static bool TryParse(string line, out Face face)
            {
                face = new Face();
                line.Trim();                                                        /* remove white space from both ends, 
                                                                                    * string should now be in format: "f x1/y1/z1" + " xn/yn/zn" 
                                                                                    * of a range of values where y, or z are optional */

                string[] tokens = line.Split(' ');                                  // split on whitespace to get "f" + each "x/y/z" token
                face.length = tokens.Length - 1;
                if (face.length == 3)
                {
                    face.vertex_indices = new int[face.length];                               //initialize arrays
                    face.texcoord_indices = new int[face.length];
                    face.normal_indices = new int[face.length];
                    face.has_texcoord = false;
                    face.has_normals = false;
                    for (var i = 1; i <= face.length; ++i)
                    {
                        string[] indices = tokens[i].Trim().Split('/');             //retrieve index tokens

                        int v, t, n;
                        switch (indices.Length)
                        {
                            case 1:
                                if (!int.TryParse(indices[0], out v)) return false;          // try to parse the vertex token, its fatal if this fails
                                else face.vertex_indices[i - 1] = v;
                                break;
                            case 2:
                                if (int.TryParse(indices[1], out t))
                                {
                                    face.texcoord_indices[i - 1] = t;
                                    face.has_texcoord = true;
                                }
                                goto case 1;
                            case 3:
                                if (int.TryParse(indices[2], out n))
                                {
                                    face.normal_indices[i - 1] = n;
                                    face.has_normals = true;
                                }
                                goto case 2;
                            default: return false;
                        }
                    }
                }
                else
                {
                    Log.Error("WavefrontObject: Input mesh must be triangulated first! Exiting.");
                    return false;
                }
                return true;
            }
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder("f ");
                int display_mode = has_texcoord ? 2 : 0;
                display_mode += has_normals ? 1 : 0;
                for (int i = 0; i < length; ++i)
                {
                    switch (display_mode)
                    {
                        case 0:
                            builder.AppendFormat("{0} ", vertex_indices[i]);
                            break;
                        case 1:
                            builder.AppendFormat("{0}//{2} ", vertex_indices[i], normal_indices[i]);
                            break;
                        case 2:
                            builder.AppendFormat("{0}/{1} ", vertex_indices[i], texcoord_indices[i]);
                            break;
                        case 3:
                            builder.AppendFormat("{0}/{1}/{2} ", vertex_indices[i], texcoord_indices[i], normal_indices[i]);
                            break;
                    }
                }
                return builder.ToString();
            }

            internal string GetToken(int index)
            {
                return string.Format("{0}:{1}:{2}", vertex_indices[index], texcoord_indices[index], normal_indices[index]);
            }
        }
    }
}
