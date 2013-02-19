using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public struct VertexWeight
    {
        public short Bone0;
        public short Bone1;
        public float Bone0_weight;
        public float Bone1_weight;

        public VertexWeight(short bone_index)
        {
            Bone0 = bone_index;
            Bone0_weight = 1.0f;

            Bone1 = -1;
            Bone1_weight = 0.0f;
        }
    }
}
