using System;
using System.Collections.Generic;
using System.Text;
using DIDAStorage;

namespace LibDIDAStorage
{
    class DIDAStorageOperations : IDIDAStorage
    {
        public List<DIDARecord> recordsList = new List<DIDARecord>();

        DIDARecord IDIDAStorage.read(string id, DIDAVersion version)
        {
            int latestVersionNumber = 0;

            foreach (DIDARecord record in recordsList)
            {
                if (record.id == id)
                    latestVersionNumber = Math.Max(latestVersionNumber, record.version.versionNumber);
            }

            if (version.versionNumber > latestVersionNumber)
                return new DIDARecord
                {
                    id = id,
                    version = version,
                    val = null
                };

            if (version.Equals(null))
                return recordsList.Find(r => r.version.versionNumber == latestVersionNumber);

            return recordsList.Find(r => r.version.Equals(version));
        }

        DIDAVersion IDIDAStorage.write(string id, string val)
        {
            int latestVersionNumber = 0;
            int replicaId = 0; //tenho de saber em q replica quero meter -- should be a list or a dict

            foreach (DIDARecord r in recordsList)
            {
                if (r.version.replicaId == replicaId)
                    latestVersionNumber = Math.Max(latestVersionNumber, r.version.versionNumber);
            }

            DIDAVersion version = new DIDAVersion
            {
                versionNumber = latestVersionNumber + 1, //starts always with 0??? -- no
                replicaId = 0, //hardcoded since we only have one replica -- fazer lista de replicas???
            };
    
            DIDARecord record = new DIDARecord
            {
                id = id,
                val = val,
                version = version
            };
            recordsList.Add(record);

            return version;
        }

        DIDAVersion IDIDAStorage.updateIfValueIs(string id, string oldvalue, string newvalue)
        {
            //TODO: all previous conditional updates and writes must have been applied
            DIDARecord record = recordsList.Find(r => r.val.Equals(oldvalue));
            if (!record.Equals(null)) //assumption from the internet
            {
                /*DIDAStorageOperations storageOp = new DIDAStorageOperations();
                IDIDAStorage storage = storageOp;*/
                DIDAVersion version = ((IDIDAStorage)this).write(id, newvalue); //don't know if works as expected
                return version;
            } else
            {
                return new DIDAVersion
                {
                    versionNumber = -1,
                    replicaId = 0, //no clue what i should use here
                };
            }
        }
    }
}
