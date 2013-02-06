using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Moonfish.Core
{
    /// <summary>
    /// Holds a tag in it's cached format: acts as an intermediary for various functions
    /// </summary>
    struct TagCache
    {
        const int header_size = 64;

        public TagClass tag_type;
        public TagIdentifier id;
        public Stream meta_stream;
        public int[] raw_data_address;
        public Stream raw_data_stream;
        public TagIdentifier[] tag_references;

        public void Save(Stream output_stream)
        {
            BinaryWriter bin_writer = new BinaryWriter(output_stream);
            int start_address = (int)output_stream.Position;

            // Fill out header struct with values then write the header to the output stream
            tag_cache_header header = new tag_cache_header()
            {
                fourcc = Encoding.UTF8.GetBytes("moon"),
                id = id,
                type = tag_type,
                meta_address = header_size,
                meta_length = (int)meta_stream.Length,
                raw_data_count = raw_data_address.Length,
                raw_data_address = Padding.Pad(meta_stream.Length, 4) + header_size,
                tag_reference_count = tag_references.Length
            };
            bin_writer.Write(header.Serialize());

            // Copy the meta stream to the output stream and pad to 4 bytes
            output_stream.Seek(header.meta_address, SeekOrigin.Begin);
            meta_stream.Seek(0, SeekOrigin.Begin);
            meta_stream.CopyTo(output_stream);
            output_stream.Pad(4);

            // Write raw addresses to output stream, then write the raw data stream and pad to 4 bytes
            foreach (var value in raw_data_address)
                bin_writer.Write(value);

            raw_data_stream.Seek(0, SeekOrigin.Begin);
            raw_data_stream.CopyTo(output_stream);
            bin_writer.BaseStream.Pad(4);

            //  write tag_id references... why?
            foreach (var tag_id in tag_references)
                bin_writer.Write(tag_id);

            output_stream.Flush();
        }

        public struct tag_cache_header
        {
            const int Size = 64;

            public byte[] fourcc;
            public TagClass type;
            public int id;
            public int meta_address;
            public int meta_length;
            public int raw_data_count;
            public int raw_data_address;
            public int tag_reference_count;

            public byte[] Serialize()
            {
                byte[] buffer = new byte[Size];
                using (MemoryStream memory_stream = new MemoryStream(buffer))
                {
                    BinaryWriter binary_writer = new BinaryWriter(memory_stream);

                    binary_writer.Write(fourcc, 0, 4);
                    binary_writer.Write((int)type);
                    binary_writer.Write(id);
                    binary_writer.Write(meta_address);
                    binary_writer.Write(meta_length);
                    binary_writer.Write(raw_data_count);
                    binary_writer.Write(raw_data_address);
                    binary_writer.Write(tag_reference_count);
                }
                return buffer;
            }
        }
    }





}