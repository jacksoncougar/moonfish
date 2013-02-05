using Moonfish.Core.Model;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Moonfish.Core
{
    [TagClass("mode")]
    public class model : TagBlock
    {
        public TagBlockList<BoundingBox> Compression { get { return this.fixed_fields[1].Object as TagBlockList<BoundingBox>; } }
        public TagBlockList<Region> Regions { get { return this.fixed_fields[2].Object as TagBlockList<Region>; } }
        public TagBlockList<Section> Sections { get { return this.fixed_fields[3].Object as TagBlockList<Section>; } }
        public TagBlockList<Group> Groups { get { return this.fixed_fields[5].Object as TagBlockList<Group>; } }
        public TagBlockList<Node> Nodes { get { return this.fixed_fields[6].Object as TagBlockList<Node>; } }
        public TagBlockList<MarkerGroup> MarkerGroups { get { return this.fixed_fields[7].Object as TagBlockList<MarkerGroup>; } }
        public TagBlockList<Shader> Shaders { get { return this.fixed_fields[8].Object as TagBlockList<Shader>; } }

        public BoundingBox GetBoundingBox() { return (this.fixed_fields[1].Object as TagBlockList<BoundingBox>)[0]; }

        public model()
            : base(132, new TagBlockField[]{
            new TagBlockField(new StringID()),
            new TagBlockField(null, 16),
            new TagBlockField(new TagBlockList<BoundingBox>()),
            new TagBlockField(new TagBlockList<Region>()),
            new TagBlockField(new TagBlockList<Section>()),
            new TagBlockField(new TagBlockList<tagblock0_3>()),
            new TagBlockField(new TagBlockList<Group>()),
            new TagBlockField(null, 12),
            new TagBlockField(new TagBlockList<Node>()),
            new TagBlockField(null, 8),
            new TagBlockField(new TagBlockList<MarkerGroup>()),
            new TagBlockField(new TagBlockList<Shader>()),
            new TagBlockField(null, 12),
            new TagBlockField(new TagBlockList<tagblock0_8>()),
            new TagBlockField(new TagBlockList<tagblock0_9>()),
            }) { }

        public class BoundingBox : TagBlock
        {
            public BoundingBox() : base(56) { }

            public BoundingBox(Definitions.DCompressionRanges compression_ranges)
                : this()
            {
                // TODO: Complete member initialization
                SetDefinitionData(compression_ranges);
            }
        }
        public class Region : TagBlock
        {
            public TagBlockList<Permutation> Permutations { get { return this.fixed_fields[1].Object as TagBlockList<Permutation>; } }

            public Region()
                : base(16, new TagBlockField[]{
            new TagBlockField(new StringID()),
            new TagBlockField(null, 4),
            new TagBlockField(new TagBlockList<Permutation>()),
            }) { }

            public Region(Definitions.DRegion dRegion)
                : this()
            {
                this.SetDefinitionData(dRegion);
            }

            public class Permutation : TagBlock
            {
                public Permutation() : base(16, new TagBlockField(new StringID())) { }
            }
        }
        public class Section : TagBlock
        {
            public ModelPointer GetRawPointer()
            {
                BinaryReader binary_reader = new BinaryReader(this.GetMemory());
                binary_reader.BaseStream.Position = 56;
                return new ModelPointer() { Address = binary_reader.ReadInt32(), Length = binary_reader.ReadInt32() };
            }
            public TagBlockList<Resource> Resources { get { return base.fixed_fields[1].Object as TagBlockList<Resource>; } }
            public Model.Mesh.DResource[] GetSectionResources()
            {
                var resources = this.Resources.ToArray();
                Model.Mesh.DResource[] section_resources = new Model.Mesh.DResource[resources.Length];
                for (int i = 0; i < section_resources.Length; ++i)
                {
                    section_resources[i] = resources[i].GetResource();
                }
                return section_resources;
            }
            public struct ModelPointer
            {
                public int Address;
                public int Length;
            }

            public Section()
                : base(92, new TagBlockField[]{
                new TagBlockField(null, 48),
            new TagBlockField(new TagBlockList<tagblock1_0>()),
            /*raw here*/new TagBlockField(null, 8),     
            new TagBlockField(null, 8),         
            new TagBlockField(new TagBlockList<Resource>()),
            new TagBlockField(new tag_id()),
            }) { }

            public Section(Definitions.DSection dSection)
                : this()
            {
                // TODO: Complete member initialization
                this.SetDefinitionData(dSection);
            }
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0() : base(88) { }
            }
            public class Resource : TagBlock
            {
                public Model.Mesh.DResource GetResource()
                {
                    return new Model.Mesh.DResource(this.GetMemory().ToArray());
                }
                public Resource() : base(16) { }

                public Resource(Mesh.DResource dResource):this()
                {
                    this.SetDefinitionData(dResource);
                }
            }
        }
        public class tagblock0_3 : TagBlock
        {
            public tagblock0_3() : base(4) { }
        }
        public class Group : TagBlock
        {

            public Group()
                : base(12,
                    new TagBlockField(null, 4),
                    new TagBlockField(new TagBlockList<CompoundNode>())
                    ) { }

            public Group(Definitions.DGroup dGroup)
                : this()
            {
                // TODO: Complete member initialization
                this.SetDefinitionData(dGroup);
            }

            public class CompoundNode : TagBlock
            {
                public CompoundNode() : base(16) { }
            }
        }
        public class Node : TagBlock
        {
            public Node()
                : base(96,
                    new TagBlockField(new StringID())
                    ) { }

            public Node(Definitions.DNode dNode)
                : this()
            {
                this.SetDefinitionData(dNode);
            }
        }
        public class MarkerGroup : TagBlock
        {
            public MarkerGroup()
                : base(12,
                    new TagBlockField(new StringID()),
                    new TagBlockField(new TagBlockList<Marker>())
                    ) { }
            public class Marker : TagBlock
            {
                public Marker() : base(36) { }
            }
        }
        public class Shader : TagBlock
        {
            public Shader()
                : base(32,
                    new TagBlockField(new tag_pointer()),
                    new TagBlockField(new tag_pointer()),
                    new TagBlockField(new TagBlockList<tagblock1_0>())
                    ) { }

            public Shader(Definitions.DShader dShader)
                : this()
            {
                this.SetDefinitionData(dShader);
            }
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0() : base(8) { }
            }
        }
        public class tagblock0_8 : TagBlock
        {
            public tagblock0_8()
                : base(88,
                    new TagBlockField(null, 20),
                    new TagBlockField(new TagBlockList<tagblock1_0>()),
                    new TagBlockField(new TagBlockList<tagblock1_1>()),
                    new TagBlockField(null, 16),
                    /*unk raw here*/new TagBlockField(null, 16),
                    new TagBlockField(null, 8),
                    new TagBlockField(new TagBlockList<tagblock1_2>()),
                    new TagBlockField(new tag_id())
                    ) { }
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0()
                    : base(12,
                        new TagBlockField(null, 4),
                        new TagBlockField(new TagBlockList<tagblock2_0>())
                        ) { }
                public class tagblock2_0 : TagBlock
                {
                    public tagblock2_0() : base(8) { }
                }
            }
            public class tagblock1_1 : TagBlock
            {
                public tagblock1_1() : base(4) { }
            }
            public class tagblock1_2 : TagBlock
            {
                public tagblock1_2() : base(16) { }
            }
        }
        public class tagblock0_9 : TagBlock
        {
            public tagblock0_9()
                : base(8,
                    new TagBlockField(new TagBlockList<tagblock1_0>())
                    ) { }
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0()
                    : base(16,
                        new TagBlockField(new TagBlockList<tagblock2_0>()),
                        new TagBlockField(new TagBlockList<tagblock2_1>())
                        ) { }
                public class tagblock2_0 : TagBlock
                {
                    public tagblock2_0() : base(8) { }
                }
                public class tagblock2_1 : TagBlock
                {
                    public tagblock2_1() : base(8) { }
                }
            }
        }
    }
}