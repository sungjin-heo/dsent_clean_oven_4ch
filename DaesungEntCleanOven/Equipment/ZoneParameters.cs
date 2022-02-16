using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.ViewModel;

namespace DaesungEntCleanOven4.Equipment
{
    class ZoneParameters
    {
        public ZoneParameters()
        {
            this.Items = new List<RegNumeric>();
        }
        public List<RegNumeric> Items { get; protected set; }
        public string Name { get; set; }
    }
}
