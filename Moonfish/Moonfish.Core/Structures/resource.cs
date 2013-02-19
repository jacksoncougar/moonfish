using Moonfish.Core.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Structures
{
    public class Resource : TagBlock
    {
        public Resource() : base(16) { }

        public Resource(DResource dResource)
            : this()
        {
            this.SetDefinitionData(dResource);
        }

        public static explicit operator DResource(Resource resource)
        {
            return resource.GetDefinition<DResource>();
        }
    }
}
