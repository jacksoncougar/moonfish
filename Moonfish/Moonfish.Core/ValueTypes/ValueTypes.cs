using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;
using System.Globalization;

namespace Moonfish.Core
{
    public class TagPointer : IField
    {
        IStructure parent;
        TagClass tag_class;
        TagIdentifier tag_identifier; 
        const int size = 8;

        public static implicit operator TagIdentifier(TagPointer pointer)
        {
            return pointer.tag_identifier;
        }

        byte[] IField.GetFieldData()
        {
            byte[] return_bytes = new byte[size];
            BitConverter.GetBytes((int)tag_class).CopyTo(return_bytes, 0);
            BitConverter.GetBytes(tag_identifier).CopyTo(return_bytes, 4);
            return return_bytes;
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            tag_class = (TagClass)BitConverter.ToInt32(field_data, 0);
            tag_identifier = BitConverter.ToInt32(field_data, 4);
        }

        int IField.SizeOfField
        {
            get { return size; }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }
    }

    public struct TagClass : IEquatable<TagClass>
    {
        private readonly byte a;
        private readonly byte b;
        private readonly byte c;
        private readonly byte d;

        public TagClass(params byte[] bytes)
        {
            a = default(byte);
            b = default(byte);
            c = default(byte);
            d = default(byte);
            switch (bytes.Length)
            {
                case 4:
                    d = bytes[3];
                    goto case 3;
                case 3:
                    c = bytes[2];
                    goto case 2;
                case 2:
                    b = bytes[1];
                    goto case 1;
                case 1:
                    a = bytes[0];
                    break;
                case 0:                 // Check if there are no bytes passed
                    break;
                default:                // The defualt case is now byte.Length > 4 so goto case 4 and truncate
                    goto case 4;
            }
        }

        public static explicit operator TagClass(string str)
        {
            return new TagClass(Encoding.UTF8.GetBytes(new string(str.ToCharArray().Reverse().ToArray())));
        }

        public static explicit operator string(TagClass tagclass)
        {
            return tagclass.ToString();
        }

        public static explicit operator TagClass(int integer)
        {
            return new TagClass(BitConverter.GetBytes(integer));
        }

        public static explicit operator int(TagClass type)
        {
            return BitConverter.ToInt32(new byte[] { type.a, type.b, type.c, type.d }, 0);
        }

        public static bool operator ==(TagClass object1, TagClass object2)
        {
            return (int)object1 == (int)object2;
        }

        public static bool operator !=(TagClass object1, TagClass object2)
        {
            return (int)object1 != (int)object2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TagClass)) return false;
            return this == (TagClass)obj;
        }

        public override int GetHashCode()
        {
            int i = (int)this; return i.GetHashCode();
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(new byte[] { d, c, b, a });
        }
        
        bool IEquatable<TagClass>.Equals(TagClass other)
        {
            return this == other;
        }
    }

    public class TagIdentifier : IField
    {
        IStructure parent;
        const short SaltValue = -7820;
        short index;
        public short Index { get { return index; } }
        short salt_;

        public TagIdentifier(short index)
        {
            parent = default(IStructure);
            this.index = index;
            salt_ = (short)(SaltValue + index);
        }
        public TagIdentifier(short index, short salt)
        {
            parent = default(IStructure);
            this.index = index;
            this.salt_ = salt;
        }
        public TagIdentifier(TagIdentifier copy)
        {
            this.index = copy.Index;
            this.salt_ = copy.salt_;
            this.parent = copy.parent;
        }
        public TagIdentifier() { }

        public static implicit operator int(TagIdentifier tagIndex)
        {
            return (tagIndex.salt_ << 16) | (ushort)tagIndex.Index;
        }

        public static implicit operator TagIdentifier(int i)
        {
            return new TagIdentifier((short)(i & 0x0000FFFF), (short)((i & 0xFFFF0000) >> 16));
        }

        public override string ToString()
        {
        return String.Format("{0}:{1}", Index, Convert.ToString(salt_, 16));
        }

        public const int null_identifier = -1;

        byte[] IField.GetFieldData()
        {
            return BitConverter.GetBytes(this);
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            this.Copy(new TagIdentifier(BitConverter.ToInt32(field_data, 0)));
        }

        private void Copy(TagIdentifier copy)
        {
            this.index = copy.Index;
            this.salt_ = copy.salt_;
            this.parent = copy.parent;
        }

        int IField.SizeOfField
        {
            get { return 4; }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }
    }

    public struct resource_identifier
    {
        public int Identifier;
        public TagIdentifier ParentTagIdentifier;
        public int Offset;
        public Type ResourceType;

        public resource_identifier(resource_identifier other)
        {
            Identifier = other.Identifier;
            ParentTagIdentifier = other.ParentTagIdentifier;
            Offset = other.Offset;
            ResourceType = other.ResourceType;
        }

        public const int null_identifier = -1;


        public override string ToString()
        {
            return string.Format("{0} : {1} - {2}, {3}", ParentTagIdentifier, Identifier, Offset, ResourceType);
        }
    }

    public struct FixedPointer
    {
        public int Count;
        public int Address;
    }

    public class tag_type_array : IEnumerable, IEnumerable<string>
    {
        public string this[int index]
        {
            get { return classes[index]; }
        }

        static readonly List<string> classes = new List<string>() {
                                    #region Class Strings
"$#!+",
"*cen","*eap","*ehi","*igh","*ipd","*qip","*rea","*sce",
"/**/",
"<fx>",
"BooM",
"DECP","DECR",
"MGS2",
"PRTM",
"adlg",
"ai**","ant!",
"bipd","bitm","bloc","bsdt",
"char","cin*","clu*","clwd","coll","coln","colo","cont","crea","ctrl",
"dc*s","dec*","deca","devi","devo","dgr*","dobc",
"effe","egor","eqip",
"fog ","foot","fpch",
"garb","gldf","goof","grhi",
"hlmt","hmt ","hsc*","hud#","hudg",
"item","itmc",
"jmad","jpt!",
"lens","lifi","ligh","lsnd","ltmp",
"mach","matg","mdlg","metr","mode","mpdt","mply","mulg",
"nhdt",
"obje",
"phmo","phys","pmov","pphy","proj","prt3",
"sbsp","scen","scnr","sfx+","shad","sily","skin","sky ","slit","sncl","snd!","snde","snmx","spas","spk!","ssce","sslt","stem","styl",
"tdtl","trak","trg*",
"udlg","ugh!","unhi","unic","unit",
"vehc","vehi","vrtx",
"weap","weat","wgit","wgtz","whip","wigl","wind","wphi",
#endregion
                                  };

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return classes.GetEnumerator();
        }

        #endregion

        #region IEnumerable<string> Members

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return classes.GetEnumerator();
        }

        #endregion
    }    
}