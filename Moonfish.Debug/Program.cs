using Moonfish.Core;
using System.Collections.Generic;
using System.IO;

namespace Moonfish.Debug
{
    class Program
    {
        static void Main(string[] args)
        {
            //TagBlockWrapper antenna_tag = new TagBlockWrapper((tag_class)"ant!", null, null);
            //using (FileStream file = File.OpenRead(@"D:\halo_2\test_cereal.txt")) antenna_tag.Deserialize(file);
            //return;
            MapStream map = new MapStream(@"C:\Users\stem\Documents\zanzibar.map");
            {
                const int moon_shader_tag = 6;
                const int hill_shader_tag = 4886;
                TagBlockWrapper hill_shader = map.PreProcessTag(map.Tags[hill_shader_tag]);
                TagBlockWrapper moon_shader = map.PreProcessTag(map.Tags[moon_shader_tag]);
            }
            return;
            foreach (var tag in map.Tags)
            {
                if (tag.Type.ToString() == "shad" && map.Paths[tag.Id.Index].Contains("moon"))
                {
                    const int moon_shader_tag = 6;
                    const int hill_shader_tag = 4886;
                    string path = map.Paths[tag.Id.Index];
                    TagBlockWrapper hill_shader = map.PreProcessTag(map.Tags[hill_shader_tag]);
                    TagBlockWrapper moon_shader = map.PreProcessTag(map.Tags[moon_shader_tag]);
                    break;
                }
            }
        }
    }
}
