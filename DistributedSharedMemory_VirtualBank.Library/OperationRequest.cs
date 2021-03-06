﻿using MsgPack.Serialization;
using System.Collections.Generic;

namespace DistributedSharedMemory_VirtualBank.Library
{
    public class OperationRequest
    {
        [MessagePackMember(id: 0, Name = "OperationCode")]
        public byte OperationCode { get; set; }
        [MessagePackMember(id: 1, Name = "Parameters")]
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<byte, object> Parameters { get; set; }
    }
}
