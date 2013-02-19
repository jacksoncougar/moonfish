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

            var map = new MapStream(@"C:\Users\stem\Documents\headlong.map");
            var tag = map["sbsp", ""].Export() as binary_seperation_plane_structure; map.Close();
            Mesh m = new Mesh();
            m.Load(tag.DetailObjects[0]);
            m.Show();
            //Model model = new Model(tag);
            //using (var file = File.Create(@"D:\model_raw7.bin"))
            //{
            //    file.Write(tag.DetailObjects[0].Raw.ToArray(), 0, tag.DetailObjects[0].Raw.Count);
            //}
            //model.ExportToCOLLADA();
            //model.Show();
            return;
        }
    }
}
