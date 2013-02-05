using System;
using System.Collections.Generic;
using System.IO;

namespace Moonfish.Core
{
    public interface IField
    {
        byte[] GetFieldData();
        void SetFieldData(byte[] field_data, IStructure caller = null);
        int SizeOfField { get; }

        void Initialize(IStructure calling_structure);
    }

    public interface IStructure
    {
        void SetField(IField calling_field);
        IField GetField(int field_index);
    }

    public interface IPointable
    {
        void Parse(Memory mem);
        void PointTo(Memory mem);
        int Address { get; set; }
        int Alignment { get; }
        int SizeOf { get; }
        void CopyTo(Stream stream);
    }

    public class Memory : MemoryStream
    {
        public List<mem_ref> instance_table = new List<mem_ref>();
        int start_address = 0;
        public int Address
        {
            get { return start_address; }
        }
        internal void SetAddress(int address)
        {
            start_address = address;
        }
        public Memory Copy(int address)
        {
            mem_ref[] instance_table__ = this.instance_table.ToArray();
            int shift__ = 0;

            for (int i = 0; i < instance_table__.Length; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    int new_address = instance_table__[i].address - start_address + address;
                    int padding = instance_table__[i].GetPaddingCount(new_address);
                    shift__ += padding;
                    instance_table__[i].SetAddress(new_address, false);
                }
            }


            byte[] buffer_ = new byte[this.Length + shift__];
            Memory copy = new Memory(buffer_, this.start_address);
            instance_table__[0].client.PointTo(copy);
            copy.start_address = address;
            copy.instance_table = new List<mem_ref>(instance_table__);

            BinaryReader bin_reader = new BinaryReader(this);
            for (int i = 0; i < copy.instance_table.Count; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    copy.Position = copy.instance_table[i].address - copy.start_address;
                    this.Position = this.instance_table[i].address - start_address;
                    int length = instance_table__[i].client.SizeOf;
                    copy.Write(bin_reader.ReadBytes(length), 0, length);
                }
            }
            for (int i = 0; i < instance_table__.Length; ++i)
            {
                if (instance_table__[i].external == false)
                {
                    instance_table__[i].SetAddress(instance_table__[i].address);
                }
            }
            return copy;
        }

        public Memory(byte[] buffer, int translation = 0)
            : base(buffer, 0, buffer.Length, true, true)
        {
            start_address = translation;
        }
        public Memory()
            : base() { }
        public bool Contains(IPointable calling_object)
        {
            return (calling_object.Address - start_address >= 0
                && calling_object.Address - start_address + calling_object.SizeOf <= Length);
        }
        public MemoryStream getmem(IPointable data)
        {
            if (this.Contains(data))
                return new MemoryStream(base.GetBuffer(), data.Address - start_address, data.SizeOf);
            else return null;
        }

        /// <summary>
        /// This is a really poorly implemented way to parse a tagblock, and write it to a stream...
        /// </summary>
        /// <param name="source"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool Map(TagBlock source, Stream stream)
        {
            //calculate pointers for all the tagblocks, then set thier pointers
            //then copy all the memory to a single memory.
            var start_offset = stream.Position;
            stream.Write(source.GetMemory().ToArray(), 0, (source as IPointable).SizeOf);//reserve
            (source as IPointable).CopyTo(stream);
            stream.Position = start_offset;
            stream.Write(source.GetMemory().ToArray(), 0, (source as IPointable).SizeOf);//update
            //return true;
            //using (BinaryWriter bin = new BinaryWriter(File.Create(@"D:\debug.meta")))
            //{
            //    bin.Write(memory.ToArray());
            //}
            return true;
        }

        public struct FixedPointer
        {
            int address;
            int count;
        }
        public struct mem_ref
        {
            public IPointable client;
            public int address;
            public int count;
            public Type type;
            public bool external;
            public bool isnull { get { return count == 0 && address == Halo2.nullptr; } }

            public void SetAddress(int address, bool commit = true)
            {
                if (commit && client != null) client.Address = address;
                this.address = address;
            }

            public int GetPaddingCount(int address)
            {
                if (client != null) return (int)Padding.GetCount(address, client.Alignment);
                else throw new Exception();
            }

            public override string ToString()
            {
                return string.Format("{0} : x{1} : {2}", address, count, external);
            }
        }
    }
}
