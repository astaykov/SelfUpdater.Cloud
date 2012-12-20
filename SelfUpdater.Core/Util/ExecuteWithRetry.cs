using System;
using System.Threading;

namespace SelfUpdater.Core.Util
{
    public static class ExecuteWithRetry
    {
        public static void Action(Action action, int numRetries, int retryTimeout)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action"); // slightly safer...
            }
            do
            {
                try
                {
                    action();
                    return;
                }
                catch
                {
                    if (numRetries <= 0) throw;  // improved to avoid silent failure
                    else Thread.Sleep(retryTimeout);
                }
            }
            while (numRetries-- > 0);
        }
    }
}
