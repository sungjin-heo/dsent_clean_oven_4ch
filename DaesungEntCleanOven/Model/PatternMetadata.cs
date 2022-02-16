using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaesungEntCleanOven4.Model
{
    public class PatternMetadata : DevExpress.Mvvm.BindableBase
    {
        public int No { get; set; }      
        public string Name { get; set; }
        public bool IsAssigned { get; set; }
        public string Description { get; set; }
        public string RegisteredScanCode { get; set; }
        public void Invalidate()
        {
            System.Reflection.PropertyInfo[] Properties = typeof(PatternMetadata).GetProperties();
            RaisePropertiesChanged(Properties.Select(p => p.Name).ToArray());
        }
    }
}
