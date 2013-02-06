using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Moonfish.Core
{
    /* Intent: Create a class which contains a TagStruct and links all the references 
     * in that struct to local resources or local lookup tables. The tag in a wrapper 
     * state should have all the data it needs to recompile¹ into a cache map
     * 
     * Implementation:
     * • list of strings for stringID references, 
     * • list of filepaths(?) for TagIdentifier references
     * • list of tagIDs + addresses(?) for External Tagblock references
     * • list of addresses + ids for internal TagBlocks?
     */

    public class TagWrapper
    {
        TagBlock tag_;
        List<KeyValuePair<StringID, string>> local_strings_ = new List<KeyValuePair<StringID, string>>();
        HashSet<TagIdentifier> tag_ids_ = new HashSet<TagIdentifier>();
        List<int> tag_blocks = new List<int>();

        public TagWrapper(TagBlock tag, MapStream map)
        {
            tag_ = tag;
            foreach (StringID string_id in tag as IEnumerable<StringID>)
            {
                if (Halo2.Strings.Contains(string_id)) continue;
                else
                {
                    var value = map.Strings[string_id.Index];
                    local_strings_.Add(new KeyValuePair<StringID, string>(string_id, value));
                    // Changed StringID to class to get this to work with a foreach, which is needed for lazy recursive searching, 
                    // but I am not sure I like loose references floating around. I guess I can take heart in the knowledge that 
                    // All the fields are readonly access. I should prbobably make a seperate value-type...
                    (string_id as IField).SetFieldData(BitConverter.GetBytes((int)new StringID((short)(local_strings_.Count - 1), (sbyte)value.Length)));
                }
            }
            foreach (TagIdentifier tag_id in tag as IEnumerable<TagIdentifier>)
            {
                tag_ids_.Add(tag_id);
            }
            foreach( IArrayField array in tag as IEnumerable<IArrayField>)
            {
                tag_blocks.Add(array.Address);
            }
        }
    }
    

    //public class Reference
    //{
    //    public TagIdentifier Owner;
    //    public int TagblockID;

    //    public override string ToString()
    //    {
    //        return string.Format("{0} : {1}", Owner, TagblockID);
    //    }
    //}
    //public class TagBlockReference
    //{
    //    public int TagblockID;
    //    public int Offset;

    //    public override string ToString()
    //    {
    //        return string.Format("{0} : {1}", TagblockID, Offset);
    //    }
    //}
    //public class TagReference
    //{
    //    public int TagID;
    //    public TagClass TagClass;

    //    public override string ToString()
    //    {
    //        return string.Format("{0} : {1}", TagID, TagClass);
    //    }
    //}

    //public class TagBlockWrapper { }
    ////public class TagBlockWrapper : IReferenceList<string, string_id>, IReferenceList<tag_info, tag_id>
    ////{
    ////    tag_class Class { get; set; }
    ////    tag_id Identifier { get; set; }
    ////    // indexed string reference table
    ////    List<string> strings = new List<string>();
    ////    // indexed tag reference table
    ////    List<tag_info> tags = new List<tag_info>();
    ////    // indexed tag_block reference graph
    ////    public Graph block_graph = new Graph();
    ////    // tag definition being wrapped
    ////    TagBlock definition;
    ////    // reference list
    ////    public List<Reference> references = new List<Reference>();
    ////    public List<TagBlockReference> tagblocks = new List<TagBlockReference>();

    ////    public TagBlockWrapper(tag_class base_class, tag_id? unique_identifier, TagBlock tagblock_definition)
    ////    {
    ////        Identifier = unique_identifier ?? tag_id.null_identifier;
    ////        Class = base_class;
    ////        definition = tagblock_definition;
    ////    }

    ////    public void Serialize(Stream output_stream)
    ////    {
    ////        BinaryWriter binary_writer = new BinaryWriter(output_stream);
    ////        binary_writer.Write((int)this.Class);
    ////        binary_writer.Write(this.Identifier);

    ////        //Serialize String Table
    ////        {
    ////            int current_string_offset = 0;

    ////            binary_writer.BeginResource(2);
    ////            binary_writer.BeginResourceBlock(strings.Count);
    ////            for (var i = 0; i < strings.Count; ++i)
    ////            {
    ////                binary_writer.Write(current_string_offset);
    ////                int string_length = Encoding.UTF8.GetByteCount(strings[i]);
    ////                current_string_offset += string_length + 1;
    ////            }
    ////            binary_writer.EndResourceBlock();

    ////            binary_writer.BeginResourceBlock(current_string_offset);
    ////            for (var i = 0; i < strings.Count; ++i)
    ////            {
    ////                binary_writer.Write(Encoding.UTF8.GetBytes(strings[i]));
    ////                binary_writer.Write(byte.MinValue);
    ////            }
    ////            binary_writer.BaseStream.Pad();
    ////            binary_writer.EndResourceBlock();
    ////            binary_writer.EndResource();
    ////        }
    ////        //End

    ////        //Serialize Tags Table
    ////        {
    ////            binary_writer.BeginResource(1);
    ////            binary_writer.BeginResourceBlock(tags.Count);
    ////            for (var i = 0; i < tags.Count; ++i)
    ////            {
    ////                binary_writer.Write(i);
    ////                binary_writer.Write(tags[i].Id);
    ////                binary_writer.Write((int)tags[i].Type);
    ////            }
    ////            binary_writer.EndResourceBlock();
    ////            binary_writer.EndResource();
    ////        }
    ////        //End

    ////        //Serialize TagBlock definition
    ////        {
    ////            //var graphable_interface = (definition as IReferenceGraphable);
    ////            //graphable_interface.CreateReferenceLinks(ref tag_block_graph.Indices, graph_index.null_);//now graph indexed;}
    ////        }
    ////        {
    ////            var serializeable_definition = definition as ISerializable;
    ////            output_stream.Position = serializeable_definition.Serialize(output_stream, (int)output_stream.Position + serializeable_definition.SerializedSize);
    ////        }
    ////        //End

    ////        //Serialize TagBlock graph
    ////        block_graph.Serialize(output_stream);
    ////    }

    ////    public void Deserialize(Stream source_stream)
    ////    {
    ////        BinaryReader binary_reader = new BinaryReader(source_stream);
    ////        this.Class = binary_reader.ReadTagType();
    ////        this.Identifier = binary_reader.ReadInt32();

    ////        //Deserialize Strings list
    ////        {
    ////            binary_reader.BeginReadResource();
    ////            int count = binary_reader.BeginReadResourceBlock();
    ////            int[] string_offset = new int[count];
    ////            for (var i = 0; i < count; ++i)
    ////            {
    ////                string_offset[i] = binary_reader.ReadInt32();
    ////            }
    ////            int length = binary_reader.BeginReadResourceBlock();
    ////            strings.Clear();
    ////            strings.AddRange(Encoding.UTF8.GetString(binary_reader.ReadBytes(length)).Split(char.MinValue));
    ////            strings.RemoveAt(strings.Count - 1);
    ////            binary_reader.EndReadResource();
    ////        }
    ////        //End

    ////        //Deserialize Tags list
    ////        {
    ////            binary_reader.BeginReadResource();
    ////            int count = binary_reader.BeginReadResourceBlock();
    ////            tags.Clear();
    ////            for (var i = 0; i < count; ++i)
    ////            {
    ////                binary_reader.ReadInt32();
    ////                tag_id _id = binary_reader.ReadInt32();
    ////                tag_class _class = (tag_class)binary_reader.ReadInt32();
    ////                tags.Add(new tag_info() { Type = _class, Id = _id });
    ////            }
    ////            binary_reader.EndReadResource();
    ////        }

    ////        //Deserialize TagBlock definition 
    ////        {
    ////            definition = Halo2.CreateInstance(Class);
    ////            (definition as ISerializable).Deserialize(source_stream);
    ////        }
    ////        //End

    ////        //Deserialize TagBlock graph
    ////        {
    ////            block_graph.Deserialize(source_stream, definition);
    ////        }
    ////        //End
    ////    }

    ////    public void CreateReferenceTable()
    ////    {
    ////        int local_offset = block_graph.Where(pair => pair.Key.Identifier == 0).Select(pair => pair.Key.Offset).SingleOrDefault();
    ////        var keys_values = block_graph.Where(pair=>pair.Key.ParentTagIdentifier ==-1).Select(pair=> pair.Key).ToArray();
    ////        foreach (var item in keys_values)
    ////        {
    ////                references.Add(new Reference() { Owner = -1, TagblockID = item.Identifier});
    ////                block_graph.Remove(item);

    ////        }

    ////        tagblocks = new List<TagBlockReference>();
    ////        foreach (var item in block_graph)
    ////        {
    ////            tagblocks.Add(new TagBlockReference() { Offset = item.Key.Offset - local_offset, TagblockID = item.Key.Identifier });
    ////        }
    ////    }

    ////    string IReferenceList<string, string_id>.GetValue(string_id reference)
    ////    {
    ////        return strings[reference.Index];
    ////    }

    ////    string_id IReferenceList<string, string_id>.Link(string_id reference, string value)
    ////    {
    ////        // if we already contain a value at this index, and the value is the same as the input value
    ////        // then return the string_id of this value.
    ////        if (strings.Contains(value))
    ////        {
    ////            short index = (short)strings.IndexOf(value);
    ////            sbyte length = (sbyte)Encoding.UTF8.GetByteCount(value);
    ////            return new string_id(index, length);
    ////        }
    ////        // else add the value, and return a new string_id for this value
    ////        else
    ////        {
    ////            //TODO: add error handling for these conversions
    ////            short string_index = (short)strings.Count;
    ////            sbyte string_length = (sbyte)value.Length;
    ////            strings.Add(value);
    ////            return new string_id(string_index, string_length);
    ////        }
    ////    }

    ////    tag_info IReferenceList<tag_info, tag_id>.GetValue(tag_id reference)
    ////    {
    ////        return tags[reference.Index];
    ////    }

    ////    tag_id IReferenceList<tag_info, tag_id>.Link(tag_id reference, tag_info value)
    ////    {
    ////        // if we already contain a value at this index, and the value has a matching id
    ////        // then return the tag_id of this value.
    ////        if (reference.Index < tags.Count && tags[reference.Index].Id == value.Id) return reference;
    ////        // else add the value, and return a new string_id for this value
    ////        else
    ////        {
    ////            //TODO: add error handling for these conversions
    ////            tag_id new_tag_id = new tag_id((short)tags.Count);
    ////            tags.Add(value);
    ////            return new_tag_id;
    ////        }
    ////    }

    ////    public class Graph : Dictionary<resource_identifier, object>, IReferenceList<TagBlock, resource_identifier>, IReferenceList<ByteArray, resource_identifier>
    ////    {
    ////        public List<graph_index> Indices = new List<graph_index>();

    ////        internal void Serialize(Stream destination_stream)
    ////        {
    ////            //List<KeyValuePair<int, int>> adrii = new List<KeyValuePair<int, int>>();

    ////            //BinaryWriter binary_writer = new BinaryWriter(destination_stream);

    ////            //binary_writer.BeginResource(2);
    ////            //////this is bad, just bad all of it lol
    ////            ////MemoryStream buffer = new MemoryStream();
    ////            ////foreach (var item in this)
    ////            ////{
    ////            ////    // foreach tag block we need to store the address of the beginning of it, then link it to the graph
    ////            ////    adrii.Add(new KeyValuePair<int, int>(item.Key, (int)buffer.Length));
    ////            ////    item.Value.CopyTo(buffer);
    ////            ////}
    ////            ////binary_writer.BeginResourceBlock((int)buffer.Length);
    ////            ////destination_stream.Write(buffer.ToArray(), 0, (int)buffer.Length);
    ////            ////binary_writer.EndResourceBlock();

    ////            //binary_writer.BeginResourceBlock(this.Count);
    ////            //foreach (var item in this)
    ////            //{
    ////            //    binary_writer.Write(item.Value.tagblock_id);
    ////            //    binary_writer.Write(item.Value.GetAddress());
    ////            //}
    ////            //binary_writer.EndResourceBlock();

    ////            //binary_writer.BeginResourceBlock(Indices.Count);
    ////            //foreach (var item in Indices)
    ////            //{
    ////            //    graph_index.Write(binary_writer, item);
    ////            //}
    ////            //binary_writer.EndResourceBlock();
    ////            //binary_writer.EndResource();
    ////            //// write graph_indices
    ////        }

    ////        internal void Deserialize(Stream source_stream, TagBlock definition)
    ////        {
    ////            //BinaryReader binary_reader = new BinaryReader(source_stream);
    ////            //binary_reader.BeginReadResource();
    ////            //int count = binary_reader.BeginReadResourceBlock();
    ////            //Dictionary<int, int> adrii = new Dictionary<int, int>(count);
    ////            //for (var i = 0; i < count; ++i)
    ////            //{
    ////            //    tagblock_id index = binary_reader.ReadInt32();
    ////            //    int offset = binary_reader.ReadInt32();
    ////            //    adrii.Add(offset, index);
    ////            //}
    ////            //count = binary_reader.BeginReadResourceBlock();
    ////            //Indices = new List<graph_index>(count);
    ////            //for (var i = 0; i < count; ++i)
    ////            //{
    ////            //    Indices.Add(graph_index.Read(binary_reader));
    ////            //}
    ////            //// link teh shite plox?
    ////            //var idefinition = definition as IReferenceGraphable;
    ////            //idefinition.RelinkReferences(this, adrii);
    ////        }

    ////        //IAddressable IReferenceList<IAddressable, int>.GetValue(int reference)
    ////        //{
    ////        //    return this[reference];
    ////        //}

    ////        //int IReferenceList<IAddressable, int>.Link(int reference, IAddressable value)
    ////        //{
    ////        //    // if this contains the key value already, and the key points the the same reference, then return the key
    ////        //    if (this.ContainsKey(reference) && this[reference] == value) return reference;
    ////        //    // else create a new key value, then add the value to this, and return the new key value
    ////        //    else
    ////        //    {
    ////        //        tagblock_id new_tagblock_id = this.Count;
    ////        //        this.Add(new_tagblock_id, value);
    ////        //        return new_tagblock_id;
    ////        //    }
    ////        //}

    ////        TagBlock IReferenceList<TagBlock, resource_identifier>.GetValue(resource_identifier reference)
    ////        {
    ////            return this[reference] as TagBlock;
    ////        }

    ////        resource_identifier IReferenceList<TagBlock, resource_identifier>.Link(resource_identifier reference, TagBlock value)
    ////        { 
    ////            // if this contains the key value already, and the key points the the same reference, then return the key
    ////            if (this.ContainsKey(reference) && this[reference].Equals(value)) return reference;
    ////            // else create a new key value, then add the value to this, and return the new key value
    ////            else
    ////            {
    ////                resource_identifier new_tagblock_id = new resource_identifier(reference) { Identifier = this.Count, ResourceType = value.GetType() };
    ////                this.Add(new_tagblock_id, value);
    ////                return new_tagblock_id;
    ////            }
    ////        }

    ////        ByteArray IReferenceList<ByteArray, resource_identifier>.GetValue(resource_identifier reference)
    ////        {
    ////            return this[reference] as ByteArray;
    ////        }

    ////        resource_identifier IReferenceList<ByteArray, resource_identifier>.Link(resource_identifier reference, ByteArray value)
    ////        {
    ////            // if this contains the key value already, and the key points the the same reference, then return the key
    ////            if (this.ContainsKey(reference) && this[reference].Equals(value)) return reference;
    ////            // else create a new key value, then add the value to this, and return the new key value
    ////            else
    ////            {
    ////                resource_identifier new_tagblock_id = new resource_identifier() { Identifier = this.Count, ResourceType = value.GetType() };
    ////                this.Add(new_tagblock_id, value);
    ////                return new_tagblock_id;
    ////            }
    ////        }


    ////        void IReferenceList<TagBlock, resource_identifier>.Add(resource_identifier reference, TagBlock value)
    ////        {
    ////            this.Add(reference, value);
    ////        }


    ////        void IReferenceList<ByteArray, resource_identifier>.Add(resource_identifier reference, ByteArray value)
    ////        {
    ////            throw new NotImplementedException();
    ////        }
    ////    }


    ////    void IReferenceList<tag_info, tag_id>.Add(tag_id reference, tag_info value)
    ////    {
    ////        throw new NotImplementedException();
    ////    }


    ////    void IReferenceList<string, string_id>.Add(string_id reference, string value)
    ////    {
            
    ////    }
    ////}

    //public struct graph_index
    //{
    //    public const int null_index = -1;
    //    public int index;
    //    public int parent;
    //    public int left_sibling;
    //    public int right_sibling;
    //    public int first_child;

    //    public override string ToString()
    //    {
    //        return string.Format("{0} : {1}, {2}, {3}, {4}", index, parent, left_sibling, right_sibling, first_child);
    //    }

    //    public static void Write(BinaryWriter binary_writer, graph_index graph_index)
    //    {
    //        binary_writer.Write(graph_index.index);
    //        binary_writer.Write(graph_index.parent);
    //        binary_writer.Write(graph_index.left_sibling);
    //        binary_writer.Write(graph_index.right_sibling);
    //        binary_writer.Write(graph_index.first_child);
    //    }

    //    public static graph_index Read(BinaryReader binary_reader)
    //    {
    //        return new graph_index()
    //        {
    //            index = binary_reader.ReadInt32(),
    //            parent = binary_reader.ReadInt32(),
    //            left_sibling = binary_reader.ReadInt32(),
    //            right_sibling = binary_reader.ReadInt32(),
    //            first_child = binary_reader.ReadInt32(),
    //        };
    //    }

    //    public static graph_index null_ = new graph_index()
    //    {
    //        index = null_index,
    //        parent = null_index,
    //        first_child = null_index,
    //        left_sibling = null_index,
    //        right_sibling = null_index,
    //    };
    //}

    //internal static class ResourceExtensions
    //{
    //    public static void BeginResource(this BinaryWriter binary_writer, int block_count)
    //    {
    //        binary_writer.Write(Encoding.UTF8.GetBytes("head"), 0, 4);
    //        binary_writer.Write(block_count);
    //        binary_writer.Write(-1);
    //        push_resource_block_offset(binary_writer.BaseStream.Position);
    //        push_resource_block_offset(block_count);
    //        binary_writer.Write(new byte[block_count * sizeof(int) * 2]);
    //    }
    //    public static void BeginResourceBlock(this BinaryWriter binary_writer, int count)
    //    {
    //        binary_writer.Write(Encoding.UTF8.GetBytes("rsrc"), 0, 4);
    //        push_resource_block_offset(count);
    //        push_resource_block_offset(binary_writer.BaseStream.Position);
    //    }
    //    public static void EndResourceBlock(this BinaryWriter binary_writer)
    //    {
    //        push_resource_block_offset(binary_writer.BaseStream.Position);
    //    }
    //    public static void EndResource(this BinaryWriter binary_writer)
    //    {
    //        var _offset = (int)binary_writer.BaseStream.Position;
    //        binary_writer.Write(Encoding.UTF8.GetBytes("foot"), 0, 4);
    //        push_resource_block_offset(binary_writer.BaseStream.Position);
    //        binary_writer.BaseStream.Position = resource_offsets.Dequeue();
    //        int local_start_address = (int)binary_writer.BaseStream.Position;
    //        int length = _offset - local_start_address;
    //        binary_writer.BaseStream.Seek(-4, SeekOrigin.Current);
    //        binary_writer.Write(length);
    //        int block_count = resource_offsets.Dequeue();
    //        for (var i = 0; i < block_count; i++)
    //        {
    //            int count = resource_offsets.Dequeue();
    //            int offset = resource_offsets.Dequeue();
    //            int end_offset = resource_offsets.Dequeue();
    //            binary_writer.Write(offset - local_start_address);
    //            binary_writer.Write(count);
    //        }
    //        binary_writer.BaseStream.Position = resource_offsets.Dequeue();
    //    }

    //    public static void BeginReadResource(this BinaryReader binary_reader)
    //    {
    //        binary_reader.ReadBytes(4);
    //        int block_count = binary_reader.ReadInt32();
    //        resource_length = binary_reader.ReadInt32();
    //        resource_counts = new int[block_count];
    //        current_resource_index = -1;
    //        resource_start_offset = (int)binary_reader.BaseStream.Position;
    //        resource_block_offsets = new int[block_count];
    //        for (var i = 0; i < block_count; ++i)
    //        {
    //            resource_block_offsets[i] = binary_reader.ReadInt32();//offset
    //            resource_counts[i] = binary_reader.ReadInt32();//count
    //        }
    //    }
    //    public static int BeginReadResourceBlock(this BinaryReader binary_reader)
    //    {
    //        current_resource_index++;
    //        binary_reader.BaseStream.Position = resource_start_offset + resource_block_offsets[current_resource_index];
    //        return resource_counts[current_resource_index];
    //    }
    //    public static void EndReadResource(this BinaryReader binary_reader)
    //    {
    //        binary_reader.BaseStream.Position = resource_start_offset + resource_length + 4;
    //    }

    //    static ResourceExtensions()
    //    {
    //        resource_offsets = new Queue<int>();
    //    }
    //    private static int resource_length;
    //    private static int resource_start_offset;
    //    private static int current_resource_index;
    //    private static int[] resource_counts;
    //    private static int[] resource_block_offsets;
    //    private static Queue<int> resource_offsets;
    //    private static void push_resource_block_offset(long p)
    //    {
    //        resource_offsets.Enqueue((int)p);
    //    }
    //}
}
