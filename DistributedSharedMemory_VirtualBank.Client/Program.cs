using DistributedSharedMemory_VirtualBank.Library;
using DistributedSharedMemory_VirtualBank.Library.Procotol;
using DistributedSharedMemory_VirtualBank.Library.Procotol.OperationParameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DistributedSharedMemory_VirtualBank.Client
{
    class Program
    {
        public static Action DisconnectAllClient = null;
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("please give an input file");
                return;
            }

            string fileName = args[0];
            if (!File.Exists(fileName))
            {
                Console.WriteLine("please give an existed input file");
                return;
            }
            string[] lines = File.ReadAllLines(fileName);

            LogService.InitialService(
            infoMethod: (message) =>
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0} Info - {1}", DateTime.Now.ToString("o"), message);
                Console.ForegroundColor = originalColor;
            },
            warningMethod: (message) =>
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("{0} Warning - {1}", DateTime.Now.ToString("o"), message);
                Console.ForegroundColor = originalColor;
            },
            errorMethod: (message) =>
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0} Error - {1}", DateTime.Now.ToString("o"), message);
                Console.ForegroundColor = originalColor;
            });

            ClientPeer[] peers = new ClientPeer[1] 
            {
                new ClientPeer(new PeerService())
            };
            peers[0].Connect("140.113.123.134", 10000);

            DisconnectAllClient = () => 
            {
                for (int i = 0; i < peers.Length; i++)
                {
                    peers[i].Disconnect();
                }
            };

            Task[] tasks = new Task[peers.Length];
            tasks[0] = Task.Run(() =>
            {
                while (peers[0].Connected)
                {
                    peers[0].Service();
                    Thread.Sleep(1);
                }
                Console.WriteLine("Disconneted");
            });

            for(int i = 0; i < lines.Length; i++)
            {
                ClientPeer peer = peers[i % peers.Length];
                string[] command = lines[i].Split(' ');
                switch(command[0])
                {
                    case "init":
                        {
                            string account = command[1];
                            int initialDeposits = int.Parse(command[2]);
                            peer.Send(new OperationRequest
                            {
                                OperationCode = (byte)OperationCode.Initial,
                                Parameters = new Dictionary<byte, object>
                                {
                                    { (byte)InitialParameterCode.SequenceNumber, i },
                                    { (byte)InitialParameterCode.Account, account },
                                    { (byte)InitialParameterCode.InitialDeposits, initialDeposits }
                                }
                            });
                        }
                        break;
                    case "save":
                        {
                            string account = command[1];
                            int money = int.Parse(command[2]);
                            peer.Send(new OperationRequest
                            {
                                OperationCode = (byte)OperationCode.Save,
                                Parameters = new Dictionary<byte, object>
                                {
                                    { (byte)SaveParameterCode.SequenceNumber, i },
                                    { (byte)SaveParameterCode.Account, account },
                                    { (byte)SaveParameterCode.Money, money }
                                }
                            });
                        }
                        break;
                    case "load":
                        {
                            string account = command[1];
                            int deposits = int.Parse(command[2]);
                            peer.Send(new OperationRequest
                            {
                                OperationCode = (byte)OperationCode.Load,
                                Parameters = new Dictionary<byte, object>
                                {
                                    { (byte)LoadParameterCode.SequenceNumber, i },
                                    { (byte)LoadParameterCode.Account, account },
                                    { (byte)LoadParameterCode.Deposits, deposits }
                                }
                            });
                        }
                        break;
                    case "remit":
                        {
                            string sourceAccount = command[1];
                            string destinationAccount = command[2];
                            int money = int.Parse(command[3]);
                            peer.Send(new OperationRequest
                            {
                                OperationCode = (byte)OperationCode.Remit,
                                Parameters = new Dictionary<byte, object>
                                {
                                    { (byte)RemitParameterCode.SequenceNumber, i },
                                    { (byte)RemitParameterCode.SourceAccount, sourceAccount },
                                    { (byte)RemitParameterCode.DestinationAccount, destinationAccount },
                                    { (byte)RemitParameterCode.Money, money }
                                }
                            });
                        }
                        break;
                    case "end":
                        {
                            peer.Send(new OperationRequest
                            {
                                OperationCode = (byte)OperationCode.End,
                                Parameters = new Dictionary<byte, object>
                                {
                                    { (byte)EndParameterCode.SequenceNumber, i }
                                }
                            });
                        }
                        break;
                }
            }
            for(int i = 0; i < peers.Length; i++)
            {
                tasks[i].Wait();
            }
        }
    }
}
