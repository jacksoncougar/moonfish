using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Moonfish.Core
{
    [TagClass("mode")]
    public class model : TagBlock
    {
        public TagBlockList<Section> Sections { get { return this.fixed_fields[3].Object as TagBlockList<Section>; } }
        public BoundingBox GetBoundingBox() { return (this.fixed_fields[1].Object as TagBlockList<BoundingBox>)[0]; }
        public model()
            : base(132, new TagBlockField[]{
            new TagBlockField(new string_id()),
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
            public Moonfish.Core.Raw.CompressionRanges GetCompressionRanges()
            {
                BinaryReader binary_reader = new BinaryReader(this.GetMemory());
                return new Raw.CompressionRanges(
                     x: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     y: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     z: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     u1: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     v1: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     u2: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle()),
                     v2: new Raw.Range(binary_reader.ReadSingle(), binary_reader.ReadSingle())
                    );
            }
            public BoundingBox() : base(56) { }
        }
        public class Region : TagBlock
        {
            public Region()
                : base(16, new TagBlockField[]{
            new TagBlockField(new string_id()),
            new TagBlockField(null, 4),
            new TagBlockField(new TagBlockList<Permutation>()),
            }) { }

            public class Permutation : TagBlock
            {
                public Permutation() : base(16, new TagBlockField(new string_id())) { }
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
            IEnumerable<Resource> Resources { get { return base.fixed_fields[1].Object as IEnumerable<Resource>; } }
            public Model.Mesh.Resource[] GetSectionResources()
            {
                var resources = this.Resources.ToArray();
                Model.Mesh.Resource[] section_resources = new Model.Mesh.Resource[resources.Length];
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
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0() : base(88) { }
            }
            public class Resource : TagBlock
            {
                public Model.Mesh.Resource GetResource()
                {
                    return new Model.Mesh.Resource(this.GetMemory().ToArray());
                }
                public Resource() : base(16) { }
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

            public class CompoundNode : TagBlock
            {
                public CompoundNode() : base(16) { }
            }
        }
        public class Node : TagBlock
        {
            public Node()
                : base(96,
                    new TagBlockField(new string_id())
                    ) { }
        }
        public class MarkerGroup : TagBlock
        {
            public MarkerGroup()
                : base(12,
                    new TagBlockField(new string_id()),
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
                : base(8,
                    new TagBlockField(new tag_pointer()),
                    new TagBlockField(new tag_pointer()),
                    new TagBlockField(new TagBlockList<tagblock1_0>())
                    ) { }
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