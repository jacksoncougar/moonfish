using Moonfish.Core.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core
{
    [TagClass("sbsp")]
    public class binary_seperation_plane_structure : TagBlock
    {
        public IList<detail_object> DetailObjects { get { return base.fixed_fields[0].Object as IList<detail_object>; } }
        
        public binary_seperation_plane_structure()
            : base(588,
            new TagBlockField(null, 172),
            new TagBlockField(new TagBlockList<detail_object>())) { }

        public class detail_object : TagBlock
        {
            public DetailObjectRaw Raw { get { return base.fixed_fields[0].Object as DetailObjectRaw; } }
            public TagBlockList<Resource> Resources { get { return base.fixed_fields[1].Object as TagBlockList<Resource>; } }

            public detail_object()
                : base(176,
                new TagBlockField(null, 40),
                new TagBlockField(new DetailObjectRaw()),
                new TagBlockField(new TagBlockList<Resource>())) { }
        }
    }

    public class DetailObjectRaw : ModelRaw
    {
    }
}
