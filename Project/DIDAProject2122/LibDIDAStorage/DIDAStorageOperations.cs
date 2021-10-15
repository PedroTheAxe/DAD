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
            DIDAVersion version = new DIDAVersion
            {
                versionNumber = 0,
                replicaId = 0, //hardcoded since we only have one replica
            };
            DIDARecord record = new DIDARecord
            {
                id = id,
                val = val,
                version = version
            };
            //throw new NotImplementedException();
            return version;
        }

        DIDAVersion IDIDAStorage.updateIfValueIs(string id, string oldvalue, string newvalue)
        {
            throw new NotImplementedException();
        }
    }
}
