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
        private Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> storageClientsMap = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();  //maps the storage serverId and gRPC client to said storage
        private Dictionary<string, DIDASchedulerService.DIDASchedulerServiceClient> workerClientsMap = new Dictionary<string, DIDASchedulerService.DIDASchedulerServiceClient>(); //maps the worker host and gRPC client to said worker
        private Dictionary<string, string> populateDataMap = new Dictionary<string, string>();
        private DIDASchedulerService.DIDASchedulerServiceClient firstWorker = null;
        private int _metaRecordId = 0;

        public PuppetMasterService()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public override Task<DIDAPostInitReply> sendPostInit(DIDAPostInitRequest request, ServerCallContext context) 
        {
            return Task.FromResult(postInitImpl(request));
        }
        

        public override Task<DIDAFileSendReply> sendFile(DIDAFileSendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendFileImpl(request));
        }

        public DIDAPostInitReply postInitImpl(DIDAPostInitRequest request)
        {
            DIDAPostInitReply postInitReply = new DIDAPostInitReply
            {
                Ack = "ack"
            };

            string[] clientArgs = request.Type.Split(" ");
            if (clientArgs[0].Equals("client"))
            {
                string[] splitData = request.Data.Split(';');
                operatorsMap.Clear();
                for (int i = 0; i < splitData.Length - 1; i++)
                {
                    Console.WriteLine(splitData[i]);
                    string[] parameters = splitData[i].Split(' ');
                    lock (this)
                    {
                        //we assume that the input is correct
                        operatorsMap.Add(Int32.Parse(parameters[1]), parameters[0]);
                    }
                }
                sendToWorker(clientArgs[1]);
            }


            if (request.Type.Equals("populate"))
            {
                string[] splitData = request.Data.Split(';');
                populateDataMap.Clear();
                for (int i = 0; i < splitData.Length - 1; i++)
                {
                    Console.WriteLine(splitData[i]);
                    string[] parameters = splitData[i].Split(' ');
                    lock (this)
                    {
                        //we assume that the input is correct
                        populateDataMap.Add(parameters[0], parameters[1]);
                    }
                }
                populateStorage();
            }

            if (request.Type.Equals("listServer"))
                requestList(request.Data);

            if (request.Type.Equals("listGlobal"))
            {
                foreach (string id in storageClientsMap.Keys)
                {
                    lock (this)
                    {
                        requestList(id);
                    }
                }
                   
            }

            if (request.Type.Equals("crash"))
            {
                lock (this)
                {
                    storageNodesMap.Remove(request.Data);
                    storageClientsMap.Remove(request.Data);
                }
                sendCrashNotification(request.Data);
            }

            if (request.Type.Equals("status"))
                sendStatusRequest();

            if (request.Type.Equals("debug"))
            {
                Console.WriteLine("debuuuuuuug");
                sendDebugRequest();
            }

            return postInitReply;
        }

        public DIDAFileSendReply sendFileImpl(DIDAFileSendRequest request)
        {
            DIDAFileSendReply fileSendReply = new DIDAFileSendReply
            {
                Ack = "ack"
            };

            Console.WriteLine(request);
            string[] workers = request.Workers.Split(';');
            for (int i = 0; i < workers.Length-1; i++)
            {
                Console.WriteLine(workers[i]);
                string[] parameters = workers[i].Split(' ');
                GrpcChannel channel = GrpcChannel.ForAddress(parameters[1]);
                DIDASchedulerService.DIDASchedulerServiceClient client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);
                lock (this)
                {
                    workersMap.Add(parameters[0], parameters[1]);
                    workerClientsMap.Add(parameters[1], client);
                }
                Console.WriteLine("workerClientsMap size: " + workerClientsMap.Count);
                DIDAWorkerDelayRequest workerDelay = new DIDAWorkerDelayRequest
                {
                    Delay = parameters[2]
                };
                client.sendWorkerDelayAsync(workerDelay);
            }

            string[] storageNodes = request.StorageNodes.Split(';');
            for (int i = 0; i < storageNodes.Length - 1; i++)
            {
                Console.WriteLine(storageNodes[i]);
                string[] parameters = storageNodes[i].Split(' ');
                Console.WriteLine(parameters[1]);
                GrpcChannel channel = GrpcChannel.ForAddress(parameters[1]);
                DIDAStorageService.DIDAStorageServiceClient client = new DIDAStorageService.DIDAStorageServiceClient(channel);
                lock (this)
                {
                    storageNodesMap.Add(parameters[0], parameters[1]);
                    storageClientsMap.Add(parameters[0], client);
                }
                DIDAUpdateServerIdRequest serverIdRequest = new DIDAUpdateServerIdRequest();
                serverIdRequest.ServerId = parameters[0];
                serverIdRequest.StorageNodes.Add(storageNodes);
                serverIdRequest.GossipDelay = parameters[2];
                client.updateServerId(serverIdRequest);
            }

            return fileSendReply;
        }

        public void sendDebugRequest()
        {
            DIDAWorkerDebugRequest requestWorker = new DIDAWorkerDebugRequest { Debug = "debug" };
            DIDAStorageDebugRequest requestStorage = new DIDAStorageDebugRequest { Debug = "debug" };

            foreach (var item in storageClientsMap)
            {
                item.Value.startStorageDebugAsync(requestStorage);
            }

            foreach (var item in workerClientsMap)
            {
                item.Value.startWorkerDebugAsync(requestWorker);
            }
        }

        public void sendStatusRequest()
        {
            DIDAWorkerStatusRequest requestWorker = new DIDAWorkerStatusRequest { Request = "status" };
            DIDAStorageStatusRequest requestStorage = new DIDAStorageStatusRequest { Request = "status" };

            foreach (var item in storageClientsMap)
            {
                item.Value.getStorageStatusAsync(requestStorage);
            }

            foreach (var item in workerClientsMap)
            {
                item.Value.getWorkerStatusAsync(requestWorker);
            }
        }

        public void sendCrashNotification(string id)
        {
            DIDANotifyCrashWorkerRequest requestWorker = new DIDANotifyCrashWorkerRequest { ServerId = id };
            DIDANotifyCrashStorageRequest requestStorage = new DIDANotifyCrashStorageRequest{ ServerId = id };
            
            foreach (var item in storageClientsMap)
            {
                if (item.Key.Equals(id)) continue;
                item.Value.notifyCrashStorageAsync(requestStorage);
            }
            
            foreach (var item in workerClientsMap)
            {
                item.Value.notifyCrashWorkerAsync(requestWorker);
            }
        }

        public void populateStorage()
        {
            foreach (var item in storageClientsMap)
            {
                foreach (var identifier in populateDataMap)
                {
                    Console.WriteLine("id: " + identifier.Key);
                    Console.WriteLine("val: " + identifier.Value);
                    item.Value.write(new DIDAWriteRequest { Id = identifier.Key, Val = identifier.Value });
                }
            }


        }

        public void sendToWorker(string clientNumber)
        {
            firstWorker = workerClientsMap[lowestWorkerPort()];
            
            List<DIDAOperatorID> operatorsIDs = buildOperatorsIDs();
            DIDAAssignment[] assignments = buildAssignments(operatorsIDs);
            DIDARequest request = buildRequest(assignments, clientNumber);
            Console.WriteLine("request: " + request);
            DIDAStorageNode[] nodes = buildStorageNodes();
            DIDASendRequest sendRequest = new DIDASendRequest();
            sendRequest.Request = request;
            sendRequest.StorageNodes.Add(nodes);
            
            firstWorker.send(sendRequest);
        }

        public void requestList(string serverId)
        {
            storageClientsMap[serverId].listServer(new DIDAListServerRequest { Request = "list" });
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
            
            return assignments;
        }

        public DIDARequest buildRequest(DIDAAssignment[] assignments, string input)
        {
            DIDAMetaRecord metaRecord = new DIDAMetaRecord
            {
                Id = _metaRecordId
            };
            _metaRecordId++;

            DIDARequest request = new DIDARequest();
            request.Meta = metaRecord;
            request.Input = input;
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
        }
    }
}
