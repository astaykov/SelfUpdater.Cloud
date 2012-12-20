using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using SelfUpdater.Core.Contracts;
using SelfUpdater.Core.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SelfUpdater.Core
{
    public class SelfUpdaterEntryPoint : RoleEntryPoint
    {
        private IUpdater _updater;

        public override bool OnStart()
        {
            RoleEnvironment.StatusCheck += RoleEnvironment_StatusCheck;
            var connString = CloudConfigurationManager.GetSetting(Constants.SETTING_STORAGE_CONNECTION_STRING);
            this._updater = new UpdaterFromBlob(connString);
            return base.OnStart();
        }

        void RoleEnvironment_StatusCheck(object sender, RoleInstanceStatusCheckEventArgs e)
        {
            System.Diagnostics.Trace.WriteLine("StatusCheck called");
            if (this._updater.IsUpdating)
            {
                System.Diagnostics.Trace.WriteLine("Setting Busy state as we are updating");
                e.SetBusy();
            }
        }

        public override void OnStop()
        {
            // prevent broken assemblies in bin folder if we are updating
            do
            {
                System.Threading.Thread.Sleep(1000);
            }
            while (this._updater.IsUpdating);

            base.OnStop();
        }

        public override void Run()
        {
            var checkInterval = CloudConfigurationManager.GetSetting(Constants.SETTING_CHECK_INTERVAL_SECONDS);
            int checkIntervalInSeconds;
            if (!int.TryParse(checkInterval, out checkIntervalInSeconds))
            {
                checkIntervalInSeconds = 600;
            }
            TimeSpan sleepTime = TimeSpan.FromSeconds(checkIntervalInSeconds);
            while (true)
            {
                System.Threading.Thread.Sleep(sleepTime);
                System.Diagnostics.Trace.WriteLine("Working ..."); 
                this._updater.CheckForUpdates(RoleEnvironment.CurrentRoleInstance.Id + "_Web");
            }
        }
    }
}
