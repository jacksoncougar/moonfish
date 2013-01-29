using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Ex
{
    //public interface IStructure
    //{
    //    int Address { get; }
    //    int SizeOf { get; }
    //}

    //public interface IField
    //{
    //    byte[] ToByteArray();
    //    void FromByteArray(byte[] source);
    //}

    //public class Field
    //{
    //    public readonly int this_pointer;
    //    public readonly int this_sizeof;
    //    TagBlock source;
    //    public IField Object;

    //    public Field(IField field_object) : this(field_object, null) { }
    //    Field(IField field_object, int? offset)
    //    {
    //        if (field_object == null && offset == null) throw new ArgumentNullException();
    //        Object = field_object;
    //    }
    //}

    //public class TagBlock : IStructure
    //{
    //    // meta declarations
    //    public readonly int this_pointer;
    //    public readonly int this_sizeof;
    //    public readonly int this_alignment;
    //    public MemoryStream this_data;
    //    public Field this_fields;

    //    public void SetField(Field field)
    //    {
    //        byte[] field_data = field.Object.ToByteArray();
    //        this_data.Write(field_data, field.this_pointer, field_data.Length);
    //    }
    //    public void GetField(Field field)
    //    {
    //        byte[] field_data = new byte[field.this_sizeof];
    //        this_data.Read(field_data, field.this_pointer, field_data.Length);
    //        field.Object.FromByteArray(field_data);
    //    }

    //    public TagBlock()
    //    {
    
    //    }
    //}

    //public class Memory : MemoryStream
    //{
    //    int start_address = 0;
    //    public MemoryStream getmem(IStructure calling_object)
    //    {
    //        return new MemoryStream(base.GetBuffer(), calling_object.Address - start_address, calling_object.SizeOf);
    //    }
    //}
}
