using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SelfUpdater.Core.Contracts
{
    public interface IUpdater
    {
        void CheckForUpdates(string roleInstanceName);
        bool IsUpdating { get; }
    }
}
