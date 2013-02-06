using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Moonfish.Core.Model;
using OpenTK;
using Moonfish.Core.Definitions;

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

        public static TagClass ReadTagType(this BinaryReader binreader)
        {
            return new TagClass(binreader.ReadBytes(4));
        }

        public static void WriteFourCC(this BinaryWriter writer, string code)
        {
            byte[] buffer = new byte[4];
            byte[] charbytes = Encoding.UTF8.GetBytes(code);
            Array.Copy(charbytes, buffer, charbytes.Length % 5);
            Array.Reverse(buffer);
            writer.Write(buffer);
        }

        public static void WritePadding(this BinaryWriter writer, int alignment)
        {
            writer.Write(new byte[Padding.GetCount(writer.BaseStream.Position, alignment)]);
        }

        public static void Write(this BinaryWriter binary_writer, Vector3 vector3)
        {
            binary_writer.Write(vector3.X);
            binary_writer.Write(vector3.Y);
            binary_writer.Write(vector3.Z);
        }
        public static Vector3 ReadVector3(this BinaryReader binary_reader)
        {
            return new Vector3(binary_reader.ReadSingle(), binary_reader.ReadSingle(), binary_reader.ReadSingle());
        }

        public static void Write(this BinaryWriter binary_writer, IDefinition definition)
        {
            binary_writer.Write(definition.ToArray());
        }
        public static T ReadDefinition<T>(this BinaryReader binary_reader) where T : IDefinition, new()
        {
            var item = new T();
            item.FromArray(binary_reader.ReadBytes(item.Size));
            return item;
        }

        public static void Write(this BinaryWriter binary_writer, Quaternion quaternion)
        {
            binary_writer.Write(quaternion.X);
            binary_writer.Write(quaternion.Y);
            binary_writer.Write(quaternion.Z);
            binary_writer.Write(quaternion.W);
        }
        public static Quaternion ReadQuaternion(this BinaryReader binary_reader)
        {
            return new Quaternion(binary_reader.ReadSingle(), binary_reader.ReadSingle(), binary_reader.ReadSingle(), binary_reader.ReadSingle());
        }

        public static void Write(this BinaryWriter binary_writer, TagClass tclass)
        {
            binary_writer.Write((int)tclass);
        }
        public static TagClass ReadTagClass(this BinaryReader binary_reader)

        {
            return (TagClass)binary_reader.ReadInt32();
        }

        public static TagIdentifier ReadTagID(this BinaryReader binary_reader)
        {
            return (TagIdentifier)binary_reader.ReadInt32();
        }

        public static void Write(this BinaryWriter binary_writer, Range range)
        {
            binary_writer.Write(range.min);
            binary_writer.Write(range.max);
        }
        public static Range ReadRange(this BinaryReader binary_reader)
        {
            return new Range(binary_reader.ReadSingle(), binary_reader.ReadSingle());
        }

        public static void Write(this BinaryWriter binary_writer, StringID string_id)
        {
            binary_writer.Write((int)string_id);
        }
        public static StringID ReadStringID(this BinaryReader binary_reader)
        {
            return (StringID)binary_reader.ReadInt32();
        }
    }
}
