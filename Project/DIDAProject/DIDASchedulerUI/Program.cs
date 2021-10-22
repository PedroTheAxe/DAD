using DIDAPuppetClient;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading.Tasks;

namespace DIDASchedulerUI {

    public class PuppetMasterService : DIDAPuppetMasterService.DIDAPuppetMasterServiceBase
    {
        private GrpcChannel channel;

        public PuppetMasterService()
        {

        }

        public override Task<DIDAFileSendReply> sendFile(DIDAFileSendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendFileImpl(request));
        }

        public DIDAFileSendReply sendFileImpl(DIDAFileSendRequest request)
        {
            Console.WriteLine(request);
            DIDAFileSendReply fileSendReply = new DIDAFileSendReply
            {
                Ack = "ack"
            };

            return fileSendReply;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for scheduler 
            Console.WriteLine(args[0]);
            string[] decomposedArgs = args[0].Split(":");

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            Server server = new Server
            {
                Services = { DIDAPuppetMasterService.BindService(new PuppetMasterService()) },
                Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();

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
