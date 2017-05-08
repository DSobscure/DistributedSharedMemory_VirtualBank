using DistributedSharedMemory_VirtualBank.Library;
using DistributedSharedMemory_VirtualBank.Library.Procotol;
using DistributedSharedMemory_VirtualBank.Library.Procotol.OperationParameters;
using DistributedSharedMemory_VirtualBank.Library.Procotol.ResponseParameters;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace DistributedSharedMemory_VirtualBank.Server
{
    class Peer : PeerBase
    {
        private event Action<PeerBase> onDisconnected;
        public event Action<PeerBase> OnDisconnected { add { onDisconnected += value; } remove { onDisconnected -= value; } }

        public Peer(Guid guid, TcpClient tcpClient) : base(guid, tcpClient)
        {
        }

        protected override void OnDisconnect()
        {

        }

        protected override void OnOperationRequest(OperationRequest operationRequest)
        {
            var redisDatabase = MasterServer.Instance.Connection.GetDatabase();
            switch ((OperationCode)operationRequest.OperationCode)
            {
                case OperationCode.Initial:
                    {
                        int sequenceNumber = (int)operationRequest.Parameters[(byte)InitialParameterCode.SequenceNumber];
                        string account = ((string)operationRequest.Parameters[(byte)InitialParameterCode.Account]).ToLower();
                        int initialDeposits = (int)operationRequest.Parameters[(byte)InitialParameterCode.InitialDeposits];
                        WaitForSequenceNumber(redisDatabase, sequenceNumber);
                        if(redisDatabase.KeyExists(account))
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else if(initialDeposits < 0)
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else
                        {
                            redisDatabase.StringSet(account, initialDeposits);
                            OperationOver(redisDatabase, true);
                        }
                    }
                    break;
                case OperationCode.Save:
                    {
                        int sequenceNumber = (int)operationRequest.Parameters[(byte)SaveParameterCode.SequenceNumber];
                        string account = ((string)operationRequest.Parameters[(byte)SaveParameterCode.Account]).ToLower();
                        int money = (int)operationRequest.Parameters[(byte)SaveParameterCode.Money];
                        WaitForSequenceNumber(redisDatabase, sequenceNumber);
                        if (!redisDatabase.KeyExists(account))
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else if (money < 0)
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else
                        {
                            int deposits = int.Parse(redisDatabase.StringGet(account));
                            redisDatabase.StringSet(account, deposits + money);
                            OperationOver(redisDatabase, true);
                        }
                    }
                    break;
                case OperationCode.Load:
                    {
                        int sequenceNumber = (int)operationRequest.Parameters[(byte)LoadParameterCode.SequenceNumber];
                        string account = ((string)operationRequest.Parameters[(byte)LoadParameterCode.Account]).ToLower();
                        int deposits = (int)operationRequest.Parameters[(byte)LoadParameterCode.Deposits];
                        WaitForSequenceNumber(redisDatabase, sequenceNumber);
                        if (!redisDatabase.KeyExists(account))
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else if (deposits < 0)
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else
                        {
                            int remainedDeposits = int.Parse(redisDatabase.StringGet(account));
                            if(remainedDeposits < deposits)
                            {
                                OperationOver(redisDatabase, false);
                            }
                            else
                            {
                                redisDatabase.StringSet(account, remainedDeposits - deposits);
                                OperationOver(redisDatabase, true);
                            }
                        }
                    }
                    break;
                case OperationCode.Remit:
                    {
                        int sequenceNumber = (int)operationRequest.Parameters[(byte)RemitParameterCode.SequenceNumber];
                        string sourceAccount = ((string)operationRequest.Parameters[(byte)RemitParameterCode.SourceAccount]).ToLower();
                        string destinationAccount = ((string)operationRequest.Parameters[(byte)RemitParameterCode.DestinationAccount]).ToLower();
                        int money = (int)operationRequest.Parameters[(byte)RemitParameterCode.Money];
                        WaitForSequenceNumber(redisDatabase, sequenceNumber);
                        if (!redisDatabase.KeyExists(sourceAccount) || !redisDatabase.KeyExists(destinationAccount) || sourceAccount == destinationAccount)
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else if (money < 0)
                        {
                            OperationOver(redisDatabase, false);
                        }
                        else
                        {
                            int sourceDeposits = int.Parse(redisDatabase.StringGet(sourceAccount));
                            int destinationDeposits = int.Parse(redisDatabase.StringGet(destinationAccount));
                            if (sourceDeposits < money)
                            {
                                OperationOver(redisDatabase, false);
                            }
                            else
                            {
                                redisDatabase.StringSet(sourceAccount, sourceDeposits - money);
                                redisDatabase.StringSet(destinationAccount, destinationDeposits + money);
                                OperationOver(redisDatabase, true);
                            }
                        }
                    }
                    break;
                case OperationCode.End:
                    {
                        int sequenceNumber = (int)operationRequest.Parameters[(byte)EndParameterCode.SequenceNumber];
                        WaitForSequenceNumber(redisDatabase, sequenceNumber);
                        IServer server = MasterServer.Instance.DatabaseServer();
                        Dictionary<string, int> accountDictionary = new Dictionary<string, int>();
                        foreach(string key in server.Keys())
                        {
                            if(key == "CurrentSequenceNumber" || key == "TotalOperationCount" || key == "SuccessOperationCount")
                            {
                                continue;
                            }
                            else
                            {
                                accountDictionary.Add(key, int.Parse(redisDatabase.StringGet(key)));
                            }
                        }
                        int successOperationCount = int.Parse(redisDatabase.StringGet("SuccessOperationCount"));
                        int totalOperationCount = int.Parse(redisDatabase.StringGet("TotalOperationCount"));
                        SendResponse(new OperationResponse
                        {
                            OperationCode = (byte)OperationCode.End,
                            ReturnCode = 0,
                            DebugMessage = "",
                            Parameters = new Dictionary<byte, object>
                            {
                                { (byte)EndResponseParameterCode.AccountDictionary, accountDictionary },
                                { (byte)EndResponseParameterCode.SuccessOperationCount, successOperationCount },
                                { (byte)EndResponseParameterCode.TotalOperationCount, totalOperationCount },
                            }
                        });
                        server.FlushDatabase();
                    }
                    break;
            }           
        }
        private void WaitForSequenceNumber(IDatabase redisDatabase, int sequenceNumber)
        {
            while (sequenceNumber != 0 && (!redisDatabase.KeyExists("CurrentSequenceNumber") || (int.Parse(redisDatabase.StringGet("CurrentSequenceNumber")) != sequenceNumber)))
            {
                Thread.Sleep(1);
            }
            if(sequenceNumber == 0)
            {
                MasterServer.Instance.DatabaseServer().FlushDatabase();
            }
        }
        private void OperationOver(IDatabase redisDatabase, bool isSuccessful)
        {
            if (redisDatabase.KeyExists("TotalOperationCount"))
            {
                int totalOperationCount = int.Parse(redisDatabase.StringGet("TotalOperationCount"));
                totalOperationCount++;
                redisDatabase.StringSet("TotalOperationCount", totalOperationCount);
            }
            else
            {
                redisDatabase.StringSet("TotalOperationCount", 1);
            }

            if(isSuccessful)
            {
                if (redisDatabase.KeyExists("SuccessOperationCount"))
                {
                    int successOperationCount = int.Parse(redisDatabase.StringGet("SuccessOperationCount"));
                    successOperationCount++;
                    redisDatabase.StringSet("SuccessOperationCount", successOperationCount);
                }
                else
                {
                    redisDatabase.StringSet("SuccessOperationCount", 1);
                }
            }

            if (redisDatabase.KeyExists("CurrentSequenceNumber"))
            {
                int sequenceNumber = int.Parse(redisDatabase.StringGet("CurrentSequenceNumber"));
                sequenceNumber++;
                redisDatabase.StringSet("CurrentSequenceNumber", sequenceNumber);
            }
            else
            {
                redisDatabase.StringSet("CurrentSequenceNumber", 1);
            }
        }
    }
}
