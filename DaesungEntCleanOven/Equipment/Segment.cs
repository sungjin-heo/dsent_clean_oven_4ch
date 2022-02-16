using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven4.ViewModel;

namespace DaesungEntCleanOven4.Equipment
{
    class Segment : List<RegNumeric>
    {
        public Segment(int No)
        {
            this.No = No;
        }
        public int No { get; protected set; }
        public string Name => string.Format("SEG #{0:d2}", No);
    }
}
