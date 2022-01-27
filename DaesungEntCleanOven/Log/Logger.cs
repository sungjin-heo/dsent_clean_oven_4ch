using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace Log
{
    class Logger
    {
        public static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Dispatch(string type, string message, object arg = null)
        {
            string s = type.Substring(0, 1).ToLower();

            switch (s)
            {
                case "i":
                    if (arg == null)
                        log.Info(message);
                    else
                        log.InfoFormat(message, arg);
                    break;

                case "d":
                    if (arg == null)
                        log.Debug(message);
                    else
                        log.DebugFormat(message, arg);
                    break;

                case "w":
                    if (arg == null)
                        log.Warn(message);
                    else
                        log.WarnFormat(message, arg);
                    break;

                case "e":
                    if (arg == null)
                        log.Error(message);
                    else
                        log.ErrorFormat(message, arg);
                    break;

                case "f":
                    if (arg == null)
                        log.Fatal(message);
                    else
                        log.FatalFormat(message, arg);
                    break;
            }
        }
        public static void Dispatch(string type, string message, params object[] args)
        {
            string s = type.Substring(0, 1).ToLower();

            switch(s)
            {
                case "i":
                    if (args == null)
                        log.Info(message);
                    else
                        log.InfoFormat(message, args);
                    break;

                case "d":
                    if (args == null)
                        log.Debug(message);
                    else
                        log.DebugFormat(message, args);
                    break;

                case "w":
                    if (args == null)
                        log.Warn(message);
                    else
                        log.WarnFormat(message, args);
                    break;

                case "e":
                    if (args == null)
                        log.Error(message);
                    else
                        log.ErrorFormat(message, args);
                    break;

                case "f":
                    if (args == null)
                        log.Fatal(message);
                    else
                        log.FatalFormat(message, args);
                    break;
            }
        }
    }
}
