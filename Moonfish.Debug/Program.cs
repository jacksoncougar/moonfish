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

            //var collada = Collada141.COLLADA.Load(@"D:\halo_2\geosphere.dae");
            //Mesh mesh = new Mesh();
            //mesh.ImportFromCollada(collada);
            //mesh.Show();

            var map = new MapStream(@"C:\Users\stem\Documents\shared.map");
            var tag = map["mode", "warthog"].Export() as model; map.Close();
            //RenderMesh m = new RenderMesh();
            //m.Load(tag.Sections[3].Raw, tag.Sections[3].Resources, tag.Compression[0]);
            
            //Entity.ExportForEntity(@"D:\warthog", "racoon", m);
            //m.Show();
            //var tag = map["sbsp", ""].Export() as binary_seperation_plane_structure; map.Close();
            //RenderMesh m = new RenderMesh();
            //m.Load(tag.Water[0]);
            //m.Show();
            //Model model = new Model(tag);
            using (var file = File.Create(@"D:\tag_block_export.bin"))
            {
                tag.Serialize(file);
            }
            //model.ExportToCOLLADA();
            //model.Show();
            return;
        }
    }
}
