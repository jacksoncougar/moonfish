using Moonfish.Core.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Model
{
    public class Model
    {
        CompressionInformation Compression;
        public Region[] Regions;
        public Mesh[] Mesh;

        public void Load(model Tag)
        {
            Compression = Tag.Compression[0].GetDefinition<CompressionInformation>();
            Regions = Tag.Regions.Select(x => x.GetDefinition<Region>()).ToArray();
            for (int i = 0; i < Tag.Regions.Count; ++i)
            {
                Regions[i].Permutations = Tag.Regions[i].Permutations.Select(y => y.GetDefinition<DPermutation>()).ToArray();
            }

            Mesh = new Mesh[Tag.Sections.Count];
            for (int i = 0; i < Mesh.Length; ++i)
            {
                Mesh[i] = new Core.Model.Mesh();
                Mesh[i].Load(Tag.Sections[i].Raw, Tag.Sections[i].Resources, Tag.Compression[0]);
            }
        }

        public void Show()
        {
            ModelView render_window = new ModelView(this);
            render_window.Run(60);
        }
    }
}
