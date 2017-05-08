using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DistributedSharedMemory_VirtualBank.Library;

namespace DistributedSharedMemory_VirtualBank.Server
{
    abstract class PeerBase
    {
        public Guid Guid { get; private set; }
        TcpClient tcpClient;

        protected PeerBase(Guid guid, TcpClient tcpClient)
        {
            Guid = guid;
            this.tcpClient = tcpClient;
            Task.Run(() => PeerMain());
        }

        void PeerMain()
        {
            try
            {
                byte[] headerBuffer = new byte[256];
                byte[] receiveBuffer = new byte[65536];
                while (!(tcpClient.Client.Poll(0, SelectMode.SelectRead) && tcpClient.Available == 0))
                {
                    if (tcpClient.Available > 0)
                    {
                        int headerSize = tcpClient.GetStream().ReadByte();
                        tcpClient.GetStream().Read(headerBuffer, 0, headerSize);
                        int contentLength = 0;
                        for(int i = 0; i < headerSize; i++)
                        {
                            contentLength *= 256;
                            contentLength += headerBuffer[i];
                        }
                        tcpClient.GetStream().Read(receiveBuffer, 0, contentLength);
                        byte[] contentBytes = new byte[contentLength];
                        Array.Copy(receiveBuffer, contentBytes, contentLength);
                        CommunicationContent content = SerializationHelper.Deserialize<CommunicationContent>(contentBytes);
                        switch(content.ContentType)
                        {
                            case CommunicationContentTypeCode.OperationRequest:
                                OnOperationRequest((OperationRequest)content.Content);
                                break;
                            default:
                                LogService.Warning($"From Guid {Guid}: Invalid CommunicationContentTypeCode: {content.ContentType}");
                                break;
                        }
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                LogService.Warning($"From Guid {Guid}: {ex.Message}");
                LogService.Warning($"From Guid {Guid}: {ex.StackTrace}");
            }
            OnDisconnect();
        }

        protected abstract void OnDisconnect();

        protected abstract void OnOperationRequest(OperationRequest operationRequest);

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
                LogService.Warning($"From Guid {Guid}: {ex.Message}");
                LogService.Warning($"From Guid {Guid}: {ex.StackTrace}");
            }
        }
        internal void SendResponse(OperationResponse operationResponse)
        {
            SendCommunicationContent(new CommunicationContent { ContentType = CommunicationContentTypeCode.OperationResponse, Content = operationResponse });
        }
        internal void SendEvent(EventData eventData)
        {
            SendCommunicationContent(new CommunicationContent { ContentType = CommunicationContentTypeCode.EventData, Content = eventData });
        }
    }
}
