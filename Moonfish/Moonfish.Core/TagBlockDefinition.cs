using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace Moonfish.Core
{
    public class TagBlockList<TTagBlock> : FixedArray<TTagBlock>, ISerializable, IReferenceable<TagBlock, resource_identifier>
        where TTagBlock : TagBlock, ISerializable, IReferenceable<TagBlock, resource_identifier>, new()
    {
        void ISerializable.Deserialize(Stream source_stream)
        {
            for (var i = 0; i < count_; ++i)
            {
                TTagBlock item = new TTagBlock();
                source_stream.Position = first_element_address_ + i * item.SerializedSize;
                item.Deserialize(source_stream);
                this.Add(item);
            }
            //we've loaded the elements to memory, this value is now meaningless
            first_element_address_ = 0;
        }

        int ISerializable.Serialize(Stream destination_stream, int next_address)
        {
            // store where we will write the arrary 
            // TODO: add a state to determine if this is under graph control or address control?
            first_element_address_ = next_address;
            parent.SetField(this);
            //// move the stream past this segment (preallocate kinda) then process any fields
            int next_available_address = first_element_address_ + this[0].SerializedSize * this.Count;

            for (var i = 0; i < count_; ++i)
            {
                destination_stream.Position = first_element_address_ + i * this[i].SerializedSize;
                next_available_address = this[i].Serialize(destination_stream, next_available_address);
            }
            return next_available_address;
        }

        void ISerializable.Deserialize(Stream source_stream, Segment stream_segment)
        {
            source_stream.Position = first_element_address_;
            int stream_position = (int)source_stream.Position;
            {
                TTagBlock default_item = new TTagBlock();
                if (stream_position < stream_segment.Offset
                    || stream_position + count_ * default_item.SerializedSize > stream_segment.Offset + stream_segment.Length)
                {
                    first_element_address_ = stream_position;
                    return;
                }
            }

            for (var i = 0; i < count_; ++i)
            {
                TTagBlock item = new TTagBlock();
                source_stream.Position = first_element_address_ + i * item.SerializedSize;
                stream_position = (int)source_stream.Position;
                if (stream_position + item.SerializedSize <= stream_segment.Offset + stream_segment.Length)
                {
                    item.Deserialize(source_stream, stream_segment);
                    this.Add(item);
                }
            }
            //we've loaded the elements to memory, this value is now meaningless
            //first_element_address_ = 0;
        }

        int ISerializable.SerializedSize
        {
            get { return 8; }
        }

        void IReferenceable<TagBlock, resource_identifier>.CopyReferences(IReferenceList<TagBlock, resource_identifier> source_graph, IReferenceList<TagBlock, resource_identifier> destination_graph)
        {
            foreach (var tag_block in this)
            {
                tag_block.CopyReferences(source_graph, destination_graph);
            }
        }

        void IReferenceable<TagBlock, resource_identifier>.CreateReferences(IReferenceList<TagBlock, resource_identifier> destination_graph)
        {
            if (count_ > this.Count)
            {
                destination_graph.Add(new resource_identifier() { Identifier = this.first_element_address_, ResourceType = typeof(TTagBlock), Offset = first_element_address_, ParentTagIdentifier = -1 }, null);
            }
            foreach (var tag_block in this)
            {
                tag_block.CreateReferences(destination_graph);
            }
        }
    }

    public abstract class TagBlock : ISerializable, IStructure, IEnumerable<TagBlockField>, IReference<resource_identifier>,
        IReferenceable<string, string_id>, IReferenceable<tag_info, tag_id>, IReferenceable<TagBlock, resource_identifier>, IReferenceable<ByteArray, resource_identifier>
    {
        const int DefaultAlignment = 4;
        private readonly int size;

        protected readonly int alignment;
        protected byte[] buffer_;
        protected readonly List<TagBlockField> fixed_fields;
        internal int tagblock_id;
        internal int tagblock_address;

        internal List<TagBlockField> Fields { get { return fixed_fields; } }
        internal IEnumerable<TagBlockField> nested_tag_blocks
        {
            get
            {
                foreach (var field in fixed_fields)
                {
                    if (field.Object.GetType().IsGenericType && field.Object.GetType().GetGenericArguments()[0].IsSubclassOf(typeof(TagBlock)))
                    {
                        yield return field;
                    }
                }
            }
        }
        bool is_nested_tagblock(TagBlockField field)
        {
            return (field.Object.GetType().IsGenericType && field.Object.GetType().GetGenericArguments()[0].IsSubclassOf(typeof(TagBlock)));
        }

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

        void ISerializable.Deserialize(Stream source_stream, Segment stream_segment)
        {
            long current_position = source_stream.Position;
            tagblock_address = (int)current_position;
            buffer_ = new byte[size];
            source_stream.Read(buffer_, 0, size);

            foreach (var field in fixed_fields)
            {
                byte[] field_data = new byte[field.Object.SizeOfField];
                Array.Copy(buffer_, field.FieldOffset, field_data, 0, field_data.Length);
                field.Object.SetFieldData(field_data);

                if (is_nested_tagblock(field))
                {
                    ISerializable serializable_interface = (field.Object as ISerializable);
                    if (serializable_interface != null)
                    {
                        serializable_interface.Deserialize(source_stream, stream_segment);
                    }
                }
            }
        }

        void ISerializable.Deserialize(Stream source_stream)
        {
            long current_position = source_stream.Position;
            tagblock_address = (int)current_position;
            buffer_ = new byte[size];
            source_stream.Read(buffer_, 0, size);

            foreach (var field in fixed_fields)
            {
                byte[] field_data = new byte[field.Object.SizeOfField];
                Array.Copy(buffer_, field.FieldOffset, field_data, 0, field_data.Length);
                field.Object.SetFieldData(field_data);
                ISerializable serializable_interface = (field.Object as ISerializable);
                if (serializable_interface != null)
                {
                    serializable_interface.Deserialize(source_stream);
                }

            }
        }

        public void CopyTo(Stream destination_stream)
        {
            destination_stream.Write(this.buffer_, 0, this.size);
        }

        int ISerializable.Serialize(Stream destination_stream, int next_offset)
        {
            int current_offset = (int)destination_stream.Position;
            foreach (var field in fixed_fields)
            {
                ISerializable serializable_interface = (field.Object as ISerializable);
                if (serializable_interface != null)
                {
                    destination_stream.Position = next_offset;
                    next_offset = serializable_interface.Serialize(destination_stream, next_offset);
                }

            }
            this.tagblock_address = (int)destination_stream.Seek(current_offset, SeekOrigin.Begin);
            destination_stream.Write(this.buffer_, 0, this.size);
            return next_offset;
        }

        int ISerializable.SerializedSize
        {
            get { return size; }
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
                    field_data.CopyTo(buffer_, field.FieldOffset);
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

        #region IReferenceable
        void CopyReferences<TObject, TToken>(IReferenceList<TObject, TToken> source_graph, IReferenceList<TObject, TToken> destination_graph) where TToken : struct
        {
            foreach (var field in fixed_fields)
            {
                IReference<TToken> string_reference = field.Object as IReference<TToken>;
                if (string_reference != null)
                {
                    if (string_reference.IsNullReference) continue;
                    TToken token = string_reference.GetToken();
                    TObject value = source_graph.GetValue(token);
                    token = destination_graph.Link(token, value);
                    string_reference.SetToken(token);
                }

                var nested_tagblock = field.Object as IEnumerable<TagBlock>;
                if (nested_tagblock != null) foreach (var item in nested_tagblock)
                    {
                        var interface__ = (item as IReferenceable<TObject, TToken>);
                        if (interface__ != null)
                        {
                            interface__.CopyReferences(source_graph, destination_graph);
                        }
                    }
            }
        }

        void IReferenceable<string, string_id>.CopyReferences(IReferenceList<string, string_id> source_graph, IReferenceList<string, string_id> destination_graph)
        {
            CopyReferences<string, string_id>(source_graph, destination_graph);
        }

        void IReferenceable<tag_info, tag_id>.CopyReferences(IReferenceList<tag_info, tag_id> source_graph, IReferenceList<tag_info, tag_id> destination_graph)
        {
            CopyReferences<tag_info, tag_id>(source_graph, destination_graph);
        }

        void IReferenceable<TagBlock, resource_identifier>.CopyReferences(IReferenceList<TagBlock, resource_identifier> source_graph, IReferenceList<TagBlock, resource_identifier> destination_graph)
        {
            CopyReferences<TagBlock, resource_identifier>(source_graph, destination_graph);
        }
        #endregion

        resource_identifier IReference<resource_identifier>.GetToken()
        {
            return new resource_identifier() { Identifier = this.tagblock_id, ResourceType = this.GetType(), Offset = this.tagblock_address };
        }

        void IReference<resource_identifier>.SetToken(resource_identifier token)
        {
            tagblock_id = token.Identifier;
        }

        bool IReference<resource_identifier>.IsNullReference
        {
            get { return this.tagblock_id == -1; }
        }
        
        void IReferenceable<TagBlock, resource_identifier>.CreateReferences(IReferenceList<TagBlock, resource_identifier> destination_graph)
        {
            this.tagblock_id = destination_graph.Link(new resource_identifier() { Identifier = this.tagblock_id, ResourceType = this.GetType(), Offset = this.tagblock_address }, this).Identifier;

            foreach (var field in fixed_fields)
            {
                var referenceable_string_interface = field.Object as IReferenceable<TagBlock, resource_identifier>;
                if (referenceable_string_interface != null)
                {
                    referenceable_string_interface.CreateReferences(destination_graph);
                }
            }
        }

        void IReferenceable<tag_info, tag_id>.CreateReferences(IReferenceList<tag_info, tag_id> destination_graph)
        {
            throw new NotImplementedException();
        }

        void IReferenceable<string, string_id>.CreateReferences(IReferenceList<string, string_id> destination_graph)
        {
            throw new NotImplementedException();
        }

        void IReferenceable<ByteArray, resource_identifier>.CopyReferences(IReferenceList<ByteArray, resource_identifier> source_graph, IReferenceList<ByteArray, resource_identifier> destination_graph)
        {
            throw new NotImplementedException();
        }

        void IReferenceable<ByteArray, resource_identifier>.CreateReferences(IReferenceList<ByteArray, resource_identifier> destination_graph)
        {
            foreach (var field in fixed_fields)
            {
                var byte_array_interface = (field.Object as IReferenceable<ByteArray, resource_identifier>);
                if (byte_array_interface != null)
                {
                    byte_array_interface.CreateReferences(destination_graph);
                }
                else
                {
                    var nested_tagblock = field.Object as IEnumerable<TagBlock>;
                    if (nested_tagblock != null) foreach (var item in nested_tagblock)
                        {
                            var interface__ = (item as IReferenceable<ByteArray, resource_identifier>);
                            if (interface__ != null)
                            {
                                interface__.CreateReferences(destination_graph);
                            }
                        }
                }
            }
            //foreach (var field in fixed_fields)
            //{
            //    var referenceable_string_interface = field.Object as IReferenceable<ByteArray, resource_identifier>;
            //    if (referenceable_string_interface != null)
            //    {
            //        referenceable_string_interface.CreateReferences(destination_graph);
            //    }
            //}
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
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }
    }

    public class ByteArray : FixedArray<byte>, ISerializable, IReferenceable<ByteArray, resource_identifier>
    {
        int id_;

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