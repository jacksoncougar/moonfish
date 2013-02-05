using Moonfish.Core.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Moonfish.Core
{

    public class TagBlockList<TTagBlock> : FixedArray<TTagBlock>, IPointable, IField where TTagBlock : TagBlock, IStructure, IPointable, new()
    {
        //void ISerializable.Deserialize(Stream source_stream)
        //{
        //    for (var i = 0; i < count_; ++i)
        //    {
        //        TTagBlock item = new TTagBlock();
        //        source_stream.Position = first_element_address_ + i * item.SerializedSize;
        //        item.Deserialize(source_stream);
        //        this.Add(item);
        //    }
        //    //we've loaded the elements to memory, this value is now meaningless
        //    first_element_address_ = 0;
        //}

        //int ISerializable.Serialize(Stream destination_stream, int next_address)
        //{
        //    // store where we will write the arrary 
        //    // TODO: add a state to determine if this is under graph control or address control?
        //    first_element_address_ = next_address;
        //    parent.SetField(this);
        //    //// move the stream past this segment (preallocate kinda) then process any fields
        //    int next_available_address = first_element_address_ + this[0].SerializedSize * this.Count;

        //    for (var i = 0; i < count_; ++i)
        //    {
        //        destination_stream.Position = first_element_address_ + i * this[i].SerializedSize;
        //        next_available_address = this[i].Serialize(destination_stream, next_available_address);
        //    }
        //    return next_available_address;
        //}

        //void ISerializable.Deserialize(Stream source_stream, Segment stream_segment)
        //{
        //    source_stream.Position = first_element_address_;
        //    int stream_position = (int)source_stream.Position;
        //    {
        //        TTagBlock default_item = new TTagBlock();
        //        if (stream_position < stream_segment.Offset
        //            || stream_position + count_ * default_item.SerializedSize > stream_segment.Offset + stream_segment.Length)
        //        {
        //            first_element_address_ = stream_position;
        //            return;
        //        }
        //    }

        //    for (var i = 0; i < count_; ++i)
        //    {
        //        TTagBlock item = new TTagBlock();
        //        source_stream.Position = first_element_address_ + i * item.SerializedSize;
        //        stream_position = (int)source_stream.Position;
        //        if (stream_position + item.SerializedSize <= stream_segment.Offset + stream_segment.Length)
        //        {
        //            item.Deserialize(source_stream, stream_segment);
        //            this.Add(item);
        //        }
        //    }
        //    //we've loaded the elements to memory, this value is now meaningless
        //    //first_element_address_ = 0;
        //}

        //int ISerializable.SerializedSize
        //{
        //    get { return 8; }
        //}

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            count_ = BitConverter.ToInt32(field_data, 0);
            first_element_address_ = BitConverter.ToInt32(field_data, 4);
            for (int i = 0; i < count_; ++i)
            {
                TTagBlock child = new TTagBlock();
                child.this_pointer = first_element_address_ + i * child.SizeOf;
                this.Add(child);
            }
        }

        void IPointable.Parse(Memory mem)
        {
            mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(TTagBlock), external = !mem.Contains(this) });
            foreach (var item in this)
            {
                item.Parse(mem);
            }
        }

        int IPointable.Address
        {
            get
            {
                return this.first_element_address_;
            }
            set
            {
                int shift = value - this.first_element_address_;
                this.first_element_address_ = value;
                foreach (var item in this)
                    item.Address += shift;
                parent.SetField(this);
            }
        }

        int IPointable.SizeOf
        {
            get { return Count * new TTagBlock().SizeOf; }
        }

        int IPointable.Alignment
        {
            get { return new TTagBlock().Alignment; }
        }

        void IPointable.PointTo(Memory mem)
        {
            mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(TTagBlock), external = !mem.Contains(this) });
            foreach (var item in this)
            {
                item.PointTo(mem);
            }
        }

        void IPointable.CopyTo(Stream stream)
        {
                                                                                            ////        This code is bad and I feel bad.                            ////
            this.first_element_address_ = (int)stream.Position;                             // 1. Set the element address to this memory position
            foreach (var item in this)                                                      // 2. Write out all the elements to reserve thier memory space.
            {
                stream.Write(item.GetMemory().ToArray(), 0, (item as IPointable).SizeOf);
            }
            foreach (var item in this)                                                      // 3. foreach element 'copyto' to allow the children blocks to reserve space
            {
                item.CopyTo(stream);
            }                                                                               // 4.a this should also allow all children blocks to bubble up values
            var last_address = stream.Position;
            stream.Position = this.first_element_address_;                                  // <- Go back to our reserved memory
            foreach (var item in this)                                                      // 5. Write out all the elements again to update bubbled values
            {
                stream.Write(item.GetMemory().ToArray(), 0, (item as IPointable).SizeOf);
            }
            stream.Position = last_address;                                                 //restore last memory offset...
            if (this.Count == 0) this.first_element_address_ = 0;                           //<- if there's zero elements we should not have an address to anything...
            this.parent.SetField(this);                                                     // 6. set field to allow bubble-up of values
            
        }
    }

    public abstract class TagBlock : IStructure, IPointable, IEnumerable<TagBlockField>, IEnumerable<StringID>
    {
        const int DefaultAlignment = 4;
        protected readonly int size;

        protected readonly int alignment = DefaultAlignment;
        protected MemoryStream memory_;
        protected readonly List<TagBlockField> fixed_fields;

        public void SetDefinitionData(ITagDefinition definition)
        {
            var buffer = definition.ToArray();
            this.memory_.Position = 0;
            this.memory_.Write(buffer, 0, buffer.Length);

            for (var i = 0; i < fixed_fields.Count; i++)
            {
                byte[] field_data = new byte[fixed_fields[i].Object.SizeOfField];
                this.memory_.Position = fixed_fields[i].FieldOffset;
                this.memory_.Read(field_data, 0, field_data.Length);
                fixed_fields[i].Object.SetFieldData(field_data);
            }
        }
        public T GetDefinition<T>() where T : ITagDefinition, new()
        {
            var definition = new T();
            definition.FromArray(this.memory_.ToArray());
            return definition;
        }

        internal int tagblock_id = -1;
        internal int this_pointer = 0;

        protected TagBlock(int size, int alignment = DefaultAlignment)
            : this(size, new TagBlockField[0]) { }

        public MemoryStream GetMemory() { return memory_; }

        protected TagBlock(int size, params TagBlockField[] fields)
            : this(size, fields, DefaultAlignment) { }
        protected TagBlock(int size, TagBlockField[] fields, int alignment = DefaultAlignment)
        {
            // assign size of this tag_block
            this.size = size;
            this.alignment = alignment;
            this.memory_ = new MemoryStream(new byte[this.size], 0, this.size, true, true);//*
            this.fixed_fields = new List<TagBlockField>(fields);

            int field_offset = 0;
            for (var i = 0; i < fixed_fields.Count; i++)
            {
                if (fixed_fields[i].Object == null)
                {
                    field_offset += fixed_fields[i].FieldOffset;
                    fixed_fields.RemoveAt(i--); continue;
                }
                fixed_fields[i] = new TagBlockField(fixed_fields[i].Object, field_offset);
                field_offset += fixed_fields[i].Object.SizeOfField;
                fixed_fields[i].Object.Initialize(this);
            }
            return;
        }

        void IStructure.SetField(IField calling_field)
        {
            foreach (var field in fixed_fields)
            {
                if (field.Object.Equals(calling_field))
                {
                    // get the data from the field object
                    byte[] field_data = calling_field.GetFieldData();
                    // set field data to buffer_
                    memory_.Position = field.FieldOffset;
                    memory_.Write(field_data, 0, field_data.Length);
                    return;
                }
            }
            throw new Exception();
        }

        IField IStructure.GetField(int field_index)
        {
            return fixed_fields[field_index].Object as IField;
        }

        IEnumerator<TagBlockField> IEnumerable<TagBlockField>.GetEnumerator()
        {
            foreach (TagBlockField field in this.fixed_fields)
            {
                yield return field;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return fixed_fields.GetEnumerator();
        }

        void IPointable.Parse(Memory mem)
        {
            if ((this.memory_ = mem.getmem(this)) != null)
            {
                foreach (var field in fixed_fields)
                {
                    byte[] field_data = new byte[field.Object.SizeOfField];
                    this.memory_.Position = field.FieldOffset;
                    this.memory_.Read(field_data, 0, field_data.Length);
                    field.Object.SetFieldData(field_data);

                    var nested_tagblock = field.Object as IPointable;
                    if (nested_tagblock != null)
                    {
                        nested_tagblock.Parse(mem);
                    }
                }
            }
        }

        int IPointable.Address
        {
            get { return this.this_pointer; }
            set { this.this_pointer = value; }
        }

        int IPointable.SizeOf
        {
            get { return this.size; }
        }

        int IPointable.Alignment
        {
            get { return this.alignment; }
        }

        void IPointable.PointTo(Memory mem)
        {
            if ((this.memory_ = mem.getmem(this)) != null)
            {
                foreach (var field in fixed_fields)
                {
                    var nested_tagblock = field.Object as IPointable;
                    if (nested_tagblock != null)
                    {
                        nested_tagblock.PointTo(mem);
                    }
                }
            }
        }

        void IPointable.CopyTo(Stream output)
        {
            foreach (var field in fixed_fields)
            {
                var nested_tagblock = field.Object as IPointable;
                if (nested_tagblock != null)
                {
                    nested_tagblock.CopyTo(output);
                }
                (this as IStructure).SetField(field.Object);
                //byte[] field_data = new byte[field.Object.SizeOfField];
                //this.memory_.Position = field.FieldOffset;
                //this.memory_.Read(field_data, 0, field_data.Length);
                //field.Object.SetFieldData(field_data);                
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns a sequence of ALL string_ids in ALL nested tag_blocks supporting string_id enumeration</returns>
        IEnumerator<StringID> IEnumerable<StringID>.GetEnumerator()
        {
            foreach (TagBlockField field in this.fixed_fields)
            {
                if (field.Object is StringID)
                    yield return (StringID)field.Object;
                else
                {
                    // if this is a collection of tagblocks, enumerate each item
                    var tagblock_interface__ = field.Object as IEnumerable<TagBlock>;
                    if (tagblock_interface__ != null) foreach (var item in tagblock_interface__)
                        {
                            // if this item supports string_id enumeration, enumerate each string_id
                            var stringid_interface__ = field.Object as IEnumerable<StringID>;
                            if (stringid_interface__ != null) foreach (var string_id in stringid_interface__)
                                {
                                    // yield each string
                                    yield return string_id;
                                }
                        }
                }
            }
        }

    }

    public abstract class FixedArray<T> : List<T>, IField, IEnumerable<T>
        where T : new()
    {
        protected IStructure parent;
        protected int count_ = 0;
        protected int first_element_address_;

        byte[] IField.GetFieldData()
        {
            byte[] data_ = new byte[8];
            BitConverter.GetBytes(this.Count).CopyTo(data_, 0);
            BitConverter.GetBytes(first_element_address_).CopyTo(data_, 4);
            return data_;
        }

        int IField.SizeOfField
        {
            get { return 8; }
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            count_ = BitConverter.ToInt32(field_data, 0);
            first_element_address_ = BitConverter.ToInt32(field_data, 4);
            for (int i = 0; i < count_; ++i)
            {
                this.Add(default(T));
            }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }
    }

    public class ByteArray : FixedArray<byte>, IPointable, IReferenceable<ByteArray, resource_identifier>
    {
        int id_;
        protected MemoryStream memory_;
        int alignment = 4;

        void IReferenceable<ByteArray, resource_identifier>.CopyReferences(IReferenceList<ByteArray, resource_identifier> source_graph, IReferenceList<ByteArray, resource_identifier> destination_graph)
        {
            throw new NotImplementedException();
        }

        void IReferenceable<ByteArray, resource_identifier>.CreateReferences(IReferenceList<ByteArray, resource_identifier> destination_graph)
        {
            this.id_ = destination_graph.Link(new resource_identifier() { Identifier = this.id_, ResourceType = this.GetType() }, this).Identifier;
        }

        void IPointable.Parse(Memory mem)
        {
            mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(byte), external = !mem.Contains(this) });
            if ((this.memory_ = mem.getmem(this)) != null)
            {
                this.Clear();
                this.AddRange(this.memory_.ToArray());
            }            
        }

        int IPointable.Address
        {
            get
            {
                return this.first_element_address_;
            }
            set
            {
                int shift = value - this.first_element_address_;
                this.first_element_address_ = value;
                parent.SetField(this);
            }
        }

        int IPointable.SizeOf
        {
            get { return Count * sizeof(byte); }
        }

        int IPointable.Alignment
        {
            get { return alignment; }
        }

        void IPointable.PointTo(Memory mem)
        {
            mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(byte), external = !mem.Contains(this) });
            this.memory_ = mem.getmem(this);
        }


        void IPointable.CopyTo(Stream memory)
        {
            throw new NotImplementedException();
        }
    }

    [System.AttributeUsage(AttributeTargets.Class,
        AllowMultiple = false, Inherited = false)]
    public class TagClassAttribute : System.Attribute
    {
        public tag_class Tag_Class { get; set; }
        public TagClassAttribute(string tag_class)
        {
            Tag_Class = (tag_class)tag_class;
        }
    }

    public struct Segment
    {
        public readonly long Offset;
        public readonly int Length;
        public Segment(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }
    }

    /// <summary>
    /// Wrapper structure for linking an IField object with a field offset value
    /// </summary>    C:\Users\stem\Documents\Visual Studio 2012\Projects\moonfish\Moonfish\Moonfish.Core\TagBlockDefinition.cs
    public struct TagBlockField
    {
        public readonly IField Object;
        public readonly int FieldOffset;

        public TagBlockField(IField field)
        {
            this.Object = field;
            this.FieldOffset = -1;
        }

        public TagBlockField(IField field, int field_offset)
        {
            this.Object = field;
            this.FieldOffset = field_offset;
        }
    }
}