using Microsoft.Web.Administration;
using SelfUpdater.Core.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SelfUpdater.Core.Extensions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using SelfUpdater.Core.Util;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace SelfUpdater.Core.Implementations
{
    public class UpdaterFromBlob : IUpdater
    {
        private Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer _container;
        private Microsoft.WindowsAzure.Storage.Blob.CloudBlobContainer _leaseContainer;
        private Microsoft.WindowsAzure.Storage.CloudStorageAccount _acct;
        private string _currentLeaseId;

        public UpdaterFromBlob(string storageConnectionString)
        {
            try
            {
                this._acct = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (KeyNotFoundException knex)
            {
                // it is development storage
                this._acct = CloudStorageAccount.DevelopmentStorageAccount;
            }

            var blobClnt = this._acct.CreateCloudBlobClient();
            string containerName = CloudConfigurationManager.GetSetting(Constants.SETTING_BLOB_CONTAINER);
            this._container = blobClnt.GetContainerReference(containerName);
            this._container.CreateIfNotExists();
            string leaseContainerName = CloudConfigurationManager.GetSetting(Constants.SETTING_BLOB_CONTAINER_LEASING);
            this._leaseContainer = blobClnt.GetContainerReference(leaseContainerName);
            this._leaseContainer.CreateIfNotExists();

        }

        public void CheckForUpdates(string roleInstanceName)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                var site = serverManager.Sites.Where(s => s.Name.Equals(roleInstanceName)).FirstOrDefault();
                if (site != null)
                {
                    try
                    {
                        ExecuteWithRetry.Action(this.Lock, 15, 5000);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Trace.TraceError("Could get lease, gave up trying: " + ex.Message);
                        return;
                    }
                    try
                    {
                        site.Stop();
                        var applicationRoot = site.Applications.Where(a => a.Path == "/").Single();
                        var virtualRoot = applicationRoot.VirtualDirectories.Where(v => v.Path == "/").Single();
                        System.Diagnostics.Trace.WriteLine(virtualRoot.PhysicalPath);
                        this.TraversePath(virtualRoot.PhysicalPath);
                        site.Start();
                    }
                    finally
                    {
                        if (this.IsUpdating)
                        {
                            try
                            {
                                ExecuteWithRetry.Action(this.Unlock, 15, 1000);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Trace.TraceError("Could get release lease, gave up trying: " + ex.Message);
                                this.BreakLock();
                            }
                        }
                    }
                }
            }

        }

        public bool IsUpdating
        {
            get
            {
                return !string.IsNullOrWhiteSpace(this._currentLeaseId);
            }
        }

        private void Lock()
        {
            var blob = this._leaseContainer.GetBlockBlobReference(Constants.LOCK_BLOB_NAME);
            //blob.BreakLease(TimeSpan.Zero);
            if (!blob.Exists())
            {
                using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes("lock")))
                {
                    blob.UploadFromStream(ms);
                }
            }
            
            // try to get infinite lease
            // the maximum time for lease is 60 seconds or infinite
            // we can't finish traversing all assembilies in 60 seconds
            // so get an infinite lease
            this._currentLeaseId = blob.AcquireLease(null, null);
        }

        private void Unlock()
        {
            var blob = this._leaseContainer.GetBlockBlobReference(Constants.LOCK_BLOB_NAME);
            AccessCondition ac = new AccessCondition();
            ac.LeaseId = this._currentLeaseId;
            blob.ReleaseLease(ac);
            this._currentLeaseId = string.Empty;
        }

        private void BreakLock()
        {
            if (string.IsNullOrWhiteSpace(this._currentLeaseId))
            {
                throw new ArgumentException("There is no current lease to break!");
            }
            var blob = this._leaseContainer.GetBlockBlobReference(Constants.LOCK_BLOB_NAME);
            blob.BreakLease(TimeSpan.Zero);
        }

        private void TraversePath(string path)
        {
            var binPath = Path.Combine(path, "bin");
            DirectoryInfo di = new DirectoryInfo(binPath);
            FileInfo[] assemblies = di.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
            foreach (var assembly in assemblies)
            {
                string hash = assembly.FullName.GetChecksum();
                this.CheckForBlobUpdate(assembly, hash);
            }
        }

        private void CheckForBlobUpdate(FileInfo assembly, string hash)
        {
            var blob = this._container.GetBlockBlobReference(assembly.Name);
            try
            {
                blob.FetchAttributes();
                if (!hash.Equals(blob.Properties.ContentMD5))
                {
                    System.Diagnostics.Trace.WriteLine("Updating Assembly: " + assembly.Name);
                    using (var fs = File.OpenWrite(assembly.FullName))
                    {
                        blob.DownloadToStream(fs);
                    }
                }
            }
            catch (StorageException sEx)
            {
                System.Diagnostics.Trace.TraceInformation("Blob does not exists: " + assembly.Name);
            }
        }

    }

    
}
