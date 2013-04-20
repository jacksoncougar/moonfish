using Moonfish.Core;
using Moonfish.Core.Model;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Linq;
using Moonfish.Core.Definitions;

namespace Moonfish.Debug
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Moonfish Core:");
            Log.OnLog = new Log.LogMessageHandler(Console.WriteLine);
            
            var map = new MapStream(@"D:\h2v\ascension.map");
            return;
            var tag = map["mode", "warthog"].Export() as model; map.Close();

            Moonfish.WavefrontLoader.WavefrontOBJ obj = new WavefrontLoader.WavefrontOBJ();
            obj.Parse(@"D:\halo_2\plane.obj");
            using (var file = File.Create(@"D:\tag_block_export.bin"))
            {
                tag.Serialize(file);
            }

            return;
        }
    }
}
