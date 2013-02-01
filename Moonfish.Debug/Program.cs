using Moonfish.Core;
using Moonfish.Core.Model;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace Moonfish.Debug
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Moonfish Core:");
            //Console.ReadKey();
            Log.OnLog = new Log.LogMessageHandler(Console.WriteLine);
            //Log.OnLog  = new Log.LogMessageHandler(
            //Moonfish.Core.Raw.RadixSorter radsort = new Core.Raw.RadixSorter();
            //return;
            MapStream map = new MapStream(@"C:\Users\stem\Documents\zanzibar.map");
            model tag = (model)map.GetTag(map.FindFirst((tag_class)"mode", "dumpster"));
            var raw = tag.Sections[0].GetRawPointer();
            map.Position = raw.Address;
            byte[] raw_data = new byte[raw.Length];
            map.Read(raw_data, 0, raw_data.Length);
            Mesh mesh_data = new Mesh();
            mesh_data.Load(raw_data, tag.Sections[0].GetSectionResources(), tag.GetBoundingBox().GetCompressionRanges());
            //mesh_data.Show();
            //mesh_data.ImportFromWavefront(@"D:\halo_2\monkey.obj");
            //mesh_data.Load(raw_data, tag.Sections[0].GetSectionResources(), tag.GetBoundingBox().GetCompressionRanges());
            
            return;
            //map.Tags[map.FindFirst((tag_class)"mode", "default_object")]);
            //Moonfish.Core.Model.Mesh mesh = new Moonfish.Core.Model.Mesh();
            //if (meta.Type == (tag_class)"mode")
            //{
            //    model collection = (model)block;
            //    foreach (var bitmap in collection.Sections)
            //    {
            //        var resource = bitmap.GetRawPointer();
            //        BinaryReader bin_reader = new BinaryReader(this);
            //        bin_reader.BaseStream.Position = resource.Address;
            //        raw_data.Write(bin_reader.ReadBytes(resource.Length), 0, resource.Length);
            //        {//because
            //            {//fuck you
            //                Mesh mesh = new Mesh();
            //                mesh.Load(raw_data.ToArray(), bitmap.GetSectionResources(), collection.GetBoundingBox().GetCompressionRanges());
            //                //mesh.ExportAsWavefront(@"D:\halo_2\wavefront.obj");
            //            }
            //        }
            //    }
            ////}
            //mesh.ImportFromWavefront(@"D:\halo_2\untitled.obj");
            return;
            return;
        }
    }
}
