using DistributedSharedMemory_VirtualBank.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace DistributedSharedMemory_VirtualBank.Server
{
    class HostServer
    {
        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public IPAddress ServerIP { get; private set; }
        public bool IsTerminated { get; private set; }
        private TcpListener serverListener;
        private Dictionary<Guid, PeerBase> peerDictionary;

        public HostServer(int port)
        {
            Port = port;
            Hostname = Dns.GetHostName();
            ServerIP = Dns.GetHostEntry(Hostname).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
            IsTerminated = false;
            serverListener = new TcpListener(ServerIP, port);
            peerDictionary = new Dictionary<Guid, PeerBase>();
            MasterServer.InitialServer();
            AcceptConnection();
        }

        void AcceptConnection()
        {
            serverListener.Start();
            LogService.Info($"Hostname: {Hostname}({ServerIP}), Port: {Port}");
            LogService.Info("Waiting for connection ....");
            while (!IsTerminated)
            {
                TcpClient client = serverListener.AcceptTcpClient();
                LogService.Info($"Accept connectiion from {(client.Client.RemoteEndPoint as IPEndPoint).Address} : {(client.Client.RemoteEndPoint as IPEndPoint).Port}");
                Guid newGuid = Guid.NewGuid();
                Peer peer = new Peer(newGuid, client);
                peer.OnDisconnected += PeerDisconnect;
                peerDictionary.Add(newGuid, peer);
                Thread.Sleep(1);
            }
        }

        void PeerDisconnect(PeerBase peer)
        {
            if (peerDictionary.ContainsKey(peer.Guid))
                peerDictionary.Remove(peer.Guid);
        }
    }
}
