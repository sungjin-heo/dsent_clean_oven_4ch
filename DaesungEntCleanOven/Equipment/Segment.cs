using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven.ViewModel;

namespace DaesungEntCleanOven.Equipment
{
    class Segment : List<RegNumeric>
    {
        public Segment(int No)
        {
            this.No = No;
        }
        public int No { get; protected set; }
        public string Name { get { return string.Format("SEG #{0:d2}", No); } }       
    }
}
