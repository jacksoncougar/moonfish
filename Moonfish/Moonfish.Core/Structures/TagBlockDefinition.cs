using Moonfish.Core.Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Moonfish.Core
{

    public class TagBlockList<TTagBlock> : FixedArray<TTagBlock>, IField, IFieldArray, IFixedArray
        where TTagBlock : TagBlock, IStructure, IAField, new()
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
                child.this_pointer = first_element_address_ + i * child.Size;
                this.Add(child);
            }
        }

        //void IPointable.Parse(Memory mem)
        //{
        //    mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(TTagBlock), external = !mem.Contains(this) });
        //    foreach (var item in this)
        //    {
        //        item.Parse(mem);
        //    }
        //}

        //int IPointable.Address
        //{
        //    get
        //    {
        //        return this.first_element_address_;
        //    }
        //    set
        //    {
        //        int shift = value - this.first_element_address_;
        //        this.first_element_address_ = value;
        //        foreach (var item in this)
        //            item.Address += shift;
        //        parent.SetField(this);
        //    }
        //}

        //int IPointable.SizeOf
        //{
        //    get { return Count * new TTagBlock().SizeOf; }
        //}

        //int IPointable.Alignment
        //{
        //    get { return new TTagBlock().Alignment; }
        //}

        //void IPointable.PointTo(Memory mem)
        //{
        //    mem.instance_table.Add(new Memory.mem_ref() { client = this, address = this.first_element_address_, count = this.count_, type = typeof(TTagBlock), external = !mem.Contains(this) });
        //    foreach (var item in this)
        //    {
        //        item.PointTo(mem);
        //    }
        //}

        //void IPointable.CopyTo(Stream stream)
        //{
        //                                                                                    ////        This code is bad and I feel bad.                            ////
        //    this.first_element_address_ = (int)stream.Position;                             // 1. Set the element address to this memory position
        //    foreach (var item in this)                                                      // 2. Write out all the elements to reserve thier memory space.
        //    {
        //        stream.Write(item.GetMemory().ToArray(), 0, (item as IPointable).SizeOf);
        //    }
        //    foreach (var item in this)                                                      // 3. foreach element 'copyto' to allow the children blocks to reserve space
        //    {
        //        item.CopyTo(stream);
        //    }                                                                               // 4.a this should also allow all children blocks to bubble up values
        //    var last_address = stream.Position;
        //    stream.Position = this.first_element_address_;                                  // <- Go back to our reserved memory
        //    foreach (var item in this)                                                      // 5. Write out all the elements again to update bubbled values
        //    {
        //        stream.Write(item.GetMemory().ToArray(), 0, (item as IPointable).SizeOf);
        //    }
        //    stream.Position = last_address;                                                 //restore last memory offset...
        //    if (this.Count == 0) this.first_element_address_ = 0;                           //<- if there's zero elements we should not have an address to anything...
        //    this.parent.SetField(this);                                                     // 6. set field to allow bubble-up of values

        //}

        int IFieldArray.Address
        {
            get
            {
                return first_element_address_;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        IList<IAField> IFieldArray.Fields
        {

            get
            {
                var ttt = this.Select(x => x as IAField).ToList();
                return ttt;
            }
        }

        void IFixedArray.CopyFrom(Stream source)
        {
            foreach (TagBlock value in this)
            {
                value.Parse(source);
            }
        }
    }

    public abstract class TagBlock : IStructure,  IAField,
        IEnumerable<TagBlockField>, IEnumerable<StringID>, IEnumerable<TagIdentifier>, 
        IEnumerable<TagPointer>, IEnumerable<IFieldArray>
    {
        const int DefaultAlignment = 4;
        protected readonly int size;

        protected readonly int alignment = DefaultAlignment;
        protected MemoryStream memory_;
        protected readonly List<TagBlockField> fixed_fields;

        public void SetDefinitionData(IDefinition definition)
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
        public T GetDefinition<T>() where T : IDefinition, new()
        {
            var definition = new T();
            definition.FromArray(this.memory_.ToArray());
            return definition;
        }
        public void Parse(Stream map)
        {
            map.Position = this.this_pointer;
            map.Read(this.memory_.GetBuffer(), 0, this.size);
            foreach (var field in fixed_fields)
            {
                byte[] field_data = new byte[field.Object.SizeOfField];
                this.memory_.Position = field.FieldOffset;
                this.memory_.Read(field_data, 0, field_data.Length);
                field.Object.SetFieldData(field_data);

                /* if the field is a fixed array type I want to load all the values into it.
                 * TagBlockList<T>, ByteArray, ShortArray, ResourceArray... etc all at once
                 * */

                var nested_tagblock = field.Object as IFixedArray;
                if (nested_tagblock != null)
                {
                    nested_tagblock.CopyFrom(map);
                } 
                var nested_resource = field.Object as IResource;
                if (nested_resource != null)
                {
                    nested_resource.CopyFrom(map);
                }
            }
        }
        public void SetAddress(int addres) { this.this_pointer = addres; }
        protected int tagblock_id = -1;
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

        //Fuck this code in particular
        //void IPointable.Parse(Memory mem)
        //{
        //    if ((this.memory_ = mem.getmem(this)) != null)
        //    {
        //        foreach (var field in fixed_fields)
        //        {
        //            byte[] field_data = new byte[field.Object.SizeOfField];
        //            this.memory_.Position = field.FieldOffset;
        //            this.memory_.Read(field_data, 0, field_data.Length);
        //            field.Object.SetFieldData(field_data);

        //            var nested_tagblock = field.Object as IPointable;
        //            if (nested_tagblock != null)
        //            {
        //                nested_tagblock.Parse(mem);
        //            }
        //        }
        //    }
        //}

        //int IPointable.Address
        //{
        //    get { return this.this_pointer; }
        //    set { this.this_pointer = value; }
        //}

        //int IPointable.SizeOf
        //{
        //    get { return this.size; }
        //}

        //int IPointable.Alignment
        //{
        //    get { return this.alignment; }
        //}

        //void IPointable.PointTo(Memory mem)
        //{
        //    if ((this.memory_ = mem.getmem(this)) != null)
        //    {
        //        foreach (var field in fixed_fields)
        //        {
        //            var nested_tagblock = field.Object as IPointable;
        //            if (nested_tagblock != null)
        //            {
        //                nested_tagblock.PointTo(mem);
        //            }
        //        }
        //    }
        //}

        //void IPointable.CopyTo(Stream output)
        //{
        //    foreach (var field in fixed_fields)
        //    {
        //        var nested_tagblock = field.Object as IPointable;
        //        if (nested_tagblock != null)
        //        {
        //            nested_tagblock.CopyTo(output);
        //        }
        //        (this as IStructure).SetField(field.Object);
        //        //byte[] field_data = new byte[field.Object.SizeOfField];
        //        //this.memory_.Position = field.FieldOffset;
        //        //this.memory_.Read(field_data, 0, field_data.Length);
        //        //field.Object.SetFieldData(field_data);                
        //    }
        //}

        /// <summary>
        /// Generic class for searching nested TagBlocks for T and returning a combined Enumerable<T> object
        /// </summary>
        /// <typeparam name="T">Reference type to search for</typeparam>
        /// <returns></returns>
        IEnumerable<T> GetEnumeratorsRecursively<T>() where T : class
        {
            List<T> buffer = new List<T>();
            foreach (TagBlockField field in this.fixed_fields)
            {
                T array;
                if ((array = field.Object as T) != null)
                {
                    buffer.Add(array);
                }
                IEnumerable<TagBlock> tagblock_interface__;
                if ((tagblock_interface__ = field.Object as IEnumerable<TagBlock>) != null)
                {
                    foreach (var item in tagblock_interface__)
                    {
                        IEnumerable<T> tagid_interface__;
                        if ((tagid_interface__ = item as IEnumerable<T>) != null)
                            buffer.AddRange(item.GetEnumeratorsRecursively<T>());
                    }
                }
            }
            return buffer;
        }
        /// <summary>
        /// Returns all StringIDs from this TagBlock and all nested TagBlocks
        /// </summary>
        /// <returns>returns an IEnumerator<StringID></returns>
        IEnumerator<StringID> IEnumerable<StringID>.GetEnumerator()
        {
            foreach (var subitem in this.GetEnumeratorsRecursively<StringID>())
            {
                yield return subitem;
            }
        }
        /// <summary>
        /// Returns all TagIdentifiers¹ from this TagBlock and all nested TagBlocks
        /// ¹Also searches for tag_pointers and returns the TagIdentifier property with
        /// the enumerator
        /// </summary>
        /// <returns>returns an IEnumerator<TagIdentifier></returns>
        IEnumerator<TagIdentifier> IEnumerable<TagIdentifier>.GetEnumerator()
        {
            List<TagIdentifier> items = new List<TagIdentifier>(this.GetEnumeratorsRecursively<TagIdentifier>());
            List<TagPointer> pointer_items = new List<TagPointer>(this.GetEnumeratorsRecursively<TagPointer>());
            items.AddRange(pointer_items.Select(x => (TagIdentifier)x).ToArray());
            foreach (var subitem in items)
            {
                yield return subitem;
            }
        }
        /// <summary>
        /// Retusn all tag_pointers from this TagBlock and all nested TagBlocks
        /// </summary>
        /// <returns>returns an IEnumerator<tag_pointer></returns>
        IEnumerator<TagPointer> IEnumerable<TagPointer>.GetEnumerator()
        {
            foreach (var subitem in this.GetEnumeratorsRecursively<TagPointer>())
            {
                yield return subitem;
            }
        }
        /// <summary>
        /// Returns all IArrayFields from this TagBlock and all nested TagBlocks
        /// </summary>
        /// <returns></returns>
        IEnumerator<IFieldArray> IEnumerable<IFieldArray>.GetEnumerator()
        {
            foreach (var subitem in this.GetEnumeratorsRecursively<IFieldArray>())
            {
                yield return subitem;
            }
        }
        /// <summary>
        /// Returns all TagBlockFields from this TagBlock
        /// </summary>
        /// <returns></returns>
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

        int IAField.Size
        {
            get { return this.size; }
        }

    }

    public interface IFixedArray
    {
        void CopyFrom(Stream source);
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

    public class ByteArray : FixedArray<byte>, IPointable
    {
        protected MemoryStream memory_;
        int alignment = 4;

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

    /* Intent: Interface used for loading raw out of a mapstream when parsing 
     * a TagBlock out of it.
     * While the parsing is going on we'll use this interface to 'break-out' of
     * the normal bounds we allow the TagBlock to read from.
     */
    public interface IResource
    {
        void CopyFrom(Stream map);
    }

    public class ModelRaw : FixedArray<byte>, IField, IResource
    {
        uint Address { get; set; }
        uint Length { get; set; }
        uint HeaderSize { get; set; }
        uint ResourceDataLength { get; set; }

        byte[] IField.GetFieldData()
        {
            byte[] buffer = new byte[16];
            BitConverter.GetBytes(Address).CopyTo(buffer, 0);
            BitConverter.GetBytes(Length).CopyTo(buffer, 4);
            BitConverter.GetBytes(HeaderSize).CopyTo(buffer, 8);
            BitConverter.GetBytes(ResourceDataLength).CopyTo(buffer, 12);
            return buffer;
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            Address = BitConverter.ToUInt32(field_data, 0);
            Length = BitConverter.ToUInt32(field_data, 4);
            HeaderSize = BitConverter.ToUInt32(field_data, 8);
            ResourceDataLength = BitConverter.ToUInt32(field_data, 12);
        }

        int IField.SizeOfField
        {
            get { return 16; }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }

        void IResource.CopyFrom(Stream map)
        {
            map.Position = Address;
            byte[] buffer = new byte[this.Length];
            map.Read(buffer, 0, buffer.Length);
            this.AddRange(buffer);
        }
    }

    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class TagClassAttribute : System.Attribute
    {
        public TagClass Tag_Class { get; set; }
        public TagClassAttribute(string tag_class)
        {
            Tag_Class = (TagClass)tag_class;
        }
    }

    /// <summary>
    /// Wrapper structure for linking an IField object with a field offset value
    /// </summary>
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