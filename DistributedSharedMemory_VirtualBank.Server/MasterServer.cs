using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace DistributedSharedMemory_VirtualBank.Server
{
    class MasterServer
    {
        public static MasterServer Instance { get; private set; }
        public static void InitialServer()
        {
            Instance = new MasterServer();
        }

        public ConnectionMultiplexer Connection { get; private set; }
        private MasterServer()
        {
            Connection = ConnectionMultiplexer.Connect($"{"127.0.0.1"}:{6379},allowAdmin=true");
        }
        public IServer DatabaseServer()
        {
            return Connection.GetServer($"{"127.0.0.1"}:{6379}");
        }
    }
}
