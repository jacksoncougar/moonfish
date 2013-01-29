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
            map.SerializeTag(map.Tags[21]);
            return;
        }
    }
}
