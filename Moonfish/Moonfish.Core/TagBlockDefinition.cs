using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Moonfish.Core
{
    public class Memory : MemoryStream
    {
        public List<mem_ref> instance_table = new List<mem_ref>();
        int start_address = 0;
        public int Address
        {
            get { return start_address; }
        }
        internal void SetAddress(int address)
        {
            start_address = address;
        }
        public Memory Copy(int address)
        {
            mem_ref[] instance_table__ = this.instance_table.ToArray();
            int shift__ = 0;

            for (int i = 0; i < instance_table__.Length; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    int new_address = instance_table__[i].address - start_address + address;
                    int padding = instance_table__[i].GetPaddingCount(new_address);
                    shift__ += padding;
                    instance_table__[i].SetAddress(new_address, false);
                }
            }


            byte[] buffer_ = new byte[this.Length + shift__];
            Memory copy = new Memory(buffer_, this.start_address);
            instance_table__[0].client.PointTo(copy);
            copy.start_address = address;
            copy.instance_table = new List<mem_ref>(instance_table__);

            BinaryReader bin_reader = new BinaryReader(this);
            for (int i = 0; i < copy.instance_table.Count; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    copy.Position = copy.instance_table[i].address - copy.start_address;
                    this.Position = this.instance_table[i].address - start_address;
                    int length = instance_table__[i].client.SizeOf;
                    copy.Write(bin_reader.ReadBytes(length), 0, length);
                }
            }
            for (int i = 0; i < instance_table__.Length; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    instance_table__[i].SetAddress(instance_table__[i].address);
                }
            }
            return copy;
        }

        public Memory(byte[] buffer, int translation = 0)
            : base(buffer, 0, buffer.Length, true, true)
        {
            start_address = translation;
        }
        public bool Contains(IPointable calling_object)
        {
            return (calling_object.Address - start_address >= 0
                && calling_object.Address - start_address + calling_object.SizeOf <= Length);
        }
        public MemoryStream getmem(IPointable data)
        {
            if (this.Contains(data))
                return new MemoryStream(base.GetBuffer(), data.Address - start_address, data.SizeOf);
            else return null;
        }


        public struct mem_ref
        {
            public IPointable client;
            public int address;
            public int count;
            public Type type;
            public bool external;
            public bool isnull { get { return count == 0 && address == Halo2.nullptr; } }

            public void SetAddress(int address, bool commit = true)
            {
                if (commit && client != null) client.Address = address;
                this.address = address;
            }

            public int GetPaddingCount(int address)
            {
                if (client != null) return (int)Padding.GetCount(address, client.Alignment);
                else throw new Exception();
            }

            public override string ToString()
            {
                return string.Format("{0} : x{1} : {2}", address, count, external);
            }
        }
    }

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
    }

    public abstract class TagBlock : IStructure, IPointable, IEnumerable<TagBlockField>
    {
        const int DefaultAlignment = 4;
        private readonly int size;

        protected readonly int alignment = DefaultAlignment;
        protected MemoryStream memory_;
        protected readonly List<TagBlockField> fixed_fields;

        internal int tagblock_id = -1;
        internal int this_pointer = 0;

        protected TagBlock(int size, int alignment = DefaultAlignment)
            : this(size, new TagBlockField[0]) { }

        protected TagBlock(int size, TagBlockField[] fields, int alignment = DefaultAlignment)
        {
            // assign size of this tag_block
            this.size = size;
            this.alignment = alignment;
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
                        //foreach (var item in nested_tagblock)
                        //{
                        //    item.Parse(memory);
                        //}
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
                        nested_tagblock.Parse(mem);
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

    public class ByteArray : FixedArray<byte>, ISerializable, IPointable, IReferenceable<ByteArray, resource_identifier>
    {
        int id_;
        protected MemoryStream memory_;
        int alignment = 4;

        void ISerializable.Deserialize(Stream source_stream)
        {
            BinaryReader binary_reader = new BinaryReader(source_stream);
            this.AddRange(binary_reader.ReadBytes(this.count_));
        }

        int ISerializable.Serialize(Stream destination_stream, int next_address)
        {// store where we will write the arrary 
            // TODO: add a state to determine if this is under graph control or address control?
            first_element_address_ = next_address;
            parent.SetField(this);
            //// move the stream past this segment (preallocate kinda) then process any fields
            int next_available_address = first_element_address_ + this.Count;
            destination_stream.Position = first_element_address_;
            destination_stream.Write(this.ToArray(), 0, this.Count);
            return next_available_address;
        }

        void ISerializable.Deserialize(Stream source_stream, Segment stream_segment)
        {
            source_stream.Position = first_element_address_;
            int stream_position = (int)source_stream.Position;
            {
                if (stream_position < stream_segment.Offset
                    || stream_position + count_ > stream_segment.Offset + stream_segment.Length)
                {
                    return;
                }
            }
            BinaryReader binary_reader = new BinaryReader(source_stream);
            this.AddRange(binary_reader.ReadBytes(this.count_));
        }

        int ISerializable.SerializedSize
        {
            get { return 8; }
        }

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