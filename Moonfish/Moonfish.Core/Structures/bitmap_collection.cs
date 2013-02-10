using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{

    [TagClass("bitm")]
    public class Bitmap_Collection : TagBlock
    {
        public IEnumerable<Bitmap_Collection.bitmap> Bitmaps
        {
            get
            {
                return this.fixed_fields[1].Object as IEnumerable<bitmap>;
            }
        }

        public Bitmap_Collection()
            : base(76, new TagBlockField[] { 
            new TagBlockField(null, 60),
            new TagBlockField(new TagBlockList<sprite>()), 
            new TagBlockField(new TagBlockList<bitmap>()) })
        { }

        public class sprite : TagBlock
        {
            public sprite()
                : base(60, new TagBlockField[]{
            new TagBlockField(new TagBlockList<rectangle>(), 52)})
            { }

            public class rectangle : TagBlock
            {
                public rectangle() : base(32) { }
            }
        }

        public class bitmap : TagBlock
        {
            public BitmapRaw Raw { get { return this.fixed_fields[0].Object as BitmapRaw; } }
            public bitmap()
                : base(116,
                    new TagBlockField(null, 28),
                    new TagBlockField(new BitmapRaw())
                    ) { }
        }
    }
    public class BitmapRaw : IField, IResource  
    {
        IStructure parent;

        int offset0;
        int offset1;
        int offset2;
        int length0;
        int length1;
        int length2;

        byte[] data_0;
        byte[] data_1;
        byte[] data_2;

        byte[] IField.GetFieldData()
        {
            byte[] buffer = new byte[(this as IField).SizeOfField];
            BitConverter.GetBytes(offset0).CopyTo(buffer, 0);
            BitConverter.GetBytes(offset1).CopyTo(buffer, 4);
            BitConverter.GetBytes(offset2).CopyTo(buffer, 8);
                                                                    // <- the other 3 LODs which are never used.
            BitConverter.GetBytes(length0).CopyTo(buffer, 24);
            BitConverter.GetBytes(length1).CopyTo(buffer, 28);
            BitConverter.GetBytes(length2).CopyTo(buffer, 32);
            return buffer;
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            offset0 = BitConverter.ToInt32(field_data, 0);
            offset1 = BitConverter.ToInt32(field_data, 4);
            offset2 = BitConverter.ToInt32(field_data, 8);
                                                                // <- the other 3 LODs which are never used.
            length0 = BitConverter.ToInt32(field_data, 24);
            length1 = BitConverter.ToInt32(field_data, 28);
            length2 = BitConverter.ToInt32(field_data, 32);
        }

        int IField.SizeOfField
        {
            get { return 36; }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            this.parent = calling_structure;
        }

        void IResource.CopyFrom(Stream map)
        {
            map.Position = offset0;             // move the stream to the data position
            data_0 = new byte[length0];         // initialize the buffer to hold bitmap data
            map.Read(data_0, 0, length0);       // copy bytes form stream into buffer

            map.Position = offset1;             // move the stream to the data position
            data_1 = new byte[length1];         // initialize the buffer to hold bitmap data
            map.Read(data_1, 0, length1);       // copy bytes form stream into buffer

            map.Position = offset2;             // move the stream to the data position
            data_2 = new byte[length2];         // initialize the buffer to hold bitmap data
            map.Read(data_2, 0, length2);       // copy bytes form stream into buffer
        }
    }
}
