using System.Text;
using System.IO;
using System;
using System.Collections.Generic;
using Moonfish.Core.Model;
using System.Linq;

namespace Moonfish.Core
{
    /// <summary>
    /// A minimalist class to load essential data which can be used to parse a retail cache map.
    /// </summary>
    public class MapStream : FileStream, IReferenceList<string, StringID>, IReferenceList<tag_info, tag_id>
    {
        //NAME
        /// <summary>
        /// name of this cache (is not used in anything, just compiled into the header)
        /// </summary>
        public readonly string MapName;
        //SCENARIO
        /// <summary>
        /// path of the scenario (local directory path storing the resources of this map when decompiled)
        /// </summary>
        public readonly string Scenario;
        //MAGICS
        /// <summary>
        /// magic values are used to convert from pre-calculated memory pointers to file-addresses
        /// </summary>
        public readonly int PrimaryMagic;
        public readonly int SecondaryMagic;
        //HEADER
        //INDEX
        //UNICODE
        //STRINGS
        public readonly UnicodeValueNamePair[] Unicode;
        public readonly string[] Paths;
        public readonly string[] Strings;
        public readonly tag_info[] Tags;

        public MapStream(string filename)
            : base(filename, FileMode.Open, FileAccess.Read)
        {
            //HEADER
            BinaryReader binReader = new BinaryReader(this, Encoding.UTF8);
            this.Seek(16, SeekOrigin.Begin);

            int indexAddress = binReader.ReadInt32();
            int indexLength = binReader.ReadInt32();

            this.Seek(336, SeekOrigin.Current);

            int stringTableLength = binReader.ReadInt32();
            this.Seek(4, SeekOrigin.Current);
            int stringTableAddress = binReader.ReadInt32();

            this.Seek(36, SeekOrigin.Current);

            MapName = binReader.ReadFixedString(32);

            this.Seek(4, SeekOrigin.Current);

            Scenario = binReader.ReadFixedString(256);

            this.Seek(4, SeekOrigin.Current);
            int pathsCount = binReader.ReadInt32();
            int pathsTableAddress = binReader.ReadInt32();
            int pathsTableLength = binReader.ReadInt32();

            this.Seek(pathsTableAddress, SeekOrigin.Begin);
            Paths = Encoding.UTF8.GetString(binReader.ReadBytes(pathsTableLength - 1)).Split(char.MinValue);

            //STRINGS

            this.Seek(stringTableAddress, SeekOrigin.Begin);
            Strings = Encoding.UTF8.GetString(binReader.ReadBytes(stringTableLength - 1)).Split(char.MinValue);


            //INDEX
            this.Seek(indexAddress, SeekOrigin.Begin);
            int tagClassTableVirtualAddress = binReader.ReadInt32();
            this.Seek(4, SeekOrigin.Current);
            int tagDatumTableVirtualAddress = binReader.ReadInt32();
            int tagDatumTableOffset = tagDatumTableVirtualAddress - tagClassTableVirtualAddress;
            this.Seek(12, SeekOrigin.Current);
            int tagDatumCount = binReader.ReadInt32();

            this.Seek(4 + tagDatumTableOffset, SeekOrigin.Current);
            Tags = new tag_info[tagDatumCount];
            for (int i = 0; i < tagDatumCount; i++)
            {
                Tags[i] = new tag_info()
                {
                    Type = binReader.ReadTagType(),
                    Id = binReader.ReadInt32(),
                    VirtualAddress = binReader.ReadInt32(),
                    Length = binReader.ReadInt32()
                };
            }

            //UNICODE
            SecondaryMagic = Tags[0].VirtualAddress - (indexAddress + indexLength);
            this.Seek(Tags[0].VirtualAddress - SecondaryMagic + 400, SeekOrigin.Begin);
            int unicodeCount = binReader.ReadInt32();
            int unicodeTableLength = binReader.ReadInt32();
            int unicodeIndexAddress = binReader.ReadInt32();
            int unicodeTableAddress = binReader.ReadInt32();

            Unicode = new UnicodeValueNamePair[unicodeCount];

            StringID[] strRefs = new StringID[unicodeCount];
            int[] strOffsets = new int[unicodeCount];

            this.Seek(unicodeIndexAddress, SeekOrigin.Begin);
            for (int i = 0; i < unicodeCount; i++)
            {
                strRefs[i] = (StringID)binReader.ReadInt32();
                strOffsets[i] = binReader.ReadInt32();
            }
            for (int i = 0; i < unicodeCount; i++)
            {
                this.Seek(unicodeTableAddress + strOffsets[i], SeekOrigin.Begin);
                StringBuilder unicodeString = new StringBuilder(byte.MaxValue);
                while (binReader.PeekChar() != char.MinValue)
                    unicodeString.Append(binReader.ReadChar());
                Unicode[i] = new UnicodeValueNamePair { Name = strRefs[i], Value = unicodeString.ToString() };
            }
        }

        public override long Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                // Is this value a memory_pointer? If so we need to convert it to a file_pointer:
                if (value < 0)
                {
                    base.Position = (int)value - SecondaryMagic;
                    return;
                }
                else
                {
                    base.Position = (int)value;
                    return;
                }
            }
        }

        tag_info GetOwner(int memory_address)
        {
            foreach (var tag in Tags)
            {
                if (memory_address >= tag.VirtualAddress - SecondaryMagic && memory_address < (tag.VirtualAddress - SecondaryMagic) + tag.Length)
                    return tag;
            }
            throw new Exception();
        }

        public void SerializeTag(tag_info meta)
        {
            string name = Paths[meta.Id.Index];
            TagBlock block = Halo2.CreateInstance(meta.Type);
            (block as IPointable).Address = meta.VirtualAddress;
            Memory memory = GetTagMemory(meta);
            (block as IPointable).Parse(memory);
            for (int i = 0; i < memory.instance_table.Count; ++i)
            {
                if (memory.instance_table[i].external && !memory.instance_table[i].isnull)
                {
                    throw new Exception(":D \n\n\n\n\n");
                }
            }
            MemoryStream raw_data = new MemoryStream();
            if (meta.Type == (tag_class)"bitm")
            {
                Bitmap_Collection collection = (Bitmap_Collection)block;
                foreach (var bitmap in collection.Bitmaps)
                {
                    var resource = bitmap.get_resource();
                    BinaryReader bin_reader = new BinaryReader(this);
                    bin_reader.BaseStream.Position = resource.offset0;
                    raw_data.Write(bin_reader.ReadBytes(resource.length0), 0, resource.length0);
                }
            } 
            //if (meta.Type == (tag_class)"mode")
            //{
            //    model collection = (model)block;
            //    foreach (var bitmap in collection.Sections)
            //    {
            //        var resource = bitmap.GetRawPointer();
            //        BinaryReader bin_reader = new BinaryReader(this);
            //        bin_reader.BaseStream.Position = resource.Address;
            //        raw_data.Write(bin_reader.ReadBytes(resource.Length), 0, resource.Length);
            //        {//because
            //            {//fuck you
            //                Mesh mesh = new Mesh();
            //                mesh.Load(raw_data.ToArray(), bitmap.GetSectionResources(), collection.GetBoundingBox().GetCompressionRanges());
            //                //mesh.ExportAsWavefront(@"D:\halo_2\wavefront.obj");
            //            }
            //        }
            //    }
            //}
           
            memory = memory.Copy(16);//setup for local
            using (FileStream output = File.Create(@"D:\halo_2\shad.bin"))
            {
                BinaryWriter binary_writer = new BinaryWriter(output);
                binary_writer.Write((int)meta.Type);
                binary_writer.Write((int)meta.Id);
                binary_writer.Write((int)(Padding.GetCount(memory.Length) + memory.Length));
                binary_writer.Write((int)raw_data.Length);
                binary_writer.Write(memory.ToArray());
                binary_writer.Write(new byte[Padding.GetCount(output.Position)]);
                binary_writer.Write(raw_data.ToArray());
                binary_writer.Write(new byte[Padding.GetCount(output.Position)]);
            }
        }

        public Memory GetTagMemory(tag_info meta)
        {
            //const int tag_num = 4886;
            BinaryReader bin_reader = new BinaryReader(this);
            this.Position = meta.VirtualAddress;
            Memory mem = new Memory(bin_reader.ReadBytes(meta.Length), meta.VirtualAddress);
            TagBlock block = Halo2.CreateInstance(meta.Type);
            mem.instance_table.Add(new Memory.mem_ref() { address = meta.VirtualAddress, client = block, count = 1, external = false, type = block.GetType() });
            (block as IPointable).Address = meta.VirtualAddress;
            (block as IPointable).Parse(mem);
            //mem = mem.Copy(0);
            return mem;
        }

        //public TagBlockWrapper PreProcessTag(tag_info tag)
        //{
        //    // Create a new Tagblock instance 
        //    TagBlock item = Halo2.CreateInstance(tag.Type);
        //    // Set the stream position to this virtual offset;
        //    this.Position = tag.VirtualAddress;
        //    // Deserialize the tag data
        //    StaticBenchmark.Begin();
        //    var serializeable_interface = (item as ISerializable);
        //    serializeable_interface.Deserialize(this, new Segment((int)Position, tag.Length));
        //    StaticBenchmark.End();
        //    TagBlockWrapper tag_wrapper = new TagBlockWrapper(tag.Type, tag.Id, item);

        //    var string_reference_interface = (item as IReferenceable<string, string_id>);
        //    string_reference_interface.CopyReferences(this, tag_wrapper);

        //    var tag_reference_interface = (item as IReferenceable<tag_info, tag_id>);
        //    tag_reference_interface.CopyReferences(this, tag_wrapper);
        //    {
        //        var __interface = (item as IReferenceable<TagBlock, resource_identifier>);
        //        __interface.CreateReferences(tag_wrapper.block_graph);
        //    }
        //    {
        //        var __interface = (item as IReferenceable<ByteArray, resource_identifier>);
        //        __interface.CreateReferences(tag_wrapper.block_graph);
        //    }
        //    tag_wrapper.CreateReferenceTable();


        //    //LINK RESOURCES
        //    {
        //        tag_info? owner;
        //        for (int i = 0; i < tag_wrapper.references.Count; ++i)
        //        {
        //            if ((owner = GetOwner(tag_wrapper.references[i].TagblockID)).HasValue)
        //            {
        //                tag_wrapper.references[i].Owner = owner.Value.Id;
        //                tag_wrapper.references[i].TagblockID -= owner.Value.VirtualAddress - SecondaryMagic;
        //            }

        //        }
        //    }


        //    return tag_wrapper;
        //}

        //internal void Deserialize()
        //{
        //    List<TagBlockWrapper> tags = new List<TagBlockWrapper>(this.Tags.Length);
        //    foreach (var tag_item in this.Tags)
        //    {
        //        TagBlockWrapper wrapper = PreProcessTag(tag_item);
        //        PostProcessTag(wrapper);
        //    }
        //}

        public void PostProcessTag(TagBlockWrapper wrapper)
        {
            //var items = wrapper.references.Select(x => x.Owner).Distinct().ToArray();
            //foreach (var item in items)
            //{
            //    TagBlockWrapper resource_owner = PreProcessTag(Tags[item.Index]);
            //    var resources = wrapper.references.Where(x => x.Owner == item).ToArray();
            //    for (int resource_index = 0; resource_index < resources.Length; ++resource_index)
            //    {
            //        resources[resource_index].TagblockID = resource_owner.tagblocks
            //            .Where(x => x.Offset == resources[resource_index].TagblockID)
            //            .Select(x => x.TagblockID).First();
            //    }
            //}
            //TagBlockWrapper resource_owner = PreProcessTag(Tags[wrapper.references[i].Owner.Index]);
            //{
            //}
            //wrapper.references[i].TagblockID = resource_owner.tagblocks.Select(x => x.).Single();//Select(x => x.Offset).Single();
        }

        string IReferenceList<string, StringID>.GetValue(StringID reference)
        {
            return Strings[reference.Index];
        }

        StringID IReferenceList<string, StringID>.Link(StringID reference, string value)
        {
            throw new InvalidOperationException();
        }

        tag_info IReferenceList<tag_info, tag_id>.GetValue(tag_id reference)
        {
            return Tags[reference.Index];
        }

        tag_id IReferenceList<tag_info, tag_id>.Link(tag_id reference, tag_info value)
        {
            throw new InvalidOperationException();
        }


        void IReferenceList<string, StringID>.Add(StringID reference, string value)
        {
            throw new NotImplementedException();
        }


        void IReferenceList<tag_info, tag_id>.Add(tag_id reference, tag_info value)
        {
            throw new NotImplementedException();
        }

        public tag_info FindFirst(tag_class tag_class, string path_fragment)
        {
            foreach (var tag in this.Tags)
            {
                if (tag.Type == tag_class && Paths[tag.Id.Index].Contains(path_fragment))
                    return tag;
            }
            return new tag_info();
        }

        public TagBlock GetTag(tag_info tag)
        {
            string name = Paths[tag.Id.Index];
            TagBlock block = Halo2.CreateInstance(tag.Type);
            (block as IPointable).Address = tag.VirtualAddress;
            Memory memory = GetTagMemory(tag);
            (block as IPointable).Parse(memory);
            return block;
        }
    }


    public struct UnicodeValueNamePair
    {
        public StringID Name;
        public string Value;

        public override string ToString()
        {
            return string.Format("{0}, {1} : {2}", Name.Index, Name.Length, Value);
        }
    }

    public struct tag_info
    {
        public tag_class Type;
        public tag_id Id;
        public int VirtualAddress;
        public int Offset;
        public int Length;
    }
}
