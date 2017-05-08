namespace DistributedSharedMemory_VirtualBank.Library.Procotol
{
    public enum OperationCode : byte
    {
        Initial,
        Save,
        Load,
        Remit,
        End
    }
}
