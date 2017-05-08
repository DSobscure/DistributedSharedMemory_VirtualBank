using MsgPack.Serialization;

namespace DistributedSharedMemory_VirtualBank.Library
{
    public enum CommunicationContentTypeCode : byte
    {
        OperationRequest,
        OperationResponse,
        EventData
    }

    public class CommunicationContent
    {
        [MessagePackMember(id: 0, Name = "ContentType")]
        public CommunicationContentTypeCode ContentType { get; set; }
        [MessagePackMember(id: 1, Name = "Content")]
        [MessagePackRuntimeType]
        public object Content { get; set; }
    }
}
