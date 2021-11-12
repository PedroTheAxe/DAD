using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DIDAWorkerUI
{
    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        private DIDAMetaRecordExtension _previousMeta;
        private string _previousOutput = "";
        private DIDAMetaRecordExtension _meta = new DIDAMetaRecordExtension();
        private StorageProxy _storageProxy;
        private int _workerDelay = 0;

        public SchedulerService()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            _meta.Id = 0;
            DIDAVersion version = new DIDAVersion()
            {
                VersionNumber = -1,
                ReplicaId = -1
            };
            _meta.Version = version;
        }

        public override Task<DIDAWorkerDelayReply> sendWorkerDelay(DIDAWorkerDelayRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendWorkerDelayImpl(request));
        }

        public DIDAWorkerDelayReply sendWorkerDelayImpl(DIDAWorkerDelayRequest request)
        {

            _workerDelay = Int32.Parse(request.Delay) * 1000;
            return new DIDAWorkerDelayReply { Ack = "ack" };
        }


        public override Task<DIDANotifyCrashWorkerReply> notifyCrashWorker(DIDANotifyCrashWorkerRequest request, ServerCallContext context)
        {
            return Task.FromResult(notifyCrashWorkerImpl(request));
        }

        public DIDANotifyCrashWorkerReply notifyCrashWorkerImpl(DIDANotifyCrashWorkerRequest request)
        {
            
            _storageProxy.removeFromClients(request.ServerId);
            return new DIDANotifyCrashWorkerReply { Ack = "ack" };
        }

        public override Task<DIDAPreviousOpReply> previousVersion(DIDAPreviousOpRequest request, ServerCallContext context)
        {
            return Task.FromResult(previousVersionImpl(request));
        }

        public DIDAPreviousOpReply previousVersionImpl(DIDAPreviousOpRequest request)
        {
            DIDAVersion version = new DIDAVersion
            {
                VersionNumber = request.Meta.Version.VersionNumber,
                ReplicaId = request.Meta.Version.ReplicaId
            };

            DIDAMetaRecordExtension meta = new DIDAMetaRecordExtension {
                Id = request.Meta.Id, 
                Version = version 
            };

            _previousMeta = meta;
            return new DIDAPreviousOpReply { Ack = "ack" };
        }

        public override Task<DIDASendReply> send(DIDASendRequest request, ServerCallContext context)
        {
            return Task.FromResult(sendImpl(request));
        }

        public DIDASendReply sendImpl(DIDASendRequest request)
        {
            Console.WriteLine(request);

            string className = request.Request.Chain[request.Request.Next].Op.Classname;
            DIDAMetaRecordExtension newMeta = reflectionLoad(className, request, _meta); //.dll reflection

            _meta = newMeta;

            DIDASendReply sendReply = new DIDASendReply
            {
                Ack = "ack"
            };

            if (request.Request.Next < request.Request.ChainSize)
            {
                Console.WriteLine("-------------------------------");
                sendToNextWorker(request);
                Console.WriteLine("-------------------------------");
            }


            return sendReply;
        }

        public void sendToNextWorker(DIDASendRequest request)
        {
            request.Request.Next++;
            string host = request.Request.Chain[request.Request.Next].Host;
            string port = request.Request.Chain[request.Request.Next].Port.ToString();
            string url = "http://" + host + ":" + port;
            Console.WriteLine(url);

            GrpcChannel channel = GrpcChannel.ForAddress(url);
            DIDASchedulerService.DIDASchedulerServiceClient client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);

            DIDAPreviousOpRequest previousOpRequest = new DIDAPreviousOpRequest();
            DIDAVersion version = new DIDAVersion
            {
                ReplicaId = _meta.Version.ReplicaId,
                VersionNumber = _meta.Version.VersionNumber
            };
            DIDAMetaRecordExtension meta = new DIDAMetaRecordExtension
            {
                Id = request.Request.Meta.Id,
                Version = version
            };
            previousOpRequest.Meta = meta;
            client.previousVersionAsync(previousOpRequest);

            DIDASendRequest sendRequest = new DIDASendRequest();
            sendRequest.Request = request.Request;
            sendRequest.StorageNodes.Add(request.StorageNodes);
            
            if (_storageProxy.getApplicationTermination()) return;
            
            Thread.Sleep(_workerDelay);
            client.sendAsync(sendRequest);
        }

        public DIDAMetaRecordExtension reflectionLoad(string className, DIDASendRequest request, DIDAMetaRecordExtension meta)
        {
            string _dllNameTermination = ".dll";
            string _currWorkingDir = Directory.GetCurrentDirectory();
            string savingPath = Path.GetFullPath(Path.Combine(_currWorkingDir, @"..\..\..\..\"));
            DIDAWorker.IDIDAOperator _objLoadedByReflection;

            Console.WriteLine("directory: " + savingPath);

            foreach (string filename in Directory.EnumerateFiles(savingPath))
            {
                Console.WriteLine("file in cwd: " + filename);
                if (filename.EndsWith(_dllNameTermination))
                {
                    Console.WriteLine(".ddl found");
                    Assembly _dll = Assembly.LoadFrom(filename);
                    Type[] _typeList = _dll.GetTypes();

                    foreach (Type type in _typeList)
                    {
                        Console.WriteLine("type contained in dll: " + type.Name);
                        if (type.Name == className)
                        {
                            Console.WriteLine("Found type to load dynamically: " + className);
                            _objLoadedByReflection = (DIDAWorker.IDIDAOperator)Activator.CreateInstance(type);
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                Console.WriteLine("method from class " + className + ": " + method.Name);
                            }
                            DIDAStorageNode[] storageNodes = new DIDAStorageNode[request.StorageNodes.Count];
                            int i = 0;
                            foreach(DIDAStorageNode n in request.StorageNodes)
                            {
                                storageNodes[i] = n;
                                i++;
                                Console.WriteLine(i);
                            }
                            
                            _storageProxy = new StorageProxy(storageNodes, meta);
                            _objLoadedByReflection.ConfigureStorage(_storageProxy);
                            _previousOutput = _objLoadedByReflection.ProcessRecord(new DIDAWorker.DIDAMetaRecord { Id = request.Request.Meta.Id }, request.Request.Input, _previousOutput);
                            _storageProxy.setPreviousMeta();

                            return _storageProxy.getPreviousMeta();
                        }
                    }
                }
            }
            return null;
        }

    }

    public class StorageProxy : IDIDAStorage
    {
        Dictionary<int, DIDAStorageService.DIDAStorageServiceClient> _clients = new Dictionary<int, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, Grpc.Net.Client.GrpcChannel> _channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();
        DIDAMetaRecordExtension _previousMeta = new DIDAMetaRecordExtension();
        DIDAMetaRecordExtension _newMeta = new DIDAMetaRecordExtension();
        DIDAStorageNode[] _storageNodes;
        private int _replicationFactor = 2;
        private bool _applicationTermination = false;


        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecordExtension meta)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            _storageNodes = storageNodes;
            
            foreach (DIDAStorageNode n in storageNodes)
            {
                _channels[n.ServerId] = Grpc.Net.Client.GrpcChannel.ForAddress("http://" + n.Host + ":" + n.Port);
                _clients[calculateHash(n.ServerId)] = new DIDAStorageService.DIDAStorageServiceClient(_channels[n.ServerId]);
            }

            DIDAVersion version = new DIDAVersion()
            {
                VersionNumber = -1,
                ReplicaId = -1
            };

            _previousMeta = meta;
            _newMeta.Id = 0;
            _newMeta.Version = version;
        }

        public void removeFromClients(string id)
        {
            _clients.Remove(calculateHash(id));
            _channels.Remove(id);
            Console.WriteLine("removed " + id);
        }

        public bool getApplicationTermination()
        {
            return _applicationTermination;
        }

        public DIDAMetaRecordExtension getPreviousMeta()
        {
            return _previousMeta;
        }

        public void setPreviousMeta()
        {
            _previousMeta = _newMeta;
        }

        public int calculateHash(string id)
        {
            byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
            var value = BitConverter.ToInt32(encoded, 0) % 1000000;
            return value;
        }

        public int chooseReplica(int recordHash)
        {
            int serverNode = 0;
            int position = 0;
            var keys = new List<int>(_clients.Keys);
            keys.Sort();

            foreach (int i in keys)
            {
                if (recordHash < i)
                {
                    serverNode = i;
                    break;
                }

                position++;

                if (position == _clients.Keys.Count)
                {
                    serverNode = keys[0];
                    break;
                }

            }
            return serverNode;
        }

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            int serverNode = chooseReplica(calculateHash(r.Id));
            bool replicaNotCrashed = true;
            int initialPosition = 0;
            int position = 0;

            List<int> serverIds = new List<int>();

            foreach (DIDAStorageNode n in _storageNodes)
            {
                serverIds.Add(calculateHash(n.ServerId));
            }
            serverIds.Sort();
            foreach (var i in serverIds)
            {
                if (serverNode == i) break;
                initialPosition++; //finds where the storage is in the ring (position)
            }
            position = initialPosition;

            while (replicaNotCrashed)
            {
                try
                {
                    if (r.Version.VersionNumber == -1 && r.Version.ReplicaId == -1)
                    {
                        if (_previousMeta.Version.VersionNumber == -1 && _previousMeta.Version.ReplicaId == -1)
                        {
                            var resultStorage = _clients[serverNode].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
                            _newMeta.Version.VersionNumber = resultStorage.Version.VersionNumber;
                            _newMeta.Version.ReplicaId = resultStorage.Version.ReplicaId;
                            return new DIDAWorker.DIDARecordReply { Id = resultStorage.Id, Val = resultStorage.Val, Version = { VersionNumber = resultStorage.Version.VersionNumber, ReplicaId = resultStorage.Version.ReplicaId } };
                        }

                        var resultPrevious = _clients[serverNode].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = _previousMeta.Version.VersionNumber, ReplicaId = _previousMeta.Version.ReplicaId } });
                        _newMeta.Version.VersionNumber = resultPrevious.Version.VersionNumber;
                        _newMeta.Version.ReplicaId = resultPrevious.Version.ReplicaId;
                        return new DIDAWorker.DIDARecordReply { Id = resultPrevious.Id, Val = resultPrevious.Val, Version = { VersionNumber = resultPrevious.Version.VersionNumber, ReplicaId = resultPrevious.Version.ReplicaId } };
                    }

                    var res = _clients[serverNode].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
                    _newMeta.Version.VersionNumber = res.Version.VersionNumber;
                    _newMeta.Version.ReplicaId = res.Version.ReplicaId;
                    return new DIDAWorker.DIDARecordReply { Id = res.Id, Val = res.Val, Version = { VersionNumber = res.Version.VersionNumber, ReplicaId = res.Version.ReplicaId } };
                }
                catch (RpcException e)
                {
                    position++;
                    if ((position - initialPosition) != _replicationFactor)
                        serverNode = serverIds[position % serverIds.Count];
                    else
                        replicaNotCrashed = false;
                }
            }
            _applicationTermination = true;
            return new DIDAWorker.DIDARecordReply { Id = "", Val = "", Version = { VersionNumber = -1, ReplicaId = -1 } };
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            int serverNode = chooseReplica(calculateHash(r.Id));
            var res = _clients[serverNode].write(new DIDAWriteRequest { Id = r.Id, Val = r.Val });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            int serverNode = chooseReplica(calculateHash(r.Id));
            var res = _clients[serverNode].updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // code to start grpc server for worker 
            Console.WriteLine(args[1]);

            string[] decomposedArgs = args[1].Split(":");

            decomposedArgs[1] = decomposedArgs[1].Substring(2);
            string host = decomposedArgs[1];
            Console.WriteLine(host);

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new SchedulerService()) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadLine();
            server.ShutdownAsync().Wait();
        }
    }
}
