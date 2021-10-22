using DIDAPuppetClient;
using Grpc.Core;
using System;

namespace DIDASchedulerUI { 

    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for scheduler 
            Console.WriteLine(args[0]);
            string[] decomposedArgs = args[0].Split(":");

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            /*Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new Program()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();*/
            Console.ReadLine();

            //TODO

            /* 
                - gRPC client connect to port 10001 (PuppetMaster)
                - create new Server
                - creates new DIDAFileSendRequest 
                - executes sendFile method
                - receives 2 strings with file contents
                - construct DIDARequest

             */

        }
    }
}
