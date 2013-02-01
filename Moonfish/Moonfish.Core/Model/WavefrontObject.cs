using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
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
