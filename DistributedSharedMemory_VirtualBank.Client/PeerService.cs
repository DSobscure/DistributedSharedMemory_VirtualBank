using DistributedSharedMemory_VirtualBank.Library;
using DistributedSharedMemory_VirtualBank.Library.Procotol;
using DistributedSharedMemory_VirtualBank.Library.Procotol.ResponseParameters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DistributedSharedMemory_VirtualBank.Client
{
    class PeerService : IPeerService
    {
        public void OnEvent(EventData eventData)
        {
            LogService.Info($"OnEvent: {eventData.EventCode}");
        }

        public void OnOperationResponse(OperationResponse operationResponse)
        {
            switch((OperationCode)operationResponse.OperationCode)
            {
                case OperationCode.End:
                    Dictionary<string, int> accountDictionary = (Dictionary<string, int>)operationResponse.Parameters[(byte)EndResponseParameterCode.AccountDictionary];
                    int successOperationCount = (int)operationResponse.Parameters[(byte)EndResponseParameterCode.SuccessOperationCount];
                    int totalOperationCount = (int)operationResponse.Parameters[(byte)EndResponseParameterCode.TotalOperationCount];
                    foreach(var pair in accountDictionary.OrderBy(x => x.Key, new NaturalComparer()))
                    {
                        Console.WriteLine($"{pair.Key} : {pair.Value}");
                    }
                    Console.WriteLine();
                    Console.WriteLine($"success rate : ({successOperationCount}/{totalOperationCount})");

                    Program.DisconnectAllClient();
                    break;
            }
        }

        public void OnStatusChanged(StatusCode statusCode)
        {
            LogService.Info($"StatusChanged: {statusCode}");
        }
    }
}
