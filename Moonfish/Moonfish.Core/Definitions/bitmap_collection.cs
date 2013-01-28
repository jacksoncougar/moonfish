using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{
    [TagClass("bitm")]
    public class Bitmap_Collection : TagBlock
    {
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
            public bitmap() : base(116) { }
        }
    }
}
