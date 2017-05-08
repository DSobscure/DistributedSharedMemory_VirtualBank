using DistributedSharedMemory_VirtualBank.Library;

namespace DistributedSharedMemory_VirtualBank.Client
{
    public enum StatusCode
    {
        Connect,
        Disconnect
    }
    interface IPeerService
    {
        void OnEvent(EventData eventData);
        void OnOperationResponse(OperationResponse operationResponse);
        void OnStatusChanged(StatusCode statusCode);
    }
}
