using Moonfish.Core.Definitions;
using Moonfish.Core.Model;
using OpenTK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Moonfish.Core.Entity
{
    //public static class Entity
    //{
    //    private static DSection GenerateSectionData(uint raw_size, RenderMesh mesh)
    //    {
    //        DSection section = new DSection();
    //        section.RawSize = raw_size;
    //        section.RawDataSize = raw_size - (124);//arbitrary for now
    //        section.RawOffset = 0;
    //        section.TriangleCount = mesh.GetTriangleCount();
    //        section.VertexCount = (ushort)mesh.VertexCoordinates.Length;
    //        return section;
    //    }

    //    public static bool ExportForEntity(string desination_folder, string tagname, RenderMesh mesh)
    //    {
    //        model model = new model();                                                  // Make TagStructure object to hold our model definition data

    //        var compression_ranges = mesh.GenerateCompressionData();
    //        model.Compression.Add(new model.CompressionRanges(compression_ranges));    // Add a new Compression TagBlock, filling it from this mesh's data

    //        DResource[] resource = null;                                                 // Convert the model data into the halo 2 format and write it to a file
    //        int raw_size = -1;

    //        string output_filename = Path.Combine(desination_folder, tagname);
    //        string output_name = output_filename.Substring(output_filename.LastIndexOf('\\') + 1);
    //        string meta_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".mode");
    //        string meta_xml_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".mode.xml");
    //        string raw_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".moderaw");
    //        string raw_xml_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".moderaw.xml");
    //        string info_filepath = Path.ChangeExtension(Path.Combine(desination_folder, output_name), ".info");

    //        if (!Directory.Exists(desination_folder)) Directory.CreateDirectory(desination_folder);

    //        using (BinaryWriter bin = new BinaryWriter(File.Create(raw_filepath)))
    //        {
    //            var buffer = mesh.Serialize(out resource, out compression_ranges);
    //            raw_size = buffer.Length;
    //            bin.Write(buffer);
    //        } if (resource == null) return false;                                       // If we didn't get any resources back then the method failed.

    //        model.Regions.Add(new model.Region(new DRegion()));                         // Add a default region + default definition
    //        model.Regions[0].Permutations.Add(new model.Region.Permutation());          // Add a default permutation to that region
    //        model.Sections.Add(new model.Section(GenerateSectionData((uint)raw_size, mesh))); // Add a new Section tagBlock to hold our model information
    //        model.Sections[0].Resources.AddRange(resource.Select(x=>new Moonfish.Core.Structures.Resource(x)));
    //        model.Groups.Add(new model.Group(new DGroup()));                            // Add a default model_group + a default definition
    //        model.Nodes.Add(new model.Node(new DNode()));                               // Add a default node + default definition
    //        for (int i = 0; i < mesh.Primitives.Length; i++)
    //        {
    //            model.Shaders.Add(new model.Shader(new DShader()));                     // Add a default shader + default definition
    //        }
    //        int meta_size = 0;
    //        using (var file = File.Create(meta_filepath))
    //        {
    //            Memory.Map(model, file);
    //            meta_size = (int)file.Length;
    //        }
    //        using (XmlWriter xml = XmlWriter.Create(File.Create(meta_xml_filepath),
    //            new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
    //        {
    //            xml.WriteStartElement("Meta");
    //            xml.WriteAttributeString("TagName", tagname);
    //            xml.WriteAttributeString("Offset", "0");
    //            xml.WriteAttributeString("Size", meta_size.ToString());
    //            xml.WriteAttributeString("TagType", "mode");
    //            xml.WriteAttributeString("Magic", "0");
    //            xml.WriteAttributeString("Parsed", "True");
    //            xml.WriteAttributeString("Date", DateTime.Now.ToShortDateString());
    //            xml.WriteAttributeString("Time", DateTime.Now.ToShortTimeString());
    //            xml.WriteAttributeString("EntityVersion", "0.1");
    //            xml.WriteAttributeString("Padding", "0");

    //            WriteEntityXmlNodes(xml, model, 0, tagname);

    //            xml.WriteEndElement();
    //        }
    //        int raw_pointer_address = (model.Sections as IPointable).Address + 56;//bug lol
    //        using (XmlWriter xml = XmlWriter.Create(raw_xml_filepath,
    //            new XmlWriterSettings() { Indent = true, OmitXmlDeclaration = true }))
    //        {
    //            xml.WriteStartElement("RawData");
    //            xml.WriteAttributeString("TagType", "mode");
    //            xml.WriteAttributeString("TagName", tagname);
    //            xml.WriteAttributeString("RawType", "Model");
    //            xml.WriteAttributeString("RawChunkCount", "1");
    //            xml.WriteAttributeString("Date", DateTime.Now.ToShortDateString());
    //            xml.WriteAttributeString("Time", DateTime.Now.ToShortTimeString());
    //            xml.WriteAttributeString("EntityVersion", "0.1");
    //            {
    //                xml.WriteStartElement("RawChunk");
    //                xml.WriteAttributeString("RawDataType", "mode1");
    //                xml.WriteAttributeString("PointerMetaOffset", raw_pointer_address.ToString());
    //                xml.WriteAttributeString("RawType", "Model");
    //                xml.WriteAttributeString("ChunkSize", raw_size.ToString());
    //                xml.WriteAttributeString("PointsToOffset", "0");
    //                xml.WriteAttributeString("RawLocation", "Internal");
    //                xml.WriteEndElement();
    //            }

    //            xml.WriteEndElement();
    //        }
    //        using (StreamWriter txt = new StreamWriter(info_filepath))
    //        {
    //            txt.WriteLine(meta_filepath);
    //        }
    //        return false;
    //    }
    //    private static void WriteEntityXmlNodes(XmlWriter xml, IEnumerable<TagBlockField> tagblock, int current_offset, string tagname)
    //    {
    //        foreach (var field in tagblock)
    //        {
    //            if (field.Object.GetType() == typeof(StringID))
    //            {
    //                xml.WriteStartElement("String");
    //                xml.WriteAttributeString("Description", "Waffle");
    //                xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
    //                xml.WriteAttributeString("StringName", "default");
    //                xml.WriteAttributeString("TagType", "mode");
    //                xml.WriteAttributeString("TagName", tagname);
    //                xml.WriteEndElement();
    //            }
    //            else if (field.Object.GetType() == typeof(TagIdentifier))
    //            {
    //                xml.WriteStartElement("Ident");
    //                xml.WriteAttributeString("Description", "Waffle");
    //                xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
    //                xml.WriteAttributeString("PointsToTagType", "mode");
    //                xml.WriteAttributeString("PointsToTagName", tagname);
    //                xml.WriteAttributeString("TagType", "mode");
    //                xml.WriteAttributeString("TagName", tagname);
    //                xml.WriteEndElement();
    //            }
    //            else if (field.Object.GetType() == typeof(TagPointer))
    //            {
    //                xml.WriteStartElement("Ident");
    //                xml.WriteAttributeString("Description", "Waffle");
    //                xml.WriteAttributeString("Offset", (current_offset + (field.FieldOffset + 4)).ToString());
    //                xml.WriteAttributeString("PointsToTagType", "");
    //                xml.WriteAttributeString("PointsToTagName", "Null");
    //                xml.WriteAttributeString("TagType", "mode");
    //                xml.WriteAttributeString("TagName", tagname);
    //                xml.WriteEndElement();
    //            }
    //            else
    //            {
    //                IEnumerable<TagBlock> taglist_interface = (field.Object as IEnumerable<TagBlock>);
    //                if (taglist_interface != null)
    //                {
    //                    if (taglist_interface.Count<TagBlock>() == 0) continue;
    //                    xml.WriteStartElement("Reflexive");
    //                    xml.WriteAttributeString("Description", "Waffle");
    //                    xml.WriteAttributeString("Offset", (current_offset + field.FieldOffset).ToString());
    //                    xml.WriteAttributeString("ChunkCount", taglist_interface.Count<TagBlock>().ToString());
    //                    xml.WriteAttributeString("ChunkSize", (field.Object as IPointable).SizeOf.ToString());
    //                    xml.WriteAttributeString("Translation", (field.Object as IPointable).Address.ToString());
    //                    xml.WriteAttributeString("PointsToTagType", "mode");
    //                    xml.WriteAttributeString("PointsToTagName", tagname);
    //                    xml.WriteAttributeString("TagType", "mode");
    //                    xml.WriteAttributeString("TagName", tagname);
    //                    xml.WriteEndElement();
    //                    foreach (var item in taglist_interface)
    //                    {
    //                        WriteEntityXmlNodes(xml, item, (field.Object as IPointable).Address, tagname);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}
}
