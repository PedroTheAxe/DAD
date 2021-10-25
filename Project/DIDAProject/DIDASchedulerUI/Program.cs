using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DIDASchedulerUI {

    public class PuppetMasterService : DIDAPuppetMasterService.DIDAPuppetMasterServiceBase
    {
        private Dictionary<string, string> workersMap = new Dictionary<string, string>();
        private Dictionary<int, string> operatorsMap = new Dictionary<int, string>();
        private int metaRecordId = 0;

        public PuppetMasterService()
        {

        }

        public override Task<DIDAFileSendReply> sendFile(DIDAFileSendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendFileImpl(request));
        }

        public DIDAFileSendReply sendFileImpl(DIDAFileSendRequest request)
        {
            DIDAFileSendReply fileSendReply = new DIDAFileSendReply
            {
                Ack = "ack"
            };

            Console.WriteLine(request);
            string[] workers = request.Workers.Split(';');
            for (int i = 0; i < workers.Length-1; i++) //hardcoded pq comentario anterior -- pq como dividi por ; , o ulitmo vai estar vazio
            {
                Console.WriteLine(workers[i]);
                string[] parameters = workers[i].Split(' ');
                lock (this)
                {
                    workersMap.Add(parameters[0], parameters[1]);
                }
            }

            string[] ops = request.Operators.Split(';');
            for (int i = 0; i < ops.Length-1; i++)
            {
                Console.WriteLine(ops[i]);
                string[] parameters = ops[i].Split(' ');
                lock (this)
                {
                    //we assume that the input is correct
                    operatorsMap.Add(Int32.Parse(parameters[1]), parameters[0]);
                }
            }

            return fileSendReply;
        }

        public void sendToWorker()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            //GrpcChannel channel = GrpcChannel.ForAddress(workersMap[0]);
            //DIDAPuppetMasterService.DIDAPuppetMasterServiceClient client = new DIDAPuppetMasterService.DIDAPuppetMasterServiceClient(channel);

            //var reply = client.sendFile(new DIDAFileSendRequest { Workers = _workers, Operators = _operators });
        }

        public List<DIDAOperatorID> buildOperatorsIDs()
        {
            List<DIDAOperatorID> operatorsIDs = new List<DIDAOperatorID>();
            for (int i = 0; i < operatorsMap.Count; i++)
            {
                DIDAOperatorID operatorID = new DIDAOperatorID
                {
                    Classname = operatorsMap[i],
                    Order = i
                };
                operatorsIDs.Add(operatorID);
            }
            return operatorsIDs;
        }

        public DIDAAssignment[] buildAssignments(List<DIDAOperatorID> operatorsIDs)
        {
            DIDAAssignment[] assignments = null;
            int i = 0;
            foreach (var item in workersMap)
            {
                string[] decomposedArgs = item.Value.Split(":");
                
                decomposedArgs[1] = decomposedArgs[1].Substring(2);
                string host = decomposedArgs[1];

                int port = Int32.Parse(decomposedArgs[2]);

                DIDAAssignment assignment = new DIDAAssignment
                {
                    Op = operatorsIDs.Find(op => op.Order == i),
                    Host = host,
                    Port = port,
                    Output = null
                };
                assignments[i] = assignment;
                i++;
            }
            return assignments;
        }

        public DIDARequest buildRequest(DIDAAssignment[] assignments)
        {
            DIDAMetaRecord metaRecord = new DIDAMetaRecord
            {
                Id = metaRecordId
            };
            metaRecordId++;

            return new DIDARequest
            {
                Meta = metaRecord,
                Input = null,
                Next = 0,
                ChainSize = assignments.Length,
                //Chain
            };
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for scheduler 
            Console.WriteLine(args[0]);
            string[] decomposedArgs = args[0].Split(":");

            decomposedArgs[1] = decomposedArgs[1].Substring(2);
            string host = decomposedArgs[1];
            Console.WriteLine(host);

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            Server server = new Server
            {
                Services = { DIDAPuppetMasterService.BindService(new PuppetMasterService())},
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
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
