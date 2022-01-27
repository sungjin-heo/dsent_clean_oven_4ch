using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace DaesungEntCleanOven.Model
{
    public class Item<T>
    {
        public Item(string Name)
            : this(Name, null, null)
        {

        }
        public Item(string Name, string Unit)
            : this(Name, Unit, null)
        {

        }
        public Item(string Name, string Unit, int? Scale)
        {
            this.Name = Name;
            this.Unit = Unit;
            this.Scale = Scale;

        }
        public string Name { get; set; }
        public T Value { get; set; }
        public int? Scale { get; set; }
        public string Unit { get; set; }
        public string Description { get; set; }
    }
}
