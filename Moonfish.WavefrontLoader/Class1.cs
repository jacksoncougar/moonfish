using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Moonfish.WavefrontLoader
{
    public class WavefrontOBJ
    {
        int TextureCoordCount;
        int VertexCount;
        int NormalCount;

        float[] Vertices;
        float[] TextureCoords;
        float[] Normals;
        Object[] Objects;
        Face[] Faces;

        public void Parse(string filename)
        {
            byte[] buffer = File.ReadAllBytes(filename);
            StreamReader reader = new StreamReader(new MemoryStream(buffer));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine().Trim();
                ParseLine(line);
            }
        }

        private void ParseLine(string line)
        {
            var tokens = line.Split(' ');
            if (ElementTypes.ContainsKey(tokens[0]))
            {
                var element = Activator.CreateInstance(ElementTypes[tokens[0]]) as WavefrontElementBase;
                element.Parse(line);
            }
        }

        static Dictionary<string, Type> ElementTypes;
        static WavefrontOBJ()
        {
            var elements = Assembly.GetExecutingAssembly().GetTypes().Where(x=>x.BaseType==typeof(WavefrontElementBase));

            ElementTypes = new Dictionary<string, Type>(elements.Count());
            foreach (var item in elements)
            {
                var item_instance = Activator.CreateInstance(item) as WavefrontElementBase;
                ElementTypes.Add(item_instance.Token, item);
            }
        }

        internal abstract class WavefrontElementBase
        {
            internal abstract string Token { get; }
            internal abstract void Parse(string line);
        }

        internal class Normal : WavefrontElementBase
        {
            internal override string Token { get { return "vn"; } }
            internal override void Parse(string line)
            {
                var items = line.Split(' ');
                X = Single.Parse(items[1]);
                Y = Single.Parse(items[2]);
                Z = Single.Parse(items[3]);
            }
            internal float X { get; set; }
            internal float Y { get; set; }
            internal float Z { get; set; }

            public override string ToString()
            {
                return string.Format("{0} {1} {2} {3}", Token, X,Y,Z);
            }
        }
        internal class TextureCoordinate : WavefrontElementBase
        {
            internal override string Token { get { return "vt"; } }
            internal float U { get; set; }
            internal float V { get; set; }
            internal float W { get; set; }
            internal bool HasW { get; set; }

            internal override void Parse(string line)
            {
                var items = line.Split(' ');
                U = Single.Parse(items[1]);
                V = Single.Parse(items[2]);
                if (items.Length > 3)
                {
                    W = Single.Parse(items[3]);
                    HasW = true;
                }
                else HasW = false;
            }

            public override string ToString()
            {
                return string.Format("{0} {1} {2} {3}", Token,
                    U, V, HasW ? W.ToString() : string.Empty);
            }
        }
        internal class Vertex : WavefrontElementBase
        {
            internal override string Token { get { return "v"; } }
            internal float X { get; set; }
            internal float Y { get; set; }
            internal float Z { get; set; }

            internal override void Parse(string line)
            {
                var items = line.Split(' ');
                X = Single.Parse(items[1]);
                Y = Single.Parse(items[2]);
                Z = Single.Parse(items[3]);
            }

            public override string ToString()
            {
                return string.Format("{0} {1} {2} {3}", Token,
                    string.Format("F5", X), string.Format("F5", Y), string.Format("F5", Z));
            }
        }
        internal class Object : WavefrontElementBase
        {
            internal override string Token { get { return "o"; } }

            public string Name { get; set; }
            public int StartIndex { get; set; }
            public int Count { get; set; }

            internal override void Parse(string line)
            {
                var items = line.Split(' ');
                Name = items[1];
            }

            public override string ToString()
            {
                return string.Format("o {3} #{0} : {1}", StartIndex, Count, Name);
            }
        }
        internal class Face : WavefrontElementBase
        {
            internal override string Token { get { return "f"; } }

            public int[] Indices;

            public bool HasTexcoords;
            public bool HasNormals;

            public int Stride { get { return 1 + (HasTexcoords ? 1 : 0) + (HasNormals ? 1 : 0); } }
            public int PolygonEdgeCount;

            internal override void Parse(string line)
            {
                var items = line.Split(' ');
                PolygonEdgeCount = items.Length - 1;
                
                var parts = items[1].Split('/');
                if (parts.Length > 1 && !String.IsNullOrEmpty(parts[1]))
                    HasTexcoords = true;
                if (parts.Length > 2 && !String.IsNullOrEmpty(parts[2]))
                    HasNormals = true;

                this.Indices = new int[PolygonEdgeCount * Stride];
                for (int i = 0; i < PolygonEdgeCount; ++i)
                {
                    var indices = items[i + 1].Split('/');
                    this.Indices[i * Stride + 0] = int.Parse(indices[0]);
                    if (HasTexcoords)
                        this.Indices[i * Stride + 1] = int.Parse(indices[1]);
                    if (HasNormals)
                        this.Indices[i * Stride + 2] = int.Parse(indices[2]);
                }
            }

            public static bool TryParse(string line, out Face face)
            {
                face = new Face();
                line.Trim();                                                        /* remove white space from both ends, 
                                                                                    * string should now be in format: "f x1/y1/z1" + " xn/yn/zn" 
                                                                                    * of a range of values where y, or z are optional */

                string[] tokens = line.Split(' ');                                  // split on whitespace to get "f" + each "x/y/z" token
                face.PolygonEdgeCount = tokens.Length - 1;
                if (face.PolygonEdgeCount == 3)
                {
                    face.Indices = new int[face.PolygonEdgeCount];                               //initialize arrays
                    //face.texcoord_indices = new int[face.PolygonEdgeCount];
                    //face.normal_indices = new int[face.PolygonEdgeCount];
                    face.HasTexcoords = false;
                    face.HasNormals = false;
                    for (var i = 1; i <= face.PolygonEdgeCount; ++i)
                    {
                        string[] indices = tokens[i].Trim().Split('/');             //retrieve index tokens

                        int v, t, n;
                        switch (indices.Length)
                        {
                            case 1:
                                if (!int.TryParse(indices[0], out v)) return false;          // try to parse the vertex token, its fatal if this fails
                                else face.Indices[i - 1] = v;
                                break;
                            case 2:
                                if (int.TryParse(indices[1], out t))
                                {
                                    //face.texcoord_indices[i - 1] = t;
                                    face.HasTexcoords = true;
                                }
                                goto case 1;
                            case 3:
                                if (int.TryParse(indices[2], out n))
                                {
                                    //face.normal_indices[i - 1] = n;
                                    face.HasNormals = true;
                                }
                                goto case 2;
                            default: return false;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("WavefrontObject: Input mesh must be triangulated first! Exiting.");
                    return false;
                }
                return true;
            }
            public override string ToString()
            {
                StringBuilder builder = new StringBuilder("f ");
                var type = (HasTexcoords ? 2 : 0) + (HasNormals ? 1 : 0);
                for (int i = 0; i < PolygonEdgeCount * Stride; ++i)
                {
                    switch (type)
                    {
                        case 0:
                            builder.AppendFormat("{0} ", Indices[i]);
                            break;
                        case 1:
                            builder.AppendFormat("{0}//{2} ", Indices[i], Indices[i+1]);
                            break;
                        case 2:
                            builder.AppendFormat("{0}/{1} ", Indices[i], Indices[i+1]);
                            break;
                        case 3:
                            builder.AppendFormat("{0}/{1}/{2} ", Indices[i], Indices[i + 1], Indices[i + 2]);
                            break;
                    }
                }
                return builder.ToString();
            }

            internal string GetToken(int index)
            {
                return string.Format("{0}:{1}:{2}", Indices[index], Indices[index + 0], Indices[index + 1]);
            }
        }
    }
}
