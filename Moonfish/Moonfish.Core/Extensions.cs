using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Moonfish.Core
{
    public static class Extensions
    {
        public static string ReadFixedString(this BinaryReader binreader, int length, bool trim_null_characters = true)
        {
            if (trim_null_characters)
                return Encoding.UTF8.GetString(binreader.ReadBytes(length)).Trim(char.MinValue);
            else return Encoding.UTF8.GetString(binreader.ReadBytes(length));
        }

        public static tag_class ReadTagType(this BinaryReader binreader)
        {
            return new tag_class(binreader.ReadBytes(4));
        }
    }
}
