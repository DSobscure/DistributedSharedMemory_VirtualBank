using DistributedSharedMemory_VirtualBank.Library;
using System;
using System.Net.Sockets;

namespace DistributedSharedMemory_VirtualBank.Client
{
    class ClientPeer
    {
        TcpClient tcpClient;
        IPeerService peerService;
        public bool Connected
        {
            get
            {
                return tcpClient.Client.Connected;
            }
        }

        byte[] headerBuffer = new byte[256];
        byte[] receiveBuffer = new byte[65536];

        public ClientPeer(IPeerService peerService)
        {
            this.peerService = peerService;
            tcpClient = new TcpClient();
        }

        public bool Connect(string hostname, int port)
        {
            try
            {
                tcpClient.Connect(hostname, port);
            }
            catch (Exception ex)
            {
                LogService.Error($"Connect Fail: {ex.Message}");
                LogService.Error($"Connect Fail: {ex.StackTrace}");
            }
            return tcpClient.Connected;
        }

        public void Disconnect()
        {
            tcpClient.Close();
        }

        private void SendCommunicationContent(CommunicationContent communicationContent)
        {
            try
            {
                byte[] contentData = SerializationHelper.Serialize(communicationContent);
                byte[] headerData = new byte[1 + (contentData.Length) / 256];
                for (int i = 0; i < headerData.Length; i++)
                {
                    headerData[headerData.Length - 1 - i] = (byte)(contentData.Length >> (8 * i));
                }
                byte[] data = new byte[1 + headerData.Length + contentData.Length];
                data[0] = (byte)headerData.Length;
                Array.Copy(headerData, 0, data, 1, headerData.Length);
                Array.Copy(contentData, 0, data, 1 + headerData.Length, contentData.Length);
                tcpClient.GetStream().Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                LogService.Warning($"SendCommunicationContent Fail: {ex.Message}");
                LogService.Warning($"SendCommunicationContent Fail: {ex.StackTrace}");
            }
        }

        public void Send(OperationRequest operationRequest)
        {
            if(Connected)
                SendCommunicationContent(new CommunicationContent { ContentType = CommunicationContentTypeCode.OperationRequest, Content = operationRequest });
        }

        public void Service()
        {
            try
            {
                if (tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available != 0)
                {
                    if(tcpClient.Client.Receive(receiveBuffer, SocketFlags.Peek) == 0)
                    {
                        tcpClient.Client.Disconnect(true);
                        return;
                    }
                    if (tcpClient.Available > 0)
                    {
                        int headerSize = tcpClient.GetStream().ReadByte();
                        tcpClient.GetStream().Read(headerBuffer, 0, headerSize);
                        int contentLength = 0;
                        for (int i = 0; i < headerSize; i++)
                        {
                            contentLength *= 256;
                            contentLength += headerBuffer[i];
                        }
                        tcpClient.GetStream().Read(receiveBuffer, 0, contentLength);
                        byte[] contentBytes = new byte[contentLength];
                        Array.Copy(receiveBuffer, contentBytes, contentLength);
                        CommunicationContent content = SerializationHelper.Deserialize<CommunicationContent>(contentBytes);
                        switch (content.ContentType)
                        {
                            case CommunicationContentTypeCode.OperationResponse:
                                peerService.OnOperationResponse((OperationResponse)content.Content);
                                break;
                            case CommunicationContentTypeCode.EventData:
                                peerService.OnEvent((EventData)content.Content);
                                break;
                            default:
                                LogService.Warning($"Service Fail: Invalid CommunicationContentTypeCode: {content.ContentType}");
                                break;
                        }
                    }
                }
                else if(tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available == 0)
                {
                    tcpClient.Client.Disconnect(true);
                }
            }
            catch (Exception ex)
            {
                LogService.Warning($"Service Fail: {ex.Message}");
                LogService.Warning($"Service Fail: {ex.StackTrace}");
            }
        }
    }
}
