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
    public class MapStream : FileStream, IMap
    {
        public readonly Version BuildVersion;
        /// <summary>
        /// name of this cache (is not used in anything, just compiled into the header)
        /// </summary>
        public readonly string MapName;
        /// <summary>
        /// path of the scenario (local directory path storing the resources of this map when decompiled)
        /// </summary>
        public readonly string Scenario;
        /// <summary>
        /// magic values are used to convert from pre-calculated memory pointers to file-addresses
        /// </summary>
        public readonly int PrimaryMagic;
        /// <summary>
        /// magic values are used to convert from pre-calculated memory pointers to file-addresses
        /// </summary>
        public readonly int SecondaryMagic;

        public readonly UnicodeValueNamePair[] Unicode;
        public readonly string[] Strings;
        public readonly Tag[] Tags;

        public readonly int IndexVirtualAddress;

        public readonly MemoryMappedAddress[] MemoryBlocks;

        public MapStream(string filename)
            : base(filename, FileMode.Open, FileAccess.Read, FileShare.Read)
        {
            this.MemoryBlocks = new MemoryMappedAddress[2];
            //HEADER
            BinaryReader bin = new BinaryReader(this, Encoding.UTF8);

            this.Lock(0, 2048);
            this.Seek(0, SeekOrigin.Begin);
            if (bin.ReadTagClass() != (TagClass)"head") 
                throw new InvalidDataException("Not a halo-map file");

            this.Seek(36, SeekOrigin.Begin);
            var version = bin.ReadInt32();
            switch (version)
            {
                case 0:
                    BuildVersion = Version.XBOX_RETAIL;
                    break;
                case -1:
                    BuildVersion = Version.PC_RETAIL;
                    break;
            }

            this.Seek(16, SeekOrigin.Begin);

            int indexAddress = bin.ReadInt32();
            int indexLength = bin.ReadInt32();
            int tagCacheLength = bin.ReadInt32();

            this.Seek(332, SeekOrigin.Current);

            int stringTableLength = bin.ReadInt32();
            this.Seek(4, SeekOrigin.Current);
            int stringTableAddress = bin.ReadInt32();

            this.Seek(36, SeekOrigin.Current);

            MapName = bin.ReadFixedString(32);

            this.Seek(4, SeekOrigin.Current);

            Scenario = bin.ReadFixedString(256);

            this.Seek(4, SeekOrigin.Current);
            int pathsCount = bin.ReadInt32();
            int pathsTableAddress = bin.ReadInt32();
            int pathsTableLength = bin.ReadInt32();

            this.Unlock(0, 2048);

            this.Seek(pathsTableAddress, SeekOrigin.Begin);
            var Paths = Encoding.UTF8.GetString(bin.ReadBytes(pathsTableLength - 1)).Split(char.MinValue);

            //STRINGS

            this.Seek(stringTableAddress, SeekOrigin.Begin);
            Strings = Encoding.UTF8.GetString(bin.ReadBytes(stringTableLength - 1)).Split(char.MinValue);


            //INDEX
            this.Seek(indexAddress, SeekOrigin.Begin);
            int tagClassTableVirtualAddress = bin.ReadInt32();
            this.IndexVirtualAddress = tagClassTableVirtualAddress - 32;
            this.Seek(4, SeekOrigin.Current);
            int tagDatumTableVirtualAddress = bin.ReadInt32();
            int tagDatumTableOffset = tagDatumTableVirtualAddress - tagClassTableVirtualAddress;
            this.Seek(12, SeekOrigin.Current);
            int tagDatumCount = bin.ReadInt32();

            this.Seek(4 + tagDatumTableOffset, SeekOrigin.Current);
            Tags = new Tag[tagDatumCount];
            for (int i = 0; i < tagDatumCount; i++)
            {
                Tags[i] = new Tag()
                {
                    Type = bin.ReadTagType(),
                    Identifier = bin.ReadInt32(),
                    VirtualAddress = bin.ReadInt32(),
                    Length = bin.ReadInt32()
                };
                if (i == 0)
                {
                    SecondaryMagic = Tags[0].VirtualAddress - (indexAddress + indexLength);
                }
                Tags[i].Path = Paths[i];
            }

            this.MemoryBlocks[1] = new MemoryMappedAddress()
            {
                Address = Tags[0].VirtualAddress,
                Length = tagCacheLength,
                Magic = SecondaryMagic,
            };

            /* Intent: read the sbsp and lightmap address and lengths from the scenario tag 
             * and store them in the Tags array.
             */
            this.Seek(Tags[3].VirtualAddress - SecondaryMagic + 528, SeekOrigin.Begin);
            var count = bin.ReadInt32();
            var address = bin.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                this.Seek(address - SecondaryMagic + i * 68, SeekOrigin.Begin);
                var sbsp_offset = bin.ReadInt32();
                var sbsp_length = bin.ReadInt32();
                var sbsp_virtual_address = bin.ReadInt32();
                if (i == 0)
                {
                    this.PrimaryMagic = sbsp_virtual_address - sbsp_offset;
                    this.MemoryBlocks[0].Address = sbsp_virtual_address;
                    this.MemoryBlocks[0].Magic = this.PrimaryMagic;
                }
                this.MemoryBlocks[0].Length += sbsp_length;

                Seek(8, SeekOrigin.Current);
                var sbsp_identifier = bin.ReadTagID();
                Seek(4, SeekOrigin.Current);
                var ltmp_identifier = bin.ReadTagID();

                var ltmp_offset = bin.ReadInt32();
                var ltmp_length = sbsp_offset + sbsp_length - ltmp_offset;

                Tags[sbsp_identifier.Index].VirtualAddress = sbsp_virtual_address;
                Tags[sbsp_identifier.Index].Length = sbsp_length - ltmp_length;

                if (ltmp_identifier != TagIdentifier.null_identifier)
                {
                    Tags[ltmp_identifier.Index].VirtualAddress = sbsp_virtual_address + ltmp_offset;
                    Tags[ltmp_identifier.Index].Length = ltmp_length;
                }
            }
            
            //UNICODE
            this.Seek(Tags[0].VirtualAddress - SecondaryMagic + 400, SeekOrigin.Begin);
            int unicodeCount = bin.ReadInt32();
            int unicodeTableLength = bin.ReadInt32();
            int unicodeIndexAddress = bin.ReadInt32();
            int unicodeTableAddress = bin.ReadInt32();

            Unicode = new UnicodeValueNamePair[unicodeCount];

            StringID[] strRefs = new StringID[unicodeCount];
            int[] strOffsets = new int[unicodeCount];

            this.Seek(unicodeIndexAddress, SeekOrigin.Begin);
            for (int i = 0; i < unicodeCount; i++)
            {
                strRefs[i] = (StringID)bin.ReadInt32();
                strOffsets[i] = bin.ReadInt32();
            }
            for (int i = 0; i < unicodeCount; i++)
            {
                this.Seek(unicodeTableAddress + strOffsets[i], SeekOrigin.Begin);
                StringBuilder unicodeString = new StringBuilder(byte.MaxValue);
                while (bin.PeekChar() != char.MinValue)
                    unicodeString.Append(bin.ReadChar());
                Unicode[i] = new UnicodeValueNamePair { Name = strRefs[i], Value = unicodeString.ToString() };
            }
        }

        Tag current_tag = new Tag();
        public IMap this[string tag_class, string tag_name]
        {
            get
            {
                if (current_tag.Type == (TagClass)tag_class && current_tag.Path.Contains(tag_name)) return this;
                current_tag = FindFirst((TagClass)tag_class, tag_name);
                return this;
            }
        }

        TagBlock IMap.Export()
        {
            return GetTag(current_tag);
        }

        Tag IMap.Meta
        {
            get { return current_tag; }
            set { }
        }

        public bool GetResource(int address, int length, out byte[] resource)
        {
            if (address > 2048 && address < this.Length)
            {
                this.Position = address;
                resource = new byte[length];
                this.Read(resource, 0, length);
                return true;
            }
            else
            {
                resource = new byte[0];
                return false;
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

                    base.Position = PointerToOffset((int)value);
                    return;
                }
                else
                {
                    base.Position = (int)value;
                    return;
                }
            }
        }

        private int PointerToOffset(int value)
        {
            int offset = 0;
            foreach (var block in this.MemoryBlocks)
            {
                if (block.GetOffset(out offset, value)) return offset;
            }
            throw new InvalidOperationException();
        }

        Tag GetOwner(int memory_address)
        {
            foreach (var tag in Tags)
            {
                if (memory_address >= tag.VirtualAddress - SecondaryMagic && memory_address < (tag.VirtualAddress - SecondaryMagic) + tag.Length)
                    return tag;
            }
            throw new Exception();
        }

        //public void SerializeTag(tag_info meta)
        //{
        //    string name = Paths[meta.Id.Index];
        //    TagBlock block = Halo2.CreateInstance(meta.Type);
        //    (block as IPointable).Address = meta.VirtualAddress;
        //    Memory memory = GetTagMemory(meta);
        //    (block as IPointable).Parse(memory);
        //    for (int i = 0; i < memory.instance_table.Count; ++i)
        //    {
        //        if (memory.instance_table[i].external && !memory.instance_table[i].isnull)
        //        {
        //            throw new Exception(":D \n\n\n\n\n");
        //        }
        //    }
        //    MemoryStream raw_data = new MemoryStream();
        //    if (meta.Type == (tag_class)"bitm")
        //    {
        //        Bitmap_Collection collection = (Bitmap_Collection)block;
        //        foreach (var bitmap in collection.Bitmaps)
        //        {
        //            var resource = bitmap.get_resource();
        //            BinaryReader bin_reader = new BinaryReader(this);
        //            bin_reader.BaseStream.Position = resource.offset0;
        //            raw_data.Write(bin_reader.ReadBytes(resource.length0), 0, resource.length0);
        //        }
        //    } 
        //    //if (meta.Type == (tag_class)"mode")
        //    //{
        //    //    model collection = (model)block;
        //    //    foreach (var bitmap in collection.Sections)
        //    //    {
        //    //        var resource = bitmap.GetRawPointer();
        //    //        BinaryReader bin_reader = new BinaryReader(this);
        //    //        bin_reader.BaseStream.Position = resource.Address;
        //    //        raw_data.Write(bin_reader.ReadBytes(resource.Length), 0, resource.Length);
        //    //        {//because
        //    //            {//fuck you
        //    //                Mesh mesh = new Mesh();
        //    //                mesh.Load(raw_data.ToArray(), bitmap.GetSectionResources(), collection.GetBoundingBox().GetCompressionRanges());
        //    //                //mesh.ExportAsWavefront(@"D:\halo_2\wavefront.obj");
        //    //            }
        //    //        }
        //    //    }
        //    //}
           
        //    memory = memory.Copy(16);//setup for local
        //    using (FileStream output = File.Create(@"D:\halo_2\shad.bin"))
        //    {
        //        BinaryWriter binary_writer = new BinaryWriter(output);
        //        binary_writer.Write((int)meta.Type);
        //        binary_writer.Write((int)meta.Id);
        //        binary_writer.Write((int)(Padding.GetCount(memory.Length) + memory.Length));
        //        binary_writer.Write((int)raw_data.Length);
        //        binary_writer.Write(memory.ToArray());
        //        binary_writer.Write(new byte[Padding.GetCount(output.Position)]);
        //        binary_writer.Write(raw_data.ToArray());
        //        binary_writer.Write(new byte[Padding.GetCount(output.Position)]);
        //    }
        //}

        Memory GetTagMemory(Tag meta)
        {
            //BinaryReader bin_reader = new BinaryReader(this);
            //this.Position = meta.VirtualAddress;
            //Memory mem = new Memory(bin_reader.ReadBytes(meta.Length), meta.VirtualAddress);
            //TagBlock block = Halo2.CreateInstance(meta.Type);
            //mem.instance_table.Add(new Memory.mem_ref() { address = meta.VirtualAddress, client = block, count = 1, external = false, type = block.GetType() });
            //(block as IPointable).Address = meta.VirtualAddress;
            //(block as IPointable).Parse(mem);
            //return mem;
            return new Memory ();
        }

        //void PostProcessTag(TagBlockWrapper wrapper)
        //{
        //    //var items = wrapper.references.Select(x => x.Owner).Distinct().ToArray();
        //    //foreach (var item in items)
        //    //{
        //    //    TagBlockWrapper resource_owner = PreProcessTag(Tags[item.Index]);
        //    //    var resources = wrapper.references.Where(x => x.Owner == item).ToArray();
        //    //    for (int resource_index = 0; resource_index < resources.Length; ++resource_index)
        //    //    {
        //    //        resources[resource_index].TagblockID = resource_owner.tagblocks
        //    //            .Where(x => x.Offset == resources[resource_index].TagblockID)
        //    //            .Select(x => x.TagblockID).First();
        //    //    }
        //    //}
        //    //TagBlockWrapper resource_owner = PreProcessTag(Tags[wrapper.references[i].Owner.Index]);
        //    //{
        //    //}
        //    //wrapper.references[i].TagblockID = resource_owner.tagblocks.Select(x => x.).Single();//Select(x => x.Offset).Single();
        //}

        Tag FindFirst(TagClass tag_class, string path_fragment)
        {
            foreach (var tag in this.Tags)
            {
                if (tag.Type == tag_class && tag.Path.Contains(path_fragment))
                    return tag;
            }
            return new Tag();
        }

        TagBlock GetTag(Tag tag)
        {
            TagBlock block = Halo2.CreateInstance(tag.Type);
            block.SetAddress(tag.VirtualAddress);
            block.Parse(this);
            return block;
        }
    }

    public enum Version
    {
        XBOX_RETAIL,
        PC_RETAIL,
    }

    /* * *
     * Unicode Handling
     * ----------------
     * Store and index pointing to a table which maps to a UTF8 string for each language.
     * For each Unicode there will be a memory usage of 4 + ( language_count * 4 ) used for indexers
     * 
     * [StringID] -> [index] : 0 -> [Language Switch Mappings] -> [English] -> UTF8 String
     * 
     * Using a dictionary to map the string_id value to an index in the language map
     * using a custom struct to hold to language mappings
     * using a list to hold the UTF8 strings
     * 
     * * */

    struct UnicodeItem
    {
        int[] _indices;
        int[] Indices { get { return _indices; } }
    }

    public struct UnicodeValueNamePair
    {
        //depre.//
        public StringID Name;
        public string Value;

        public override string ToString()
        {
            return string.Format("{0}:{1} : \"{2}\"", Name.Index, Name.Length, Value);
        }
    }

    public struct MemoryMappedAddress
    {
        public int Address;
        public int Length;
        public int Magic;

        bool Contains(int address)
        {
            return address >= Address && address < Address + Length;
        }

        public bool GetOffset(out int offset, int address)
        {
            offset = 0;
            if (!this.Contains(address)) return false;
            offset = address - Magic;
            return true;
        }
    }

    public interface IMap
    {
        /// <summary>
        /// Returns a TagBlock from the current class
        /// </summary>
        /// <returns></returns>
        TagBlock Export();
        /// <summary>
        /// Access meta information about the tag
        /// </summary>
        Tag Meta { get; set; }
    }

    public class Tag
    {
        public TagClass Type;
        public string Path;
        public TagIdentifier Identifier;
        public int VirtualAddress;
        public int Length;

        internal bool Contains(int address)
        {
            return (address >= VirtualAddress && address < VirtualAddress + Length);
        }
    }
}
