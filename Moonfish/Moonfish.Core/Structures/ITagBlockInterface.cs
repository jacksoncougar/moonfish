using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Moonfish.Core
{
    public interface IMeta
    {
        void CopyFrom(Stream source);
        int Size { get; }
    }
}