using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.Equipment;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using DaesungEntCleanOven4.Model;
using System.Net.Json;

namespace DaesungEntCleanOven4.ViewModel
{
    class G
    {
        static G()
        {
            REALTIME_TREND_CAPACITY = 5;        // UNIT : HOUR.
            REALTIME_TREND_HISTORY = 24;        // UNIT : HOUR.
        }
        public static double REALTIME_TREND_CAPACITY { get; protected set; }
        public static double REALTIME_TREND_HISTORY { get; protected set; }
        public static bool REALTIME_TREND_ON_SEARCH { get; set; }
     }
}
