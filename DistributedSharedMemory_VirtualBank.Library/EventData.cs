using MsgPack.Serialization;
using System.Collections.Generic;

namespace DistributedSharedMemory_VirtualBank.Library
{
    public class EventData
    {
        [MessagePackMember(id: 0, Name = "EventCode")]
        public byte EventCode { get; set; }
        [MessagePackMember(id: 1, Name = "Parameters")]
        [MessagePackRuntimeCollectionItemType]
        public Dictionary<byte, object> Parameters { get; set; }
    }
}
