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
    public struct tag_pointer : IField, IReference<tag_id>, IReference<tag_class>
    {
        IStructure parent;
        tag_class tag_class;
        tag_id tag_identifier; 
        const int size = 8;

        byte[] IField.GetFieldData()
        {
            byte[] return_bytes = new byte[size];
            BitConverter.GetBytes((int)tag_class).CopyTo(return_bytes, 0);
            BitConverter.GetBytes(tag_identifier).CopyTo(return_bytes, 4);
            return return_bytes;
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            tag_class = (tag_class)BitConverter.ToInt32(field_data, 0);
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

        tag_id IReference<tag_id>.GetToken()
        {
            return tag_identifier;
        }

        void IReference<tag_id>.SetToken(tag_id token)
        {
            tag_identifier = token;
            parent.SetField(this);
        }

        tag_class IReference<tag_class>.GetToken()
        {
            return (tag_class)tag_class;
        }

        void IReference<tag_class>.SetToken(tag_class token)
        {
            tag_class = token;
            parent.SetField(this);
        }

        bool IReference<tag_id>.IsNullReference
        {
            get { return (this.tag_identifier == tag_id.null_identifier); }
        }


        bool IReference<tag_class>.IsNullReference
        {
            get { return ((int)this.tag_class == -1); }
        }
    }

    public struct tag_class : IEquatable<tag_class>
    {
        private readonly byte[] fourcc_;

        public tag_class(byte[] fourcc)
        {
            //  initialize our array to null, length 4
            fourcc_ = new byte[4];
            //  copy bytes from input to output, clamping the number of bytes to copy to within 4
            Array.Copy(fourcc, 0, fourcc_, 0, fourcc.Length % 5);
        }

        public static explicit operator tag_class(string str)
        {
            return new tag_class(Encoding.UTF8.GetBytes(new string(str.ToCharArray().Reverse().ToArray())));
        }

        public static explicit operator string(tag_class str)
        {
            return str.ToString();
        }

        public static explicit operator tag_class(int integer)
        {
            return new tag_class(BitConverter.GetBytes(integer));
        }

        public static explicit operator int(tag_class type)
        {
            return BitConverter.ToInt32(type.fourcc_, 0);
        }

        public static bool operator ==(tag_class object1, tag_class object2)
        {
            return (int)object1 == (int)object2;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is tag_class)) return false;
            return this == (tag_class)obj;
        }

        public override int GetHashCode()
        {
            int i = (int)this; return i.GetHashCode();
        }

        public static bool operator !=(tag_class object1, tag_class object2)
        {
            return (int)object1 != (int)object2;
        }

        public override string ToString()
        {
            return new string(Encoding.UTF8.GetString(fourcc_, 0, 4).ToCharArray().Reverse().ToArray());
        }

        string ToReverseString()
        {
            throw new NotSupportedException();
            //byte[] fourcc_copy = new byte[4];
            //Buffer.BlockCopy(fourcc_, 0, fourcc_copy, 0, 4);
            //Array.Reverse(fourcc_copy);
            //return Encoding.UTF8.GetString(fourcc_copy, 0, 4);
        }

        string ToPathSafeString()
        {
            throw new NotSupportedException();
            //StringBuilder builder = new StringBuilder(this.ToString());
            //foreach (char c in Path.GetInvalidPathChars())
            //    builder.Replace(c, ' ');
            //return builder.ToString().Trim();
        }

        bool IEquatable<tag_class>.Equals(tag_class other)
        {
            return this == other;
        }
    }

    public struct tag_id : IField
    {
        IStructure parent;
        const short DATUM = -7820;

        public short Index;

        short salt;

        public tag_id(short index)
        {
            parent = default(IStructure);
            Index = index;
            salt = (short)(DATUM + index);
        }

        public tag_id(short index, short salt)
        {
            parent = default(IStructure);
            this.Index = index;
            this.salt = salt;
        }

        public static implicit operator int(tag_id tagIndex)
        {
            return (tagIndex.salt << 16) | (ushort)tagIndex.Index;
        }

        public static implicit operator tag_id(int i)
        {
            return new tag_id((short)(i & 0x0000FFFF), (short)((i & 0xFFFF0000) >> 16));
        }

        public override string ToString()
        {
        return String.Format("{0}:{1}", Index, Convert.ToString(salt, 16));
        }

        public const int null_identifier = -1;

        byte[] IField.GetFieldData()
        {
            return BitConverter.GetBytes(this);
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            tag_id copy = BitConverter.ToInt32(field_data, 0);
            this.Index = copy.Index;
            this.salt = copy.salt;
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
        public tag_id ParentTagIdentifier;
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

    public struct string_id : IField, IReference<string_id>
    {
        IStructure parent;
        public readonly short Index;
        public readonly short Length;
        byte nullbyte;
        const int size = 4;

        public static explicit operator int(string_id strRef)
        {
            return (strRef.Length << 24)| strRef.nullbyte | (ushort)strRef.Index;
        }

        public static explicit operator string_id(int i)
        {
            byte[] bytes = BitConverter.GetBytes(i);
            return new string_id(BitConverter.ToInt16(bytes, 0), (sbyte)bytes[3], bytes[2]);
        }

        byte[] IField.GetFieldData()
        {
            return BitConverter.GetBytes((int)this);
        }

        void IField.SetFieldData(byte[] field_data, IStructure caller)
        {
            this = (string_id)BitConverter.ToInt32(field_data, 0);
            if (caller != null)
                parent.SetField(this);
        }

        int IField.SizeOfField
        {
            get { return size; }
        }

        void IField.Initialize(IStructure calling_structure)
        {
            parent = calling_structure;
        }

        string_id IReference<string_id>.GetToken()
        {
            return this;
        }

        void IReference<string_id>.SetToken(string_id token)
        {
            this = new string_id(token);
            parent.SetField(this);
        }

        bool IReference<string_id>.IsNullReference
        {
            get { return false; }
        }

        public string_id(string_id copy)
        {
            parent = copy.parent;
            nullbyte = copy.nullbyte; if (nullbyte != byte.MinValue) throw new Exception("Bad String ID. \nBad. bad. bad! >:D");
            Index = copy.Index;
            Length = copy.Length;
        }
        public string_id(short index, sbyte length, byte debug = byte.MinValue)
        {
            parent = default(IStructure);
            nullbyte = debug; if (nullbyte != byte.MinValue) throw new Exception("Bad String ID. \nBad. bad. bad! >:D");
            Index = index;
            Length = length;
        }

        public override string ToString()
        {
            return string.Format("{0} : {1} bytes", Index, Length);
        }
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