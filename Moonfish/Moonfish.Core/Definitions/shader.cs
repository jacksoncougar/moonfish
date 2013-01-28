using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{
    [TagClass("shad")]
    public class shader : TagBlock
    {
        public shader()
            : base(84, new TagBlockField[]{
                new TagBlockField(new tag_pointer()),
                new TagBlockField(new string_id()),
                new TagBlockField(new TagBlockList<tagblock0>()),
                new TagBlockField(null, 12),
                new TagBlockField(new TagBlockList<tagblock1>()),
                new TagBlockField(null, 4),
                new TagBlockField(new TagBlockList<tagblock2>()),
                new TagBlockField(new tag_pointer()),
            }) { }
        public class tagblock0 : TagBlock
        {
            public tagblock0()
                : base(80, new TagBlockField[]{
                new TagBlockField(new tag_pointer()),
                new TagBlockField(new tag_pointer()),
                new TagBlockField(null, 28),
                new TagBlockField(new tag_pointer()),
                new TagBlockField(new tag_pointer()),
            }) { }
        }
        public class tagblock1 : TagBlock
        {
            public tagblock1()
                : base(124, new TagBlockField[] { 
                new TagBlockField(null, 4),
                new TagBlockField(new TagBlockList<tagblock1_0>()),
                new TagBlockField(new TagBlockList<tagblock1_1>()),
                new TagBlockField(new TagBlockList<tagblock1_2>()),
                new TagBlockField(new TagBlockList<tagblock1_3>()),
                new TagBlockField(new TagBlockList<tagblock1_4>()),
                new TagBlockField(new TagBlockList<tagblock1_5>()),
                new TagBlockField(new TagBlockList<tagblock1_6>()),
                new TagBlockField(new TagBlockList<tagblock1_7>()),
                new TagBlockField(new TagBlockList<tagblock1_8>()),
                new TagBlockField(new TagBlockList<tagblock1_9>()),
                new TagBlockField(new TagBlockList<tagblock1_10>()),
                new TagBlockField(new TagBlockList<tagblock1_11>()),
                new TagBlockField(new TagBlockList<tagblock1_12>()),
                new TagBlockField(new TagBlockList<tagblock1_13>()),
            }) { }
            public class tagblock1_0 : TagBlock
            {
                public tagblock1_0()
                    : base(12) { }
            }
            public class tagblock1_1 : TagBlock
            {
                public tagblock1_1() : base(4) { }
            }
            public class tagblock1_2 : TagBlock
            {
                public tagblock1_2() : base(16) { }
            }
            public class tagblock1_3 : TagBlock
            {
                public tagblock1_3() : base(6) { }
            }
            public class tagblock1_4 : TagBlock
            {
                public tagblock1_4() : base(2) { }
            }
            public class tagblock1_5 : TagBlock
            {
                public tagblock1_5() : base(2) { }
            }
            public class tagblock1_6 : TagBlock
            {
                public tagblock1_6() : base(10) { }
            }
            public class tagblock1_7 : TagBlock
            {
                public tagblock1_7()
                    : base(20, new TagBlockField[] { 
                    new TagBlockField(new string_id()),
                    new TagBlockField(new string_id()),
                    new TagBlockField(null, 4),
                    new TagBlockField(new ByteArray()),
                }) { }
            }
            public class tagblock1_8 : TagBlock
            {
                public tagblock1_8() : base(4) { }
            }
            public class tagblock1_9 : TagBlock
            {
                public tagblock1_9() : base(2) { }
            }
            public class tagblock1_10 : TagBlock
            {
                public tagblock1_10() : base(4) { }
            }
            public class tagblock1_11 : TagBlock
            {
                public tagblock1_11() : base(4) { }
            }
            public class tagblock1_12 : TagBlock
            {
                public tagblock1_12() : base(12) { }
            }
            public class tagblock1_13 : TagBlock
            {
                public tagblock1_13() : base(4) { }
            }
        }
        public class tagblock2 : TagBlock
        {
            public tagblock2()
                : base(8, new TagBlockField[] { 
                new TagBlockField(null,4),
            }) { }
        }
    }
}
