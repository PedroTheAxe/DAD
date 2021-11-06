﻿using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace DIDAWorkerUI
{
    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        private DIDAMetaRecordExtension _previousMeta;
        private string _previousOutput = "";
        //DIDASendRequest _request;
        //private List<DIDAStorageNode> _storageNodes;
        private DIDAMetaRecordExtension _meta = new DIDAMetaRecordExtension();

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
            //DIDAStorageService.DIDAStorageServiceClient clientOp = new DIDAStorageService.DIDAStorageServiceClient(channel);

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
            var previousMeta = client.previousVersionAsync(previousOpRequest);

            DIDASendRequest sendRequest = new DIDASendRequest();
            sendRequest.Request = request.Request;
            sendRequest.StorageNodes.Add(request.StorageNodes);
            var reply = client.sendAsync(sendRequest);
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
                            
                            StorageProxy storageProxy = new StorageProxy(storageNodes, meta);
                            _objLoadedByReflection.ConfigureStorage(storageProxy);
                            _previousOutput = _objLoadedByReflection.ProcessRecord(new DIDAWorker.DIDAMetaRecord { Id = request.Request.Meta.Id }, request.Request.Input, _previousOutput);
                            Console.WriteLine("previous: " + _previousOutput);
                            storageProxy.setPreviousMeta();
                            return storageProxy.getPreviousMeta();
                        }
                    }
                }
            }
            return null;
        }

    }

    public class StorageProxy : IDIDAStorage
    {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _clients = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, Grpc.Net.Client.GrpcChannel> _channels = new Dictionary<string, Grpc.Net.Client.GrpcChannel>();
        DIDAMetaRecordExtension _previousMeta = new DIDAMetaRecordExtension();
        DIDAMetaRecordExtension _newMeta = new DIDAMetaRecordExtension();
      
        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecordExtension meta)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (DIDAStorageNode n in storageNodes)
            {
                _channels[n.ServerId] = Grpc.Net.Client.GrpcChannel.ForAddress("http://" + n.Host + ":" + n.Port);
                _clients[n.ServerId] = new DIDAStorageService.DIDAStorageServiceClient(_channels[n.ServerId]);
            }
            _previousMeta = meta;
            _newMeta.Id = 0;
            DIDAVersion version = new DIDAVersion()
            {
                VersionNumber = -1,
                ReplicaId = -1
            };
            _newMeta.Version = version;
        }

        public DIDAMetaRecordExtension getPreviousMeta()
        {
            return _previousMeta;
        }

        public void setPreviousMeta()
        {
            _previousMeta = _newMeta;
        }

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            if (r.Version.VersionNumber == -1 && r.Version.ReplicaId == -1)
            {
                Console.WriteLine("version null");
                if (_previousMeta.Version.VersionNumber == -1  && _previousMeta.Version.ReplicaId == -1)
                {
                    Console.WriteLine("first worker");
                    var resultStorage = _clients["s1"].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
                    Console.WriteLine("puattaa");
                    _newMeta.Version.VersionNumber = resultStorage.Version.VersionNumber;
                    _newMeta.Version.ReplicaId = resultStorage.Version.ReplicaId;
                    Console.WriteLine(resultStorage.Version.VersionNumber);
                    Console.WriteLine(resultStorage.Version.ReplicaId);
                    return new DIDAWorker.DIDARecordReply { Id = resultStorage.Id, Val = resultStorage.Val, Version = { VersionNumber = resultStorage.Version.VersionNumber, ReplicaId = resultStorage.Version.ReplicaId } };
                }
                Console.WriteLine("read previous version");
                Console.WriteLine(_previousMeta.Version.VersionNumber);
                Console.WriteLine(_previousMeta.Version.ReplicaId);
                var resultPrevious = _clients["s1"].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = _previousMeta.Version.VersionNumber, ReplicaId = _previousMeta.Version.ReplicaId } });
                _newMeta.Version.VersionNumber = resultPrevious.Version.VersionNumber;
                _newMeta.Version.ReplicaId = resultPrevious.Version.ReplicaId;
                Console.WriteLine(resultPrevious.Version.VersionNumber);
                Console.WriteLine(resultPrevious.Version.ReplicaId);
                return new DIDAWorker.DIDARecordReply { Id = resultPrevious.Id, Val = resultPrevious.Val, Version = { VersionNumber = resultPrevious.Version.VersionNumber, ReplicaId = resultPrevious.Version.ReplicaId } };

            }
            Console.WriteLine("read from version " + r.Version.VersionNumber);
            var res = _clients["s1"].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
            _newMeta.Version.VersionNumber = res.Version.VersionNumber;
            _newMeta.Version.ReplicaId = res.Version.ReplicaId;
            return new DIDAWorker.DIDARecordReply { Id = res.Id, Val = res.Val, Version = { VersionNumber = res.Version.VersionNumber, ReplicaId = res.Version.ReplicaId } };
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            var res = _clients["s1"].write(new DIDAWriteRequest { Id = r.Id, Val = r.Val });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            var res = _clients["s1"].updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }
    }



    //public class DIDAMetaRecordExtension : DIDAWorker.DIDAMetaRecord
    //{
    //    public DIDAVersion _outputVersion;
    
    //    public DIDAMetaRecordExtension(int id, DIDAVersion version)
    //    {
    //        Id = id;
    //        _outputVersion = version;
    //    }
    //}

    //class WorkerService : DIDAStorageService.DIDAStorageServiceBase
    //{
    //    private DIDAMetaRecordExtension _previousMeta;

    //    public WorkerService()
    //    {
    //        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    //    }

    //    public override Task<DIDAPreviousOpReply> previousVersion(DIDAPreviousOpRequest request, ServerCallContext context)
    //    {
    //        return Task.FromResult(previousVersionImpl(request));
    //    }

    //    public DIDAPreviousOpReply previousVersionImpl(DIDAPreviousOpRequest request)
    //    {
    //        DIDAVersion version = new DIDAVersion
    //        {
    //            VersionNumber = request.Meta.Version.VersionNumber,
    //            ReplicaId = request.Meta.Version.ReplicaId 
    //        };
            
    //        DIDAMetaRecordExtension meta = new DIDAMetaRecordExtension(request.Meta.Id, version);

    //        _previousMeta = meta;
    //        return new DIDAPreviousOpReply { Ack = _previousMeta };
    //    }
    //}
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
                Services = { DIDASchedulerService.BindService(new SchedulerService())/*, DIDAStorageService.BindService(new WorkerService())*/ },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadLine();
            server.ShutdownAsync().Wait();

            //CODE OF SEND TO OTHER WORKER (TODO GRPC, CHANNEL...)
            //request.chain[next].operator.ProcessRecord(DIDAMetaRecord, inputString,
            //previousOperatorOutput);
            //request.next = request.next + 1;
            //if (request.next < request.chainSize)
            //    forward request to(request.chain[next].host, request.chain[next].port);
        }
    }
}
