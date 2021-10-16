using DIDAWorker;
using System;
using System.Collections.Generic;
using System.Text;
//using DIDAStorageClient;

namespace LibDIDAWorker
{
    class DIDAOperator : IDIDAOperator
    {
        //Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        public void ConfigureStorage(DIDAStorageNode[] storageReplicas, delLocateStorageId locationFunction)
        {
            throw new NotImplementedException();
        }

        public string ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput)
        {
            throw new NotImplementedException();
        }
    }
}
