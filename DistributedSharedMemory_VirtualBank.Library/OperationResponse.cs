using MsgPack.Serialization;
using System.Collections.Generic;

namespace DistributedSharedMemory_VirtualBank.Library
{
    public class OperationResponse
    {
        [MessagePackMember(id: 0, Name = "OperationCode")]
        public byte OperationCode { get; set; }
        [MessagePackMember(id: 1, Name = "ReturnCode")]
        public byte ReturnCode { get; set; }
        [MessagePackMember(id: 2, Name = "DebugMessage")]
        public string DebugMessage { get; set; }
        [MessagePackMember(id: 3, Name = "Parameters")]
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<byte, object> Parameters { get; set; }
    }
}
