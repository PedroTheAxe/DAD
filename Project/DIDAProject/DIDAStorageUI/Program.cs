using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;

namespace DIDAStorageUI {
    // dummy storage
    class StorageService : DIDAStorageService.DIDAStorageServiceBase {
        
        public List<DIDARecord> recordsList = new List<DIDARecord>();

        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context) {
            return Task.FromResult<DIDARecordReply>(ReadImpl(request));
        }

        private DIDARecordReply ReadImpl(DIDAReadRequest request) {
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

            if (request.Version.Equals(null))
            {
                DIDARecord recordVersionNull = recordsList.Find(r => r.version.versionNumber == latestVersionNumber);
                return new DIDARecordReply
                {
                    Id = recordVersionNull.id,
                    Version = { ReplicaId = recordVersionNull.version.replicaId, VersionNumber = recordVersionNull.version.versionNumber },
                    Val = null
                };
            }

            DIDARecord record = recordsList.Find(r => r.version.Equals(request.Version));
            return new DIDARecordReply
            {
                Id = record.id,
                Version = { ReplicaId = record.version.replicaId, VersionNumber = record.version.versionNumber },
                Val = record.val
            };

        }

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(UpdateImpl(request));
        }

        private DIDAVersion UpdateImpl(DIDAUpdateIfRequest request) {
            //TODO: all previous conditional updates and writes must have been applied
            DIDARecord record = recordsList.Find(r => r.val.Equals(request.Oldvalue));
            if (!record.Equals(null)) //assumption from the internet
            {
                DIDAWriteRequest writeRequest = new DIDAWriteRequest
                {
                    Id = request.Id,
                    Val = request.Newvalue
                };
                DIDAVersion version = ((StorageService)this).WriteImpl(writeRequest); //why not only write??
                return version;
            }
            else
            {
                return new DIDAVersion
                {
                    VersionNumber = -1,
                    ReplicaId = 0, //no clue what i should use here
                };
            }
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(WriteImpl(request));
        }

        private DIDAVersion WriteImpl(DIDAWriteRequest request) {
            int latestVersionNumber = -1;
            int replicaId = 0; //tenho de saber em q replica quero meter -- should be a list or a dict

            foreach (DIDARecord r in recordsList)
            {
                if (r.version.replicaId == replicaId)
                    latestVersionNumber = Math.Max(latestVersionNumber, r.version.versionNumber);
            }

            DIDAVersion version = new DIDAVersion
            {
                ReplicaId = replicaId,
                VersionNumber = latestVersionNumber + 1
            };

            DIDARecord record = new DIDARecord
            {
                id = request.Id,
                val = request.Val,
                version = { replicaId = version.ReplicaId, versionNumber = version.VersionNumber },
            };
            recordsList.Add(record);

            return version;
        }
    }

    class Program {
        static void Main(string[] args) {
            int Port = 2001;
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(new StorageService()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
