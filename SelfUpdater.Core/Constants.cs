using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SelfUpdater.Core
{
    internal class Constants
    {
        internal const string SETTING_STORAGE_CONNECTION_STRING = "SelfUpdater.Core.StorageConnectionString";
        internal const string SETTING_BLOB_CONTAINER = "SelfUpdater.Core.BlobContainer";
        internal const string SETTING_BLOB_CONTAINER_LEASING = "SelfUpdater.Core.BlobContainerLeasing";
        internal const string SETTING_CHECK_INTERVAL_SECONDS = "SelfUpdater.Core.CheckIntervalInSeconds";
        internal const string LOCK_BLOB_NAME = "lock";
    }
}
