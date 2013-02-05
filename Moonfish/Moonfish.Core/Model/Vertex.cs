using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public struct StandardVertex
    {
        public Vector2 TextureCoordinates;
        public Vector3 Normal;
        public Vector3 Position;
        public Vector3 Bitangent;
        public Vector3 Tangent;

        public override string ToString()
        {
            return string.Format("{0}, {1},\n{2}, {3}, {4}", Position, TextureCoordinates, Normal, Bitangent, Tangent);
        }
    }
}
