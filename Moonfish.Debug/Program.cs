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
            var map = new MapStream(@"C:\Users\stem\Documents\headlong.map");
            var tag = map.GetTag(map.FindFirst((TagClass)"mode", "pallet"));
            TagWrapper fiddycent = new TagWrapper(tag, map);
            return;
            var wavefront_object = @"D:\halo_2\quadshere-x.obj";
            var folder = @"D:\halo_2\temp\";
            var tagname = string.Format(@"_remnant\custom\{0}\objects\{1}\{1}", DateTime.Now.Ticks, "debug");
            Mesh mesh_data = new Mesh();
            if (mesh_data.ImportFromWavefront(wavefront_object))
            {
                mesh_data.ExportForEntity(folder, tagname);
                Mesh mesh_data2 = new Mesh();
                DResource[] outs;
                byte[] buffer = mesh_data.Serialize(out outs);
                mesh_data2.Load(buffer, outs, mesh_data.Compression);
                mesh_data2.Show();
            }
        }
//        static void _Main(string[] args)
//        {
//            Console.WriteLine("Moonfish Core:");
//            Log.OnLog = new Log.LogMessageHandler(Console.WriteLine);
//            string source_filename = string.Empty;
//            string desination_folder = string.Empty;
//            string tagname = string.Empty;
//            bool exit = false;
//            while (!exit)
//            {
                
//                Console.WriteLine(
//@"Commands: -s [filename] -d [folder] -tn [tagname] or -x to exit
//[filename] is the source file (wavefront.obj)
//[folder] is the destination location for tag export
//[tagname] is the name of the tag in the map");
//                source_filename = string.Empty;
//                desination_folder = string.Empty;
//                tagname = string.Empty; 
//                for (int i = 0; i < args.Length; ++i)
//                {
//                    if (args[i].ToLower() == "-x")
//                    {
//                        exit = true;
//                        break;
//                    }
//                    if (args[i].ToLower() == "-s")
//                    {
//                        source_filename = args[i + 1];
//                    }
//                    else if (args[i].ToLower() == "-d")
//                    {
//                        desination_folder = args[i + 1];
//                    }
//                    else if (args[i].ToLower() == "-tn")
//                    {
//                        tagname = args[i + 1];
//                    } 
//                    if (source_filename != string.Empty && desination_folder != string.Empty && tagname != string.Empty)
//                    {
//                        Mesh mesh_data = new Mesh();
//                        if (mesh_data.ImportFromWavefront(source_filename))
//                        {
//                            mesh_data.ExportForEntity(desination_folder, tagname);
//                            Mesh mesh_data2 = new Mesh();
//                            DResource[] outs;
//                            byte[] buffer = mesh_data.Serialize(out outs);
//                            mesh_data2.Load(buffer, outs, mesh_data.Compression);
//                            mesh_data.Show();
//                        }
//                    }
//                }
//                args = Console.ReadLine().Split(' ');
//            }           
            
//            return;
//        }
    }
}
