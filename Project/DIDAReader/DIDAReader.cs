using DIDAWorker;
using DIDAStorageClient;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;

namespace DIDAOperator
{
    public class DIDAReader : DIDAWorker.IDIDAOperator
    {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, GrpcChannel> _storageChannels = new Dictionary<string, GrpcChannel>();
        delLocateStorageId _locationFunction;

        public void ConfigureStorage(DIDAStorageNode[] storageReplicas, delLocateStorageId locationFunction)
        {
            DIDAStorageService.DIDAStorageServiceClient client;
            GrpcChannel channel;

            _locationFunction = locationFunction;

            foreach (DIDAStorageNode n in storageReplicas)
            {
                channel = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port + "/");
                client = new DIDAStorageService.DIDAStorageServiceClient(channel);
                _storageServers.Add(n.serverId, client);
                _storageChannels.Add(n.serverId, channel);
            }
        }

        public string ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput)
        {
            Console.WriteLine("input string was: " + input);
            Console.Write("reading data record: " + meta.id + " with value: ");

            string storageServer = _locationFunction(meta.id.ToString(), OperationType.ReadOp).serverId;
            var val = _storageServers[storageServer]
                .read(new DIDAReadRequest { Id = meta.id.ToString(), Version = new DIDAStorageClient.DIDAVersion { VersionNumber = -1, ReplicaId = -1 } }); //aldrabated
            
            string storedString = val.Val;
            Console.WriteLine(storedString);
            
            return storedString;
        }
    }
}
