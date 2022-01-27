using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaesungEntCleanOven.Model
{
    class AlertConf
    {
        public AlertConf(string Name, int Level)
            : this(Name, Level, null, null)
        {

        }
        public AlertConf(string Name, int Level, int? DelayTime)
            : this(Name, Level, DelayTime, null)
        {

        }
        public AlertConf(string Name, int Level, int? DelayTime, double? Value)
        {
            this.Name = Name;
            this.Level = Level;
            this.DelayTime = DelayTime;
            this.Value = Value;
        }
        public string Name { get; set; }
        public int Level { get; set; }
        public int? DelayTime { get; set; }
        public double? Value { get; set; } 
    }
}
