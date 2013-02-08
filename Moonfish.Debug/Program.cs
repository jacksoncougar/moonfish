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

            var map = new MapStream(@"C:\Users\stem\Documents\shared.map");
            var tag = map["mode", "warthog"].Export() as model; map.Close();
            Model model = new Model();
            model.Load(tag);
            model.Show();
            return;
        }
    }
}
