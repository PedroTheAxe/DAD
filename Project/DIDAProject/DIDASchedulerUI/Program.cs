using Google.Protobuf.Collections;
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
        private Dictionary<string, string> storageNodesMap = new Dictionary<string, string>();
        private Dictionary<string, string> populateDataMap = new Dictionary<string, string>();
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

            string[] storageNodes = request.StorageNodes.Split(';');
            for (int i = 0; i < storageNodes.Length - 1; i++) //hardcoded pq comentario anterior -- pq como dividi por ; , o ulitmo vai estar vazio
            {
                Console.WriteLine(storageNodes[i]);
                string[] parameters = storageNodes[i].Split(' ');
                lock (this)
                {
                    storageNodesMap.Add(parameters[0], parameters[1]);
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

            string[] pops = request.PopulateData.Split(';');
            for (int i = 0; i < pops.Length-1; i++)
            {
                Console.WriteLine(pops[i]);
                string[] parameters = pops[i].Split(' ');
                lock (this)
                {
                    //we assume that the input is correct
                    populateDataMap.Add(parameters[0], parameters[1]);
                }
            }

            sendToWorker();

            return fileSendReply;
        }

        public void sendToWorker()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            GrpcChannel channel = GrpcChannel.ForAddress(lowestWorkerPort());
            DIDASchedulerService.DIDASchedulerServiceClient client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);
            
            List<DIDAOperatorID> operatorsIDs = buildOperatorsIDs();
            DIDAAssignment[] assignments = buildAssignments(operatorsIDs);
            DIDARequest request = buildRequest(assignments);
            Console.WriteLine("request: " + request);
            DIDAStorageNode[] nodes = buildStorageNodes();
            DIDASendRequest sendRequest = new DIDASendRequest();
            sendRequest.Request = request;
            sendRequest.StorageNodes.Add(nodes);
            var reply = client.send(sendRequest);
            Console.WriteLine(reply);
            //while (!reply.Ack.Equals("ack"))
            //{
            //    reply = client.send(new DIDASendRequest { Request = request });
            //}
            //var reply = client.send(new DIDASendRequest { Request = buildRequest(buildAssignments(buildOperatorsIDs())) });
        }

        public DIDAStorageNode[] buildStorageNodes()
        {
            DIDAStorageNode[] nodes = new DIDAStorageNode[storageNodesMap.Count];
            var keys = new List<string>(storageNodesMap.Keys);
            for (int i = 0; i < storageNodesMap.Count; i++)
            {
                string key = keys[i];
                string[] decomposedArgs = storageNodesMap[key].Split(":");
                string host = decomposedArgs[1][2..];
                int port = Int32.Parse(decomposedArgs[2]);
                nodes[i] = new DIDAStorageNode { ServerId = key, Host = host, Port = port };
            }

            return nodes;
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
            DIDAAssignment[] assignments = new DIDAAssignment[operatorsIDs.Count];
            var keys = new List<string>(workersMap.Keys);
          
            for (int i = 0; i < operatorsIDs.Count; i++) //circular vector
            {
                string key = keys[i % workersMap.Count];
                string worker = workersMap[key];

                string[] decomposedArgs = worker.Split(":");
                string host = decomposedArgs[1][2..];
                int port = Int32.Parse(decomposedArgs[2]);
                
                DIDAAssignment assignment = new DIDAAssignment
                {
                    Op = operatorsIDs.Find(op => op.Order == i),
                    Host = host,
                    Port = port,
                    Output = ""
                };
                assignments[i] = assignment;
            }
            //foreach (var item in workersMap)
            //{
            //    Console.WriteLine(item);
            //    string[] decomposedArgs = item.Value.Split(":");

            //    decomposedArgs[1] = decomposedArgs[1][2..];
            //    string host = decomposedArgs[1];

            //    int port = Int32.Parse(decomposedArgs[2]);

            //    DIDAAssignment assignment = new DIDAAssignment
            //    {
            //        Op = operatorsIDs.Find(op => op.Order == i),
            //        Host = host,
            //        Port = port,
            //        Output = null
            //    };
            //    assignments[i] = assignment;
            //    i++;
            //}
            return assignments;
        }

        public DIDARequest buildRequest(DIDAAssignment[] assignments)
        {
            DIDAMetaRecord metaRecord = new DIDAMetaRecord
            {
                Id = metaRecordId
            };
            metaRecordId++;

            DIDARequest request = new DIDARequest();
            request.Meta = metaRecord;
            request.Input = "0";
            request.Next = 0;
            request.ChainSize = assignments.Length;
            request.Chain.Add(assignments);

            return request;
        }

        public string lowestWorkerPort()
        {
            int minPort = Int32.MaxValue;
            string worker = null;

            foreach (var item in workersMap)
            {
                string[] decomposedArgs = item.Value.Split(":");

                int port = Int32.Parse(decomposedArgs[2]);

                minPort = Math.Min(minPort, port);

                if (minPort == port)
                    worker = item.Value;
            }
            
            return worker;
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
