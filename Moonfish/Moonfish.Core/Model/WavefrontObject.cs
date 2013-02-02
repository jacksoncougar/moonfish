using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model.Wavefront
{
    public static class WavefrontExtensions
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
    internal class WavefrontObject
    {
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
            bool has_texcoord;
            bool has_normals;
            public int length;

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
        }
    }
}
