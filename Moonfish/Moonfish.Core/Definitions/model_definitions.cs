using Moonfish.Core.Model;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Moonfish.Core.Definitions
{
    public interface ITagDefinition
    {
        byte[] ToArray();
        void FromArray(byte[] buffer);
    }

    public class DCompressionRanges : ITagDefinition
    {
        public Range X;
        public Range Y;
        public Range Z;
        public Range U;
        public Range V;

        byte[] ITagDefinition.ToArray()
        {
            MemoryStream buffer = new MemoryStream(40);
            BinaryWriter bin = new BinaryWriter(buffer);
            bin.Write(X);
            bin.Write(Y);
            bin.Write(Z);
            bin.Write(U);
            bin.Write(V);
            return buffer.ToArray();
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
            X = bin.ReadRange();
            Y = bin.ReadRange();
            Z = bin.ReadRange();
            U = bin.ReadRange();
            V = bin.ReadRange();
        }

        internal void Expand(float p)
        {
            X = Range.Expand(X, p);
            Y = Range.Expand(Y, p);
            Z = Range.Expand(Z, p);
            U = Range.Expand(U, p);
            V = Range.Expand(V, p);
        }
    }

    public class DRegion : ITagDefinition
    {
        public StringID Name = StringID.Zero;
        public short NodeMapOffset = -1;
        public short NodeMapSize = 0;

        byte[] ITagDefinition.ToArray()
        {
            MemoryStream buffer = new MemoryStream(16);
            BinaryWriter bin = new BinaryWriter(buffer);
            bin.Write(Name);
            bin.Write(NodeMapOffset);
            bin.Write(NodeMapSize);
            return buffer.ToArray();
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
            Name = bin.ReadStringID();
            NodeMapOffset = bin.ReadInt16();
            NodeMapSize = bin.ReadInt16();
        }
    }

    public class DSection : ITagDefinition
    {
        public enum VertexDefinition
        {
            Fixed = 0,
            RigidBone = 1,
            WeightedBone = 2,
        }
        [Flags]
        public enum CompressionFlags : ushort
        {
            Uncompressed = 0,
            CompressVertexData = 1,
            CompressTexcoordData = 2,

        }

        public VertexDefinition VertexType = VertexDefinition.Fixed;
        public ushort VertexCount;
        public ushort TriangleCount;
        public CompressionFlags Compression = CompressionFlags.CompressTexcoordData | CompressionFlags.CompressVertexData;
        public uint RawOffset;
        public uint RawSize;
        public uint HeaderSize = 112;
        public uint RawDataSize;

        byte[] ITagDefinition.ToArray()
        {
            MemoryStream buffer = new MemoryStream();
            BinaryWriter bin = new BinaryWriter(buffer);
            bin.Write((int)VertexType);
            bin.Write((ushort)VertexCount);
            bin.Write(TriangleCount);
            bin.Seek(16, SeekOrigin.Current);
            bin.Write((int)Compression);
            bin.Seek(28, SeekOrigin.Current);
            bin.Write(RawOffset);
            bin.Write(RawSize);
            bin.Write(HeaderSize);
            bin.Write(RawDataSize);
            return buffer.ToArray();
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
            VertexType = (VertexDefinition)bin.ReadInt16();
            VertexCount = bin.ReadUInt16();
            TriangleCount = bin.ReadUInt16();
            bin.BaseStream.Seek(16, SeekOrigin.Current);
            Compression = (CompressionFlags)bin.ReadUInt16();
            bin.BaseStream.Seek(28, SeekOrigin.Current);
            RawOffset = bin.ReadUInt32();
            RawSize = bin.ReadUInt32();
            HeaderSize = bin.ReadUInt32();
            RawDataSize = bin.ReadUInt32();
        }
    }

    public class DGroup : ITagDefinition
    {
        [Flags]
        public enum DetailLevel
        {
            LOD1,
            LOD2,
            LOD3,
            LOD4,
            LOD5,
            LOD6,
            All = LOD1 | LOD2 | LOD3 | LOD4 | LOD5 | LOD6,
        }
        public DetailLevel Levels = DetailLevel.All;

        byte[] ITagDefinition.ToArray()
        {
            return BitConverter.GetBytes((int)Levels);
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            Levels = (DetailLevel)BitConverter.ToUInt32(buffer, 0);
        }
    }

    public class DNode : ITagDefinition
    {
        public StringID Name = StringID.Zero;
        public short Parent_NodeIndex = -1;
        public short FirstChild_NodeIndex = -1;
        public short NextSibling_NodeIndex = -1;
        public Quaternion Rotation = new Quaternion(0, 0, 0, -1);
        public Vector3 Position = Vector3.Zero;
        public float Scale = 1.0f;
        public Vector3 Right = Vector3.UnitX;
        public Vector3 Forward = Vector3.UnitY;
        public Vector3 Up = Vector3.UnitZ;

        byte[] ITagDefinition.ToArray()
        {
            MemoryStream buffer = new MemoryStream();
            BinaryWriter bin = new BinaryWriter(buffer);
            bin.Write(Name);
            bin.Write(Parent_NodeIndex);
            bin.Write(FirstChild_NodeIndex);
            bin.Write(NextSibling_NodeIndex);
            bin.Seek(sizeof(short), SeekOrigin.Current);
            bin.Write(Position);
            bin.Write(Rotation);
            bin.Write(Scale);
            bin.Write(Right);
            bin.Write(Forward);
            bin.Write(Up);
            return buffer.ToArray();
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
            Name = bin.ReadStringID();
            Parent_NodeIndex = bin.ReadInt16();
            FirstChild_NodeIndex = bin.ReadInt16();
            NextSibling_NodeIndex = bin.ReadInt16();
            bin.BaseStream.Seek(sizeof(short), SeekOrigin.Current);
            Position = bin.ReadVector3();
            Rotation = bin.ReadQuaternion();
            Scale = bin.ReadSingle();
            Right = bin.ReadVector3();
            Forward = bin.ReadVector3();
            Up = bin.ReadVector3();
        }
    }

    public class DShader : ITagDefinition
    {
        tag_id Shader = tag_id.null_identifier;

        byte[] ITagDefinition.ToArray()
        {
            MemoryStream buffer = new MemoryStream();
            BinaryWriter bin = new BinaryWriter(buffer);
            bin.Write((tag_class)"shad");
            bin.Write(tag_id.null_identifier);
            bin.Write((tag_class)"shad");
            bin.Write(Shader);
            return buffer.ToArray();
        }

        void ITagDefinition.FromArray(byte[] buffer)
        {
            BinaryReader bin = new BinaryReader(new MemoryStream(buffer));
            bin.BaseStream.Seek(12, SeekOrigin.Begin);
            Shader = bin.ReadTagID();
        }
    }
}
