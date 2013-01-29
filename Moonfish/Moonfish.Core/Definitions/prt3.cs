using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Definitions
{
    [TagClass("prt3")]
    class prt3 : TagBlock
    {
        public prt3()
            : base(188, new TagBlockField[] { 
            new TagBlockField(null, 12),
            new TagBlockField(new tag_pointer()),
            new TagBlockField(new TagBlockList<tagblock0>()),
            new TagBlockField(null, 8),
            new TagBlockField(new ByteArray()),
            new TagBlockField(null, 8),
            new TagBlockField(new ByteArray()),
            new TagBlockField(null, 8),
            new TagBlockField(new ByteArray()),
            new TagBlockField(null, 8),
            new TagBlockField(new ByteArray()),
            new TagBlockField(null, 8),
            new TagBlockField(new ByteArray()),
            new TagBlockField(new tag_pointer()),
            new TagBlockField(new tag_pointer()),
            new TagBlockField(new TagBlockList<tagblock1>()),
            new TagBlockField(new TagBlockList<tagblock2>()),
            new TagBlockField(new TagBlockList<shader.tagblock1>()),
        }) { }
        public class tagblock0 : TagBlock
        {
            public tagblock0()
                : base(40, new TagBlockField[]{
                new TagBlockField(new string_id()),
                new TagBlockField(null, 4),
                new TagBlockField(new tag_pointer()),
            }) { }
        }
        public class tagblock1 : TagBlock
        {
            public tagblock1()
                : base(4, new TagBlockField[]{
                new TagBlockField(new string_id()),
            }) { }
        }
        public class tagblock2 : TagBlock
        {
            public tagblock2()
                : base(56, new TagBlockField[]{
                new TagBlockField(new tag_pointer()),
                new TagBlockField(null, 40),
                new TagBlockField(new TagBlockList<tagblock2_0>()),
            }) { }
            public class tagblock2_0 : TagBlock
            {
                public tagblock2_0()
                    : base(184, new TagBlockField[]{
                    new TagBlockField(new tag_pointer()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 12),
                    new TagBlockField(new ByteArray()),
                    new TagBlockField(null, 8),
                    new TagBlockField(new ByteArray()),
                }) { }
            }
        }
    }
}
