using SelfUpdater.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SelfUpdater.Impl1
{
    public class MessageGetter : IMessageGetter
    {
        public string GetMessage()
        {
            return "Hello ~ (wow) ~";
        }
    }
}
