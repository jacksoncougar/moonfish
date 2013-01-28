using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Moonfish.Core
{
    #region structure & fields interfaces

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

    #endregion

    public interface ISerializable
    {
        void Deserialize(Stream source_stream);
        int Serialize(Stream destination_stream, int destination_address);
        void Deserialize(Stream source_stream, Segment stream_segment);
        int SerializedSize { get; }
    }

    public interface IReferenceList<T, TToken> where TToken : struct
    {
        /// <summary>
        /// return the object we are token-referencing to
        /// </summary>
        /// <param name="reference"></param>
        /// <returns></returns>
        T GetValue(TToken reference);

        /// <summary>
        /// if the graph contains the reference value return the token to it: or else add the value and return a token to it
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        TToken Link(TToken reference, T value);
        void Add(TToken reference, T value);
    }

    //public interface IAddressable
    //{
    //    int GetIdentifier();
    //    int GetAddress();
    //}

    //public interface IReferenceGraphable
    //{
    //    void CreateReferences(IReferenceList<IAddressable, int> destination_graph);
    //    graph_index CreateReferenceLinks(ref List<graph_index> indices, graph_index parent_index);
    //    void RelinkReferences(IReferenceList<IAddressable, int> destination_graph, Dictionary<int, int> token_list);
    //}

    #region Reference (strings, tags) interfaces

    public interface IReferenceable<T, TToken> where TToken : struct
    {
        /// <summary>
        /// foreach IReference object move the reference and value from source to destination
        /// </summary>
        /// <param name="source_graph"></param>
        /// <param name="destination_graph"></param>
        void CopyReferences(IReferenceList<T, TToken> source_graph, IReferenceList<T, TToken> destination_graph);
        void CreateReferences(IReferenceList<T, TToken> destination_graph);
    }

    public interface IReference<TToken> where TToken : struct
    {
        /// <summary>
        /// Retrieves an identifying_token for the referenced object
        /// </summary>
        /// <returns></returns>
        TToken GetToken();
        bool IsNullReference { get; }
        void SetToken(TToken token);
    }

    #endregion
}