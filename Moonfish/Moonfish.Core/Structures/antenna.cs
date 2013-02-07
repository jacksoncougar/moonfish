using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{
    [TagClass("ant!")]
    public class antenna : TagBlock
    {
        public antenna()
            : base(160, new TagBlockField[]{
            new TagBlockField(new StringID()), 
            new TagBlockField(new TagPointer()),
            new TagBlockField(new TagPointer()),
            new TagBlockField(null, 132),
            new TagBlockField(new TagBlockList<_tagblock0>())})
        { }

        public class _tagblock0 : TagBlock
        {
            public _tagblock0() : base(128) { }
        }
    }
}
