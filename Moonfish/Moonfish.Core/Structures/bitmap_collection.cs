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
            public bitmap_resource get_resource()
            {
                bitmap_resource resource = new bitmap_resource();
                BinaryReader binary_reader = new BinaryReader(this.GetMemory());
                binary_reader.BaseStream.Position = 28;
                resource.offset0 = binary_reader.ReadInt32();
                resource.offset1 = binary_reader.ReadInt32();
                resource.offset2 = binary_reader.ReadInt32();
                binary_reader.BaseStream.Position += 12;
                resource.length0 = binary_reader.ReadInt32();
                resource.length1 = binary_reader.ReadInt32();
                resource.length2 = binary_reader.ReadInt32();
                return resource;
            }
            public bitmap() : base(116) { }
            
        }
    }

    public struct bitmap_resource
    {
        public int offset0;
        public int offset1;
        public int offset2;
        public int length0;
        public int length1;
        public int length2;
    }
}
