using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;

namespace DIDAStorageUI
{

    class StorageService : DIDAStorageService.DIDAStorageServiceBase {
        
        public List<DIDARecord> recordsList = new List<DIDARecord>();
        private string _serverId = "";
        private Dictionary<string, string> storageNodesMap = new Dictionary<string, string>();
        private int replicationFactor = 1;
        private Dictionary<DIDAVersion, DIDAUpdateIfRequest> updateLog = new Dictionary<DIDAVersion, DIDAUpdateIfRequest>();
        private Dictionary<DIDAVersion, DIDAWriteRequest> writeLog = new Dictionary<DIDAVersion, DIDAWriteRequest>();


        //TODO Replication Function -> add to Storage.proto + Consistency algorithm
        // Replication factor -> DONE
        // Log de updates e writes -> DONE
        // Function de replication + impl -> TODO (Push)
        // Periodicamente fazer a replication -> TODO

        //public override Task<DIDAReplicationReply> replicate(DIDAReplicationRequest request, ServerCallContext context)
        //{
        //    return Task.FromResult<DIDAUpdateServerIdReply>(replicateImpl(request));
        //}

        //public DIDAReplicationReply replicateImpl(DIDAReplicationRequest request)
        //{
        //TODO: consistency and updating logs (and recordsList?)

        // Consistency:
        //
        //              look at updateLog
        //                      if updateLog.sameDIDARecord.DIDAVersion.VersionNumber > writeLog.sameDIDARecord.DIDAVersion.VersionNumber (loop)
        //                              do WriteRequest(s) then
        //                              do UpdateRequest
        //
        //              before executing request
        //                      if writeLog.DIDARecord.id (or updateLog.DIDARecord.id) == recordsList.DIDARecord.id and  writeLog.DIDARecord.DIDAVersion.VersionNumber (or updateLog.DIDARecord.id) == recordsList.DIDARecord.DIDAVersion.VersionNumber
        //                              update Log and recordsList(?) with Record of highest replicaId (to discuss)
        //
        //}

        public Dictionary<DIDAVersion, DIDAUpdateIfRequest> getUpdateLog()
        {
            return updateLog;
        }

        public Dictionary<DIDAVersion, DIDAWriteRequest> getWriteLog()
        {
            return writeLog;
        }

        public override Task<DIDAUpdateServerIdReply> updateServerId(DIDAUpdateServerIdRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAUpdateServerIdReply>(UpdateServerIdImpl(request));
        }

        public DIDAUpdateServerIdReply UpdateServerIdImpl(DIDAUpdateServerIdRequest request)
        {
            _serverId = request.ServerId;
            for (int i = 0; i < request.StorageNodes.Count - 1; i++)
            {
                Console.WriteLine(request.StorageNodes[i]);
                string[] parameters = request.StorageNodes[i].Split(' ');
                Console.WriteLine(parameters[1]);
                lock (this)
                {
                    storageNodesMap.Add(parameters[0], parameters[1]);
                }
            }
            Console.WriteLine(_serverId);
            return new DIDAUpdateServerIdReply { Ack = "ack" };
        }

        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context) {
            return Task.FromResult<DIDARecordReply>(ReadImpl(request));
        }

        private DIDARecordReply ReadImpl(DIDAReadRequest request) {
            Console.WriteLine("read");
            int latestVersionNumber = 0;
            foreach (DIDARecord r in recordsList)
            {
                //Console.WriteLine(r);
                if (r.id == request.Id)
                    latestVersionNumber = Math.Max(latestVersionNumber, r.version.versionNumber);
            }

            if (request.Version.VersionNumber > latestVersionNumber)
                return new DIDARecordReply
                {
                    Id = request.Id,
                    Version = request.Version,
                    Val = null
                };

            if (request.Version.VersionNumber == -1 && request.Version.ReplicaId == -1)
            {
                DIDARecord recordVersionNull = recordsList.Find(r => r.version.versionNumber == latestVersionNumber);

                DIDAVersion v = new DIDAVersion();
                v.ReplicaId = recordVersionNull.version.replicaId;
                v.VersionNumber = recordVersionNull.version.versionNumber;
                //Console.WriteLine(v);
                DIDARecordReply r = new DIDARecordReply();
                r.Id = recordVersionNull.id;
                r.Version = v;
                r.Val = recordVersionNull.val;

                return r;
            }

            DIDARecord record = recordsList.Find(r => r.version.Equals(request.Version));
            
            DIDAVersion version = new DIDAVersion();
            version.ReplicaId = record.version.replicaId;
            version.VersionNumber = record.version.versionNumber;

            return new DIDARecordReply
            {
                Id = record.id,
                Version = version,
                Val = record.val
            };

        }

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(UpdateImpl(request));
        }

        private DIDAVersion UpdateImpl(DIDAUpdateIfRequest request) {
            Console.WriteLine("update");
            //TODO: all previous conditional updates and writes must have been applied
            DIDARecord record = recordsList.Find(r => r.val.Equals(request.Oldvalue));
            if (!record.Equals(null)) //assumption from the internet
            {
                DIDAWriteRequest writeRequest = new DIDAWriteRequest
                {
                    Id = request.Id,
                    Val = request.Newvalue
                };
                DIDAVersion version = ((StorageService)this).WriteImpl(writeRequest);
                return version;
            }
            else
            {
                return new DIDAVersion
                {
                    VersionNumber = -1,
                    ReplicaId = -1, //no clue what i should use here
                };
            }
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(WriteImpl(request));
        }

        private DIDAVersion WriteImpl(DIDAWriteRequest request) {
            Console.WriteLine("write");
            int latestVersionNumber = -1;
            int replicaId = _serverId.GetHashCode();

            foreach (DIDARecord r in recordsList)
            {
                if (r.version.replicaId == replicaId)
                    latestVersionNumber = Math.Max(latestVersionNumber, r.version.versionNumber);
            }

            DIDAVersion v = new DIDAVersion();
            v.ReplicaId = replicaId;
            v.VersionNumber = latestVersionNumber + 1;

            DIDAStorage.DIDAVersion vS = new DIDAStorage.DIDAVersion();
            vS.replicaId = replicaId;
            vS.versionNumber = latestVersionNumber + 1;

            DIDARecord record = new DIDARecord();
            record.id = request.Id;
            record.val = request.Val;
            record.version = vS;

            Console.WriteLine("id: " + record.id);
            Console.WriteLine("val: " + record.val);
            Console.WriteLine("Vnumber: " + record.version.versionNumber);

            recordsList.Add(record);

            return v;
        }

        public override Task<DIDAListServerReply> listServer(DIDAListServerRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAListServerReply>(listServerImpl(request));
        }

        public DIDAListServerReply listServerImpl(DIDAListServerRequest request)
        {
            if (request.Request.Equals("list"))
            {
                foreach (DIDARecord record in recordsList)
                {
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine("Record id: " + record.id);
                    Console.WriteLine("Record stored value: " + record.val);
                    Console.WriteLine("Record version number: " + record.version.versionNumber);
                }
            }

            return new DIDAListServerReply { Ack = "ack" };
        }
    }

    class Program {

        static void Main(string[] args) {
            Console.WriteLine(args[1]);
            string[] decomposedArgs = args[1].Split(":");

            decomposedArgs[1] = decomposedArgs[1].Substring(2);
            string host = decomposedArgs[1];
            Console.WriteLine(host);

            int port = Int32.Parse(decomposedArgs[2]);
            Console.WriteLine(port);

            StorageService storage = new StorageService();

            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(storage) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            //storage.replicate() 
            //private Timer timer1;
            //public void InitTimer()
            //{
            //    timer1 = new Timer();
            //    timer1.Tick += new EventHandler(timer1_Tick);
            //    timer1.Interval = 2000; // in miliseconds
            //    timer1.Start();
            //}

            //private void timer1_Tick(object sender, EventArgs e)
            //{
            //    isonline();
            //}
            server.ShutdownAsync().Wait();
            Console.ReadLine();
        }
    }
}
