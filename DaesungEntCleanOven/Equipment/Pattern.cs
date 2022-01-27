using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DaesungEntCleanOven.ViewModel;

namespace DaesungEntCleanOven.Equipment
{
    class Pattern : List<Segment>
    {
        public Pattern()
        {
            this.Conditions = new List<RegNumeric>();
        }
        public List<RegNumeric> Conditions { get; protected set; }
    }
}
