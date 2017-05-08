using DistributedSharedMemory_VirtualBank.Library;
using System;

namespace DistributedSharedMemory_VirtualBank.Server
{
    class Program
    {
        static void Main(string[] args)
        {
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
            HostServer server = new HostServer(10000);
            Console.ReadLine();
        }
    }
}
