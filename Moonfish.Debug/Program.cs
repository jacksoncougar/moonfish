using Moonfish.Core;
using Moonfish.Core.Model;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System;
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
            var tag = map["bitm", "coconut"].Export() as Bitmap_Collection; map.Close();
            //Model model = new Model(tag);
            //model.ExportNodesToCollada();
            //model.Show();
            return;
        }
    }
}
