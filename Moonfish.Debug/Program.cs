using Moonfish.Core;
using System.Collections.Generic;
using System.IO;

namespace Moonfish.Debug
{
    class Program
    {
        static void Main(string[] args)
        {
            MapStream map = new MapStream(@"C:\Users\stem\Documents\zanzibar.map");
            map.SerializeTag(map.Tags[6]);
            return;

            ////Memory block_of_memory = new Memory();
            ////BinaryWriter bin_writer = new BinaryWriter(block_of_memory);
            ////bin_writer.Write(new byte[256]);
            ////TagBlock some_struct = new TagBlock() { this_sizeof = 32, this_pointer = 8 };
            ////MemoryStream stream = block_of_memory.getmem(some_struct);
            ////bin_writer = new BinaryWriter(stream);
            ////stream.Position = 0;
            ////bin_writer.Write(new byte[] { 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD, 0xCD });
            ////return;
            ////TagBlockWrapper antenna_tag = new TagBlockWrapper((tag_class)"ant!", null, null);
            ////using (FileStream file = File.OpenRead(@"D:\halo_2\test_cereal.txt")) antenna_tag.Deserialize(file);
            ////return;
            //MapStream map = new MapStream(@"C:\Users\stem\Documents\zanzibar.map");
            //{
            //    const int moon_shader_tag = 6;
            //    const int hill_shader_tag = 4886;
            //    {
            //        TagBlockWrapper hill_shader = map.PreProcessTag(map.Tags[hill_shader_tag]);
            //        map.PostProcessTag(hill_shader);
            //    }
            //}
            //return;
            //foreach (var tag in map.Tags)
            //{
            //    if (tag.Type.ToString() == "shad" && map.Paths[tag.Id.Index].Contains("moon"))
            //    {
            //        const int moon_shader_tag = 6;
            //        const int hill_shader_tag = 4886;
            //        string path = map.Paths[tag.Id.Index];
            //        TagBlockWrapper hill_shader = map.PreProcessTag(map.Tags[hill_shader_tag]);
            //        TagBlockWrapper moon_shader = map.PreProcessTag(map.Tags[moon_shader_tag]);
            //        break;
            //    }
            //}
        }
    }
}
