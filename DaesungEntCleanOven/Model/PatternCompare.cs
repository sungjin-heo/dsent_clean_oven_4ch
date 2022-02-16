using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaesungEntCleanOven4.Model
{
    class PattenCompare : IEqualityComparer<Segment>
    {
        public bool Equals(Segment x, Segment y)
        {
            if (object.ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;
            return x.Equals(y);
        }
        public int GetHashCode(Segment obj)
        {
            if (obj == null)
                return 0;
            return obj.Name.GetHashCode();
        }
    }
}
