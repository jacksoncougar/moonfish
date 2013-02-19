using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    /// <summary>
    /// Vertex-level skeleton animation: two skeleton_nodes with a percentage of influence for each skeleton_node
    /// </summary>
    public struct VertexWeight
    {
        public short Bone0;
        public short Bone1;
        public float Bone0_weight;
        public float Bone1_weight;

        /// <summary>
        /// Creates a VertexWeight object for a single skeleton-node:
        /// by setting the second skeleton-node to 0 and giving 100% influence to the first skeleton-node
        /// </summary>
        /// <param name="bone_index"></param>
        public VertexWeight(short bone_index)
        {
            Bone0 = bone_index;
            Bone0_weight = 1.0f;

            Bone1 = 0;
            Bone1_weight = 0.0f;
        }

        public override string ToString()
        {
            return string.Format("b0 {0}: {1}f - b1 {2}: {3}f", Bone0, Bone0_weight, Bone1, Bone1_weight);    
        }
    }
}
