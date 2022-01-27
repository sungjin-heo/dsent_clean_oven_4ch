using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Log
{
    class MessageTracer
    {
        public virtual void TraceInfo(string message) 
        {
            Logger.Dispatch("i", message);
        }
        public virtual void TraceInfo(string message, params object[] args)
        {
            Logger.Dispatch("i", message, args);
        }
        public virtual void TraceWarning(string message) 
        {
            Logger.Dispatch("w", message);
        }
        public virtual void TraceWarning(string message, params object[] args)
        {
            Logger.Dispatch("w", message, args);
        }
        public virtual void TraceError(string message) 
        {
            Logger.Dispatch("e", message);
        }
        public virtual void TraceError(string message, params object[] args)
        {
            Logger.Dispatch("e", message, args);
        }
    }
}
