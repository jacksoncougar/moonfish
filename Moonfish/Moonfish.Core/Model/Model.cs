using Moonfish.Core.Definitions;
using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public class Model
    {
        public Region[] Regions;
        public Mesh[] Mesh;
        public DNode[] Nodes;

        public Model(model Tag)
        {
            Load(Tag);
        }
        void Load(model Tag)
        {
            Regions = Tag.Regions.Select(x => x.GetDefinition<Region>()).ToArray();
            for (int i = 0; i < Tag.Regions.Count; ++i)
            {
                Regions[i].Permutations = Tag.Regions[i].Permutations.Select(y => y.GetDefinition<DPermutation>()).ToArray();
            }

            Mesh = new Mesh[Tag.Sections.Count];
            for (int i = 0; i < Mesh.Length; ++i)
            {
                Mesh[i] = new Core.Model.Mesh();
                Mesh[i].Name = string.Format("{0}_{1}", "default", i);
                Mesh[i].Load(Tag.Sections[i].Raw, Tag.Sections[i].Resources, Tag.Compression[0]);
            }
            Nodes = Tag.Nodes.Select(x => x.GetDefinition<DNode>()).ToArray();
            var ranges = Tag.Compression[0].GetDefinition<DCompressionRanges>();
            Center = new Vector3(Range.Median(ranges.X), Range.Median(ranges.Y), Range.Median(ranges.Z));
        }

        public void Show()
        {
            ModelView render_window = new ModelView(this);
            render_window.Run(60);
        }

        Vector3 center_ = Vector3.Zero;
        public OpenTK.Vector3 Center { get; set; }

        Collada141.COLLADA GenerateCOLLADA()
        {
            Collada141.COLLADA collada = new Collada141.COLLADA();
            collada.version = Collada141.VersionType.Item141;
            collada.asset = new Collada141.asset()
            {
                contributor = new Collada141.assetContributor[] { 
                    new Collada141.assetContributor() { author = "", authoring_tool = "Moonfish 2013" }},
                created = DateTime.Now,
                modified = DateTime.Now,
                up_axis = Collada141.UpAxisType.Y_UP,
                unit = new Collada141.assetUnit() { meter = 1, name = "meter" }
            }; 

            collada.scene = new Collada141.COLLADAScene()
            {
                instance_visual_scene = new Collada141.InstanceWithExtra() { url = "#Scene" }
            };
            return collada;
        }

        public bool ExportNodesToCollada()
        {
            LoadCollada();
            Collada141.COLLADA collada = new Collada141.COLLADA();
            collada.version = Collada141.VersionType.Item141;
            collada.asset = new Collada141.asset()
            {
                contributor = new Collada141.assetContributor[] { 
                    new Collada141.assetContributor() { author = "", authoring_tool = "Moonfish 2013" }},
                created = DateTime.Now,
                modified = DateTime.Now,
                up_axis = Collada141.UpAxisType.Y_UP,
                unit = new Collada141.assetUnit() { meter = 1, name = "meter" }
            };

            var visual_scenes = new Collada141.library_visual_scenes();
            visual_scenes.visual_scene = new Collada141.visual_scene[]{
                new Collada141.visual_scene()
            };
            visual_scenes.visual_scene[0].node = GenerateColladaNodes();


            collada.Items = new object[] { 
                visual_scenes,
            };

            collada.scene = new Collada141.COLLADAScene()
            {
                instance_visual_scene = new Collada141.InstanceWithExtra() { url = "#Scene" }
            };
            collada.Save(@"D:\debug.dae");
            return false;
        }

        public bool LoadCollada()
        {
            Collada141.COLLADA collada = Collada141.COLLADA.Load(@"D:\halo_2\single_bone.dae");
            return false;
        }

        public void ExportToCOLLADA()
        {
            var COLLADA = GenerateCOLLADA();
            var geometry = new List<Collada141.geometry>(this.Regions.Length);
            foreach (var region in this.Regions)
            {
                geometry.Add(Mesh[region.Permutations[0].HighLOD].ExportAsCOLLADAGeometry());
            }
            var visual_scenes = new Collada141.library_visual_scenes();

            visual_scenes.visual_scene = new Collada141.visual_scene[]
            {
                new Collada141.visual_scene()
            };
            var instances = new List<Collada141.instance_geometry>();
            foreach (var g in geometry)
            {
                var new_instance =new Collada141.instance_geometry()
                {
                    url = string.Format("#{0}", g.id)
                };                
            }
            visual_scenes.visual_scene[0].node = new Collada141.node[]
            {
                new Collada141.node()
                {
                    instance_geometry = instances.ToArray(),
                }
            };

            COLLADA.Items = new object[] {
                new Collada141.library_geometries
                { 
                    geometry = geometry.ToArray(),
                },
                visual_scenes,
            };
            COLLADA.Save(@"D:\debug_mesh.dae");
        }

        private Collada141.node[] GenerateColladaNodes()
        {
            List<Collada141.node> collada_nodes = new List<Collada141.node>();
            collada_nodes.Add(GenerateColladaNode(this.Nodes[0]));
            return collada_nodes.ToArray();
        }

        private Collada141.node GenerateColladaNode(DNode dNode)
        {
            var mat = Matrix4.Translation(dNode.Position);
            mat *= Matrix4.Rotate(dNode.Rotation);
            //Root node
            Collada141.node node = new Collada141.node()
            {
                name = "Armature",
                id = "Armature",
                type = Collada141.NodeType.NODE,
                Items = new object[] { 
                    new Collada141.TargetableFloat3() { sid = "location", Values = new double[]{ dNode.Position.X,  dNode.Position.Y,  dNode.Position.Z } }, 
                    new Collada141.rotate(){ sid = "rotationX", Values =new double[]{ 1f, 0f, 0f } },
                    new Collada141.rotate(){ sid = "rotationY", Values =new double[]{ 0f, 1f, 0f } },
                    new Collada141.rotate(){ sid = "rotationZ", Values =new double[]{ 0f, 0f, 1f } },
                    new Collada141.TargetableFloat3() { sid = "scale", Values =new double[] { 1f, 1f, 1f} }, 
                },
                ItemsElementName = new Collada141.ItemsChoiceType2[]{
                    Collada141.ItemsChoiceType2.translate,
                    Collada141.ItemsChoiceType2.rotate,
                    Collada141.ItemsChoiceType2.rotate,
                    Collada141.ItemsChoiceType2.rotate,
                    Collada141.ItemsChoiceType2.scale
                },
                node1 = ParseNode(dNode)
            };
            return node;
        }

        private Collada141.node[] ParseNode(DNode dNode)
        {
            Matrix4 matrix = Matrix4.CreateTranslation(dNode.Position);

            List<Collada141.node> nodes = new List<Collada141.node>();

            if (dNode.NextSibling_NodeIndex != -1) nodes.AddRange(ParseNode(this.Nodes[dNode.NextSibling_NodeIndex]));

            var location = dNode.Position;
            var rotation = Matrix4.Rotate(dNode.Rotation);


            var current_node = new Collada141.node()
            {
                type = Collada141.NodeType.JOINT,
                name = "bone",
                sid = "bone",
                Items = new object[]{
                    new Collada141.TargetableFloat3() { sid = "location", Values = dNode.Position.ToArray() }, 
                    new Collada141.matrix(){ sid = "rotation", Values = Matrix4ToFloatArray(rotation) },
                    new Collada141.TargetableFloat3() { sid = "scale", Values =new double[] { 1f, 1f, 1f} }, 
                },
                ItemsElementName = new Collada141.ItemsChoiceType2[]{
                    Collada141.ItemsChoiceType2.translate,
                    Collada141.ItemsChoiceType2.matrix,
                    Collada141.ItemsChoiceType2.scale
                }
            };

            if (dNode.FirstChild_NodeIndex != -1) current_node.node1 = ParseNode(this.Nodes[dNode.FirstChild_NodeIndex]);

            nodes.Add(current_node);

            return nodes.ToArray();
        }

        private double[] Matrix4ToFloatArray(Matrix4 tansformation)
        {
            return new double[]{
                tansformation.M11, tansformation.M21, tansformation.M31, tansformation.M41,
                tansformation.M12, tansformation.M22, tansformation.M32, tansformation.M42,
                tansformation.M13, tansformation.M23, tansformation.M33, tansformation.M43,
                tansformation.M14, tansformation.M24, tansformation.M34, tansformation.M44,
            };
        }
    }
}