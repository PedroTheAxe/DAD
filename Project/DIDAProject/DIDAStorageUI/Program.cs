using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DIDAStorage;
using Grpc.Core;
using Grpc.Net.Client;

namespace DIDAStorageUI
{

    class StorageService : DIDAStorageService.DIDAStorageServiceBase {


        private string url = "";
        public List<DIDARecord> recordsList = new List<DIDARecord>();
        private string _serverId = "";
        private Dictionary<int, string> storageNodesMap = new Dictionary<int, string>();
        private Dictionary<int, DIDAStorageService.DIDAStorageServiceClient> storageClientsMap = new Dictionary<int, DIDAStorageService.DIDAStorageServiceClient>();
        private Dictionary<string, bool> storageNodesAliveMap = new Dictionary<string, bool>();
        private int replicationFactor = 2;
        private Dictionary<DIDARecordInfo, DIDAUpdateIfRequest> updateLog = new Dictionary<DIDARecordInfo, DIDAUpdateIfRequest>();
        private Dictionary<DIDARecordInfo, DIDAWriteRequest> writeLog = new Dictionary<DIDARecordInfo, DIDAWriteRequest>();
        private int MaxVersions = 5;
        Timer t = new Timer();

        public StorageService()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            t.Elapsed += new ElapsedEventHandler(ExecuteReplication);
            t.Interval = 10000; //miliseconds
            t.AutoReset = true;
            t.Start();
        }

        private void ExecuteReplication(Object source, ElapsedEventArgs e)
        {
            if (_serverId.Equals(""))
            {
                Console.WriteLine("no serverId yet!");
                return;
            }

            DIDAReplicationRequest request = new DIDAReplicationRequest();

            DIDAWriteLog[] writeLogArray = new DIDAWriteLog[writeLog.Count];
            int i = 0;

            foreach (var item in writeLog)
            {
                DIDAWriteLog writeRequestLog = new DIDAWriteLog();
                writeRequestLog.Record = item.Key;
                writeRequestLog.Request = item.Value;
                writeLogArray[i] = writeRequestLog;
                i++;
            }

            DIDAUpdateLog[] updateLogArray = new DIDAUpdateLog[updateLog.Count];
            int j = 0;

            foreach (var item in updateLog)
            {
                DIDAUpdateLog udpdateRequestLog = new DIDAUpdateLog();
                udpdateRequestLog.Record = item.Key;
                udpdateRequestLog.Request = item.Value;
                updateLogArray[j] = udpdateRequestLog;
                j++;
            }

            request.WriteLog.Add(writeLogArray);
            request.UpdateLog.Add(updateLogArray);

            lock(this)
            {
                foreach (var item in storageClientsMap)
                {
                    item.Value.replicateAsync(request);
                }

            }

            //replication done -- clear logs
            request.WriteLog.Clear();
            writeLog.Clear();

            request.UpdateLog.Clear();
            updateLog.Clear();

            //Console.WriteLine("-------------------------DEBUG-------------------------------");
        }

        public override Task<DIDAStorageStatusReply> getStorageStatus(DIDAStorageStatusRequest request, ServerCallContext context)
        {
            return Task.FromResult(getStorageStatusImpl(request));
        }

        public DIDAStorageStatusReply getStorageStatusImpl(DIDAStorageStatusRequest request)
        {
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("SERVER ID: " + _serverId);
            Console.WriteLine("SERVER ID HASH: " + calculateHash(_serverId));
            Console.WriteLine("SERVER URL: " + url);
            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("INFORMATION ABOUT THE RECORD DATABASE: ");
            foreach (var record in recordsList)
            {
                Console.WriteLine("Record id: " + record.id);
                Console.WriteLine("Record stored value: " + record.val);
                Console.WriteLine("Record version number: " + record.version.versionNumber);
                Console.WriteLine("Replica associated to record: id = " + record.version.replicaId);
                Console.WriteLine("\r\n");
            }

            Console.WriteLine("-----------------------------------------------------------------");
            Console.WriteLine("INFORMATION ABOUT OTHER STORAGE NODES: ");
            foreach (var item in storageNodesAliveMap)
            {
                if (item.Value) 
                {
                    Console.WriteLine("Storage server " + item.Key + " is alive");
                }
                else
                {
                    Console.WriteLine("Storage server " + item.Key + " is presumed dead");
                }
            }
            Console.WriteLine("-----------------------------------------------------------------");
            return new DIDAStorageStatusReply { Ack = "ack" };
        }

        public override Task<DIDANotifyCrashStorageReply> notifyCrashStorage(DIDANotifyCrashStorageRequest request, ServerCallContext context)
        {
            return Task.FromResult(notifyCrashStorageImpl(request));
        }

        public DIDANotifyCrashStorageReply notifyCrashStorageImpl(DIDANotifyCrashStorageRequest request)
        {
            lock(this)
            {
                storageNodesMap.Remove(calculateHash(request.ServerId));
                storageClientsMap.Remove(calculateHash(request.ServerId));
                storageNodesAliveMap[request.ServerId] = false;
            }
            createConnection();
            Console.WriteLine("removed " + request.ServerId);
            return new DIDANotifyCrashStorageReply { Ack = "ack" };
        }

        public override Task<DIDAReplicationReply> replicate(DIDAReplicationRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAReplicationReply>(replicateImpl(request));
        }

        public DIDAReplicationReply replicateImpl(DIDAReplicationRequest request)
        {
            //Console.WriteLine(request);
            //Console.WriteLine("entrar");
            List<DIDAWriteLog> writeQueue = new List<DIDAWriteLog>();
            List<DIDAUpdateLog> updateQueue = new List<DIDAUpdateLog>();
            
            foreach (var update in request.UpdateLog)
            {
                updateQueue.Add(update);
                request.UpdateLog.Remove(update);
            }
            updateQueue = updateQueue.OrderBy(wr => wr.Record.Version.VersionNumber).ToList();

            foreach (var update in updateQueue)
            {
                foreach(var write in request.WriteLog)
                {
                    if (update.Record.Id.Equals(write.Record.Id) && update.Record.Version.VersionNumber > write.Record.Version.VersionNumber)
                    {
                        writeQueue.Add(write);
                        request.WriteLog.Remove(write);
                    }      
                }

                writeQueue = writeQueue.OrderBy(wr => wr.Record.Version.VersionNumber).ToList();
                foreach(var w in writeQueue)
                {
                    WriteAndDelete(w.Request);
                }

                writeQueue.Clear();
                UpdateAndDelete(update.Request);
            }

            foreach (var write in request.WriteLog)
            {
                WriteAndDelete(write.Request);
            }

            return new DIDAReplicationReply { Ack = "ack" };
        }

        public override Task<DIDAUpdateServerIdReply> updateServerId(DIDAUpdateServerIdRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAUpdateServerIdReply>(UpdateServerIdImpl(request));
        }

        public DIDAUpdateServerIdReply UpdateServerIdImpl(DIDAUpdateServerIdRequest request)
        {
            _serverId = request.ServerId;
            t.Interval = Int32.Parse(request.GossipDelay) * 1000; //miliseconds
            for (int i = 0; i < request.StorageNodes.Count - 1; i++)
            {
                Console.WriteLine(request.StorageNodes[i]);
                string[] parameters = request.StorageNodes[i].Split(' ');
                Console.WriteLine(parameters[1]);
                lock (this)
                {
                    storageNodesMap.Add(calculateHash(parameters[0]), parameters[1]);
                    storageNodesAliveMap.Add(parameters[0], true);
                }
            }
            createConnection();
            Console.WriteLine(_serverId);
            Console.WriteLine(calculateHash(_serverId));
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

                DIDARecordReply r = new DIDARecordReply();
                r.Id = recordVersionNull.id;
                r.Version = v;
                r.Val = recordVersionNull.val;

                return r;
            }

            DIDARecord record = recordsList.Find(r => r.version.versionNumber == latestVersionNumber);
            
            DIDAVersion version = new DIDAVersion();
            version.ReplicaId = record.version.replicaId;
            version.VersionNumber = latestVersionNumber;


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
            DIDARecord record = recordsList.Find(r => r.val.Equals(request.Oldvalue));
            if (!record.Equals(null)) //assumption from the internet
            {
                DIDAWriteRequest writeRequest = new DIDAWriteRequest
                {
                    Id = request.Id,
                    Val = request.Newvalue
                };
                DIDAVersion version = ((StorageService)this).WriteAndDelete(writeRequest);
                DIDARecordInfo recordInfo = new DIDARecordInfo
                {
                    Id = request.Id,
                    Version = version
                };
                updateLog.Add(recordInfo, request);
                return version;
            }
            else
            {
                DIDAVersion version = new DIDAVersion
                {
                    VersionNumber = -1,
                    ReplicaId = -1,
                };
                DIDARecordInfo recordInfo = new DIDARecordInfo
                {
                    Id = request.Id,
                    Version = version
                };
                updateLog.Add(recordInfo, request);
                return version;
            }
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(WriteImpl(request));
        }

        private DIDAVersion WriteImpl(DIDAWriteRequest request) {
            Console.WriteLine("write");
            int latestVersionNumber = 0;
            int replicaId = calculateHash(_serverId);

            foreach (DIDARecord r in recordsList)
            {
                if (r.id.Equals(request.Id))
                {
                    Console.WriteLine("estou aqui");
                    latestVersionNumber = Math.Max(latestVersionNumber, r.version.versionNumber);
                }
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


            if (recordsList.FindAll(r => r.id.Equals(record.id)).Count == MaxVersions + 1)
            {
                Console.WriteLine("removed Record for MaxVersions");
                recordsList.Remove(findLowestVersionNumber(record.id));
            }

            DIDARecordInfo recordInfo = new DIDARecordInfo
            {
                Id = request.Id,
                Version = v
            };
            writeLog.Add(recordInfo, request);
            return v;
        }

        public DIDAVersion WriteAndDelete(DIDAWriteRequest request)
        {
            DIDAVersion version = WriteImpl(request);
            DIDARecordInfo recordInfo = new DIDARecordInfo
            {
                Id = request.Id,
                Version = version
            };
            writeLog.Remove(recordInfo);
            return version;
        }

        public DIDAVersion UpdateAndDelete(DIDAUpdateIfRequest request)
        {
            DIDAVersion version = UpdateImpl(request);
            DIDARecordInfo recordInfo = new DIDARecordInfo
            {
                Id = request.Id,
                Version = version
            };
            updateLog.Remove(recordInfo);
            return version;
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

        public DIDARecord findLowestVersionNumber(string id)
        {
            int minVersionNumber = Int32.MaxValue;
            DIDARecord record = new DIDARecord();

            foreach (DIDARecord r in recordsList)
            {
                if (r.id.Equals(id))
                {
                    minVersionNumber = Math.Min(minVersionNumber, r.version.versionNumber);

                    if (minVersionNumber == r.version.versionNumber)
                    {
                        record.id = r.id;
                        record.version = r.version;
                        record.val = r.val;
                    }
                }
            }
            Console.WriteLine("record id after this function " + record.version.versionNumber);
            return record;
        }

        public int calculateHash(string id)
        {
            byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(id));
            var value = BitConverter.ToInt32(encoded, 0) % 1000000;
            return value;
        }
        public void createConnection()
        {
            var keys = new List<int>(storageNodesMap.Keys);
            keys.Sort();
            int position = 0;
            int key = 0;
            string url = "";
            foreach (var i in keys)
            {
                if (calculateHash(_serverId) == i) break;
                position++; //finds where the storage is in the ring (position)
            }
            for (int i = 1; i < replicationFactor; i++)
            {
                key = keys[(position + i) % keys.Count];
                url = storageNodesMap[key];
                
                if (storageClientsMap.ContainsKey(key)) continue;
                
                GrpcChannel channel = GrpcChannel.ForAddress(url);
                DIDAStorageService.DIDAStorageServiceClient client = new DIDAStorageService.DIDAStorageServiceClient(channel);
                storageClientsMap.Add(key, client);
            }            
        }

        public void setCredentials(string newUrl)
        {
            url = newUrl;

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
            storage.setCredentials(args[1]);
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
            Console.ReadLine();
        }
    }
}
